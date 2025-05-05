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
                
                // Query to get all user tables with their information
                string query = @"
                    SELECT 
                        s.name AS SchemaName,
                        t.name AS TableName,
                        p.rows AS RowCount,
                        SUM(a.total_pages) * 8 / 1024 AS TotalSizeMB,
                        t.create_date AS CreateDate,
                        t.modify_date AS ModifyDate,
                        CASE WHEN t.temporal_type = 1 THEN 'System-Versioned' 
                             WHEN t.temporal_type = 2 THEN 'History Table' 
                             ELSE 'Normal' END AS TableType,
                        (SELECT COUNT(*) FROM sys.indexes i WHERE i.object_id = t.object_id) AS IndexCount,
                        (SELECT COUNT(*) FROM sys.foreign_keys fk WHERE fk.parent_object_id = t.object_id) AS ForeignKeyCount
                    FROM 
                        sys.tables t
                    JOIN 
                        sys.schemas s ON t.schema_id = s.schema_id
                    JOIN 
                        sys.indexes i ON t.object_id = i.object_id
                    JOIN 
                        sys.partitions p ON i.object_id = p.object_id AND i.index_id = p.index_id
                    JOIN 
                        sys.allocation_units a ON p.partition_id = a.container_id
                    WHERE 
                        t.is_ms_shipped = 0
                    GROUP BY 
                        s.name, t.name, p.rows, t.create_date, t.modify_date, t.temporal_type, t.object_id
                    ORDER BY 
                        s.name, t.name";

                using (var command = new SqlCommand(query, connection))
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
                            IndexCount: Convert.ToInt32(reader["IndexCount"]),
                            ForeignKeyCount: Convert.ToInt32(reader["ForeignKeyCount"]),
                            TableType: reader["TableType"].ToString() ?? string.Empty
                        );

                        result.Add(tableInfo);
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
        /// Determines if the current database is the master database.
        /// </summary>
        /// <returns>True if connected to master, false otherwise</returns>
        public async Task<bool> IsMasterDatabaseAsync(CancellationToken cancellationToken = default)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync(cancellationToken);
                
                using (var command = new SqlCommand("SELECT DB_NAME()", connection))
                {
                    string? dbName = (string?)await command.ExecuteScalarAsync(cancellationToken);
                    return string.Equals(dbName, "master", StringComparison.OrdinalIgnoreCase);
                }
            }
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