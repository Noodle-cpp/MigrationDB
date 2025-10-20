using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestParse.Models;
using TestParse.Models.InfoModels;
using TestParse.Queries;
using TestParse.Scripts.Abstractions;
using TestParse.Services.Interfaces;

namespace TestParse.Services
{
    public class ScriptGenerationService : IScriptGenerationService
    {
        private readonly IMigrationScript _migrationScript;

        public ScriptGenerationService(IMigrationScript migrationScript)
        {
            _migrationScript = migrationScript;
        }

        public async Task GenerateCreateTableScriptsAsync(IEnumerable<TableDifference> missingTables, string sourceConnectionString)
        {
            await using var sourceConn = new SqlConnection(sourceConnectionString);
            await sourceConn.OpenAsync();

            foreach (var table in missingTables)
            {
                using var command = new SqlCommand(_migrationScript.CreateTableScript, sourceConn);
                command.Parameters.AddWithValue("@TableName", table.TableName);
                var script = (await command.ExecuteScalarAsync())?.ToString() ??
                    throw new ArgumentException(nameof(_migrationScript.CreateTableScript));
                table.CreateTableScript = script;
            }
        }

        public async Task GenerateMissingColumnScriptsAsync(IEnumerable<ColumnDifference> missingColumns, string sourceConnectionString)
        {
            await using var sourceConn = new SqlConnection(sourceConnectionString);
            await sourceConn.OpenAsync();

            foreach (var column in missingColumns)
            {
                using var command = new SqlCommand(_migrationScript.CreateAttributeScript, sourceConn);
                command.Parameters.AddWithValue("@TableName", column.TableName);
                command.Parameters.AddWithValue("@ColumnName", column.ColumnName);

                var script = (await command.ExecuteScalarAsync())?.ToString() ??
                    throw new ArgumentException(nameof(_migrationScript.CreateAttributeScript));
                column.CreateColumnScript = script;
            }
        }

        public async Task GenerateMissingSchemaScriptsAsync(IEnumerable<SchemaInfo> missingSchemas, string sourceConnectionString)
        {
            await using var sourceConn = new SqlConnection(sourceConnectionString);
            await sourceConn.OpenAsync();

            foreach (var schema in missingSchemas)
            {
                using var command = new SqlCommand(_migrationScript.CreateSchemaScript, sourceConn);
                command.Parameters.AddWithValue("@SchemaName", schema.SchemaName);

                var script = (await command.ExecuteScalarAsync())?.ToString() ??
                    throw new ArgumentException(nameof(_migrationScript.CreateSchemaScript));

                schema.CreateSchemaScript = script;
            }
        }

        public async Task GenerateDifferentColumnScriptsAsync(IEnumerable<ColumnDifference> differentColumns, string sourceConnectionString)
        {
            await using var sourceConn = new SqlConnection(sourceConnectionString);
            await sourceConn.OpenAsync();

            foreach (var column in differentColumns)
            {
                using var command = new SqlCommand(_migrationScript.AlterAttributeScript, sourceConn);
                command.Parameters.AddWithValue("@TableName", column.TableName);
                command.Parameters.AddWithValue("@ColumnName", column.ColumnName);

                var script = (await command.ExecuteScalarAsync())?.ToString() ??
                    throw new ArgumentException(nameof(_migrationScript.AlterAttributeScript));
                column.AlterColumnScript = script;
            }
        }

        public async Task GenerateCreateForeignKeyScriptsAsync(IEnumerable<ForeignKeyInfo> foreignKeys, string sourceConnectionString)
        {
            await using var sourceConn = new SqlConnection(sourceConnectionString);
            await sourceConn.OpenAsync();

            foreach (var fk in foreignKeys)
            {
                using var command = new SqlCommand(_migrationScript.CreateForeignKeyScript, sourceConn);
                command.Parameters.AddWithValue("@ForeignKeyName", fk.ForeignKeyName);

                var script = (await command.ExecuteScalarAsync())?.ToString() ??
                    throw new ArgumentException(nameof(_migrationScript.CreateForeignKeyScript));
                if (!string.IsNullOrEmpty(script))
                    fk.CreateForeignKeyScript = script;
            }
        }

        public async Task GenerateCreateIndexScriptAsync(IEnumerable<IndexInfo> indexes, string sourceConnectionString)
        {
            await using var sourceConn = new SqlConnection(sourceConnectionString);
            await sourceConn.OpenAsync();

            foreach (var index in indexes)
            {
                using var command = new SqlCommand(_migrationScript.CreateIndexScript, sourceConn);
                command.Parameters.AddWithValue("@IndexName", index.IndexName);
                command.Parameters.AddWithValue("@TableName", index.TableName);

                var script = (await command.ExecuteScalarAsync())?.ToString() ??
                    throw new ArgumentException(nameof(_migrationScript.CreateIndexScript));
                if (!string.IsNullOrEmpty(script))
                    index.CreateIndexScript = script;
            }
        }

        public async Task<IEnumerable<string>> GenerateDropAllIndexesScriptAsync(string targetConnectionString)
        {
            await using var targetConn = new SqlConnection(targetConnectionString);
            await targetConn.OpenAsync();

            var dropScripts = new List<string>();

            using var command = new SqlCommand(_migrationScript.DropAllIndexesScript, targetConn);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
                dropScripts.Add(reader["DropIndexScript"].ToString());

            return dropScripts;
        }

        public async Task<IEnumerable<string>> GenerateDropAllForeignKeysScriptAsync(string targetConnectionString)
        {
            var dropScripts = new List<string>();

            await using var targetConn = new SqlConnection(targetConnectionString);
            await targetConn.OpenAsync();

            using var command = new SqlCommand(_migrationScript.DropAllForeignKeysScript, targetConn);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
                dropScripts.Add(reader["DropForeignKeyScript"].ToString());

            return dropScripts;
        }

        public async Task<string> GenerateIdentityCountScriptAsync(string targetConnectionString, string tableName)
        {
            await using var targetConn = new SqlConnection(targetConnectionString);
            await targetConn.OpenAsync();

            using var command = new SqlCommand(_migrationScript.IdentityCountScript, targetConn);
            command.Parameters.AddWithValue("@TableName", tableName);
            var script = (await command.ExecuteScalarAsync())?.ToString();

            return script;
        }

        public async Task<string> GenerateClearDataScriptAsync(string targetConnectionString, string tableName)
        {
            await using var targetConn = new SqlConnection(targetConnectionString);
            await targetConn.OpenAsync();

            using var command = new SqlCommand(_migrationScript.ClearDataScript, targetConn);
            command.Parameters.AddWithValue("@TableName", tableName);
            var script = (await command.ExecuteScalarAsync())?.ToString();

            return script;
        }

        public async Task<SelectDataResult> GenerateSelectDataScriptAsync(string sourceConnectionString, string tableName)
        {
            await using var sourceConn = new SqlConnection(sourceConnectionString);
            await sourceConn.OpenAsync();

            using var command = new SqlCommand(_migrationScript.SelectDataScript, sourceConn);
            command.Parameters.AddWithValue("@TableName", tableName);
            await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            await reader.ReadAsync().ConfigureAwait(false);

            //TODO: Рассмотреть другие варианты, т.к. выглядит как неверное разделение ответственности из-за возврата схемы
            return new SelectDataResult()
            {
                SchemaName = reader["SchemaName"].ToString(),
                Script = reader["SelectData"].ToString()
            };
        }
    }
}
