using Microsoft.Data.SqlClient;

namespace TestParse.Helpers.Interfaces
{
    public interface ISqlConnectionManager
    {
        Task OpenConnectionWithTransactionAsync();
        Task OpenConnectionAsync();
        Task CommitAsync();
        Task RollbackAsync();
    }
}
