using TestParse.Models;
using TestParse.Models.InfoModels;
using TestParse.Services.Interfaces;

namespace TestParse.Services
{
    public class DatabaseComparator : IDatabaseComparator
    {
        private readonly ISchemaReader _schemaReader;

        public DatabaseComparator(ISchemaReader schemaReader)
        {
            _schemaReader = schemaReader;
        }

        public async Task<DatabaseComparisonResult> CompareDatabasesAsync(string sourceConnectionString,
                                                                            string targetConnectionString,
                                                                            bool clearDataBeforeInsert)
        {
            var result = new DatabaseComparisonResult();

            var sourceSchemas = await _schemaReader.GetSchemasAsync(sourceConnectionString).ConfigureAwait(false);
            var targetSchemas = await _schemaReader.GetSchemasAsync(targetConnectionString).ConfigureAwait(false);
            result.MissingSchemas = FindMissingSchemas(sourceSchemas, targetSchemas);

            var sourceTables = await _schemaReader.GetDatabaseTablesAsync(sourceConnectionString).ConfigureAwait(false);
            var targetTables = await _schemaReader.GetDatabaseTablesAsync(targetConnectionString).ConfigureAwait(false);
            result.MissingTables = FindMissingTables(sourceTables, targetTables);

            result.MissingColumns = FindMissingColumns(sourceTables, targetTables);
            result.DifferentColumns = FindDifferentColumns(sourceTables, targetTables);

            result.MissingIndexes = await _schemaReader.GetIndexesAsync(sourceTables, sourceConnectionString).ConfigureAwait(false);
            result.MissingForeignKeys = await _schemaReader.GetForeignKeysAsync(sourceConnectionString).ConfigureAwait(false);

            return result;
        }

        private List<TableDifference> FindMissingTables(Dictionary<string, List<ColumnInfo>> sourceTables, Dictionary<string, List<ColumnInfo>> targetTables)
        {
            return sourceTables.Keys
                .Where(tableName => !targetTables.ContainsKey(tableName))
                .Select(tableName => new TableDifference
                {
                    TableName = tableName.Split('.').Last(),
                    SchemaName = tableName.Split('.').First().Replace("[", "").Replace("]", "")
                })
                .ToList();
        }

        private List<SchemaInfo> FindMissingSchemas(List<SchemaInfo> sourceSchemaNames, List<SchemaInfo> targetSchemaNames)
        {
            return sourceSchemaNames
                .Where(schemaName => !targetSchemaNames.Select(targetSchema => targetSchema.SchemaName).Contains(schemaName.SchemaName))
                .ToList();
        }

        private List<ForeignKeyInfo> FindMissingForeignKeys(List<ForeignKeyInfo> sourceForeignKeys, List<ForeignKeyInfo> targetForeignKeys)
        {
            return sourceForeignKeys.Where(sourceFk =>
                !targetForeignKeys.Any(targetFk => targetFk.TableName == sourceFk.TableName &&
                                        targetFk.ReferencedTableName == sourceFk.ReferencedTableName &&
                                        targetFk.ColumnName == sourceFk.ColumnName &&
                                        targetFk.ReferencedColumnName == sourceFk.ReferencedColumnName))
                .ToList();
        }

        private List<ColumnDifference> FindMissingColumns(Dictionary<string, List<ColumnInfo>> sourceTables, Dictionary<string, List<ColumnInfo>> targetTables)
        {
            var missingColumns = new List<ColumnDifference>();

            foreach (var sourceTable in sourceTables)
            {
                var tableName = sourceTable.Key;

                if (!targetTables.ContainsKey(tableName)) continue;

                var targetColumns = targetTables[tableName];
                var sourceColumns = sourceTable.Value;

                foreach (var sourceColumn in sourceColumns)
                {
                    var targetColumn = targetColumns.FirstOrDefault(c => c.ColumnName.Equals(sourceColumn.ColumnName, StringComparison.OrdinalIgnoreCase));

                    if (targetColumn is not null) continue;

                    missingColumns.Add(new ColumnDifference
                    {
                        TableName = tableName,
                        ColumnName = sourceColumn.ColumnName,
                        SourceDataType = GetFullDataType(sourceColumn),
                        IsNullable = sourceColumn.IsNullable
                    });
                }
            }

            return missingColumns;
        }

        private List<ColumnDifference> FindDifferentColumns(Dictionary<string, List<ColumnInfo>> sourceTables, Dictionary<string, List<ColumnInfo>> targetTables)
        {
            var differentColumns = new List<ColumnDifference>();

            foreach (var sourceTable in sourceTables)
            {
                var tableName = sourceTable.Key;

                if (!targetTables.ContainsKey(tableName)) continue;

                var targetColumns = targetTables[tableName];
                var sourceColumns = sourceTable.Value;

                foreach (var sourceColumn in sourceColumns)
                {
                    var targetColumn = targetColumns.FirstOrDefault(c =>
                        c.ColumnName.Equals(sourceColumn.ColumnName, StringComparison.OrdinalIgnoreCase));

                    if (targetColumn is null || IsColumnsEqual(sourceColumn, targetColumn)) continue;

                    differentColumns.Add(new ColumnDifference
                    {
                        TableName = tableName,
                        ColumnName = sourceColumn.ColumnName,
                        SourceDataType = GetFullDataType(sourceColumn),
                        TargetDataType = GetFullDataType(targetColumn),
                        IsNullable = sourceColumn.IsNullable
                    });
                }
            }

            return differentColumns;
        }

        private List<IndexInfo> FindMissingIndexes(List<IndexInfo> source, List<IndexInfo> target)
        {
            return source.Where(sourceIndex =>
                !target.Any(targetIndex =>
                    targetIndex.TableName == sourceIndex.TableName &&
                    targetIndex.IndexName == sourceIndex.IndexName))
                .ToList();
        }

        private bool IsColumnsEqual(ColumnInfo source, ColumnInfo target)
        {
            return source.DataType.Equals(target.DataType, StringComparison.OrdinalIgnoreCase) &&
                   source.IsNullable == target.IsNullable &&
                   source.MaxLength == target.MaxLength &&
                   source.NumericPrecision == target.NumericPrecision &&
                   source.NumericScale == target.NumericScale;
        }

        private string GetFullDataType(ColumnInfo column)
        {
            var dataType = column.DataType.ToUpper();

            return dataType switch
            {
                "VARCHAR" or "NVARCHAR" or "CHAR" or "NCHAR" or "VARBINARY" =>
                    column.MaxLength == -1 ?
                    $"{dataType}(MAX)" : column.MaxLength == 128 ? $"SYSNAME" :
                    $"{dataType}({column.MaxLength})",
                "DECIMAL" or "NUMERIC" => $"{dataType}({column.NumericPrecision},{column.NumericScale})",
                _ => dataType,
            };
        }
    }
}
