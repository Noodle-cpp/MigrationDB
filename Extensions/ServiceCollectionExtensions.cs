using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
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
        string sourceConnectionString, string targetConnectionString, DatabaseType databaseType,
        bool clearDataBeforeInsert, bool includeDatabase, bool includeData)
        {
            services.AddScoped<ISchemaReader, SchemaReader>();
            services.AddScoped<IScriptGenerator, ScriptGenerator>();
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

            services.AddScoped<IMigrationCoordinator>(provider =>
                new MigrationCoordinator(
                        sourceConnectionString,
                        targetConnectionString,
                        clearDataBeforeInsert,
                        includeDatabase,
                        includeData,
                        provider.GetRequiredService<ISchemaReader>(),
                        provider.GetRequiredService<IScriptGenerator>(),
                        provider.GetRequiredService<IDataMigrationService>(),
                        provider.GetRequiredService<IScriptExecutor>(),
                        provider.GetRequiredService<IDatabaseComparator>()));

            services.AddLogging(configure =>
            {
                configure.AddNLog("NLog.config");
                configure.SetMinimumLevel(LogLevel.Trace);
            });

            return services;
        }
    }
}
