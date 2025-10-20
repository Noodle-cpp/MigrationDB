using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestParse.Helpers.Interfaces
{
    public interface IScriptExecutor
    {
        Task ExecuteScriptAsync(string script, string successMessage, SqlConnectionManager connectionManager, Dictionary<string, object>? commandParams = null);
        Task ExecuteScriptsAsync(IEnumerable<string> scripts, string successMessage, SqlConnectionManager connectionManager);
    }
}
