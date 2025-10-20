using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestParse.Scripts.Abstractions;

namespace TestParse.Queries
{
    public class MigrationMSSQLScripts : BaseMigrationScript
    {
        public override string CreateAttributeScript => $@"
            SELECT 
                'ALTER TABLE [' + TABLE_SCHEMA + '].[' + TABLE_NAME + '] ADD [' + COLUMN_NAME + '] ' +
                {ColumnDataType}
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = @TableName AND COLUMN_NAME = @ColumnName";

        public override string AlterAttributeScript => $@"
            SELECT 
                'ALTER TABLE [' + TABLE_SCHEMA + '].[' + TABLE_NAME + '] ALTER COLUMN [' + COLUMN_NAME + '] ' +
                {ColumnDataType}
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = @TableName AND COLUMN_NAME = @ColumnName";

        public override string CreateTableScript => $@"
            SELECT 
                'CREATE TABLE [' + TABLE_SCHEMA + '].[' + TABLE_NAME + '] (' +
                STUFF((
                    SELECT ', [' + COLUMN_NAME + '] ' + 
                            {ColumnDataType}
                    FROM INFORMATION_SCHEMA.COLUMNS c2
                    WHERE c2.TABLE_NAME = c.TABLE_NAME AND c2.TABLE_SCHEMA = c.TABLE_SCHEMA
                    ORDER BY ORDINAL_POSITION
                    FOR XML PATH(''), TYPE
                ).value('.', 'NVARCHAR(MAX)'), 1, 1, '') +
                ');' as CreateScript
            FROM INFORMATION_SCHEMA.COLUMNS c
            WHERE TABLE_NAME = @TableName
            GROUP BY TABLE_SCHEMA, TABLE_NAME";

        public override string GetTableInfoScript => @"
            SELECT 
                    TABLE_NAME,
                    COLUMN_NAME,
                    DATA_TYPE,
                    IS_NULLABLE,
                    CHARACTER_MAXIMUM_LENGTH,
                    NUMERIC_PRECISION,
                    NUMERIC_SCALE
                FROM INFORMATION_SCHEMA.COLUMNS
                ORDER BY TABLE_NAME, ORDINAL_POSITION";

        /// <summary>
        /// 2 блок STUFF - INCLUDED столбцы
        /// </summary>
        public override string GetIndexesScript => $@"
           SELECT 
                t.name AS TableName,
                i.name AS IndexName
            FROM sys.indexes i
            INNER JOIN sys.tables t ON i.object_id = t.object_id
            WHERE i.name IS NOT NULL ";

        public override string CreateIndexScript => $@"
            SELECT 
                ('CREATE ' +
                CASE WHEN i.is_unique = 1 THEN 'UNIQUE ' ELSE '' END +
                i.type_desc COLLATE DATABASE_DEFAULT + ' INDEX [' + i.name + '] ' +
                'ON [' + SCHEMA_NAME(t.schema_id) + '].[' + t.name + '] ' +
                '(' + 
                {GetColumnIndexes} +
                ') ' +
                CASE 
                    WHEN EXISTS (
                        SELECT 1 FROM sys.index_columns ic 
                        WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 1
                    ) THEN 
                        'INCLUDE (' +
                        {GetIncludedColumnIndexes} +
                        ') '
                    ELSE ''
                END +
                CASE WHEN i.has_filter = 1 THEN 'WHERE ' + i.filter_definition + ' ' ELSE '' END +
                'WITH (' +
                'PAD_INDEX = ' + CASE WHEN i.is_padded = 1 THEN 'ON' ELSE 'OFF' END +
                CASE WHEN i.fill_factor > 0 THEN ', FILLFACTOR = ' + CAST(i.fill_factor AS VARCHAR(3)) ELSE '' END +
                ')') COLLATE DATABASE_DEFAULT
                AS CreateIndexScript
            FROM sys.indexes i
            JOIN sys.tables t ON i.object_id = t.object_id
            WHERE i.name = @IndexName
            AND t.name = @TableName";

