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
        /// Генерирует запрос на создание атрибута таблицы, если его не существует
        /// </summary>
        string GenerateCreateAttributeScript { get; }

        /// <summary>
        /// Генерирует запрос на изменение атрибута, если имеются отличия с исходным
        /// </summary>
        string GenerateAlterAttributeScript { get; }

        /// <summary>
        /// Генерирует запрос на создание таблицы, если ее не существует
        /// </summary>
        string GenerateCreateTableScript { get; }

        /// <summary>
        /// Получает информацию о таблице
        /// </summary>
        string GetTableInfoScript { get; }

        /// <summary>
        /// Получает информацию об индексах
        /// </summary>
        string GetIndexesScript { get; }

        /// <summary>
        /// Генерирует запрос на создание индекса
        /// </summary>
        string GenerateCreateIndexScript { get; }

        /// <summary>
        /// Получает информацию о внешних ключах
        /// </summary>
        string GetForeignKeysScript { get; }

        /// <summary>
        /// Генерирует запрос на создание внешнего ключа
        /// </summary>
        string GenerateCreateForeignKeyScript { get; }

        /// <summary>
        /// Получает схемы 
        /// </summary>
        string GetSchemasScript { get; }

        /// <summary>
        /// Генерирует запрос на создание схемы
        /// </summary>
        string GenerateCreateSchemaScript { get; }

        /// <summary>
        /// Генерирует запрос на удаление всех некластеризованных индексов
        /// </summary>
        string GenerateDropAllIndexesScript { get; }

        /// <summary>
        /// Генерирует запрос на очищение CONSTRAINT
        /// </summary>
        string GenerateDropAllForeignKeysScript { get; }

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
        /// Генерирует запрос на очищение данных в БД
        /// </summary>
        string GenerateClearDataScript { get; }

        /// <summary>
        /// Генерирует запрос на выборку всех столбцов с указанием схемы и имени таблицы
        /// </summary>
        string GenerateSelectDataScript { get; }

        /// <summary>
        /// Генерирует запрос, который возвращает количество identity-столбцов (0 или 1)
        /// </summary>
        string GenerateIdentityCountScript { get; }

        /// <summary>
        /// Получает кол-во строк в таблице
        /// </summary>
        public abstract string GetTableRowCountScript { get; }

        /// <summary>
        /// Получает данные из таблицы пакетами
        /// </summary>
        public abstract string BatchSelectScript { get; }
    }
}
