using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestParse.Models;
using TestParse.Models.InfoModels;

namespace TestParse.Services.Interfaces
{
    public interface IScriptGenerationService
    {
        Task GenerateCreateTableScriptsAsync(IEnumerable<TableDifference> missingTables, string sourceConnectionString);
        Task GenerateMissingColumnScriptsAsync(IEnumerable<ColumnDifference> missingColumns, string sourceConnectionString);
        Task GenerateDifferentColumnScriptsAsync(IEnumerable<ColumnDifference> differentColumns, string sourceConnectionString);
        Task GenerateCreateForeignKeyScriptsAsync(IEnumerable<ForeignKeyInfo> foreignKeys, string sourceConnectionString);
        Task GenerateCreateIndexScriptAsync(IEnumerable<IndexInfo> index, string sourceConnectionString);
        Task GenerateMissingSchemaScriptsAsync(IEnumerable<SchemaInfo> missingSchemas, string sourceConnectionString);
        Task<IEnumerable<string>> GenerateDropAllIndexesScriptAsync(string targetConnectionString);
        Task<string> GenerateIdentityCountScriptAsync(string targetConnectionString, string tableName);
        Task<string> GenerateClearDataScriptAsync(string targetConnectionString, string tableName);
        Task<SelectDataResult> GenerateSelectDataScriptAsync(string sourceConnectionString, string tableName);
        Task<IEnumerable<string>> GenerateDropAllForeignKeysScriptAsync(string targetConnectionString);
    }
}
