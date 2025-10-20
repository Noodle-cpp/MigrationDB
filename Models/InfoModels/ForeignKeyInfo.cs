using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestParse.Models.InfoModels
{
    public class ForeignKeyInfo
    {
        public string ForeignKeyName { get; set; }
        public string TableName { get; set; }
        public string ReferencedTableName { get; set; }
        public string ColumnName { get; set; }
        public string ReferencedColumnName { get; set; }
        public string CreateForeignKeyScript { get; set; }
    }
}
