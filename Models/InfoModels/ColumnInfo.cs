using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestParse.Models.InfoModels
{
    public class ColumnInfo
    {
        public string SchemaName { get; set; }
        public string ColumnName { get; set; }
        public string DataType { get; set; }
        public bool IsNullable { get; set; }
        public int? MaxLength { get; set; }
        public byte? NumericPrecision { get; set; }
        public int? NumericScale { get; set; }
    }
}
