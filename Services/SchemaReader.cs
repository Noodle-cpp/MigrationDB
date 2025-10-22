using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestParse.Helpers;
using TestParse.Models.InfoModels;
using TestParse.Queries;
using TestParse.Scripts.Abstractions;
using TestParse.Services.Interfaces;

namespace TestParse.Services
{
    public class SchemaReader : ISchemaReader
    {
        private readonly IMigrationScript _migrationScript;

        public SchemaReader(IMigrationScript migrationScript)
        {
            _migrationScript = migrationScript;
        }

        public async Task<Dictionary<string, List<ColumnInfo>>> GetDatabaseTablesAsync(string connectionString)
        {
            var tables = new Dictionary<string, List<ColumnInfo>>();

            await using var connection = new SqlConnectionManager(connectionString);
            await connection.OpenConnectionAsync().ConfigureAwait(false);

            await using var command = new SqlCommand(_migrationScript.GetTableInfoScript, connection.Connection);
            await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var tableName = $"[{reader["TABLE_SCHEMA"]}].{reader["TABLE_NAME"]}";

                var columnInfo = new ColumnInfo
                {
                    SchemaName = reader["TABLE_SCHEMA"].ToString(),
                    ColumnName = reader["COLUMN_NAME"].ToString(),
                    DataType = reader["DATA_TYPE"].ToString(),
                    IsNullable = reader["IS_NULLABLE"].ToString() == "YES",
                    MaxLength = reader["CHARACTER_MAXIMUM_LENGTH"] as int?,
                    NumericPrecision = reader["NUMERIC_PRECISION"] as byte?,
                    NumericScale = reader["NUMERIC_SCALE"] as int?
                };

                if (!tables.ContainsKey(tableName))
                    tables[tableName] = [];

                tables[tableName].Add(columnInfo);
            }

            return tables;
        }

        public async Task<List<SchemaInfo>> GetSchemasAsync(string connectionString)
        {
            var schemas = new List<SchemaInfo>();

            await using var connection = new SqlConnectionManager(connectionString);
            await connection.OpenConnectionAsync().ConfigureAwait(false);

            await using var command = new SqlCommand(_migrationScript.GetSchemasScript, connection.Connection);
            await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

            while (await reader.ReadAsync().ConfigureAwait(false))
                schemas.Add(new SchemaInfo
                {
                    SchemaName = reader["SchemaName"].ToString(),
                });

            return schemas;
        }

        public async Task<List<IndexInfo>> GetIndexesAsync(Dictionary<string, List<ColumnInfo>> tables, string connectionString)
        {
            var indexes = new List<IndexInfo>();

            await using var connection = new SqlConnectionManager(connectionString);
            await connection.OpenConnectionAsync().ConfigureAwait(false);

            await using var command = new SqlCommand(_migrationScript.GetIndexesScript, connection.Connection);
            await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                indexes.Add(new IndexInfo
                {
                    SchemaName = reader["SchemaName"].ToString(),
                    TableName = reader["TableName"].ToString(),
                    IndexName = reader["IndexName"].ToString(),
                });
            }

            return indexes;
        }

        public async Task<List<ForeignKeyInfo>> GetForeignKeysAsync(string connectionString)
        {
            var foreignKeys = new List<ForeignKeyInfo>();

            await using var connection = new SqlConnectionManager(connectionString);
            await connection.OpenConnectionAsync().ConfigureAwait(false);

            await using var command = new SqlCommand(_migrationScript.GetForeignKeysScript, connection.Connection);
            await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

            while (await reader.ReadAsync().ConfigureAwait(false))
                foreignKeys.Add(new ForeignKeyInfo
                {
                    ForeignKeyName = reader["ForeignKeyName"].ToString(),
                    TableName = reader["TableName"].ToString(),
                    ColumnName = reader["Columns"].ToString(),
                    ReferencedTableName = reader["ReferencedTableName"].ToString(),
                    ReferencedColumnName = reader["ReferencedColumns"].ToString(),
                });

            return foreignKeys;
        }
    }
}
