using Microsoft.Data.SqlClient;
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

        public DataMigrationService(IScriptGenerator scriptGenerationService, IMigrationScript migrationScript, IScriptExecutor scriptExecutor)
        {
            _scriptGenerationService = scriptGenerationService;
            _migrationScript = migrationScript;
            _scriptExecutor = scriptExecutor;
        }

        public async Task ClearAllTablesDataAsync(SqlConnectionManager targetConn, List<string> tableNames)
        {
            Console.WriteLine("Очистка данных в целевой БД...");

            await DisableConstraintsAsync(targetConn).ConfigureAwait(false);

            foreach (var tableName in tableNames.AsEnumerable().Reverse())
            {
                try
                {
                    var script = await _scriptGenerationService.GenerateClearDataScriptAsync(targetConn, tableName).ConfigureAwait(false);
                    await _scriptExecutor.ExecuteScriptAsync(script, $"Очищена таблица: {tableName}", targetConn).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка очистки {tableName}: {ex.Message}");
                }
            }

            await EnableConstraintsAsync(targetConn).ConfigureAwait(false);
        }

        public async Task MigrateTableDataAsync(SqlConnectionManager sourceConn, SqlConnectionManager targetConn, string tableName, List<ColumnInfo> columns)
        {
            try
            {
                bool hasIdentity = await HasIdentityColumnAsync(targetConn, tableName).ConfigureAwait(false);

                if (hasIdentity) await MigrateTableWithIdentityAsync(sourceConn, targetConn, tableName, columns).ConfigureAwait(false);
                else await MigrateTableWithoutIdentityAsync(sourceConn, targetConn, tableName, columns).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка переноса {tableName}: {ex.Message}");
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
                { "@TableName", tableName }
            };

            await _scriptExecutor.ExecuteScriptAsync(_migrationScript.EnableIdentityScript, $"Включены все FK constraints", targetConn, commandParams).ConfigureAwait(false);
            
            try
            {
                await BulkCopyTableDataAsync(sourceConn, targetConn, tableName, columns);
                Console.WriteLine($"Данные перенесены (с IDENTITY): {tableName}");
            }
            catch (Exception)
            {
                await targetConn.RollbackAsync().ConfigureAwait(false);
            }
            finally
            {
                await using var disableCommand = new SqlCommand(_migrationScript.DisableIdentityScript, targetConn.Connection, targetConn.Transaction);
                disableCommand.Parameters.AddWithValue("@TableName", tableName);
                await disableCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }

        private async Task MigrateTableWithoutIdentityAsync(SqlConnectionManager sourceConn, SqlConnectionManager targetConn, string tableName, List<ColumnInfo> columns)
        {
            await BulkCopyTableDataAsync(sourceConn, targetConn, tableName, columns).ConfigureAwait(false);
            Console.WriteLine($"Данные перенесены (без IDENTITY): {tableName}");
        }

        private async Task BulkCopyTableDataAsync(SqlConnectionManager sourceConn, SqlConnectionManager targetConn, string tableName, List<ColumnInfo> columns)
        {
            var selectDataResult = await _scriptGenerationService.GenerateSelectDataScriptAsync(sourceConn, tableName).ConfigureAwait(false);

            await using var selectCommand = new SqlCommand(selectDataResult.Script, sourceConn.Connection);
            await using var reader = await selectCommand.ExecuteReaderAsync().ConfigureAwait(false);

            using var bulkCopy = new SqlBulkCopy(targetConn.Connection, SqlBulkCopyOptions.Default, targetConn.Transaction)
            {
                DestinationTableName = $"[{selectDataResult.SchemaName}].{tableName}",
                BulkCopyTimeout = 300,
                BatchSize = 5000,
            };

            foreach (var column in columns)
                bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);

            bulkCopy.WriteToServer(reader);
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