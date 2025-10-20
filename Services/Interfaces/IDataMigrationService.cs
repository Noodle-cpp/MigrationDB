using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestParse.Models.InfoModels;

namespace TestParse.Services.Interfaces
{
    public interface IDataMigrationService
    {
        Task ClearAllTablesDataAsync(string targetConnectionString, List<string> tableNames);
        Task MigrateTableDataAsync(string sourceConnectionString, string targetConnectionString, string tableName, List<ColumnInfo> columns);
    }
}
