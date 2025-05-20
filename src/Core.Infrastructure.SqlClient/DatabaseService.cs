using Microsoft.Data.SqlClient;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Infrastructure.SqlClient.Interfaces;
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
        private SqlServerCapability? _capabilities;
        private bool _capabilitiesDetected = false;

        /// <summary>
        /// Initializes a new instance of the DatabaseService class.
        /// </summary>
        /// <param name="connectionString">The SQL Server connection string</param>
        /// <param name="capabilityDetector">The SQL Server capability detector</param>
        public DatabaseService(string connectionString, ISqlServerCapabilityDetector capabilityDetector)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _capabilityDetector = capabilityDetector ?? throw new ArgumentNullException(nameof(capabilityDetector));
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
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A collection of table information</returns>
        public async Task<IEnumerable<TableInfo>> ListTablesAsync(string? databaseName = null, CancellationToken cancellationToken = default)
        {
            var capabilities = await GetCapabilitiesAsync(cancellationToken);
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
                
                // Build a query based on detected capabilities
                string query = BuildTableListQuery(capabilities);
                
                using (var command = new SqlCommand(query, connection))
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

                // Enhance table information if not all data was available in the initial query
                await EnhanceTableInfoAsync(result, connection, capabilities, cancellationToken);
                
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
                query += ",\n                    0 AS RowCount"; // Will be enhanced later
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
        /// <param name="cancellationToken">Optional cancellation token</param>
        private async Task EnhanceTableInfoAsync(
            List<TableInfo> tables, 
            SqlConnection connection,
            SqlServerCapability capabilities,
            CancellationToken cancellationToken = default)
        {
            if (tables == null || tables.Count == 0)
                return;
                
            try
            {
                // Get row counts if supported
                if (capabilities.SupportsExactRowCount)
                {
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
                
                // Try to get table sizes if supported
                if (capabilities.MajorVersion >= 11) // SQL Server 2012+
                {
                    try
                    {
                        foreach (var table in tables)
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
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A collection of database information</returns>
        public async Task<IEnumerable<DatabaseInfo>> ListDatabasesAsync(CancellationToken cancellationToken = default)
        {
            var capabilities = await GetCapabilitiesAsync(cancellationToken);
            var result = new List<DatabaseInfo>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync(cancellationToken);
                
                // Build query based on capabilities
                string query = BuildDatabaseListQuery(capabilities);

                using (var command = new SqlCommand(query, connection))
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
            
            // For the TableSchemaInfo output, use the original table name for better UX
            return new TableSchemaInfo(tableName, currentDbName, columns);
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