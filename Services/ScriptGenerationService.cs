using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestParse.Helpers;
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

        public async Task GenerateCreateTableScriptsAsync(IEnumerable<TableDifference> missingTables, SqlConnectionManager sourceConn)
        {
            foreach (var table in missingTables)
            {
                using var command = new SqlCommand(_migrationScript.CreateTableScript, sourceConn.Connection);
                command.Parameters.AddWithValue("@TableName", table.TableName);
                var script = (await command.ExecuteScalarAsync())?.ToString() ??
                    throw new ArgumentException(nameof(_migrationScript.CreateTableScript));
                table.CreateTableScript = script;
            }
        }

        public async Task GenerateMissingColumnScriptsAsync(IEnumerable<ColumnDifference> missingColumns, SqlConnectionManager sourceConn)
        {
            foreach (var column in missingColumns)
            {
                using var command = new SqlCommand(_migrationScript.CreateAttributeScript, sourceConn.Connection);
                command.Parameters.AddWithValue("@TableName", column.TableName);
                command.Parameters.AddWithValue("@ColumnName", column.ColumnName);

                var script = (await command.ExecuteScalarAsync())?.ToString() ??
                    throw new ArgumentException(nameof(_migrationScript.CreateAttributeScript));
                column.CreateColumnScript = script;
            }
        }

        public async Task GenerateMissingSchemaScriptsAsync(IEnumerable<SchemaInfo> missingSchemas, SqlConnectionManager sourceConn)
        {
            foreach (var schema in missingSchemas)
            {
                using var command = new SqlCommand(_migrationScript.CreateSchemaScript, sourceConn.Connection);
                command.Parameters.AddWithValue("@SchemaName", schema.SchemaName);

                var script = (await command.ExecuteScalarAsync())?.ToString() ??
                    throw new ArgumentException(nameof(_migrationScript.CreateSchemaScript));

                schema.CreateSchemaScript = script;
            }
        }

        public async Task GenerateDifferentColumnScriptsAsync(IEnumerable<ColumnDifference> differentColumns, SqlConnectionManager sourceConn)
        {
            foreach (var column in differentColumns)
            {
                using var command = new SqlCommand(_migrationScript.AlterAttributeScript, sourceConn.Connection);
                command.Parameters.AddWithValue("@TableName", column.TableName);
                command.Parameters.AddWithValue("@ColumnName", column.ColumnName);

                var script = (await command.ExecuteScalarAsync())?.ToString() ??
                    throw new ArgumentException(nameof(_migrationScript.AlterAttributeScript));
                column.AlterColumnScript = script;
            }
        }

        public async Task GenerateCreateForeignKeyScriptsAsync(IEnumerable<ForeignKeyInfo> foreignKeys, SqlConnectionManager sourceConn)
        {
            foreach (var fk in foreignKeys)
            {
                using var command = new SqlCommand(_migrationScript.CreateForeignKeyScript, sourceConn.Connection);
                command.Parameters.AddWithValue("@ForeignKeyName", fk.ForeignKeyName);

                var script = (await command.ExecuteScalarAsync())?.ToString() ??
                    throw new ArgumentException(nameof(_migrationScript.CreateForeignKeyScript));
                if (!string.IsNullOrEmpty(script))
                    fk.CreateForeignKeyScript = script;
            }
        }

        public async Task GenerateCreateIndexScriptAsync(IEnumerable<IndexInfo> indexes, SqlConnectionManager sourceConn)
        {
            foreach (var index in indexes)
            {
                using var command = new SqlCommand(_migrationScript.CreateIndexScript, sourceConn.Connection);
                command.Parameters.AddWithValue("@IndexName", index.IndexName);
                command.Parameters.AddWithValue("@TableName", index.TableName);

                var script = (await command.ExecuteScalarAsync())?.ToString() ??
                    throw new ArgumentException(nameof(_migrationScript.CreateIndexScript));
                if (!string.IsNullOrEmpty(script))
                    index.CreateIndexScript = script;
            }
        }

        public async Task<IEnumerable<string>> GenerateDropAllIndexesScriptAsync(SqlConnectionManager targetConn)
        {
            var dropScripts = new List<string>();

            using var command = new SqlCommand(_migrationScript.DropAllIndexesScript, targetConn.Connection, targetConn.Transaction);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
                dropScripts.Add(reader["DropIndexScript"].ToString());

            return dropScripts;
        }

        public async Task<IEnumerable<string>> GenerateDropAllForeignKeysScriptAsync(SqlConnectionManager targetConn)
        {
            var dropScripts = new List<string>();

            using var command = new SqlCommand(_migrationScript.DropAllForeignKeysScript, targetConn.Connection, targetConn.Transaction);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
                dropScripts.Add(reader["DropForeignKeyScript"].ToString());

            return dropScripts;
        }

        public async Task<string> GenerateIdentityCountScriptAsync(SqlConnectionManager targetConn, string tableName)
        {
            using var command = new SqlCommand(_migrationScript.IdentityCountScript, targetConn.Connection, targetConn.Transaction);
            command.Parameters.AddWithValue("@TableName", tableName);
            var script = (await command.ExecuteScalarAsync())?.ToString();

            return script;
        }

        public async Task<string> GenerateClearDataScriptAsync(SqlConnectionManager targetConn, string tableName)
        {
            using var command = new SqlCommand(_migrationScript.ClearDataScript, targetConn.Connection, targetConn.Transaction);
            command.Parameters.AddWithValue("@TableName", tableName);
            var script = (await command.ExecuteScalarAsync())?.ToString();

            return script;
        }

        public async Task<SelectDataResult> GenerateSelectDataScriptAsync(SqlConnectionManager sourceConn, string tableName)
        {
            using var command = new SqlCommand(_migrationScript.SelectDataScript, sourceConn.Connection);
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