        public override string GetForeignKeysScript => $@"
            SELECT 
                fk.name AS ForeignKeyName,
                OBJECT_NAME(fk.parent_object_id) AS TableName,
                OBJECT_NAME(fk.referenced_object_id) AS ReferencedTableName,
                {GetForeignKeysColumns} AS Columns,
                {GetForeignKeysReferencedColumns} AS ReferencedColumns
            FROM sys.foreign_keys fk
            ORDER BY TableName, ForeignKeyName";

        public override string CreateForeignKeyScript => $@"
            SELECT 
                'ALTER TABLE [' + OBJECT_SCHEMA_NAME(fk.parent_object_id) + '].[' + OBJECT_NAME(fk.parent_object_id) + '] ' +
                'ADD CONSTRAINT [' + fk.name + '] ' +
                'FOREIGN KEY (' + 
                {GetForeignKeysColumns} +
                'REFERENCES [' + OBJECT_SCHEMA_NAME(fk.referenced_object_id) + '].[' + OBJECT_NAME(fk.referenced_object_id) + '] (' + 
                {GetForeignKeysReferencedColumns} AS CreateForeignKeyScript
            FROM sys.foreign_keys fk
            WHERE fk.name = @ForeignKeyName";

        public override string GetSchemasScript => @"
            SELECT s.name as SchemaName
            FROM sys.schemas s
            INNER JOIN sys.database_principals p ON s.principal_id = p.principal_id
            WHERE s.name NOT IN ('sys', 'INFORMATION_SCHEMA', 'guest', 'db_owner', 
                                'db_accessadmin', 'db_securityadmin', 'db_ddladmin', 
                                'db_backupoperator', 'db_datareader', 'db_datawriter', 
                                'db_denydatareader', 'db_denydatawriter')
            ORDER BY s.name";

        public override string CreateSchemaScript => @"
            SELECT 'CREATE SCHEMA [' + name + ']' + 
                   CASE WHEN principal_id > 1 THEN 
                       ' AUTHORIZATION [' + USER_NAME(principal_id) + ']' 
                   ELSE '' END AS CreateSchemaScript
            FROM sys.schemas 
            WHERE name = @SchemaName";

        public override string DropAllIndexesScript => @"
            SELECT 
                'DROP INDEX [' + i.name + '] ON [' + SCHEMA_NAME(t.schema_id) + '].[' + t.name + ']' AS DropIndexScript
            FROM sys.indexes i
            JOIN sys.tables t ON i.object_id = t.object_id
            WHERE i.index_id > 1  
            AND i.type IN (2, 6)  
            AND i.is_primary_key = 0
            AND i.is_unique_constraint = 0
            AND t.is_ms_shipped = 0";


        public override string DropAllForeignKeysScript => @"
            SELECT ' ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id)) + '.' + QUOTENAME(OBJECT_NAME(parent_object_id)) + ' DROP CONSTRAINT ' + QUOTENAME(name) + ';' AS DropForeignKeyScript
            FROM sys.objects
            WHERE type_desc LIKE '%CONSTRAINT%';";

        /// <summary>
        /// sp_MSforeachtable - системная процедура, существует только в SQL Server
        /// Ей можно передать текст команды или запроса, который будет выполнен для каждой таблицы в базе
        /// </summary>
        public override string DisableConstraintsScript => @"EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';";

        /// <summary>
        /// sp_MSforeachtable - системная процедура, существует только в SQL Server
        /// Ей можно передать текст команды или запроса, который будет выполнен для каждой таблицы в базе
        /// </summary>
        public override string EnableConstraintsScript => @"EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';";

        public override string DisableIdentityScript => @"SET IDENTITY_INSERT @TableName OFF;";

        public override string EnableIdentityScript => @"SET IDENTITY_INSERT @TableName ON;";

        public override string ClearDataScript => @"
        SELECT
            'DELETE FROM [' + TABLE_SCHEMA + '].' + TABLE_NAME + '' AS ClearData
        FROM INFORMATION_SCHEMA.COLUMNS c
        WHERE TABLE_NAME = @TableName
        GROUP BY TABLE_SCHEMA, TABLE_NAME;";

