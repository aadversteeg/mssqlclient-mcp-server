using Microsoft.Data.SqlClient;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Infrastructure.SqlClient.Interfaces;
using System.Data;
using System.Collections.Generic;

namespace Core.Infrastructure.SqlClient
{
    /// <summary>
    /// Core database service that provides SQL Server operations with database context switching.
    /// This is used by both UserDatabaseService and MasterDatabaseService.
    /// </summary>
    public class DatabaseService : IDatabaseService
    {
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the DatabaseService class.
        /// </summary>
        /// <param name="connectionString">The SQL Server connection string</param>
        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        /// <summary>
        /// Lists all tables in the database with optional database context switching.
        /// </summary>
        /// <param name="databaseName">Optional database name to switch context. If null, uses current database.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A collection of table information</returns>
        public async Task<IEnumerable<TableInfo>> ListTablesAsync(string? databaseName = null, CancellationToken cancellationToken = default)
        {
            var result = new List<TableInfo>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync(cancellationToken);
                
                // If a database name is specified, change the database context
                if (!string.IsNullOrWhiteSpace(databaseName))
                {
                    string changeDbCommand = $"USE [{databaseName}]";
                    using (var command = new SqlCommand(changeDbCommand, connection))
                    {
                        await command.ExecuteNonQueryAsync(cancellationToken);
                    }
                }
                
                try
                {
                    // First try a simpler query that should work on all SQL Server versions
                    string simpleQuery = @"
                        SELECT 
                            s.name AS SchemaName,
                            t.name AS TableName,
                            0 AS RowCount, -- Default to 0 for row count
                            0 AS TotalSizeMB, -- Default to 0 for size
                            t.create_date AS CreateDate,
                            t.modify_date AS ModifyDate,
                            'Normal' AS TableType,
                            0 AS IndexCount, -- Default to 0 for index count
                            0 AS ForeignKeyCount -- Default to 0 for foreign key count
                        FROM 
                            sys.tables t
                        JOIN 
                            sys.schemas s ON t.schema_id = s.schema_id
                        WHERE 
                            t.is_ms_shipped = 0
                        ORDER BY 
                            s.name, t.name";

                    using (var command = new SqlCommand(simpleQuery, connection))
                    using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                    {
                        while (await reader.ReadAsync(cancellationToken))
                        {
                            var tableInfo = new TableInfo(
                                Schema: reader["SchemaName"].ToString() ?? string.Empty,
                                Name: reader["TableName"].ToString() ?? string.Empty,
                                RowCount: reader["RowCount"] is DBNull ? 0 : Convert.ToInt64(reader["RowCount"]),
                                SizeMB: reader["TotalSizeMB"] is DBNull ? 0 : Convert.ToDouble(reader["TotalSizeMB"]),
                                CreateDate: Convert.ToDateTime(reader["CreateDate"]),
                                ModifyDate: Convert.ToDateTime(reader["ModifyDate"]),
                                IndexCount: reader["IndexCount"] is DBNull ? 0 : Convert.ToInt32(reader["IndexCount"]),
                                ForeignKeyCount: reader["ForeignKeyCount"] is DBNull ? 0 : Convert.ToInt32(reader["ForeignKeyCount"]),
                                TableType: reader["TableType"].ToString() ?? string.Empty
                            );

                            result.Add(tableInfo);
                        }
                    }

                    // Try to get additional information in separate queries 
                    // to be more compatible with different SQL Server versions
                    await EnhanceTableInfoAsync(result, connection, cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error executing complex query: {ex.Message}. Falling back to basic query.");
                    
                    // If the query fails, use an even simpler query as fallback
                    string fallbackQuery = @"
                        SELECT 
                            SCHEMA_NAME(schema_id) AS SchemaName,
                            name AS TableName,
                            create_date AS CreateDate,
                            modify_date AS ModifyDate
                        FROM 
                            sys.tables
                        WHERE 
                            is_ms_shipped = 0
                        ORDER BY 
                            SchemaName, TableName";
                    
                    // Clear previous results if any
                    result.Clear();
                        
                    using (var command = new SqlCommand(fallbackQuery, connection))
                    using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                    {
                        while (await reader.ReadAsync(cancellationToken))
                        {
                            // Create TableInfo with minimal information
                            var tableInfo = new TableInfo(
                                Schema: reader["SchemaName"].ToString() ?? string.Empty,
                                Name: reader["TableName"].ToString() ?? string.Empty,
                                RowCount: 0, // Default since we couldn't get this info
                                SizeMB: 0, // Default since we couldn't get this info
                                CreateDate: Convert.ToDateTime(reader["CreateDate"]),
                                ModifyDate: Convert.ToDateTime(reader["ModifyDate"]),
                                IndexCount: 0, // Default since we couldn't get this info
                                ForeignKeyCount: 0, // Default since we couldn't get this info
                                TableType: "Normal" // Default since we couldn't get this info
                            );

                            result.Add(tableInfo);
                        }
                    }
                }
                
                // If we switched database contexts, switch back to the original database
                if (!string.IsNullOrWhiteSpace(databaseName))
                {
                    var builder = new SqlConnectionStringBuilder(_connectionString);
                    string originalDatabase = builder.InitialCatalog;
                    
                    if (!string.IsNullOrWhiteSpace(originalDatabase))
                    {
                        string switchBackCommand = $"USE [{originalDatabase}]";
                        using (var command = new SqlCommand(switchBackCommand, connection))
                        {
                            await command.ExecuteNonQueryAsync(cancellationToken);
                        }
                    }
                }
            }

            return result;
        }
        
