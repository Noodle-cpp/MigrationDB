using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestParse.Helpers;
using TestParse.Models.InfoModels;

namespace TestParse.Services.Interfaces
{
    public interface IDataMigrationService
    {
        Task ClearAllTablesDataAsync(SqlConnectionManager targetConn, List<string> tableNames);
        Task MigrateTableDataAsync(SqlConnectionManager sourceConn, SqlConnectionManager targetConn, string tableName, List<ColumnInfo> columns);
    }
}
