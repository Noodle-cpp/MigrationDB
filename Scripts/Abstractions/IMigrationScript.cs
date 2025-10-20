using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestParse.Scripts.Abstractions
{
    public interface IMigrationScript
    {
        /// <summary>
        /// Создает атрибут таблицы, если его не существует
        /// </summary>
        string CreateAttributeScript { get; }
        /// <summary>
        /// Изменяет атрибут, если имеются отличия с исходным
        /// </summary>
        string AlterAttributeScript { get; }
        /// <summary>
        /// Создает таблицу, если ее не существует
        /// </summary>
        string CreateTableScript { get; }
        /// <summary>
        /// Получает информацию о таблице
        /// </summary>
        string GetTableInfoScript { get; }
        /// <summary>
        /// Получает информацию об индексах
        /// </summary>
        string GetIndexesScript { get; }
        /// <summary>
        /// Создает индекс
        /// </summary>
        string CreateIndexScript { get; }
        /// <summary>
        /// Получает информацию о внешних ключах
        /// </summary>
        string GetForeignKeysScript { get; }
        /// <summary>
        /// Создает внешний ключ
        /// </summary>
        string CreateForeignKeyScript { get; }
        /// <summary>
        /// Получает схемы 
        /// </summary>
        string GetSchemasScript { get; }
        /// <summary>
        /// Создает схему
        /// </summary>
        string CreateSchemaScript { get; }
        /// <summary>
        /// Удаляет все некластеризованные индексы
        /// </summary>
        string DropAllIndexesScript { get; }
        /// <summary>
        /// Очищение CONSTRAINT
        /// </summary>
        string DropAllForeignKeysScript { get; }
        /// <summary>
        /// Отключает ВСЕ ограничений (CONSTRAINTS) во ВСЕХ таблицах базы данных
        /// </summary>
        string DisableConstraintsScript { get; }
        /// <summary>
        /// Включает ВСЕ ограничений (CONSTRAINTS) во ВСЕХ таблицах базы данных
        /// </summary>
        string EnableConstraintsScript { get; }
        /// <summary>
        /// Отключает IDENTITY_INSERT для указанной таблицы
        /// Позволяет запретить явную вставку значений в identity-столбец
        /// </summary>
        string DisableIdentityScript { get; }
        /// <summary>
        /// Включает IDENTITY_INSERT для указанной таблицы
        /// Позволяет выполнять явную вставку значений в identity-столбец
        /// </summary>
        string EnableIdentityScript { get; }
        /// <summary>
        /// Очищает данные в БД
        /// </summary>
        string ClearDataScript { get; }
        /// <summary>
        /// Получает данные из БД
        /// Генерирует запрос на выборку всех столбцов с указанием схемы и имени таблицы
        /// </summary>
        string SelectDataScript { get; }
        /// <summary>
        /// Проверяет наличие identity-столбцов в таблице
        /// Генерирует запрос, который возвращает количество identity-столбцов (0 или 1)
        /// </summary>
        string IdentityCountScript { get; }
    }
}