        /// <summary>
        /// Enhances table information with additional details in a way that's compatible
        /// with different SQL Server versions.
        /// </summary>
        /// <param name="tables">The list of tables to enhance</param>
        /// <param name="connection">An open SQL connection</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        private async Task EnhanceTableInfoAsync(
            List<TableInfo> tables, 
            SqlConnection connection, 
            CancellationToken cancellationToken = default)
        {
            if (tables == null || tables.Count == 0)
                return;
                
            try
            {
                // Get row counts - this approach is more compatible
                foreach (var table in tables)
                {
                    try
                    {
                        string countQuery = $"SELECT COUNT(*) FROM [{table.Schema}].[{table.Name}]";
                        using (var command = new SqlCommand(countQuery, connection))
                        {
                            var count = await command.ExecuteScalarAsync(cancellationToken);
                            if (count != null && count != DBNull.Value)
                            {
                                // This creates a new TableInfo with updated row count but preserves other properties
                                var index = tables.IndexOf(table);
                                if (index >= 0)
                                {
                                    tables[index] = table with { RowCount = Convert.ToInt64(count) };
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // If counting rows fails for a table, just continue with the next one
                        Console.Error.WriteLine($"Failed to get row count for table {table.Schema}.{table.Name}: {ex.Message}");
                    }
                }

                // Try to get index counts
                try
                {
                    string indexQuery = @"
                        SELECT 
                            SCHEMA_NAME(t.schema_id) AS SchemaName,
                            t.name AS TableName,
                            COUNT(i.index_id) AS IndexCount
                        FROM 
                            sys.tables t
                        LEFT JOIN 
                            sys.indexes i ON t.object_id = i.object_id
                        WHERE 
                            t.is_ms_shipped = 0
                        GROUP BY 
                            t.schema_id, t.name";

                    using (var command = new SqlCommand(indexQuery, connection))
                    using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                    {
                        while (await reader.ReadAsync(cancellationToken))
                        {
                            string schema = reader["SchemaName"].ToString() ?? string.Empty;
                            string name = reader["TableName"].ToString() ?? string.Empty;
                            int indexCount = Convert.ToInt32(reader["IndexCount"]);
                            
                            // Find matching table and update index count
                            var table = tables.FirstOrDefault(t => 
                                t.Schema.Equals(schema, StringComparison.OrdinalIgnoreCase) && 
                                t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                                
                            if (table != null)
                            {
                                var index = tables.IndexOf(table);
                                if (index >= 0)
                                {
                                    tables[index] = table with { IndexCount = indexCount };
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Failed to get index counts: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error enhancing table information: {ex.Message}");
            }
        }

        /// <summary>
        /// Lists all databases on the server.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A collection of database information</returns>
        public async Task<IEnumerable<DatabaseInfo>> ListDatabasesAsync(CancellationToken cancellationToken = default)
        {
            var result = new List<DatabaseInfo>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync(cancellationToken);
                
                string query = @"
                    SELECT 
                        name AS Name,
                        state_desc AS State,
                        (SELECT SUM(size * 8.0 / 1024) FROM sys.master_files WHERE database_id = db.database_id) AS SizeMB,
                        SUSER_SNAME(owner_sid) AS Owner,
                        compatibility_level AS CompatibilityLevel,
                        collation_name AS CollationName,
                        create_date AS CreateDate,
                        recovery_model_desc AS RecoveryModel,
                        is_read_only AS IsReadOnly
                    FROM 
                        sys.databases db
                    ORDER BY 
                        name";

                using (var command = new SqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        var databaseInfo = new DatabaseInfo(
                            Name: reader["Name"].ToString() ?? string.Empty,
                            State: reader["State"].ToString() ?? string.Empty,
                            SizeMB: reader["SizeMB"] is DBNull ? 0 : Convert.ToDouble(reader["SizeMB"]),
                            Owner: reader["Owner"].ToString() ?? string.Empty,
                            CompatibilityLevel: reader["CompatibilityLevel"].ToString() ?? string.Empty,
                            CollationName: reader["CollationName"].ToString() ?? string.Empty,
                            CreateDate: Convert.ToDateTime(reader["CreateDate"]),
                            RecoveryModel: reader["RecoveryModel"].ToString() ?? string.Empty,
                            IsReadOnly: Convert.ToBoolean(reader["IsReadOnly"])
                        );

                        result.Add(databaseInfo);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Checks if a database exists and is accessible.
        /// </summary>
        /// <param name="databaseName">Name of the database to check</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>True if the database exists and is accessible, otherwise false</returns>
        public async Task<bool> DoesDatabaseExistAsync(string databaseName, CancellationToken cancellationToken = default)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync(cancellationToken);

                string query = @"
                    SELECT COUNT(*) 
                    FROM sys.databases 
                    WHERE name = @DatabaseName 
                    AND state_desc = 'ONLINE'";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DatabaseName", databaseName);
                    var result = await command.ExecuteScalarAsync(cancellationToken);
                    return Convert.ToInt32(result) > 0;
                }
            }
        }

        /// <summary>
        /// Gets the current database name from the connection string.
        /// </summary>
        /// <returns>The current database name</returns>
        public string GetCurrentDatabaseName()
        {
            var builder = new SqlConnectionStringBuilder(_connectionString);
            return builder.InitialCatalog;
        }

        /// <summary>
        /// Determines if the current database is the master database by examining the connection string.
        /// </summary>
        /// <returns>True if connected to master, false otherwise</returns>
        public Task<bool> IsMasterDatabaseAsync(CancellationToken cancellationToken = default)
        {
            var builder = new SqlConnectionStringBuilder(_connectionString);
            string databaseName = builder.InitialCatalog;
            
            // Also check for Database parameter directly if InitialCatalog is empty
            if (string.IsNullOrEmpty(databaseName) && _connectionString.Contains("Database=", StringComparison.OrdinalIgnoreCase))
            {
                var dbParamStart = _connectionString.IndexOf("Database=", StringComparison.OrdinalIgnoreCase);
                if (dbParamStart >= 0)
                {
                    dbParamStart += "Database=".Length;
                    var dbParamEnd = _connectionString.IndexOf(';', dbParamStart);
                    if (dbParamEnd < 0)
                        dbParamEnd = _connectionString.Length;

                    databaseName = _connectionString.Substring(dbParamStart, dbParamEnd - dbParamStart);
                }
            }
            
            return Task.FromResult(string.Equals(databaseName, "master", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Executes a SQL query with optional database context switching.
        /// </summary>
        /// <param name="query">The SQL query to execute</param>
        /// <param name="databaseName">Optional database name to switch context. If null, uses current database.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>An IAsyncDataReader with the results of the query</returns>
        public async Task<IAsyncDataReader> ExecuteQueryAsync(string query, string? databaseName = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException("Query cannot be empty", nameof(query));
            }
            
            // Create a new connection that will be owned by the reader
            var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            
            // If a database name is specified, change the database context
            if (!string.IsNullOrWhiteSpace(databaseName))
            {
                // First check if the database exists
                string checkDbQuery = "SELECT COUNT(*) FROM sys.databases WHERE name = @dbName AND state_desc = 'ONLINE'";
                using (var checkCommand = new SqlCommand(checkDbQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@dbName", databaseName);
                    int dbCount = Convert.ToInt32(await checkCommand.ExecuteScalarAsync(cancellationToken));
                    
                    if (dbCount == 0)
                    {
                        connection.Dispose();
                        throw new InvalidOperationException($"Database '{databaseName}' does not exist or is not online");
                    }
                }
                
                // Change database context
                string useDbCommand = $"USE [{databaseName}]";
                using (var useCommand = new SqlCommand(useDbCommand, connection))
                {
                    await useCommand.ExecuteNonQueryAsync(cancellationToken);
                }
            }
            
            // Execute the query
            var command = new SqlCommand(query, connection);
            
            // We're returning the reader which will keep the connection open
            // The caller is responsible for disposing both the reader and the connection when done
            var sqlReader = await command.ExecuteReaderAsync(CommandBehavior.CloseConnection, cancellationToken);
            
            // Wrap the SqlDataReader with an AsyncDataReaderAdapter
            return new AsyncDataReaderAdapter(sqlReader);
        }
        
        /// <summary>
        /// Gets the schema information for a specific table with optional database context switching.
        /// </summary>
        /// <param name="tableName">The name of the table to get schema for</param>
        /// <param name="databaseName">Optional database name to switch context. If null, uses current database.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Table schema information</returns>
        public async Task<TableSchemaInfo> GetTableSchemaAsync(string tableName, string? databaseName = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentException("Table name cannot be empty", nameof(tableName));
            }

            var columns = new List<TableColumnInfo>();
            string currentDbName;

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync(cancellationToken);
                
                // Get the current database name for the context
                string currentDbQuery = "SELECT DB_NAME()";
                using (var command = new SqlCommand(currentDbQuery, connection))
                {
                    currentDbName = (string?)await command.ExecuteScalarAsync(cancellationToken) ?? GetCurrentDatabaseName();
                }
                
                // If a database name is specified, change the database context
                if (!string.IsNullOrWhiteSpace(databaseName))
                {
                    // First check if the database exists
                    string checkDbQuery = "SELECT COUNT(*) FROM sys.databases WHERE name = @dbName AND state_desc = 'ONLINE'";
                    using (var checkCommand = new SqlCommand(checkDbQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@dbName", databaseName);
                        int dbCount = Convert.ToInt32(await checkCommand.ExecuteScalarAsync(cancellationToken));
                        
                        if (dbCount == 0)
                        {
                            throw new InvalidOperationException($"Database '{databaseName}' does not exist or is not online");
                        }
                    }
                    
                    // Change database context
                    string useDbCommand = $"USE [{databaseName}]";
                    using (var useCommand = new SqlCommand(useDbCommand, connection))
                    {
                        await useCommand.ExecuteNonQueryAsync(cancellationToken);
                    }
                    
                    // Update the current database name
                    currentDbName = databaseName;
                }
                
                // Get schema information for the table
                var schemaTable = connection.GetSchema("Columns", new[] { null, null, tableName });
                
                if (schemaTable.Rows.Count == 0)
                {
                    throw new InvalidOperationException($"Table '{tableName}' does not exist in database '{currentDbName}' or you don't have permission to access it");
                }
                
                foreach (DataRow row in schemaTable.Rows)
                {
                    string columnName = row["COLUMN_NAME"].ToString() ?? string.Empty;
                    string dataType = row["DATA_TYPE"].ToString() ?? string.Empty;
                    string maxLength = row["CHARACTER_MAXIMUM_LENGTH"].ToString() ?? "-";
                    string isNullable = row["IS_NULLABLE"].ToString() ?? string.Empty;
                    
                    columns.Add(new TableColumnInfo(columnName, dataType, maxLength, isNullable));
                }
                
                // If we switched database contexts, switch back to the original database
                if (!string.IsNullOrWhiteSpace(databaseName))
                {
                    var builder = new SqlConnectionStringBuilder(_connectionString);
                    string originalDatabase = builder.InitialCatalog;
                    
                    if (!string.IsNullOrWhiteSpace(originalDatabase))
                    {
                        string switchBackCommand = $"USE [{originalDatabase}]";
                        using (var command = new SqlCommand(switchBackCommand, connection))
                        {
                            await command.ExecuteNonQueryAsync(cancellationToken);
                        }
                    }
                }
            }

            return new TableSchemaInfo(tableName, currentDbName, columns);
        }
    }
}