using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using TestParse.Helpers;
using TestParse.Helpers.Interfaces;
using TestParse.Models.InfoModels;
using TestParse.Queries;
using TestParse.Scripts.Abstractions;
using TestParse.Services.Interfaces;

namespace TestParse.Services
{
    public class DataMigrationService : IDataMigrationService
    {
        private readonly IScriptGenerator _scriptGenerationService;
        private readonly IMigrationScript _migrationScript;
        private readonly IScriptExecutor _scriptExecutor;
        private readonly ILogger<DataMigrationService> _logger;

        public DataMigrationService(IScriptGenerator scriptGenerationService,
                                    IMigrationScript migrationScript,
                                    IScriptExecutor scriptExecutor,
                                    ILogger<DataMigrationService> logger)
        {
            _scriptGenerationService = scriptGenerationService;
            _migrationScript = migrationScript;
            _scriptExecutor = scriptExecutor;
            this._logger = logger;
        }

        public async Task ClearAllTablesDataAsync(SqlConnectionManager targetConn, List<string> tableNames)
        {
            _logger.LogInformation("Очистка данных в целевой БД...");

            await DisableConstraintsAsync(targetConn).ConfigureAwait(false);

            foreach (var tableName in tableNames.AsEnumerable().Reverse())
            {
                try
                {
                    var script = await _scriptGenerationService.GenerateClearDataScriptAsync(targetConn, tableName.Split('.').Last()).ConfigureAwait(false);
                    await _scriptExecutor.ExecuteScriptAsync(script, $"Очищена таблица: {tableName}", targetConn).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Ошибка очистки {tableName}: {ex.Message}");
                }
            }

            await EnableConstraintsAsync(targetConn).ConfigureAwait(false);
        }

        public async Task MigrateTableDataAsync(SqlConnectionManager sourceConn, SqlConnectionManager targetConn, string tableName, List<ColumnInfo> columns)
        {
            try
            {
                bool hasIdentity = await HasIdentityColumnAsync(targetConn, tableName.Split('.').Last()).ConfigureAwait(false);

                if (hasIdentity) await MigrateTableWithIdentityAsync(sourceConn, targetConn, tableName, columns).ConfigureAwait(false);
                else await MigrateTableWithoutIdentityAsync(sourceConn, targetConn, tableName, columns).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка переноса {tableName}: {ex.Message}");
            }
        }

        private async Task DisableConstraintsAsync(SqlConnectionManager targetConn)
        {
            await _scriptExecutor.ExecuteScriptAsync(_migrationScript.DisableConstraintsScript, $"Отключены все FK constraints", targetConn).ConfigureAwait(false);
        }

        private async Task EnableConstraintsAsync(SqlConnectionManager targetConn)
        {
            await _scriptExecutor.ExecuteScriptAsync(_migrationScript.EnableConstraintsScript, $"Включены все FK constraints", targetConn).ConfigureAwait(false);
        }

        private async Task MigrateTableWithIdentityAsync(SqlConnectionManager sourceConn, SqlConnectionManager targetConn, string tableName, List<ColumnInfo> columns)
        {
            var commandParams = new Dictionary<string, object>
            {
                { "@TableName", tableName.Split('.').Last() }
            };

            await _scriptExecutor.ExecuteScriptAsync(_migrationScript.EnableIdentityScript, $"Включен IDENTITY_INSERT", targetConn, commandParams).ConfigureAwait(false);

            try
            {
                await BulkCopyTableDataAsync(sourceConn, targetConn, tableName, columns).ConfigureAwait(false);
                _logger.LogInformation($"Данные перенесены (с IDENTITY): {tableName}");
            }
            catch (Exception)
            {
                await targetConn.RollbackAsync().ConfigureAwait(false);
            }
            finally
            {
                await _scriptExecutor.ExecuteScriptAsync(_migrationScript.DisableIdentityScript, $"Выключен IDENTITY_INSERT", targetConn, commandParams).ConfigureAwait(false);
            }
        }

        private async Task MigrateTableWithoutIdentityAsync(SqlConnectionManager sourceConn, SqlConnectionManager targetConn, string tableName, List<ColumnInfo> columns)
        {
            await BulkCopyTableDataAsync(sourceConn, targetConn, tableName, columns).ConfigureAwait(false);
            _logger.LogInformation($"Данные перенесены (без IDENTITY): {tableName}");
        }

        private async Task BulkCopyTableDataAsync(SqlConnectionManager sourceConn, SqlConnectionManager targetConn, string tableName, List<ColumnInfo> columns)
        {
            const long batchSize = 200000;
            long totalRows = await GetTableRowCountAsync(sourceConn, tableName).ConfigureAwait(false);
            long processedRows = 0;

            var keyColumns = await GetKeyColumnsAsync(sourceConn, tableName).ConfigureAwait(false);

            if (!keyColumns.Any())
            {
                keyColumns = columns.Select(c => c.ColumnName).ToList();
            }

            while (processedRows < totalRows)
            {
                var tempTableName = $"#TempBatch_{Guid.NewGuid():N}";
                await CreateTempTableAsync(targetConn, tempTableName, columns).ConfigureAwait(false);

                try
                {
                    await LoadDataToTempTableAsync(sourceConn, targetConn, tableName, tempTableName, columns, processedRows + 1, processedRows + batchSize).ConfigureAwait(false);
                    await InsertMissingRecordsAsync(targetConn, tableName, tempTableName, columns, keyColumns).ConfigureAwait(false);

                    processedRows += batchSize;
                    _logger.LogInformation($"Прогресс {tableName}: {Math.Min(processedRows, totalRows)}/{totalRows} строк");
                }
                finally
                {
                    await DropTempTableAsync(targetConn, tempTableName).ConfigureAwait(false);
                }

                await Task.Delay(1000);
            }
        }

