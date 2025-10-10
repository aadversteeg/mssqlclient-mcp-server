using Microsoft.Data.SqlClient;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Infrastructure.SqlClient.Interfaces;
using Core.Infrastructure.SqlClient.Utilities;
using System.Data;

namespace Core.Infrastructure.SqlClient
{
    /// <summary>
    /// Core database service that provides SQL Server operations with database context switching.
    /// This is used by both UserDatabaseService and MasterDatabaseService.
    /// </summary>
    public class DatabaseService : IDatabaseService
    {
        private readonly string _connectionString;
        private readonly ISqlServerCapabilityDetector _capabilityDetector;
        private readonly DatabaseConfiguration _configuration;
        private SqlServerCapability? _capabilities;
        private bool _capabilitiesDetected = false;

        /// <summary>
        /// Initializes a new instance of the DatabaseService class.
        /// </summary>
        /// <param name="connectionString">The SQL Server connection string</param>
        /// <param name="capabilityDetector">The SQL Server capability detector</param>
        /// <param name="configuration">Database configuration with timeout settings</param>
        public DatabaseService(string connectionString, ISqlServerCapabilityDetector capabilityDetector, DatabaseConfiguration configuration)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _capabilityDetector = capabilityDetector ?? throw new ArgumentNullException(nameof(capabilityDetector));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            // Capabilities will be detected on first use
        }

        /// <summary>
        /// Gets the capabilities of the connected SQL Server instance.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>The SQL Server capabilities</returns>
        private async Task<SqlServerCapability> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
        {
            if (!_capabilitiesDetected)
            {
                _capabilities = await _capabilityDetector.DetectCapabilitiesAsync(cancellationToken);
                _capabilitiesDetected = true;
            }
            return _capabilities!;
        }

        /// <summary>
        /// Lists all tables in the database with optional database context switching.
        /// </summary>
        /// <param name="databaseName">Optional database name to switch context. If null, uses current database.</param>
        /// <param name="timeoutSeconds">Optional timeout in seconds. If null, uses default timeout.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A collection of table information</returns>
        public async Task<IEnumerable<TableInfo>> ListTablesAsync(string? databaseName = null, ToolCallTimeoutContext? timeoutContext = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        {
            var capabilities = await GetCapabilitiesAsync(cancellationToken);
            var result = new List<TableInfo>();

            // For Azure SQL Database, we need to specify the database in the connection string
            string connectionString = _connectionString;
            if (capabilities.IsAzureSqlDatabase && !string.IsNullOrWhiteSpace(databaseName))
            {
                // Create a new connection string with the specified database
                var builder = new SqlConnectionStringBuilder(_connectionString);
                builder.InitialCatalog = databaseName;
                connectionString = builder.ToString();
            }

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync(cancellationToken);
                
                // If a database name is specified and it's not Azure SQL, change the database context
                if (!string.IsNullOrWhiteSpace(databaseName) && !capabilities.IsAzureSqlDatabase)
                {
                    string changeDbCommand = $"USE [{databaseName}]";
                    using (var command = new SqlCommand(changeDbCommand, connection))
                    {
                        command.CommandTimeout = timeoutSeconds ?? _configuration.DefaultCommandTimeoutSeconds;
                        await command.ExecuteNonQueryAsync(cancellationToken);
                    }
                }
                
                // Build a query based on detected capabilities
                string query = BuildTableListQuery(capabilities);
                
                using (var command = new SqlCommand(query, connection))
                {
                    command.CommandTimeout = timeoutSeconds ?? _configuration.DefaultCommandTimeoutSeconds;
                    using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                    {
                    var fieldMap = GetReaderFieldMap(reader);
                    
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        var tableInfoBuilder = TableInfoBuilder.Create()
                            .WithSchema(reader["SchemaName"].ToString() ?? string.Empty)
                            .WithName(reader["TableName"].ToString() ?? string.Empty)
                            .WithCreateDate(Convert.ToDateTime(reader["CreateDate"]))
                            .WithModifyDate(Convert.ToDateTime(reader["ModifyDate"]));

                        // Add optional fields based on the columns available in the reader
                        if (fieldMap.TryGetValue("RowCount", out int rowCountIndex) && reader[rowCountIndex] != DBNull.Value)
                            tableInfoBuilder.WithRowCount(Convert.ToInt64(reader[rowCountIndex]));
                            
                        if (fieldMap.TryGetValue("TotalSizeMB", out int sizeIndex) && reader[sizeIndex] != DBNull.Value)
                            tableInfoBuilder.WithSizeMB(Convert.ToDouble(reader[sizeIndex]));
                            
                        if (fieldMap.TryGetValue("IndexCount", out int indexCountIndex) && reader[indexCountIndex] != DBNull.Value)
                            tableInfoBuilder.WithIndexCount(Convert.ToInt32(reader[indexCountIndex]));
                            
                        if (fieldMap.TryGetValue("ForeignKeyCount", out int fkCountIndex) && reader[fkCountIndex] != DBNull.Value)
                            tableInfoBuilder.WithForeignKeyCount(Convert.ToInt32(reader[fkCountIndex]));
                            
                        if (fieldMap.TryGetValue("TableType", out int typeIndex))
                            tableInfoBuilder.WithTableType(reader[typeIndex].ToString() ?? "Normal");

                        result.Add(tableInfoBuilder.Build());
                    }
                    }
                }

                // Enhance table information if not all data was available in the initial query
                await EnhanceTableInfoAsync(result, connection, capabilities, timeoutSeconds, cancellationToken);
                
                // If we switched database contexts and it's not Azure SQL, switch back to the original database
                if (!string.IsNullOrWhiteSpace(databaseName) && !capabilities.IsAzureSqlDatabase)
                {
                    var builder = new SqlConnectionStringBuilder(_connectionString);
                    string originalDatabase = builder.InitialCatalog;
                    
                    if (!string.IsNullOrWhiteSpace(originalDatabase))
                    {
                        string switchBackCommand = $"USE [{originalDatabase}]";
                        using (var command = new SqlCommand(switchBackCommand, connection))
                        {
                            command.CommandTimeout = timeoutSeconds ?? _configuration.DefaultCommandTimeoutSeconds;
                            await command.ExecuteNonQueryAsync(cancellationToken);
                        }
                    }
                }
            }

