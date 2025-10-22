using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using System;
using TestParse.Extensions;
using TestParse.Models;
using TestParse.Services;
using TestParse.Services.Interfaces;

/// <summary>
/// 
/// [Импортируются] 
/// > Схемы,
/// > Таблицы,
/// > Колонки,
/// > Индексы,
/// > Ключи,
/// > Данные
/// 
/// [Возможно дополнительно нужно импортировать]
/// > Пользователи/Роли,
/// > Триггеры/Функции/Процедуры
/// > Выражения столбцов
/// </summary>
class Program
{
    private static IConfiguration _configuration;
    private static ServiceProvider _serviceProvider;
    private static NLog.ILogger logger = LogManager.GetCurrentClassLogger();

    static async Task Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        NLog.LogManager.GetCurrentClassLogger().Fatal($"{e.ExceptionObject}");

        SetupConfiguration();
        var services = SetupDependencyInjection();

        var coordinator = services.GetRequiredService<IMigrationCoordinator>();
        var dataMigration = services.GetRequiredService<IDataMigrationService>();

        try
        {
            logger.Info("Начинаем сравнение баз данных...");
            var comparisonResult = await coordinator.CompareDatabasesAsync();

            logger.Info("Сравнение завершено. Результаты:");
            LogComparisonResults(comparisonResult);

            logger.Info("Начинаем синхронизацию...");

            await coordinator.GenerateScriptsAsync(comparisonResult);
            await coordinator.SynchronizeDatabasesAsync(comparisonResult).ConfigureAwait(false);

            logger.Info("Синхронизация завершена!");
        }
        catch (Exception ex)
        {
            logger.Error($"Fatal error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    static void LogComparisonResults(DatabaseComparisonResult result)
    {
        logger.Info($"=== РЕЗУЛЬТАТЫ СРАВНЕНИЯ БАЗ ДАННЫХ НА {DateTime.UtcNow} ===");

        logger.Info("\nОтсутствующие таблицы:");
        foreach (var table in result.MissingTables)
            logger.Info($"- {table.TableName}");

        logger.Info("\nОтсутствующие колонки:");
        foreach (var column in result.MissingColumns)
            logger.Info($"- {column.TableName}.{column.ColumnName} ({column.SourceDataType})");

        logger.Info("\nОтличающиеся колонки:");
        foreach (var column in result.DifferentColumns)
            logger.Info($"- {column.TableName}.{column.ColumnName}: {column.TargetDataType} -> {column.SourceDataType}");

        logger.Info("\nОтличающиеся внешние ключи:");
        foreach (var fk in result.MissingForeignKeys)
            logger.Info($"- {fk.ForeignKeyName}: {fk.ReferencedTableName} -> {fk.TableName}");

        logger.Info($"\nИтого: {result.MissingTables.Count()} отсутствующих таблиц, " +
                        $"{result.MissingColumns.Count()} отсутствующих колонок, " +
                        $"{result.DifferentColumns.Count()} отличающихся колонок, " +
                        $"{result.MissingForeignKeys.Count()} отсутствующих внешних ключей, " +
                        $"{result.MissingIndexes.Count()} отсутствующих индексов, ");
    }

    private static void SetupConfiguration()
    {
        _configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .Build() ?? throw new ArgumentNullException(nameof(_configuration));
    }

    private static IServiceProvider SetupDependencyInjection()
    {

        var source = new DatabaseConnection
        {
            Server = _configuration["SourceConnection:Server"],
            Database = _configuration["SourceConnection:Database"],
            Username = _configuration["SourceConnection:Username"],
            Password = _configuration["SourceConnection:Password"],
        };

        var target = new DatabaseConnection
        {
            Server = _configuration["TargetConnection:Server"],
            Database = _configuration["TargetConnection:Database"],
            Username = _configuration["TargetConnection:Username"],
            Password = _configuration["TargetConnection:Password"],
        };

        bool clearDataBeforeInsert = Convert.ToBoolean(_configuration["Migration:ClearDataBeforeInsert"]);
        bool includeDatabase = Convert.ToBoolean(_configuration["Migration:IncludeDatabase"]);
        bool includeData = Convert.ToBoolean(_configuration["Migration:IncludeData"]);

        _serviceProvider = new ServiceCollection()
            .AddDatabaseMigrationServices(source.ConnectionString, target.ConnectionString, DatabaseType.MSSQL,
                                            clearDataBeforeInsert, includeDatabase, includeData)
            .BuildServiceProvider();

        return _serviceProvider;
    }
}