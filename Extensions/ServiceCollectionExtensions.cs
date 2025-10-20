using Microsoft.Extensions.DependencyInjection;
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
            services.AddScoped<IDatabaseComparisonService, DatabaseComparisonService>();
            services.AddScoped<IScriptGenerationService, ScriptGenerationService>();
            services.AddScoped<IDataMigrationService, DataMigrationService>();

            services.AddScoped<MigrationMSSQLScripts>();

            services.AddScoped<IMigrationScript>(provider =>
            {
                return databaseType switch
                {
                    DatabaseType.MSSQL => provider.GetRequiredService<MigrationMSSQLScripts>(),

                    _ => throw new Exception()
                };
            });

            services.AddScoped<IDatabaseMigrationService>(provider =>
                new DatabaseMigrationService(
                    sourceConnectionString,
                    targetConnectionString,
                    provider.GetRequiredService<IDatabaseSchemaReader>(),
                    provider.GetRequiredService<IDatabaseComparisonService>(),
                    provider.GetRequiredService<IScriptGenerationService>(),
                    provider.GetRequiredService<IDataMigrationService>()));

            return services;
        }
    }
}
