using Microsoft.Data.SqlClient;

namespace TestParse.Helpers.Interfaces
{
    public interface ISqlConnectionManager : IAsyncDisposable
    {
        Task OpenConnectionWithTransactionAsync();
        Task OpenConnectionAsync();
        Task CommitAsync();
        Task RollbackAsync();
        ValueTask DisposeAsync();
    }
}
