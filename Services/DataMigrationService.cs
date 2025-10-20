using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;
using TestParse.Models.InfoModels;
using TestParse.Queries;
using TestParse.Scripts.Abstractions;
using TestParse.Services.Interfaces;

namespace TestParse.Services
{
    public class DataMigrationService : IDataMigrationService
    {
        private readonly IScriptGenerationService _scriptGenerationService;
        private readonly IMigrationScript _migrationScript;

        public DataMigrationService(IScriptGenerationService scriptGenerationService, IMigrationScript migrationScript)
        {
            _scriptGenerationService = scriptGenerationService;
            _migrationScript = migrationScript;
        }

        public async Task ClearAllTablesDataAsync(string targetConnectionString, List<string> tableNames)
        {
            await using var targetConn = new SqlConnection(targetConnectionString);
            await targetConn.OpenAsync().ConfigureAwait(false);

            Console.WriteLine("Очистка данных в целевой БД...");

            await DisableConstraintsAsync(targetConnectionString).ConfigureAwait(false);

            foreach (var tableName in tableNames.AsEnumerable().Reverse())
            {
                try
                {
                    var script = await _scriptGenerationService.GenerateClearDataScriptAsync(targetConnectionString, tableName).ConfigureAwait(false);
                    await using var command = new SqlCommand(script, targetConn);
                    var affectedRows = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                    Console.WriteLine($"Очищена таблица: {tableName} ({affectedRows} строк)");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка очистки {tableName}: {ex.Message}");
                }
            }

            await EnableConstraintsAsync(targetConnectionString).ConfigureAwait(false);
        }

        public async Task MigrateTableDataAsync(string sourceConnectionString, string targetConnectionString, string tableName, List<ColumnInfo> columns)
        {
            try
            {
                await using var sourceConn = new SqlConnection(sourceConnectionString);
                await using var targetConn = new SqlConnection(targetConnectionString);

                await sourceConn.OpenAsync().ConfigureAwait(false);
                await targetConn.OpenAsync().ConfigureAwait(false);

                bool hasIdentity = await HasIdentityColumnAsync(targetConnectionString, tableName).ConfigureAwait(false);

                if (hasIdentity) await MigrateTableWithIdentityAsync(sourceConnectionString, targetConnectionString, tableName, columns).ConfigureAwait(false);
                else await MigrateTableWithoutIdentityAsync(sourceConnectionString, targetConnectionString, tableName, columns).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка переноса {tableName}: {ex.Message}");
            }
        }

        private async Task DisableConstraintsAsync(string targetConnectionString)
        {
            await using var targetConn = new SqlConnection(targetConnectionString);

            await targetConn.OpenAsync().ConfigureAwait(false);

            await using var command = new SqlCommand(_migrationScript.DisableConstraintsScript, targetConn);
            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            Console.WriteLine("Отключены все FK constraints");
        }

        private async Task EnableConstraintsAsync(string targetConnectionString)
        {
            await using var targetConn = new SqlConnection(targetConnectionString);

            await targetConn.OpenAsync().ConfigureAwait(false);

            await using var command = new SqlCommand(_migrationScript.EnableConstraintsScript, targetConn);
            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            Console.WriteLine("Включены все FK constraints");
        }

        private async Task MigrateTableWithIdentityAsync(string sourceConnectionString, string targetConnectionString, string tableName, List<ColumnInfo> columns)
        {
            await using var sourceConn = new SqlConnection(sourceConnectionString);
            await using var targetConn = new SqlConnection(targetConnectionString);

            await sourceConn.OpenAsync().ConfigureAwait(false);
            await targetConn.OpenAsync().ConfigureAwait(false);

            await using var enableCommand = new SqlCommand(_migrationScript.EnableIdentityScript, targetConn);
            enableCommand.Parameters.AddWithValue("@TableName", tableName);
            await enableCommand.ExecuteNonQueryAsync().ConfigureAwait(false);

            try
            {
                await BulkCopyTableDataAsync(sourceConnectionString, targetConnectionString, tableName, columns);
                Console.WriteLine($"Данные перенесены (с IDENTITY): {tableName}");
            }
            finally
            {
                await using var disableCommand = new SqlCommand(_migrationScript.DisableIdentityScript, targetConn);
                disableCommand.Parameters.AddWithValue("@TableName", tableName);
                await disableCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }

        private async Task MigrateTableWithoutIdentityAsync(string sourceConnectionString, string targetConnectionString, string tableName, List<ColumnInfo> columns)
        {
            await BulkCopyTableDataAsync(sourceConnectionString, targetConnectionString, tableName, columns).ConfigureAwait(false);
            Console.WriteLine($"Данные перенесены (без IDENTITY): {tableName}");
        }

        private async Task BulkCopyTableDataAsync(string sourceConnectionString, string targetConnectionString, string tableName, List<ColumnInfo> columns)
        {
            await using var sourceConn = new SqlConnection(sourceConnectionString);
            await using var targetConn = new SqlConnection(targetConnectionString);

            await sourceConn.OpenAsync().ConfigureAwait(false);
            await targetConn.OpenAsync().ConfigureAwait(false);

            var selectDataResult = await _scriptGenerationService.GenerateSelectDataScriptAsync(sourceConnectionString, tableName).ConfigureAwait(false);

            await using var selectCommand = new SqlCommand(selectDataResult.Script, sourceConn);
            await using var reader = await selectCommand.ExecuteReaderAsync().ConfigureAwait(false);

            using var bulkCopy = new SqlBulkCopy(targetConn)
            {
                DestinationTableName = $"[{selectDataResult.SchemaName}].{tableName}",
                BulkCopyTimeout = 300,
                BatchSize = 5000
            };

            foreach (var column in columns)
                bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);

            bulkCopy.WriteToServer(reader);
        }

        private async Task<bool> HasIdentityColumnAsync(string targetConnectionString, string tableName)
        {
            await using var targetConn = new SqlConnection(targetConnectionString);

            await targetConn.OpenAsync().ConfigureAwait(false);

            var script = await _scriptGenerationService.GenerateIdentityCountScriptAsync(targetConnectionString, tableName).ConfigureAwait(false);

            await using var command = new SqlCommand(script, targetConn);
            command.Parameters.AddWithValue("@TableName", tableName);

            var result = await command.ExecuteScalarAsync().ConfigureAwait(false);
            return Convert.ToInt32(result) > 0;
        }
    }
}
