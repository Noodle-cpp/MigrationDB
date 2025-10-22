using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestParse.Models
{
    public class DatabaseConnection
    {
        public string Server { get; set; }
        public string Database { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public string ConnectionString => $"Server={Server};" +
            $"Database={Database};" +
            $"User Id={Username};" +
            $"Password={Password};" +
            $"TrustServerCertificate=true;" +
            $"Encrypt=false;";
    }

    public enum DatabaseType
    {
        MSSQL
    }
}
