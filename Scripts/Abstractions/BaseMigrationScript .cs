using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestParse.Scripts.Abstractions
{
    public abstract class BaseMigrationScript : IMigrationScript
    {
        public abstract string CreateAttributeScript { get; }
        public abstract string AlterAttributeScript { get; }
        public abstract string CreateTableScript { get; }
        public abstract string GetTableInfoScript { get; }
        public abstract string GetIndexesScript { get; }
        public abstract string CreateIndexScript { get; }
        public abstract string GetForeignKeysScript { get; }
        public abstract string CreateForeignKeyScript { get; }
        public abstract string GetSchemasScript { get; }
        public abstract string CreateSchemaScript { get; }
        public abstract string DropAllIndexesScript { get; }
        public abstract string DropAllForeignKeysScript { get; }
        public abstract string DisableConstraintsScript { get; }
        public abstract string EnableConstraintsScript { get; }
        public abstract string DisableIdentityScript { get; }
        public abstract string EnableIdentityScript { get; }
        public abstract string ClearDataScript { get; }
        public abstract string SelectDataScript { get; }
        public abstract string IdentityCountScript { get; }
    }
}
