using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestParse.Models;

namespace TestParse.Services.Interfaces
{
    public interface IDatabaseMigrationService
    {
        Task<DatabaseComparisonResult> CompareDatabasesAsync();
        Task SynchronizeDatabasesAsync(DatabaseComparisonResult comparisonResult);
        Task GenerateScriptsAsync(DatabaseComparisonResult result);
    }
}
