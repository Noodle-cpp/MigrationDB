using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestParse.Models.InfoModels;

namespace TestParse.Models
{
    public class DatabaseComparisonResult
    {
        public IEnumerable<TableDifference> MissingTables { get; set; } = [];
        public IEnumerable<ColumnDifference> MissingColumns { get; set; } = [];
        public IEnumerable<ColumnDifference> DifferentColumns { get; set; } = [];
        public IEnumerable<IndexInfo> MissingIndexes { get; set; } = [];
        public IEnumerable<ForeignKeyInfo> MissingForeignKeys { get; set; } = [];
        public IEnumerable<SchemaInfo> MissingSchemas { get; set; } = [];
    }

}
