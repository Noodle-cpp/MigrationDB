using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
/// 
/// </summary>
internal class Program
{
    private static IConfiguration _configuration;
    private static ServiceProvider _serviceProvider;

    static async Task Main(string[] args)
    {
        SetupConfiguration();
        var services = SetupDependencyInjection();

        var databaseMigrationService = services.GetRequiredService<IDatabaseMigrationService>();
        var dataMigrationService = services.GetRequiredService<IDataMigrationService>();

        try
        {

            Console.WriteLine("Начинаем сравнение баз данных...");
            var comparisonResult = await databaseMigrationService.CompareDatabasesAsync();

            Console.WriteLine("Сравнение завершено. Результаты:");
            PrintComparisonResults(comparisonResult);

            Console.WriteLine("Начинаем синхронизацию...");
            
            await databaseMigrationService.GenerateScriptsAsync(comparisonResult);
            await databaseMigrationService.SynchronizeDatabasesAsync(comparisonResult).ConfigureAwait(false) ;

            Console.WriteLine("Синхронизация завершена!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    static void PrintComparisonResults(DatabaseComparisonResult result)
    {
        Console.WriteLine("=== РЕЗУЛЬТАТЫ СРАВНЕНИЯ БАЗ ДАННЫХ ===");

        Console.WriteLine("\nОтсутствующие таблицы:");
        foreach (var table in result.MissingTables)
        {
            Console.WriteLine($"- {table.TableName}");
        }

        Console.WriteLine("\nОтсутствующие колонки:");
        foreach (var column in result.MissingColumns)
        {
            Console.WriteLine($"- {column.TableName}.{column.ColumnName} ({column.SourceDataType})");
        }

        Console.WriteLine("\nОтличающиеся колонки:");
        foreach (var column in result.DifferentColumns)
        {
            Console.WriteLine($"- {column.TableName}.{column.ColumnName}: {column.TargetDataType} -> {column.SourceDataType}");
        }

        Console.WriteLine("\nОтличающиеся внешние ключи:");
        foreach (var fk in result.MissingForeignKeys)
        {
            Console.WriteLine($"- {fk.ForeignKeyName}: {fk.ReferencedTableName} -> {fk.TableName}");
        }

        Console.WriteLine($"\nИтого: {result.MissingTables.Count()} отсутствующих таблиц, " +
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
            Server = _configuration["Source:Server"],
            Database = _configuration["Source:Database"],
            Username = _configuration["Source:Username"],
            Password = _configuration["Source:Password"],
        };

        var target = new DatabaseConnection
        {
            Server = _configuration["Target:Server"],
            Database = _configuration["Target:Database"],
            Username = _configuration["Target:Username"],
            Password = _configuration["Target:Password"],
        };

        _serviceProvider = new ServiceCollection()
            .AddDatabaseMigrationServices(source.ConnectionString, target.ConnectionString, DatabaseType.MSSQL)
            .BuildServiceProvider();

        return _serviceProvider;
    }
}