        //TODO: Перенести в скрипты
        private async Task<List<string>> GetKeyColumnsAsync(SqlConnectionManager conn, string tableName)
        {
            var keyColumns = new List<string>();

            var sql = @"
                SELECT c.name AS ColumnName
                FROM sys.index_columns ic
                JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                JOIN sys.indexes i ON ic.object_id = i.object_id AND ic.index_id = i.index_id
                WHERE i.is_primary_key = 1
                AND OBJECT_NAME(ic.object_id) = @TableName
                ORDER BY ic.key_ordinal";

            await using var command = new SqlCommand(sql, conn.Connection);
            command.Parameters.AddWithValue("@TableName", tableName.Split('.').Last().Replace("[", "").Replace("]", ""));

            await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                keyColumns.Add(reader["ColumnName"].ToString());
            }

            return keyColumns;
        }

        private async Task CreateTempTableAsync(SqlConnectionManager conn, string tempTableName, List<ColumnInfo> columns)
        {
            var columnDefinitions = string.Join(", ", columns.Select(c =>
                $"[{c.ColumnName}] {GetColumnDefinition(c)}"));

            var sql = $"CREATE TABLE {tempTableName} ({columnDefinitions})";

            await using var command = new SqlCommand(sql, conn.Connection, conn.Transaction);
            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        private string GetColumnDefinition(ColumnInfo column)
        {
            var dataType = column.DataType.ToUpper();

            return dataType switch
            {
                "VARCHAR" or "NVARCHAR" or "CHAR" or "NCHAR" or "VARBINARY" =>
                    column.MaxLength == -1 ?
                    $"{dataType}(MAX)" :
                    $"{dataType}({column.MaxLength})",
                "DECIMAL" or "NUMERIC" =>
                    $"{dataType}({column.NumericPrecision},{column.NumericScale})",
                _ => dataType,
            } + (column.IsNullable ? " NULL" : " NOT NULL");
        }

        private async Task LoadDataToTempTableAsync(SqlConnectionManager sourceConn, SqlConnectionManager targetConn,
                                                    string sourceTableName, string tempTableName, List<ColumnInfo> columns,
                                                    long startRow, long endRow)
        {
            var selectColumns = string.Join(", ", columns.Select(c => $"[{c.ColumnName}]"));

            await using var sourceCommand = new SqlCommand($@"
                SELECT {selectColumns} FROM (
                    SELECT *, ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) as RowNum
                    FROM {sourceTableName}
                ) t
                WHERE RowNum BETWEEN {startRow} AND {endRow}", sourceConn.Connection);

            await using var sourceReader = await sourceCommand.ExecuteReaderAsync().ConfigureAwait(false);

            using var bulkCopy = new SqlBulkCopy(targetConn.Connection, SqlBulkCopyOptions.Default, targetConn.Transaction)
            {
                DestinationTableName = tempTableName,
                BulkCopyTimeout = 120,
                BatchSize = 10000,
                EnableStreaming = true
            };

            foreach (var column in columns)
                bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);

            await bulkCopy.WriteToServerAsync(sourceReader).ConfigureAwait(false);
        }

        private async Task InsertMissingRecordsAsync(SqlConnectionManager targetConn, string targetTableName,
            string tempTableName, List<ColumnInfo> columns, List<string> keyColumns)
        {
            var targetColumns = string.Join(", ", columns.Select(c => $"[{c.ColumnName}]"));
            var tempColumns = string.Join(", ", columns.Select(c => $"src.[{c.ColumnName}]"));

            var joinConditions = string.Join(" AND ", keyColumns.Select(col =>
                $"tgt.[{col}] = src.[{col}]"));

            var sql = $@"
            INSERT INTO {targetTableName} ({targetColumns})
            SELECT {tempColumns}
            FROM {tempTableName} src
            WHERE NOT EXISTS (
                SELECT 1 FROM {targetTableName} tgt 
                WHERE {joinConditions}
        )";

            await using var command = new SqlCommand(sql, targetConn.Connection, targetConn.Transaction);
            var insertedRows = await command.ExecuteNonQueryAsync().ConfigureAwait(false);

            _logger.LogInformation($"Вставлено {insertedRows} отсутствующих записей в {targetTableName}");
        }

        private async Task DropTempTableAsync(SqlConnectionManager conn, string tempTableName)
        {
            await using var command = new SqlCommand($"DROP TABLE {tempTableName}", conn.Connection, conn.Transaction);
            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
        }


        private async Task<long> GetTableRowCountAsync(SqlConnectionManager conn, string tableName)
        {
            var command = new SqlCommand($"SELECT COUNT_BIG(*) FROM {tableName}", conn.Connection);

            return (long)await command.ExecuteScalarAsync().ConfigureAwait(false);
        }

        private async Task<bool> HasIdentityColumnAsync(SqlConnectionManager targetConn, string tableName)
        {
            var script = await _scriptGenerationService.GenerateIdentityCountScriptAsync(targetConn, tableName).ConfigureAwait(false);

            await using var command = new SqlCommand(script, targetConn.Connection, targetConn.Transaction);
            command.Parameters.AddWithValue("@TableName", tableName);

            var result = await command.ExecuteScalarAsync().ConfigureAwait(false);
            return Convert.ToInt32(result) > 0;
        }
    }
}