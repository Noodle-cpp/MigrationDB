using Microsoft.Data.SqlClient;
using System.Text;
using System.Transactions;
using TestParse.Helpers;
using TestParse.Helpers.Interfaces;
using TestParse.Models;
using TestParse.Models.InfoModels;
using TestParse.Queries;
using TestParse.Services.Interfaces;
using static System.Runtime.InteropServices.Marshalling.IIUnknownCacheStrategy;

namespace TestParse.Services
{
    public class DatabaseMigrationCoordinator : IDatabaseMigrationCoordinator
    {
        private readonly IDatabaseSchemaReader _schemaReader;
        private readonly IScriptGenerationService _scriptGenerationService;
        private readonly IDataMigrationService _dataMigrationService;
        private readonly string _sourceConnectionString;
        private readonly string _targetConnectionString;

        private readonly IScriptExecutor _scriptExecutor;
        private readonly IDatabaseComparator _databaseComparator;
        private SqlConnectionManager sourceConn;
        private SqlConnectionManager targetConn;

    public DatabaseMigrationCoordinator(string sourceConnectionString,
                                        string targetConnectionString,
                                        IDatabaseSchemaReader schemaService,
                                        IScriptGenerationService scriptGenerationService,
                                        IDataMigrationService dataMigrationService,
                                        IScriptExecutor scriptExecutor,
                                        IDatabaseComparator databaseComparator)
        {
            _schemaReader = schemaService;
            _scriptGenerationService = scriptGenerationService;
            _dataMigrationService = dataMigrationService;

            _sourceConnectionString = sourceConnectionString;
            _targetConnectionString = targetConnectionString;

            _scriptExecutor = scriptExecutor;
            _databaseComparator = databaseComparator;
            sourceConn = new SqlConnectionManager(sourceConnectionString);
            targetConn = new SqlConnectionManager(targetConnectionString);
        }

        public async Task<DatabaseComparisonResult> CompareDatabasesAsync()
        {
            return await _databaseComparator.CompareDatabasesAsync(_sourceConnectionString, _targetConnectionString).ConfigureAwait(false);
        }

        public async Task GenerateScriptsAsync(DatabaseComparisonResult result)
        {
            await sourceConn.OpenConnectionAsync().ConfigureAwait(false);

            await _scriptGenerationService.GenerateMissingSchemaScriptsAsync(result.MissingSchemas, sourceConn);
            await _scriptGenerationService.GenerateCreateTableScriptsAsync(result.MissingTables, sourceConn);
            await _scriptGenerationService.GenerateMissingColumnScriptsAsync(result.MissingColumns, sourceConn);
            await _scriptGenerationService.GenerateDifferentColumnScriptsAsync(result.DifferentColumns, sourceConn);
            await _scriptGenerationService.GenerateCreateIndexScriptAsync(result.MissingIndexes, sourceConn);
            await _scriptGenerationService.GenerateCreateForeignKeyScriptsAsync(result.MissingForeignKeys, sourceConn);

            await sourceConn.DisposeAsync().ConfigureAwait(false);
        }

        public async Task SynchronizeDatabasesAsync(DatabaseComparisonResult comparisonResult)
        {
            await sourceConn.OpenConnectionAsync().ConfigureAwait(false);
            await targetConn.OpenConnectionWithTransactionAsync().ConfigureAwait(false);

            var targetTables = await _schemaReader.GetDatabaseTablesAsync(_targetConnectionString).ConfigureAwait(false);
            var sourceTables = await _schemaReader.GetDatabaseTablesAsync(_sourceConnectionString).ConfigureAwait(false);

            await _dataMigrationService.ClearAllTablesDataAsync(targetConn, [.. targetTables.Keys]);

            await ExecuteSchemaScripts(comparisonResult).ConfigureAwait(false);

            await MigrateData(sourceTables).ConfigureAwait(false);

            await CreateIndexesAndConstraints(comparisonResult).ConfigureAwait(false);

            await targetConn.CommitAsync().ConfigureAwait(false);

            await targetConn.DisposeAsync().ConfigureAwait(false);
            await sourceConn.DisposeAsync().ConfigureAwait(false);
        }

        private async Task ExecuteSchemaScripts(DatabaseComparisonResult comparisonResult)
        {
            await _scriptExecutor.ExecuteScriptsAsync(comparisonResult.MissingSchemas.Select(t => t.CreateSchemaScript), "Создана схема", targetConn).ConfigureAwait(false);
            await _scriptExecutor.ExecuteScriptsAsync(comparisonResult.MissingTables.Select(t => t.CreateTableScript), "Создана таблица", targetConn).ConfigureAwait(false);
            await _scriptExecutor.ExecuteScriptsAsync(comparisonResult.DifferentColumns.Select(c => c.AlterColumnScript), "Изменена колонка", targetConn).ConfigureAwait(false);
            await _scriptExecutor.ExecuteScriptsAsync(comparisonResult.MissingColumns.Select(c => c.CreateColumnScript), "Добавлена колонка", targetConn).ConfigureAwait(false);
        }

        private async Task MigrateData(Dictionary<string, List<ColumnInfo>> sourceTables)
        {
            var dropIndexes = await _scriptGenerationService.GenerateDropAllIndexesScriptAsync(targetConn);
            await _scriptExecutor.ExecuteScriptsAsync(dropIndexes, "Очищен некластеризованный индекс", targetConn);

            var dropFKs = await _scriptGenerationService.GenerateDropAllForeignKeysScriptAsync(targetConn);
            await _scriptExecutor.ExecuteScriptsAsync(dropFKs, "Очищен FK", targetConn);

            foreach (var table in sourceTables)
                await _dataMigrationService.MigrateTableDataAsync(sourceConn, targetConn, table.Key, table.Value);
        }

        private async Task CreateIndexesAndConstraints(DatabaseComparisonResult comparisonResult)
        {
            await _scriptExecutor.ExecuteScriptsAsync(comparisonResult.MissingIndexes.Select(i => i.CreateIndexScript), "Добавлен индекс", targetConn);
            await _scriptExecutor.ExecuteScriptsAsync(comparisonResult.MissingForeignKeys.Select(fk => fk.CreateForeignKeyScript), "Создан внешний ключ", targetConn);
        }
    }
}