            return result;
        }
        
        /// <summary>
        /// Builds a table list query based on SQL Server capabilities.
        /// </summary>
        /// <param name="capabilities">The detected SQL Server capabilities</param>
        /// <returns>A SQL query string</returns>
        private string BuildTableListQuery(SqlServerCapability capabilities)
        {
            // For older SQL Server versions (10.x and below), use a simpler query
            if (capabilities.MajorVersion <= 10)
            {
                return @"
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
            }
            
            // For SQL Server 2012 and above, include more information
            string query = @"
                SELECT 
                    s.name AS SchemaName,
                    t.name AS TableName,
                    t.create_date AS CreateDate,
                    t.modify_date AS ModifyDate";
            
            // Conditionally add columns based on capabilities
            if (capabilities.SupportsExactRowCount)
            {
                query += ",\n                    0 AS [RowCount]"; // Will be enhanced later
            }
            
            if (capabilities.SupportsDetailedIndexMetadata)
            {
                query += ",\n                    0 AS IndexCount"; // Will be enhanced later
                query += ",\n                    0 AS ForeignKeyCount"; // Will be enhanced later
            }
            
            // Add table type for SQL Server 2016+ (13.x and above)
            if (capabilities.MajorVersion >= 13)
            {
                query += ",\n                    CASE WHEN t.temporal_type = 1 THEN 'History' WHEN t.temporal_type = 2 THEN 'Temporal' ELSE 'Normal' END AS TableType";
            }
            else 
            {
                query += ",\n                    'Normal' AS TableType";
            }
            
            // Include size information for SQL Server 2012+ (11.x and above)
            if (capabilities.MajorVersion >= 11)
            {
                query += ",\n                    0 AS TotalSizeMB"; // Will be enhanced later
            }
            
            // Complete the query
            query += @"
                FROM 
                    sys.tables t
                JOIN 
                    sys.schemas s ON t.schema_id = s.schema_id
                WHERE 
                    t.is_ms_shipped = 0
                ORDER BY 
                    s.name, t.name";
                    
            return query;
        }
        
        /// <summary>
        /// Gets a mapping of field names to ordinal positions in the DataReader.
        /// </summary>
        /// <param name="reader">The data reader</param>
        /// <returns>A dictionary mapping field names to their ordinal positions</returns>
        private Dictionary<string, int> GetReaderFieldMap(SqlDataReader reader)
        {
            var fieldMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < reader.FieldCount; i++)
            {
                fieldMap[reader.GetName(i)] = i;
            }
            return fieldMap;
        }
        
        /// <summary>
        /// Enhances table information with additional details in a way that's compatible
        /// with different SQL Server versions.
        /// </summary>
        /// <param name="tables">The list of tables to enhance</param>
        /// <param name="connection">An open SQL connection</param>
        /// <param name="capabilities">The SQL Server capabilities</param>
        /// <param name="timeoutSeconds">Optional timeout in seconds. If null, uses default timeout.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        private async Task EnhanceTableInfoAsync(
            List<TableInfo> tables, 
            SqlConnection connection,
            SqlServerCapability capabilities,
            int? timeoutSeconds = null,
            CancellationToken cancellationToken = default)
        {
            if (tables == null || tables.Count == 0)
                return;
                
            try
            {
                // Get row counts if supported
                if (capabilities.SupportsExactRowCount)
                {
                    foreach (var table in tables.ToList())
                    {
                        try
                        {
                            string countQuery = $"SELECT COUNT(*) FROM [{table.Schema}].[{table.Name}]";
                            using (var command = new SqlCommand(countQuery, connection))
                            {
                                command.CommandTimeout = timeoutSeconds ?? _configuration.DefaultCommandTimeoutSeconds;
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
                }

                // Try to get index counts if detailed metadata is supported
                if (capabilities.SupportsDetailedIndexMetadata)
                {
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
                        {
                            command.CommandTimeout = timeoutSeconds ?? _configuration.DefaultCommandTimeoutSeconds;
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
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Failed to get index counts: {ex.Message}");
                    }
                }
                
                // Try to get table sizes if supported
                if (capabilities.MajorVersion >= 11) // SQL Server 2012+
                {
                    try
                    {
                        foreach (var table in tables.ToList())
                        {
                            try
                            {
                                // This query works on SQL Server 2012 and above
                                string sizeQuery = $@"
                                    SELECT
                                        SUM(p.used_page_count) * 8.0 / 1024 AS TotalSizeMB
                                    FROM
                                        sys.dm_db_partition_stats p
                                    JOIN
                                        sys.tables t ON p.object_id = t.object_id
                                    JOIN
                                        sys.schemas s ON t.schema_id = s.schema_id
                                    WHERE
                                        s.name = '{table.Schema}' AND t.name = '{table.Name}'
                                    GROUP BY
                                        s.name, t.name";

                                using (var command = new SqlCommand(sizeQuery, connection))
                                {
                                    command.CommandTimeout = timeoutSeconds ?? _configuration.DefaultCommandTimeoutSeconds;
                                    var size = await command.ExecuteScalarAsync(cancellationToken);
                                    if (size != null && size != DBNull.Value)
                                    {
                                        var index = tables.IndexOf(table);
                                        if (index >= 0)
                                        {
                                            tables[index] = table with { SizeMB = Convert.ToDouble(size) };
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.Error.WriteLine($"Failed to get size for table {table.Schema}.{table.Name}: {ex.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Failed to get table sizes: {ex.Message}");
                    }
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
        /// <param name="timeoutSeconds">Optional timeout in seconds. If null, uses default timeout.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A collection of database information</returns>
        public async Task<IEnumerable<DatabaseInfo>> ListDatabasesAsync(ToolCallTimeoutContext? timeoutContext = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        {
            var capabilities = await GetCapabilitiesAsync(cancellationToken);
            var result = new List<DatabaseInfo>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync(cancellationToken);
                
                // Build query based on capabilities
                string query = BuildDatabaseListQuery(capabilities);

                using (var command = new SqlCommand(query, connection))
                {
                    command.CommandTimeout = timeoutSeconds ?? _configuration.DefaultCommandTimeoutSeconds;
                    using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    var fieldMap = GetReaderFieldMap(reader);
                    
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        var dbInfoBuilder = new DatabaseInfoBuilder()
                            .WithName(reader["Name"].ToString() ?? string.Empty)
                            .WithState(reader["State"].ToString() ?? string.Empty)
                            .WithCreateDate(Convert.ToDateTime(reader["CreateDate"]));
                            
                        // Add optional fields based on what's available in the reader
                        if (fieldMap.TryGetValue("SizeMB", out int sizeIndex) && reader[sizeIndex] != DBNull.Value)
                            dbInfoBuilder.WithSizeMB(Convert.ToDouble(reader[sizeIndex]));
                            
                        if (fieldMap.TryGetValue("Owner", out int ownerIndex) && reader[ownerIndex] != DBNull.Value)
                            dbInfoBuilder.WithOwner(reader[ownerIndex].ToString() ?? string.Empty);
                            
                        if (fieldMap.TryGetValue("CompatibilityLevel", out int compatIndex) && reader[compatIndex] != DBNull.Value)
                            dbInfoBuilder.WithCompatibilityLevel(reader[compatIndex].ToString() ?? string.Empty);
                            
                        if (fieldMap.TryGetValue("CollationName", out int collationIndex) && reader[collationIndex] != DBNull.Value)
                            dbInfoBuilder.WithCollationName(reader[collationIndex].ToString() ?? string.Empty);
                            
                        if (fieldMap.TryGetValue("RecoveryModel", out int recoveryIndex) && reader[recoveryIndex] != DBNull.Value)
                            dbInfoBuilder.WithRecoveryModel(reader[recoveryIndex].ToString() ?? string.Empty);
                            
                        if (fieldMap.TryGetValue("IsReadOnly", out int readOnlyIndex) && reader[readOnlyIndex] != DBNull.Value)
                            dbInfoBuilder.WithIsReadOnly(Convert.ToBoolean(reader[readOnlyIndex]));

                        result.Add(dbInfoBuilder.Build());
                    }
                    }
                }
            }

            return result;
        }
        
        /// <summary>
        /// Builds a database list query based on SQL Server capabilities.
        /// </summary>
        /// <param name="capabilities">The detected SQL Server capabilities</param>
        /// <returns>A SQL query string</returns>
        private string BuildDatabaseListQuery(SqlServerCapability capabilities)
        {
            // Basic query that works on all SQL Server versions
            string query = @"
                SELECT 
                    name AS Name,
                    state_desc AS State,
                    create_date AS CreateDate";
                    
            // Add more fields for SQL Server 2008 R2 and above
            if (capabilities.MajorVersion >= 10 && capabilities.MinorVersion >= 50)
            {
                query += ",\n                    (SELECT SUM(size * 8.0 / 1024) FROM sys.master_files WHERE database_id = db.database_id) AS SizeMB";
                query += ",\n                    SUSER_SNAME(owner_sid) AS Owner";
                query += ",\n                    compatibility_level AS CompatibilityLevel";
                query += ",\n                    collation_name AS CollationName";
                query += ",\n                    recovery_model_desc AS RecoveryModel";
                query += ",\n                    is_read_only AS IsReadOnly";
            }
            
            // Complete the query
            query += @"
                FROM 
                    sys.databases db
                ORDER BY 
                    name";
                
            return query;
        }

        /// <summary>
        /// Checks if a database exists and is accessible.
        /// </summary>
        /// <param name="databaseName">Name of the database to check</param>
        /// <param name="timeoutSeconds">Optional timeout in seconds. If null, uses default timeout.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>True if the database exists and is accessible, otherwise false</returns>
        public async Task<bool> DoesDatabaseExistAsync(string databaseName, ToolCallTimeoutContext? timeoutContext = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
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
                    command.CommandTimeout = timeoutSeconds ?? _configuration.DefaultCommandTimeoutSeconds;
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
        /// Executes a SQL query with optional database context switching.
        /// </summary>
        /// <param name="query">The SQL query to execute</param>
        /// <param name="databaseName">Optional database name to switch context. If null, uses current database.</param>
        /// <param name="timeoutSeconds">Optional timeout in seconds. If null, uses default timeout.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>An IAsyncDataReader with the results of the query</returns>
        public async Task<IAsyncDataReader> ExecuteQueryAsync(string query, string? databaseName = null, ToolCallTimeoutContext? timeoutContext = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException("Query cannot be empty", nameof(query));
            }
            
            // Get capabilities to check if this is Azure SQL Database
            var capabilities = await GetCapabilitiesAsync(cancellationToken);
            
            // For Azure SQL Database, we need to specify the database in the connection string
            string connectionString = _connectionString;
            if (capabilities.IsAzureSqlDatabase && !string.IsNullOrWhiteSpace(databaseName))
            {
                // Create a new connection string with the specified database
                var builder = new SqlConnectionStringBuilder(_connectionString);
                builder.InitialCatalog = databaseName;
                connectionString = builder.ToString();
            }
            
            // Create a new connection that will be owned by the reader
            var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            
            // If a database name is specified and it's not Azure SQL, change the database context
            if (!string.IsNullOrWhiteSpace(databaseName) && !capabilities.IsAzureSqlDatabase)
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
            var command = new SqlCommand(query, connection)
            {
                CommandTimeout = timeoutSeconds ?? _configuration.DefaultCommandTimeoutSeconds
            };
            
            // Register cancellation to cancel the command if requested
            cancellationToken.Register(() => command.Cancel());
            
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
        /// <param name="timeoutSeconds">Optional timeout in seconds. If null, uses default timeout.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Table schema information</returns>
        public async Task<TableSchemaInfo> GetTableSchemaAsync(string tableName, string? databaseName = null, ToolCallTimeoutContext? timeoutContext = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentException("Table name cannot be empty", nameof(tableName));
            }

            // Parse schema and table name
            string? schemaName = null;
            string tableNameOnly = tableName;
            
            if (tableName.Contains("."))
            {
                var parts = tableName.Split(new[] {'.'}, 2);
                schemaName = parts[0].Trim(new[] {'[', ']'});
                tableNameOnly = parts[1].Trim(new[] {'[', ']'});
            }

            var columns = new List<TableColumnInfo>();
            string currentDbName;

            // Get capabilities to check if this is Azure SQL Database
            var capabilities = await GetCapabilitiesAsync(cancellationToken);
            
            // For Azure SQL Database, we need to specify the database in the connection string
            string connectionString = _connectionString;
            if (capabilities.IsAzureSqlDatabase && !string.IsNullOrWhiteSpace(databaseName))
            {
                // Create a new connection string with the specified database
                var builder = new SqlConnectionStringBuilder(_connectionString);
                builder.InitialCatalog = databaseName;
                connectionString = builder.ToString();
            }

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync(cancellationToken);

                // Get the current database name for the context
                string currentDbQuery = "SELECT DB_NAME()";
                using (var command = new SqlCommand(currentDbQuery, connection))
                {
                    command.CommandTimeout = timeoutSeconds ?? _configuration.DefaultCommandTimeoutSeconds;
                    currentDbName = (string?)await command.ExecuteScalarAsync(cancellationToken) ?? GetCurrentDatabaseName();
                }

                // If a database name is specified and it's not Azure SQL, change the database context
                if (!string.IsNullOrWhiteSpace(databaseName) && !capabilities.IsAzureSqlDatabase)
                {
                    // First check if the database exists
                    string checkDbQuery = "SELECT COUNT(*) FROM sys.databases WHERE name = @dbName AND state_desc = 'ONLINE'";
                    using (var checkCommand = new SqlCommand(checkDbQuery, connection))
                    {
                        checkCommand.CommandTimeout = timeoutSeconds ?? _configuration.DefaultCommandTimeoutSeconds;
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
                        useCommand.CommandTimeout = timeoutSeconds ?? _configuration.DefaultCommandTimeoutSeconds;
                        await useCommand.ExecuteNonQueryAsync(cancellationToken);
                    }

                    // Update the current database name
                    currentDbName = databaseName;
                }

                // Get schema information for the table
                var schemaTable = connection.GetSchema("Columns", new[] { null, schemaName, tableNameOnly });
                if (schemaTable.Rows.Count == 0)
                {
                    // If no rows were found and a schema was provided, try to check if the table exists at all
                    if (!string.IsNullOrWhiteSpace(schemaName))
                    {
                        string checkTableQuery = @"
                            SELECT COUNT(*) 
                            FROM sys.tables t
                            JOIN sys.schemas s ON t.schema_id = s.schema_id
                            WHERE s.name = @schemaName AND t.name = @tableName";

                        using (var command = new SqlCommand(checkTableQuery, connection))
                        {
                            command.CommandTimeout = timeoutSeconds ?? _configuration.DefaultCommandTimeoutSeconds;
                            command.Parameters.AddWithValue("@schemaName", schemaName);
                            command.Parameters.AddWithValue("@tableName", tableNameOnly);
                            int tableCount = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));

                            if (tableCount > 0)
                            {
                                throw new InvalidOperationException($"Table '{schemaName}.{tableNameOnly}' exists in database '{currentDbName}' but you might not have permission to access its schema information");
                            }
                        }
                    }

                    throw new InvalidOperationException($"Table '{tableName}' does not exist in database '{currentDbName}' or you don't have permission to access it");
                }

                var columnMsDescriptions = await GetMsDescriptionForTableColumnsAsync(
                    connection,
                    schemaName,
                    tableName,
                    timeoutSeconds
                    );

                foreach (DataRow row in schemaTable.Rows)
                {
                    string columnName = row["COLUMN_NAME"].ToString() ?? string.Empty;
                    string dataType = row["DATA_TYPE"].ToString() ?? string.Empty;
                    string maxLength = row["CHARACTER_MAXIMUM_LENGTH"].ToString() ?? "-";
                    string isNullable = row["IS_NULLABLE"].ToString() ?? string.Empty;

                    _ = columnMsDescriptions.TryGetValue(columnName, out var columnMsDescription);
                    columns.Add(new TableColumnInfo(columnName, dataType, maxLength, isNullable, columnMsDescription ?? string.Empty));
                }

                // If we switched database contexts and it's not Azure SQL, switch back to the original database
                if (!string.IsNullOrWhiteSpace(databaseName) && !capabilities.IsAzureSqlDatabase)
                {
                    var builder = new SqlConnectionStringBuilder(_connectionString);
                    string originalDatabase = builder.InitialCatalog;

                    if (!string.IsNullOrWhiteSpace(originalDatabase))
                    {
                        string switchBackCommand = $"USE [{originalDatabase}]";
                        using (var command = new SqlCommand(switchBackCommand, connection))
                        {
                            command.CommandTimeout = timeoutSeconds ?? _configuration.DefaultCommandTimeoutSeconds;
                            await command.ExecuteNonQueryAsync(cancellationToken);
                        }
                    }
                }

                var tableMsDescription = await GetMsDescriptionForTableAsync(
                    connection,
                    schemaName,
                    tableName,
                    timeoutSeconds
                    );

                // For the TableSchemaInfo output, use the original table name for better UX
                return new TableSchemaInfo(tableName, currentDbName, tableMsDescription ?? string.Empty, columns);
            }
        }

        /// <summary>
        /// Lists all stored procedures with optional database context switching.
        /// </summary>
        /// <param name="databaseName">Optional database name to switch context. If null, uses current database.</param>
        /// <param name="timeoutSeconds">Optional timeout in seconds. If null, uses default timeout.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A collection of stored procedure information</returns>
        public async Task<IEnumerable<StoredProcedureInfo>> ListStoredProceduresAsync(string? databaseName = null, ToolCallTimeoutContext? timeoutContext = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        {
            var capabilities = await GetCapabilitiesAsync(cancellationToken);
            var result = new List<StoredProcedureInfo>();

            // For Azure SQL Database, we need to specify the database in the connection string
            string connectionString = _connectionString;
            if (capabilities.IsAzureSqlDatabase && !string.IsNullOrWhiteSpace(databaseName))
            {
                // Create a new connection string with the specified database
                var builder = new SqlConnectionStringBuilder(_connectionString);
                builder.InitialCatalog = databaseName;
                connectionString = builder.ToString();
            }

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync(cancellationToken);
                
                // If a database name is specified and it's not Azure SQL, change the database context
                if (!string.IsNullOrWhiteSpace(databaseName) && !capabilities.IsAzureSqlDatabase)
                {
                    string changeDbCommand = $"USE [{databaseName}]";
                    using (var command = new SqlCommand(changeDbCommand, connection))
                    {
                        command.CommandTimeout = timeoutSeconds ?? _configuration.DefaultCommandTimeoutSeconds;
                        await command.ExecuteNonQueryAsync(cancellationToken);
                    }
                }
                
                // Query to get basic stored procedure information
                string query = @"
                    SELECT 
                        s.name AS SchemaName,
                        p.name AS ProcedureName,
                        p.create_date AS CreateDate,
                        p.modify_date AS ModifyDate,
                        ISNULL(USER_NAME(p.principal_id), '') AS Owner,
                        p.is_ms_shipped,
                        CAST(CASE WHEN p.type = 'P' THEN 0 ELSE 1 END AS BIT) AS IsFunction
                    FROM 
                        sys.procedures p
                    JOIN 
                        sys.schemas s ON p.schema_id = s.schema_id
                    WHERE 
                        p.is_ms_shipped = 0
                    ORDER BY 
                        s.name, p.name";
                
                using (var command = new SqlCommand(query, connection))
                {
                    command.CommandTimeout = timeoutSeconds ?? _configuration.DefaultCommandTimeoutSeconds;
                    using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        string schemaName = reader["SchemaName"].ToString() ?? string.Empty;
                        string procName = reader["ProcedureName"].ToString() ?? string.Empty;
                        DateTime createDate = Convert.ToDateTime(reader["CreateDate"]);
                        DateTime modifyDate = Convert.ToDateTime(reader["ModifyDate"]);
                        string owner = reader["Owner"].ToString() ?? string.Empty;
                        bool isFunction = Convert.ToBoolean(reader["IsFunction"]);
                        
                        // Create a basic stored procedure info
                        result.Add(new StoredProcedureInfo(
                            SchemaName: schemaName,
                            Name: procName,
                            CreateDate: createDate,
                            ModifyDate: modifyDate,
                            Owner: owner,
                            Parameters: new List<StoredProcedureParameterInfo>(), // Will be populated later
                            IsFunction: isFunction,
                            LastExecutionTime: null,
                            ExecutionCount: null,
                            AverageDurationMs: null
                        ));
                    }
                    }
                }
                
                // Enhance stored procedure information with parameters
                // Create a copy of the list to avoid modifying the collection during iteration
                var procsCopy = new List<StoredProcedureInfo>(result);
                foreach (var proc in procsCopy)
                {
                    try
                    {
                        string paramQuery = @"
                            SELECT 
                                p.name AS ParameterName,
                                t.name AS DataType,
                                p.max_length AS Length,
                                p.precision AS Precision,
                                p.scale AS Scale,
                                p.is_output AS IsOutput,
                                p.is_nullable AS IsNullable,
                                p.default_value
                            FROM 
                                sys.parameters p
                            JOIN 
                                sys.types t ON p.user_type_id = t.user_type_id
                            JOIN 
                                sys.procedures sp ON p.object_id = sp.object_id
                            JOIN 
                                sys.schemas s ON sp.schema_id = s.schema_id
                            WHERE 
                                s.name = @schemaName AND sp.name = @procName
                            ORDER BY 
                                p.parameter_id";
                                
                        var parameters = new List<StoredProcedureParameterInfo>();
                        
                        using (var paramCommand = new SqlCommand(paramQuery, connection))
                        {
                            paramCommand.CommandTimeout = timeoutSeconds ?? _configuration.DefaultCommandTimeoutSeconds;
                            paramCommand.Parameters.AddWithValue("@schemaName", proc.SchemaName);
                            paramCommand.Parameters.AddWithValue("@procName", proc.Name);
                            
                            using (var paramReader = await paramCommand.ExecuteReaderAsync(cancellationToken))
                            {
                                while (await paramReader.ReadAsync(cancellationToken))
                                {
                                    string paramName = paramReader["ParameterName"].ToString() ?? string.Empty;
                                    string dataType = paramReader["DataType"].ToString() ?? string.Empty;
                                    int length = Convert.ToInt32(paramReader["Length"]);
                                    int precision = Convert.ToInt32(paramReader["Precision"]);
                                    int scale = Convert.ToInt32(paramReader["Scale"]);
                                    bool isOutput = Convert.ToBoolean(paramReader["IsOutput"]);
                                    bool isNullable = Convert.ToBoolean(paramReader["IsNullable"]);
                                    string? defaultValue = paramReader["default_value"] != DBNull.Value ? 
                                        paramReader["default_value"].ToString() : null;
                                        
                                    parameters.Add(new StoredProcedureParameterInfo(
                                        Name: paramName,
                                        DataType: dataType,
                                        Length: length,
                                        Precision: precision,
                                        Scale: scale,
                                        IsOutput: isOutput,
                                        IsNullable: isNullable,
                                        DefaultValue: defaultValue
                                    ));
                                }
                            }
                        }
                        
                        // Replace the stored procedure info with one that includes parameters
                        int index = result.IndexOf(proc);
                        if (index >= 0)
                        {
                            result[index] = proc with { Parameters = parameters };
                        }
                    }
                    catch (Exception ex)
                    {
                        // If getting parameters fails for a stored procedure, just continue with the next one
                        Console.Error.WriteLine($"Failed to get parameters for stored procedure {proc.SchemaName}.{proc.Name}: {ex.Message}");
                    }
                }
                
                // Try to get execution statistics if available (SQL Server 2008 R2 and above)
                if (capabilities.MajorVersion >= 10 && capabilities.MinorVersion >= 50)
                {
                    try
                    {
                        string statsQuery = @"
                            SELECT 
                                SCHEMA_NAME(o.schema_id) AS SchemaName,
                                o.name AS ProcedureName,
                                MAX(s.last_execution_time) AS LastExecutionTime,
                                MAX(s.execution_count) AS ExecutionCount,
                                AVG(s.total_elapsed_time / s.execution_count) AS AvgDurationMs
                            FROM 
                                sys.dm_exec_procedure_stats s
                            JOIN 
                                sys.objects o ON s.object_id = o.object_id
                            WHERE 
                                o.type = 'P' AND o.is_ms_shipped = 0
                            GROUP BY 
                                o.schema_id, o.name";
                                
                        using (var statsCommand = new SqlCommand(statsQuery, connection))
                        {
                            statsCommand.CommandTimeout = timeoutSeconds ?? _configuration.DefaultCommandTimeoutSeconds;
                            try
                            {
                                using (var statsReader = await statsCommand.ExecuteReaderAsync(cancellationToken))
                                {
                                    // Create a copy of the list again for this loop to avoid modifying during iteration
                                    var procsCopy2 = new List<StoredProcedureInfo>(result);
                                    while (await statsReader.ReadAsync(cancellationToken))
                                    {
                                        string schemaName = statsReader["SchemaName"].ToString() ?? string.Empty;
                                        string procName = statsReader["ProcedureName"].ToString() ?? string.Empty;
                                        
                                        // Find matching procedure
                                        var proc = procsCopy2.FirstOrDefault(p => 
                                            p.SchemaName.Equals(schemaName, StringComparison.OrdinalIgnoreCase) && 
                                            p.Name.Equals(procName, StringComparison.OrdinalIgnoreCase));
                                            
                                        if (proc != null)
                                        {
                                            int index = result.IndexOf(proc);
                                            if (index >= 0)
                                            {
                                                DateTime? lastExecTime = statsReader["LastExecutionTime"] != DBNull.Value ?
                                                    Convert.ToDateTime(statsReader["LastExecutionTime"]) : null;
                                                    
                                                int? execCount = statsReader["ExecutionCount"] != DBNull.Value ?
                                                    Convert.ToInt32(statsReader["ExecutionCount"]) : null;
                                                    
                                                int? avgDuration = statsReader["AvgDurationMs"] != DBNull.Value ?
                                                    Convert.ToInt32(statsReader["AvgDurationMs"]) : null;
                                                
                                                result[index] = proc with { 
                                                    LastExecutionTime = lastExecTime,
                                                    ExecutionCount = execCount,
                                                    AverageDurationMs = avgDuration
                                                };
                                            }
                                        }
                                    }
                                }
                            }
                            catch (SqlException ex)
                            {
                                // This query requires specific permissions, so it might fail
                                Console.Error.WriteLine($"Failed to get execution statistics for stored procedures: {ex.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error enhancing stored procedure information with statistics: {ex.Message}");
                    }
                }
                
                // If we switched database contexts and it's not Azure SQL, switch back to the original database
                if (!string.IsNullOrWhiteSpace(databaseName) && !capabilities.IsAzureSqlDatabase)
                {
                    var builder = new SqlConnectionStringBuilder(_connectionString);
                    string originalDatabase = builder.InitialCatalog;
                    
                    if (!string.IsNullOrWhiteSpace(originalDatabase))
                    {
                        string switchBackCommand = $"USE [{originalDatabase}]";
                        using (var command = new SqlCommand(switchBackCommand, connection))
                        {
                            command.CommandTimeout = timeoutSeconds ?? _configuration.DefaultCommandTimeoutSeconds;
                            await command.ExecuteNonQueryAsync(cancellationToken);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the definition information for a specific stored procedure with optional database context switching.
        /// </summary>
        /// <param name="procedureName">The name of the stored procedure</param>
        /// <param name="databaseName">Optional database name to switch context. If null, uses current database.</param>
        /// <param name="timeoutSeconds">Optional timeout in seconds. If null, uses default timeout.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Stored procedure definition as SQL string</returns>
        public async Task<string> GetStoredProcedureDefinitionAsync(string procedureName, string? databaseName = null, ToolCallTimeoutContext? timeoutContext = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(procedureName))
            {
                throw new ArgumentException("Procedure name cannot be empty", nameof(procedureName));
            }

            // Parse schema and procedure name
            string? schemaName = null;
            string procNameOnly = procedureName;
            
            if (procedureName.Contains("."))
            {
                var parts = procedureName.Split(new[] {'.'}, 2);
                schemaName = parts[0].Trim(new[] {'[', ']'});
                procNameOnly = parts[1].Trim(new[] {'[', ']'});
            }
            else
            {
                // Default schema is dbo if not specified
                schemaName = "dbo";
            }

            string definition = string.Empty;

            // Get capabilities to check if this is Azure SQL Database
            var capabilities = await GetCapabilitiesAsync(cancellationToken);
            
            // For Azure SQL Database, we need to specify the database in the connection string
            string connectionString = _connectionString;
            if (capabilities.IsAzureSqlDatabase && !string.IsNullOrWhiteSpace(databaseName))
            {
                // Create a new connection string with the specified database
                var builder = new SqlConnectionStringBuilder(_connectionString);
                builder.InitialCatalog = databaseName;
                connectionString = builder.ToString();
            }

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync(cancellationToken);
                
                // If a database name is specified and it's not Azure SQL, change the database context
                if (!string.IsNullOrWhiteSpace(databaseName) && !capabilities.IsAzureSqlDatabase)
                {
                    // First check if the database exists
                    string checkDbQuery = "SELECT COUNT(*) FROM sys.databases WHERE name = @dbName AND state_desc = 'ONLINE'";
                    using (var checkCommand = new SqlCommand(checkDbQuery, connection))
                    {
                        checkCommand.CommandTimeout = timeoutSeconds ?? _configuration.DefaultCommandTimeoutSeconds;
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
                        useCommand.CommandTimeout = timeoutSeconds ?? _configuration.DefaultCommandTimeoutSeconds;
                        await useCommand.ExecuteNonQueryAsync(cancellationToken);
                    }
                }
                
                // Check if the stored procedure exists
                string checkProcQuery = @"
                    SELECT COUNT(*) 
                    FROM sys.procedures p
                    JOIN sys.schemas s ON p.schema_id = s.schema_id
                    WHERE s.name = @schemaName AND p.name = @procName";
                    
                using (var checkCommand = new SqlCommand(checkProcQuery, connection))
                {
                    checkCommand.CommandTimeout = timeoutSeconds ?? _configuration.DefaultCommandTimeoutSeconds;
                    checkCommand.Parameters.AddWithValue("@schemaName", schemaName);
                    checkCommand.Parameters.AddWithValue("@procName", procNameOnly);
                    int procCount = Convert.ToInt32(await checkCommand.ExecuteScalarAsync(cancellationToken));
                    
                    if (procCount == 0)
                    {
                        throw new InvalidOperationException($"Stored procedure '{schemaName}.{procNameOnly}' does not exist in database '{databaseName ?? GetCurrentDatabaseName()}' or you don't have permission to access it");
                    }
                }
                
                // Get the definition
                // Use the sys.sql_modules view to get the definition
                string definitionQuery = @"
                    SELECT ISNULL(m.definition, '') AS Definition
                    FROM sys.procedures p
                    JOIN sys.schemas s ON p.schema_id = s.schema_id
                    LEFT JOIN sys.sql_modules m ON p.object_id = m.object_id
                    WHERE s.name = @schemaName AND p.name = @procName";
                    
                using (var command = new SqlCommand(definitionQuery, connection))
                {
                    command.CommandTimeout = timeoutSeconds ?? _configuration.DefaultCommandTimeoutSeconds;
                    command.Parameters.AddWithValue("@schemaName", schemaName);
                    command.Parameters.AddWithValue("@procName", procNameOnly);
                    
                    var result = await command.ExecuteScalarAsync(cancellationToken);
                    
                    if (result == null || result == DBNull.Value)
                    {
                        throw new InvalidOperationException($"Cannot retrieve definition for stored procedure '{schemaName}.{procNameOnly}'. It might be encrypted or you don't have sufficient permissions.");
                    }
                    
                    definition = result.ToString() ?? string.Empty;
                }
                
                // If the definition couldn't be retrieved using sys.sql_modules, try using OBJECT_DEFINITION()
                if (string.IsNullOrWhiteSpace(definition))
                {
                    string altDefinitionQuery = @"
                        SELECT ISNULL(OBJECT_DEFINITION(OBJECT_ID(@fullProcName)), '') AS Definition";
                        
                    using (var command = new SqlCommand(altDefinitionQuery, connection))
                    {
                        command.CommandTimeout = timeoutSeconds ?? _configuration.DefaultCommandTimeoutSeconds;
                        command.Parameters.AddWithValue("@fullProcName", $"{schemaName}.{procNameOnly}");
                        
                        var result = await command.ExecuteScalarAsync(cancellationToken);
                        
                        if (result != null && result != DBNull.Value)
                        {
                            definition = result.ToString() ?? string.Empty;
                        }
                    }
                }
                
                // If we still don't have a definition, try getting it through sp_helptext
                if (string.IsNullOrWhiteSpace(definition))
                {
                    string spHelpTextQuery = @"
                        DECLARE @text TABLE (Text NVARCHAR(MAX))
                        INSERT INTO @text
                        EXEC sp_helptext @objname = @fullProcName
                        SELECT STRING_AGG(Text, CHAR(13) + CHAR(10)) AS Definition FROM @text";
                        
                    using (var command = new SqlCommand(spHelpTextQuery, connection))
                    {
                        command.CommandTimeout = timeoutSeconds ?? _configuration.DefaultCommandTimeoutSeconds;
                        command.Parameters.AddWithValue("@fullProcName", $"{schemaName}.{procNameOnly}");
                        
                        try
                        {
                            var result = await command.ExecuteScalarAsync(cancellationToken);
                            
                            if (result != null && result != DBNull.Value)
                            {
                                definition = result.ToString() ?? string.Empty;
                            }
                        }
                        catch (SqlException)
                        {
                            // sp_helptext might fail on older SQL Server versions or if STRING_AGG isn't available
                            // In this case, we'll try one more approach
                        }
                    }
                }
                
                // If we still don't have a definition, it's likely encrypted or we don't have permissions
                if (string.IsNullOrWhiteSpace(definition))
                {
                    // Check if the stored procedure is encrypted
                    string encryptedCheckQuery = @"
                        SELECT CAST(CASE WHEN p.is_encrypted = 1 THEN 1 ELSE 0 END AS BIT) AS IsEncrypted
                        FROM sys.procedures p
                        JOIN sys.schemas s ON p.schema_id = s.schema_id
                        WHERE s.name = @schemaName AND p.name = @procName";
                        
                    using (var command = new SqlCommand(encryptedCheckQuery, connection))
                    {
                        command.CommandTimeout = timeoutSeconds ?? _configuration.DefaultCommandTimeoutSeconds;
                        command.Parameters.AddWithValue("@schemaName", schemaName);
                        command.Parameters.AddWithValue("@procName", procNameOnly);
                        
                        var result = await command.ExecuteScalarAsync(cancellationToken);
                        
                        if (result != null && result != DBNull.Value && Convert.ToBoolean(result))
                        {
                            definition = "-- The stored procedure is encrypted and its definition cannot be retrieved.";
                        }
                        else
                        {
                            definition = "-- Unable to retrieve stored procedure definition. You may not have sufficient permissions.";
                        }
                    }
                }
                
                // If we switched database contexts and it's not Azure SQL, switch back to the original database
                if (!string.IsNullOrWhiteSpace(databaseName) && !capabilities.IsAzureSqlDatabase)
                {
                    var builder = new SqlConnectionStringBuilder(_connectionString);
                    string originalDatabase = builder.InitialCatalog;
                    
                    if (!string.IsNullOrWhiteSpace(originalDatabase))
                    {
                        string switchBackCommand = $"USE [{originalDatabase}]";
                        using (var command = new SqlCommand(switchBackCommand, connection))
                        {
                            command.CommandTimeout = timeoutSeconds ?? _configuration.DefaultCommandTimeoutSeconds;
                            await command.ExecuteNonQueryAsync(cancellationToken);
                        }
                    }
                }
            }
            
            return definition;
        }
        
        /// <summary>
        /// Executes a stored procedure with optional database context switching.
        /// </summary>
        /// <param name="procedureName">The name of the stored procedure to execute</param>
        /// <param name="parameters">Dictionary of parameter names and values</param>
        /// <param name="databaseName">Optional database name to switch context. If null, uses current database.</param>
        /// <param name="timeoutSeconds">Optional timeout in seconds. If null, uses default timeout.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>An IAsyncDataReader with the results of the stored procedure</returns>
        public async Task<IAsyncDataReader> ExecuteStoredProcedureAsync(string procedureName, Dictionary<string, object?> parameters, string? databaseName = null, ToolCallTimeoutContext? timeoutContext = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(procedureName))
            {
                throw new ArgumentException("Procedure name cannot be empty", nameof(procedureName));
            }
            
            // Normalize input parameters
            var normalizedParameters = ParameterNormalizer.NormalizeParameterDictionary(parameters);
            
            // Parse schema and procedure name
            string schemaName = "dbo";
            string procNameOnly = procedureName;
            
            if (procedureName.Contains("."))
            {
                var parts = procedureName.Split(new[] {'.'}, 2);
                schemaName = parts[0].Trim(new[] {'[', ']'});
                procNameOnly = parts[1].Trim(new[] {'[', ']'});
            }
            
            // Get capabilities to check if this is Azure SQL Database
            var capabilities = await GetCapabilitiesAsync(cancellationToken);
            
            // For Azure SQL Database, we need to specify the database in the connection string
            string connectionString = _connectionString;
            if (capabilities.IsAzureSqlDatabase && !string.IsNullOrWhiteSpace(databaseName))
            {
                // Create a new connection string with the specified database
                var builder = new SqlConnectionStringBuilder(_connectionString);
                builder.InitialCatalog = databaseName;
                connectionString = builder.ToString();
            }
            
            var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            
            try
            {
                // Handle database context switching if needed - for non-Azure SQL Server only
                if (!string.IsNullOrWhiteSpace(databaseName) && !capabilities.IsAzureSqlDatabase)
                {
                    await SwitchDatabaseContext(connection, databaseName, cancellationToken);
                }
                
                // Verify stored procedure exists
                await VerifyStoredProcedureExists(connection, schemaName, procNameOnly, cancellationToken);
                
                // Get stored procedure metadata
                var procMetadata = await GetStoredProcedureMetadata(
                    connection, schemaName, procNameOnly, cancellationToken);
                
                // Validate parameters
                ValidateParameters(procMetadata, normalizedParameters, schemaName, procNameOnly);
                
                // Create and configure command
                var command = new SqlCommand($"[{schemaName}].[{procNameOnly}]", connection)
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandTimeout = timeoutSeconds ?? _configuration.DefaultCommandTimeoutSeconds
                };
                
                // Register cancellation to cancel the command if requested
                cancellationToken.Register(() => command.Cancel());
                
                // Add parameters with proper type conversion
                foreach (var metadata in procMetadata)
                {
                    if (string.IsNullOrEmpty(metadata.ParameterName))
                        continue;
                    
                    var normalizedName = ParameterNormalizer.NormalizeParameterName(metadata.ParameterName);
                    normalizedParameters.TryGetValue(normalizedName, out var value);
                    
                    var sqlParam = SqlTypeMapper.CreateSqlParameter(
                        metadata.ParameterName,
                        value,
                        metadata.DataType,
                        metadata.MaxLength,
                        metadata.Precision,
                        metadata.Scale,
                        metadata.IsOutput
                    );
                    
                    command.Parameters.Add(sqlParam);
                }
                
                // Add return value parameter
                var returnParam = new SqlParameter("@RETURN_VALUE", SqlDbType.Int)
                {
                    Direction = ParameterDirection.ReturnValue
                };
                command.Parameters.Add(returnParam);
                
                // Execute the stored procedure
                var sqlReader = await command.ExecuteReaderAsync(
                    CommandBehavior.CloseConnection, cancellationToken);
                
                return new AsyncDataReaderAdapter(sqlReader);
            }
            catch (Exception)
            {
                connection.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Switches database context if the specified database exists and is online.
        /// For Azure SQL Database, creates a new connection with the target database in the connection string.
        /// For regular SQL Server, uses the USE statement.
        /// </summary>
        private async Task SwitchDatabaseContext(SqlConnection connection, string databaseName, CancellationToken cancellationToken)
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
            
            // Get capabilities to check if this is Azure SQL Database
            var capabilities = await GetCapabilitiesAsync(cancellationToken);
            
            if (capabilities.IsAzureSqlDatabase)
            {
                // For Azure SQL, we cannot use USE statement - the connection must be closed and reopened
                // with the target database specified in the connection string
                
                // We do nothing here as the caller is responsible for managing the connection
                // The caller should check capabilities.IsAzureSqlDatabase and create a new connection
                // with the target database specified if needed
                
                // This method is called from ExecuteStoredProcedureAsync where we use a fresh connection
                // that is managed by the caller, so we can just return here
                
                // We're intentionally NOT closing the connection here as that should be managed by the caller
                
                // Log info about Azure SQL database - will be handled by caller
                Console.WriteLine($"Azure SQL Database detected. Database context switching will be handled by creating a new connection to '{databaseName}'.");
            }
            else
            {
                // For regular SQL Server, use the USE statement
                string useDbCommand = $"USE [{databaseName}]";
                using (var useCommand = new SqlCommand(useDbCommand, connection))
                {
                    await useCommand.ExecuteNonQueryAsync(cancellationToken);
                }
            }
        }

        /// <summary>
        /// Verifies that the specified stored procedure exists.
        /// </summary>
        private async Task VerifyStoredProcedureExists(SqlConnection connection, string schemaName, string procedureName, CancellationToken cancellationToken)
        {
            string checkProcQuery = @"
                SELECT COUNT(*) 
                FROM sys.procedures p
                JOIN sys.schemas s ON p.schema_id = s.schema_id
                WHERE s.name = @schemaName AND p.name = @procName";
                
            using (var checkCommand = new SqlCommand(checkProcQuery, connection))
            {
                checkCommand.Parameters.AddWithValue("@schemaName", schemaName);
                checkCommand.Parameters.AddWithValue("@procName", procedureName);
                int procCount = Convert.ToInt32(await checkCommand.ExecuteScalarAsync(cancellationToken));
                
                if (procCount == 0)
                {
                    throw new InvalidOperationException($"Stored procedure '{schemaName}.{procedureName}' does not exist or you don't have permission to access it");
                }
            }
        }

        /// <summary>
        /// Gets stored procedure parameter metadata from SQL Server system tables.
        /// </summary>
        private async Task<List<StoredProcedureParameterMetadata>> GetStoredProcedureMetadata(
            SqlConnection connection,
            string schemaName,
            string procedureName,
            CancellationToken cancellationToken)
        {
            var query = @"
                SELECT 
                    p.name AS ParameterName,
                    p.parameter_id AS ParameterId,
                    t.name AS DataType,
                    p.max_length AS MaxLength,
                    p.precision AS Precision,
                    p.scale AS Scale,
                    p.is_output AS IsOutput,
                    p.has_default_value AS HasDefaultValue,
                    p.default_value AS DefaultValue
                FROM sys.parameters p
                JOIN sys.types t ON p.user_type_id = t.user_type_id
                JOIN sys.procedures sp ON p.object_id = sp.object_id
                JOIN sys.schemas s ON sp.schema_id = s.schema_id
                WHERE s.name = @schemaName AND sp.name = @procedureName
                ORDER BY p.parameter_id";
            
            var metadata = new List<StoredProcedureParameterMetadata>();
            
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@schemaName", schemaName);
                command.Parameters.AddWithValue("@procedureName", procedureName);
                
                using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        metadata.Add(new StoredProcedureParameterMetadata
                        {
                            ParameterName = reader["ParameterName"].ToString() ?? "",
                            ParameterId = Convert.ToInt32(reader["ParameterId"]),
                            DataType = reader["DataType"].ToString() ?? "",
                            MaxLength = Convert.ToInt32(reader["MaxLength"]),
                            Precision = Convert.ToByte(reader["Precision"]),
                            Scale = Convert.ToByte(reader["Scale"]),
                            IsOutput = Convert.ToBoolean(reader["IsOutput"]),
                            HasDefaultValue = Convert.ToBoolean(reader["HasDefaultValue"]),
                            DefaultValue = reader["DefaultValue"]
                        });
                    }
                }
            }
            
            return metadata;
        }

        /// <summary>
        /// Validates that all required parameters are provided and no unknown parameters are included.
        /// </summary>
        private void ValidateParameters(
            List<StoredProcedureParameterMetadata> procMetadata,
            Dictionary<string, object?> normalizedParameters,
            string schemaName,
            string procedureName)
        {
            var errors = new List<string>();
            
            // Check for missing required parameters
            foreach (var param in procMetadata.Where(p => 
                !p.IsOutput && 
                !p.HasDefaultValue && 
                p.ParameterId > 0)) // Skip return value
            {
                var normalizedName = ParameterNormalizer.NormalizeParameterName(param.ParameterName);
                if (!normalizedParameters.ContainsKey(normalizedName))
                {
                    errors.Add($"Missing required parameter: {param.ParameterName} ({param.GetDisplayType()})");
                }
            }
            
            // Check for unknown parameters
            var validParamNames = procMetadata
                .Where(p => !string.IsNullOrEmpty(p.ParameterName))
                .Select(p => ParameterNormalizer.NormalizeParameterName(p.ParameterName))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            
            foreach (var providedParam in normalizedParameters.Keys)
            {
                if (!validParamNames.Contains(providedParam))
                {
                    errors.Add($"Unknown parameter: {providedParam}");
                }
            }
            
            if (errors.Any())
            {
                var expectedParams = procMetadata
                    .Where(p => !string.IsNullOrEmpty(p.ParameterName))
                    .Select(p => $"{p.ParameterName} ({p.GetDisplayType()}){(p.IsOutput ? " OUTPUT" : "")}{(p.HasDefaultValue ? " = " + p.DefaultValue : "")}");
                
                throw new ArgumentException(
                    $"Parameter validation failed for '{schemaName}.{procedureName}':\n" +
                    string.Join("\n", errors) + "\n\n" +
                    $"Expected parameters:\n{string.Join("\n", expectedParams)}");
            }
        }

        /// <summary>
        /// Collects and returns a MS Description extended property for a given table.
        /// This info is valuable for LLM context.
        /// </summary>
        private async Task<string?> GetMsDescriptionForTableAsync(
            SqlConnection connection,
            string? schemaName,
            string tableName,
            int? timeoutSeconds = null
            )
        {
            ArgumentNullException.ThrowIfNull(connection);
            ArgumentException.ThrowIfNullOrEmpty(tableName);

            schemaName ??= "dbo";

            const string CheckDbQuery =
$"""
SELECT 
    sep.value [Description]
FROM sys.tables st
LEFT JOIN sys.extended_properties sep ON
    st.object_id = sep.major_id
    and sep.minor_id = 0
    and sep.name = 'MS_Description'
WHERE
    st.name = @tableName
    AND st.schema_id = SCHEMA_ID(@schemaName)
""";
            using (var command = new SqlCommand(CheckDbQuery, connection))
            {
                command.CommandTimeout = timeoutSeconds ?? _configuration.DefaultCommandTimeoutSeconds;
                command.Parameters.AddWithValue("@tableName", tableName);
                command.Parameters.AddWithValue("@schemaName", schemaName);

                var description = (await command.ExecuteScalarAsync()) as string;
                return description;
            }
        }

        /// <summary>
        /// Collects and returns a columns MS Description extended properties for a given table.
        /// This info is valuable for LLM context.
        /// </summary>
        private async Task<Dictionary<string, string?>> GetMsDescriptionForTableColumnsAsync(
            SqlConnection connection,
            string? schemaName,
            string tableName,
            int? timeoutSeconds = null
            )
        {
            ArgumentNullException.ThrowIfNull(connection);
            ArgumentException.ThrowIfNullOrEmpty(tableName);

            schemaName ??= "dbo";

            var result = new Dictionary<string, string?>();

            const string ColumnColumnName = "Column";
            const string DescriptionColumnName = "Description";
            const string CheckDbQuery =
$"""
SELECT 
    sc.name [{ColumnColumnName}],
    sep.value [{DescriptionColumnName}]
FROM sys.tables st
JOIN sys.columns sc ON st.object_id = sc.object_id
LEFT JOIN sys.extended_properties sep ON
    st.object_id = sep.major_id
    and sc.column_id = sep.minor_id
    and sep.name = 'MS_Description'
WHERE
    st.name = @tableName
    AND st.schema_id = SCHEMA_ID(@schemaName)
""";
            using (var command = new SqlCommand(CheckDbQuery, connection))
            {
                command.CommandTimeout = timeoutSeconds ?? _configuration.DefaultCommandTimeoutSeconds;
                command.Parameters.AddWithValue("@tableName", tableName);
                command.Parameters.AddWithValue("@schemaName", schemaName);

                using (var qr = await command.ExecuteReaderAsync())
                {
                    while (qr.Read())
                    {
                        if (qr.IsDBNull(DescriptionColumnName))
                        {
                            continue;
                        }

                        var columnName = qr.GetString(ColumnColumnName);
                        var description = qr.GetString(DescriptionColumnName);
                        result[columnName] = description;
                    }
                }
            }

            return result;
        }

    }

    /// <summary>
    /// Helper class to build DatabaseInfo objects with optional properties.
    /// </summary>
    public class DatabaseInfoBuilder
    {
        private string _name = string.Empty;
        private string _state = string.Empty;
        private double? _sizeMB;
        private string? _owner;
        private string? _compatibilityLevel;
        private string? _collationName;
        private DateTime _createDate;
        private string? _recoveryModel;
        private bool? _isReadOnly;
        
        public DatabaseInfoBuilder WithName(string name)
        {
            _name = name;
            return this;
        }
        
        public DatabaseInfoBuilder WithState(string state)
        {
            _state = state;
            return this;
        }
        
        public DatabaseInfoBuilder WithSizeMB(double sizeMB)
        {
            _sizeMB = sizeMB;
            return this;
        }
        
        public DatabaseInfoBuilder WithOwner(string owner)
        {
            _owner = owner;
            return this;
        }
        
        public DatabaseInfoBuilder WithCompatibilityLevel(string compatibilityLevel)
        {
            _compatibilityLevel = compatibilityLevel;
            return this;
        }
        
        public DatabaseInfoBuilder WithCollationName(string collationName)
        {
            _collationName = collationName;
            return this;
        }
        
        public DatabaseInfoBuilder WithCreateDate(DateTime createDate)
        {
            _createDate = createDate;
            return this;
        }
        
        public DatabaseInfoBuilder WithRecoveryModel(string recoveryModel)
        {
            _recoveryModel = recoveryModel;
            return this;
        }
        
        public DatabaseInfoBuilder WithIsReadOnly(bool isReadOnly)
        {
            _isReadOnly = isReadOnly;
            return this;
        }
        
        public DatabaseInfo Build()
        {
            return new DatabaseInfo(
                Name: _name,
                State: _state,
                SizeMB: _sizeMB,
                Owner: _owner ?? string.Empty,
                CompatibilityLevel: _compatibilityLevel ?? string.Empty,
                CollationName: _collationName ?? string.Empty,
                CreateDate: _createDate,
                RecoveryModel: _recoveryModel ?? string.Empty,
                IsReadOnly: _isReadOnly ?? false
            );
        }
    }
}