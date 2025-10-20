using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestParse.Models;
using TestParse.Models.InfoModels;

namespace TestParse.Services.Interfaces
{
    public interface IDatabaseComparisonService
    {
        List<TableDifference> FindMissingTables(Dictionary<string, List<ColumnInfo>> sourceTables, Dictionary<string, List<ColumnInfo>> targetTables);
        List<ColumnDifference> FindMissingColumns(Dictionary<string, List<ColumnInfo>> sourceTables, Dictionary<string, List<ColumnInfo>> targetTables);
        List<ColumnDifference> FindDifferentColumns(Dictionary<string, List<ColumnInfo>> sourceTables, Dictionary<string, List<ColumnInfo>> targetTables);
        List<IndexInfo> FindMissingIndexes(List<IndexInfo> source, List<IndexInfo> target);
        List<SchemaInfo> FindMissingSchemas(List<SchemaInfo> sourceSchemaNames, List<SchemaInfo> targetSchemaNames);
    }
}
