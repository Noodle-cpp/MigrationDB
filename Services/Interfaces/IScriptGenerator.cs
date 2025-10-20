using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestParse.Helpers;
using TestParse.Models;
using TestParse.Models.InfoModels;

namespace TestParse.Services.Interfaces
{
    public interface IScriptGenerator
    {
        Task GenerateCreateTableScriptsAsync(IEnumerable<TableDifference> missingTables, SqlConnectionManager sourceConn);
        Task GenerateMissingColumnScriptsAsync(IEnumerable<ColumnDifference> missingColumns, SqlConnectionManager sourceConn);
        Task GenerateDifferentColumnScriptsAsync(IEnumerable<ColumnDifference> differentColumns, SqlConnectionManager sourceConn);
        Task GenerateCreateForeignKeyScriptsAsync(IEnumerable<ForeignKeyInfo> foreignKeys, SqlConnectionManager sourceConn);
        Task GenerateCreateIndexScriptAsync(IEnumerable<IndexInfo> index, SqlConnectionManager sourceConn);
        Task GenerateMissingSchemaScriptsAsync(IEnumerable<SchemaInfo> missingSchemas, SqlConnectionManager sourceConn);
        Task<IEnumerable<string>> GenerateDropAllIndexesScriptAsync(SqlConnectionManager targetConn);
        Task<string> GenerateIdentityCountScriptAsync(SqlConnectionManager targetConn, string tableName);
        Task<string> GenerateClearDataScriptAsync(SqlConnectionManager targetConn, string tableName);
        Task<SelectDataResult> GenerateSelectDataScriptAsync(SqlConnectionManager sourceConn, string tableName);
        Task<IEnumerable<string>> GenerateDropAllForeignKeysScriptAsync(SqlConnectionManager targetConn);
    }
}
