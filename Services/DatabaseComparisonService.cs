using TestParse.Models;
using TestParse.Models.InfoModels;
using TestParse.Services.Interfaces;

namespace TestParse.Services
{
    public class DatabaseComparisonService : IDatabaseComparisonService
    {
        public List<TableDifference> FindMissingTables(Dictionary<string, List<ColumnInfo>> sourceTables, Dictionary<string, List<ColumnInfo>> targetTables)
        {
            return sourceTables.Keys
                .Where(tableName => !targetTables.ContainsKey(tableName))
                .Select(tableName => new TableDifference { TableName = tableName })
                .ToList();
        }
        
        public List<SchemaInfo> FindMissingSchemas(List<SchemaInfo> sourceSchemaNames, List<SchemaInfo> targetSchemaNames)
        {
            return sourceSchemaNames
                .Where(schemaName => !targetSchemaNames.Select(targetSchema => targetSchema.SchemaName).Contains(schemaName.SchemaName))
                .ToList();
        }
        
        public List<ColumnDifference> FindMissingColumns(Dictionary<string, List<ColumnInfo>> sourceTables, Dictionary<string, List<ColumnInfo>> targetTables)
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

        public List<ColumnDifference> FindDifferentColumns(Dictionary<string, List<ColumnInfo>> sourceTables, Dictionary<string, List<ColumnInfo>> targetTables)
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

        public List<IndexInfo> FindMissingIndexes(List<IndexInfo> source, List<IndexInfo> target)
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

            switch (dataType)
            {
                case "VARCHAR":
                case "NVARCHAR":
                case "CHAR":
                case "NCHAR":
                    return column.MaxLength == -1 ?
                        $"{dataType}(MAX)" :
                        $"{dataType}({column.MaxLength})";

                case "DECIMAL":
                case "NUMERIC":
                    return $"{dataType}({column.NumericPrecision},{column.NumericScale})";

                default:
                    return dataType;
            }
        }
    }
}
