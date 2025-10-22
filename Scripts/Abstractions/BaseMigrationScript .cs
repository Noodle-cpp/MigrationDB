using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestParse.Scripts.Abstractions
{
    public abstract class BaseMigrationScript : IMigrationScript
    {
        public abstract string GenerateCreateAttributeScript { get; }
        public abstract string GenerateAlterAttributeScript { get; }
        public abstract string GenerateCreateTableScript { get; }
        public abstract string GetTableInfoScript { get; }
        public abstract string GetIndexesScript { get; }
        public abstract string GenerateCreateIndexScript { get; }
        public abstract string GetForeignKeysScript { get; }
        public abstract string GenerateCreateForeignKeyScript { get; }
        public abstract string GetSchemasScript { get; }
        public abstract string GenerateCreateSchemaScript { get; }
        public abstract string GenerateDropAllIndexesScript { get; }
        public abstract string GenerateDropAllForeignKeysScript { get; }
        public abstract string DisableConstraintsScript { get; }
        public abstract string EnableConstraintsScript { get; }
        public abstract string DisableIdentityScript { get; }
        public abstract string EnableIdentityScript { get; }
        public abstract string GenerateClearDataScript { get; }
        public abstract string GenerateSelectDataScript { get; }
        public abstract string GenerateIdentityCountScript { get; }
        public abstract string GetTableRowCountScript { get; }
        public abstract string BatchSelectScript { get; }
    }
}
