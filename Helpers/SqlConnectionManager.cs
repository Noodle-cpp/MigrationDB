using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestParse.Helpers.Interfaces;

namespace TestParse.Helpers
{
    public class SqlConnectionManager : ISqlConnectionManager, IAsyncDisposable
    {
        private readonly string _connectionString;

        public SqlConnection Connection { get; private set; }
        public SqlTransaction? Transaction { get; private set; } = null;

        public SqlConnectionManager(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task OpenConnectionWithTransactionAsync()
        {
            Connection = new SqlConnection(_connectionString);

            await Connection.OpenAsync();
            Transaction = await Connection.BeginTransactionAsync().ConfigureAwait(false) as SqlTransaction;
        }

        public async Task OpenConnectionAsync()
        {
            Connection = new SqlConnection(_connectionString);
            Transaction = null;
            await Connection.OpenAsync();
        }

        public async Task CommitAsync()
        {
            if (Transaction is not null)
                await Transaction.CommitAsync().ConfigureAwait(false);
        }

        public async Task RollbackAsync()
        {
            if (Transaction is not null)
                await Transaction.RollbackAsync().ConfigureAwait(false);
        }

        public async ValueTask DisposeAsync()
        {
            if (Transaction is not null)
                await Transaction.DisposeAsync();

            if (Connection is not null)
            {
                await Connection.CloseAsync().ConfigureAwait(false);
                await Connection.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