        public override string SelectDataScript => @"SELECT
            TABLE_SCHEMA AS SchemaName,
            'SELECT * FROM [' + TABLE_SCHEMA + '].' + TABLE_NAME + '' AS SelectData
        FROM INFORMATION_SCHEMA.COLUMNS c
        WHERE TABLE_NAME = @TableName
        GROUP BY TABLE_SCHEMA, TABLE_NAME";

        public override string IdentityCountScript => @"
        SELECT
        'SELECT COUNT(*) 
            FROM sys.columns c
            JOIN sys.tables t ON c.object_id = t.object_id
            JOIN sys.schemas s ON t.schema_id = s.schema_id
            WHERE s.name = ''' + TABLE_SCHEMA + '''
            AND t.name = ''' + TABLE_NAME + '''
            AND c.is_identity = 1' AS GetCount
        FROM INFORMATION_SCHEMA.COLUMNS c
        GROUP BY TABLE_SCHEMA, TABLE_NAME";

        /// <summary>
        /// В 1 блоке CASE текстовый, плавающая точка, остальные
        /// В 2 блоке CASE определяется NULLABLE
        /// В 3 блоке - остальные
        /// </summary>
        private const string ColumnDataType = @"
                DATA_TYPE +
                CASE 
                    WHEN DATA_TYPE IN ('varchar', 'nvarchar', 'char', 'nchar') THEN
                        CASE 
                            WHEN CHARACTER_MAXIMUM_LENGTH = -1 THEN '(MAX)'
                            ELSE '(' + CAST(CHARACTER_MAXIMUM_LENGTH AS VARCHAR) + ')'
                        END
                    WHEN DATA_TYPE IN ('decimal', 'numeric') THEN
                        '(' + CAST(NUMERIC_PRECISION AS VARCHAR) + ',' + CAST(NUMERIC_SCALE AS VARCHAR) + ')'
                    ELSE ''
                END +
                CASE WHEN IS_NULLABLE = 'NO' THEN ' NOT NULL' ELSE ' NULL' END";


        /// <summary>
        /// Столбцы ключа индекса без INCLUDED столбцов
        /// </summary>
        private const string GetColumnIndexes = @"
                STUFF((
                    SELECT ', [' + c.name + '] ' + 
                           CASE WHEN ic.is_descending_key = 1 THEN 'DESC' ELSE 'ASC' END
                    FROM sys.index_columns ic
                    JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                    WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 0
                    ORDER BY ic.key_ordinal
                    FOR XML PATH('')
                ), 1, 2, '')";

        /// <summary>
        /// INCLUDED столбцы ключа индекса
        /// </summary>
        private const string GetIncludedColumnIndexes = @"
            STUFF((
                SELECT ', [' + c.name + ']'
                FROM sys.index_columns ic
                JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 1
                ORDER BY ic.key_ordinal
                FOR XML PATH('')
            ), 1, 2, '')";

        /// <summary>
        /// Столбцы внешнего ключа в дочерней таблице
        /// </summary>
        private const string GetForeignKeysReferencedColumns = @"
            STUFF((
                    SELECT ', [' + c.name + ']'
                    FROM sys.foreign_key_columns fkc
                    JOIN sys.columns c ON fkc.referenced_object_id = c.object_id AND fkc.referenced_column_id = c.column_id
                    WHERE fkc.constraint_object_id = fk.object_id
                    ORDER BY fkc.constraint_column_id
                    FOR XML PATH('')
                ), 1, 2, '') + ') '";

        /// <summary>
        /// Столбцы внешнего ключа в первичной таблице
        /// </summary>
        private const string GetForeignKeysColumns = @"
            STUFF((
                    SELECT ', [' + c.name + ']'
                    FROM sys.foreign_key_columns fkc
                    JOIN sys.columns c ON fkc.parent_object_id = c.object_id AND fkc.parent_column_id = c.column_id
                    WHERE fkc.constraint_object_id = fk.object_id
                    ORDER BY fkc.constraint_column_id
                    FOR XML PATH('')
                ), 1, 2, '') + ') '";
    }
}
