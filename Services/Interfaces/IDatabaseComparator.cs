using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestParse.Models;

namespace TestParse.Services.Interfaces
{
    public interface IDatabaseComparator
    {
        Task<DatabaseComparisonResult> CompareDatabasesAsync(string sourceConnectionString, string targetConnectionString, bool clearDataBeforeInsert);
    }
}
