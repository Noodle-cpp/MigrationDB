using Microsoft.Extensions.DependencyInjection;
using TestParse.Helpers;
using TestParse.Helpers.Interfaces;
using TestParse.Models;
using TestParse.Queries;
using TestParse.Scripts.Abstractions;
using TestParse.Services;
using TestParse.Services.Interfaces;

namespace TestParse.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDatabaseMigrationServices(this IServiceCollection services,
        string sourceConnectionString, string targetConnectionString, DatabaseType databaseType)
        {
            services.AddScoped<IDatabaseSchemaReader, DatabaseSchemaReader>();
            services.AddScoped<IScriptGenerationService, ScriptGenerationService>();
            services.AddScoped<IDataMigrationService, DataMigrationService>();
            services.AddScoped<IScriptExecutor, ScriptExecutor>();

            services.AddScoped<IDatabaseComparator, DatabaseComparator>();

            services.AddScoped<MigrationMSSQLScripts>();

            services.AddScoped<IMigrationScript>(provider =>
            {
                return databaseType switch
                {
                    DatabaseType.MSSQL => provider.GetRequiredService<MigrationMSSQLScripts>(),

                    _ => throw new Exception()
                };
            });

            services.AddScoped<IDatabaseMigrationCoordinator>(provider =>
                new DatabaseMigrationCoordinator(
                    sourceConnectionString,
                    targetConnectionString,
                    provider.GetRequiredService<IDatabaseSchemaReader>(),
                    provider.GetRequiredService<IScriptGenerationService>(),
                    provider.GetRequiredService<IDataMigrationService>(),
                    provider.GetRequiredService<IScriptExecutor>(),
                    provider.GetRequiredService<IDatabaseComparator>()));

            return services;
        }
    }
}
