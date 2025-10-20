using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestParse.Helpers.Interfaces;

namespace TestParse.Helpers
{
    public class ScriptExecutor : IScriptExecutor
    {
        public async Task ExecuteScriptsAsync(IEnumerable<string> scripts, string successMessage, SqlConnectionManager connection)
        {
            foreach (var script in scripts.Where(s => !string.IsNullOrEmpty(s)))
                await ExecuteScriptAsync(script, successMessage, connection).ConfigureAwait(false);
        }

        public async Task ExecuteScriptAsync(string script, string successMessage, SqlConnectionManager connection, Dictionary<string, object>? commandParams = null)
        {
            try
            {
                await using var command = connection.Transaction is null 
                                            ? new SqlCommand(script, connection.Connection) 
                                            : new SqlCommand(script, connection.Connection, connection.Transaction);

                if (commandParams is not null)
                    foreach (var commandParam in commandParams)
                        command.Parameters.AddWithValue(commandParam.Key, commandParam.Value);

                await command.ExecuteNonQueryAsync().ConfigureAwait(false);

                Console.WriteLine($"{successMessage}");
            }
            catch (Exception)
            {
                if(connection.Transaction is not null)
                    connection.Transaction.Rollback();
                throw;
            }
        }
    }
}
