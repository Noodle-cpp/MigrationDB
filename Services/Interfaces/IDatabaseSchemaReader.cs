using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestParse.Models.InfoModels;

namespace TestParse.Services.Interfaces
{
    public interface IDatabaseSchemaReader
    {
        Task<Dictionary<string, List<ColumnInfo>>> GetDatabaseTablesAsync(string connectionString);
        Task<List<IndexInfo>> GetIndexesAsync(Dictionary<string, List<ColumnInfo>> tables, string connectionString);
        Task<List<ForeignKeyInfo>> GetForeignKeysAsync(string connectionString);
        Task<List<SchemaInfo>> GetSchemasAsync(string connectionString);
    }
}
