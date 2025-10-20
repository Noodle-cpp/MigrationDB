using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestParse.Models.InfoModels
{
    public class IndexInfo
    {
        public string TableName { get; set; }
        public string IndexName { get; set; }
        public string CreateIndexScript { get; set; }
    }
}
