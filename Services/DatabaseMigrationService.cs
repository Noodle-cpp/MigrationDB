using Microsoft.Data.SqlClient;
using System.Text;
using System.Transactions;
using TestParse.Models;
using TestParse.Queries;
using TestParse.Services.Interfaces;

namespace TestParse.Services
{
    public class DatabaseMigrationService : IDatabaseMigrationService
    {
        private readonly IDatabaseSchemaReader _schemaService;
        private readonly IDatabaseComparisonService _comparisonService;
        private readonly IScriptGenerationService _scriptGenerationService;
        private readonly IDataMigrationService _dataMigrationService;
        private readonly string _sourceConnectionString;
        private readonly string _targetConnectionString;

        public DatabaseMigrationService(string sourceConnectionString,
                                        string targetConnectionString,
                                        IDatabaseSchemaReader schemaService,
                                        IDatabaseComparisonService comparisonService,
                                        IScriptGenerationService scriptGenerationService,
                                        IDataMigrationService dataMigrationService)
        {
            _schemaService = schemaService;
            _comparisonService = comparisonService;
            _scriptGenerationService = scriptGenerationService;
            _dataMigrationService = dataMigrationService;

            _sourceConnectionString = sourceConnectionString;
            _targetConnectionString = targetConnectionString;
        }
        
        //TODO: Разделить ответственности
        public async Task<DatabaseComparisonResult> CompareDatabasesAsync()
        {
            var result = new DatabaseComparisonResult();

            await using var sourceConn = new SqlConnection(_sourceConnectionString);
            await using var targetConn = new SqlConnection(_targetConnectionString);

            await sourceConn.OpenAsync().ConfigureAwait(false);
            await targetConn.OpenAsync().ConfigureAwait(false);

            var sourceSchemas = await _schemaService.GetSchemasAsync(_sourceConnectionString).ConfigureAwait(false);
            var targetSchemas = await _schemaService.GetSchemasAsync(_targetConnectionString).ConfigureAwait(false);
            result.MissingSchemas = _comparisonService.FindMissingSchemas(sourceSchemas, targetSchemas);

            var sourceTables = await _schemaService.GetDatabaseTablesAsync(_sourceConnectionString).ConfigureAwait(false);
            var targetTables = await _schemaService.GetDatabaseTablesAsync(_targetConnectionString).ConfigureAwait(false);
            result.MissingTables = _comparisonService.FindMissingTables(sourceTables, targetTables);

            result.MissingColumns = _comparisonService.FindMissingColumns(sourceTables, targetTables);
            result.DifferentColumns = _comparisonService.FindDifferentColumns(sourceTables, targetTables);

            var sourceIndexes = await _schemaService.GetIndexesAsync(sourceTables, _sourceConnectionString).ConfigureAwait(false);
            var targetIndexes = await _schemaService.GetIndexesAsync(targetTables, _targetConnectionString).ConfigureAwait(false);
            result.MissingIndexes = _comparisonService.FindMissingIndexes(sourceIndexes, targetIndexes);

            result.MissingForeignKeys = await _schemaService.GetForeignKeysAsync(_sourceConnectionString).ConfigureAwait(false);

            await targetConn.CloseAsync();
            await sourceConn.CloseAsync();

            return result;
        }

        public async Task GenerateScriptsAsync(DatabaseComparisonResult result)
        {
            await using var sourceConn = new SqlConnection(_sourceConnectionString);

            await sourceConn.OpenAsync().ConfigureAwait(false);

            await _scriptGenerationService.GenerateMissingSchemaScriptsAsync(result.MissingSchemas, _sourceConnectionString);
            await _scriptGenerationService.GenerateCreateTableScriptsAsync(result.MissingTables, _sourceConnectionString);
            await _scriptGenerationService.GenerateMissingColumnScriptsAsync(result.MissingColumns, _sourceConnectionString);
            await _scriptGenerationService.GenerateDifferentColumnScriptsAsync(result.DifferentColumns, _sourceConnectionString);
            await _scriptGenerationService.GenerateCreateIndexScriptAsync(result.MissingIndexes, _sourceConnectionString);
            await _scriptGenerationService.GenerateCreateForeignKeyScriptsAsync(result.MissingForeignKeys, _sourceConnectionString);
        }

        public async Task SynchronizeDatabasesAsync(DatabaseComparisonResult comparisonResult)
        {
            await using var sourceConn = new SqlConnection(_sourceConnectionString);
            await using var targetConn = new SqlConnection(_targetConnectionString);

            await sourceConn.OpenAsync().ConfigureAwait(false);
            await targetConn.OpenAsync().ConfigureAwait(false);

            var targetTables = await _schemaService.GetDatabaseTablesAsync(_targetConnectionString).ConfigureAwait(false);
            var sourceTables = await _schemaService.GetDatabaseTablesAsync(_sourceConnectionString).ConfigureAwait(false);

            await _dataMigrationService.ClearAllTablesDataAsync(_targetConnectionString, [.. targetTables.Keys]);

            await ExecuteScriptsAsync(comparisonResult.MissingSchemas.Select(t => t.CreateSchemaScript), "Создана схема").ConfigureAwait(false);
            await ExecuteScriptsAsync(comparisonResult.MissingTables.Select(t => t.CreateTableScript), "Создана таблица").ConfigureAwait(false);
            await ExecuteScriptsAsync(comparisonResult.DifferentColumns.Select(c => c.AlterColumnScript), "Изменена колонка").ConfigureAwait(false);
            await ExecuteScriptsAsync(comparisonResult.MissingColumns.Select(c => c.CreateColumnScript), "Добавлена колонка").ConfigureAwait(false);

            var dropIndexes = await _scriptGenerationService.GenerateDropAllIndexesScriptAsync(_targetConnectionString);
            await ExecuteScriptsAsync(dropIndexes, "Очищен некластеризованный индекс").ConfigureAwait(false);

            var dropFKs = await _scriptGenerationService.GenerateDropAllForeignKeysScriptAsync(_targetConnectionString);
            await ExecuteScriptsAsync(dropFKs, "Очищен FK").ConfigureAwait(false);

            foreach (var table in sourceTables)
                await _dataMigrationService.MigrateTableDataAsync(_sourceConnectionString, _targetConnectionString, table.Key, table.Value).ConfigureAwait(false);

            await ExecuteScriptsAsync(comparisonResult.MissingIndexes.Select(i => i.CreateIndexScript), "Добавлен индекс").ConfigureAwait(false);
            await ExecuteScriptsAsync(comparisonResult.MissingForeignKeys.Select(fk => fk.CreateForeignKeyScript), "Создан внешний ключ").ConfigureAwait(false);
        }

        private async Task ExecuteScriptsAsync(IEnumerable<string> scripts, string successMessage)
        {
            foreach (var script in scripts.Where(s => !string.IsNullOrEmpty(s)))
                await ExecuteScriptAsync(script, successMessage).ConfigureAwait(false);
        }

        private async Task ExecuteScriptAsync(string script, string successMessage)
        {
            await using var targetConn = new SqlConnection(_targetConnectionString);

            await targetConn.OpenAsync().ConfigureAwait(false);

            await using var command = new SqlCommand(script, targetConn);
            int rowsAffected = await command.ExecuteNonQueryAsync().ConfigureAwait(false);

            Console.WriteLine($"{successMessage}");
        }
    }
}
