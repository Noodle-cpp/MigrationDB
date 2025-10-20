using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestParse.Models
{
    public class ColumnDifference
    {
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public string SourceDataType { get; set; }
        public string TargetDataType { get; set; }
        public bool IsNullable { get; set; }
        public string CreateColumnScript { get; set; }
        public string AlterColumnScript { get; set; }
    }
}
