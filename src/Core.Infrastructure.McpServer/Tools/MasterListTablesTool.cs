using Core.Infrastructure.McpServer.Configuration;
using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class MasterListTablesTool
    {
        private readonly string? _connectionString;
        private readonly SqlConnectionContext _connectionContext;

        public MasterListTablesTool(DatabaseConfiguration dbConfig, SqlConnectionContext connectionContext)
        {
            _connectionString = dbConfig.ConnectionString;
            _connectionContext = connectionContext ?? throw new ArgumentNullException(nameof(connectionContext));
            Console.Error.WriteLine($"MasterListTablesTool constructed with connection string: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
            Console.Error.WriteLine($"Connected to master: {_connectionContext.IsConnectedToMaster}");
        }

        [McpServerTool(Name = "list_tables_in_database"), Description("List tables in the specified database (requires master database connection).")]
        public string ListTablesInDatabase(string databaseName)
        {
            Console.Error.WriteLine($"ListTablesInDatabase called with databaseName: {databaseName}");
            Console.Error.WriteLine($"Connection string is: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
            
            if (string.IsNullOrEmpty(_connectionString))
            {
                return "Error: No connection string provided. Set the MSSQL_CONNECTIONSTRING environment variable.";
            }

            if (!_connectionContext.IsConnectedToMaster)
            {
                return "Error: This tool can only be used when connected to the master database. Please reconnect to the master database and try again.";
            }

            if (string.IsNullOrWhiteSpace(databaseName))
            {
                return "Error: Database name cannot be empty.";
            }

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"# Tables in Database: {databaseName}");
                sb.AppendLine();

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // First, verify the database exists
                    string checkDbQuery = "SELECT COUNT(*) FROM sys.databases WHERE name = @DatabaseName";
                    using (var checkDbCommand = new SqlCommand(checkDbQuery, connection))
                    {
                        checkDbCommand.Parameters.AddWithValue("@DatabaseName", databaseName);
                        int dbCount = (int)checkDbCommand.ExecuteScalar();
                        
                        if (dbCount == 0)
                        {
                            return $"Error: Database '{databaseName}' does not exist on this server.";
                        }
                    }

                    // Check if the database is accessible
                    string accessibleQuery = "SELECT state_desc FROM sys.databases WHERE name = @DatabaseName";
                    using (var accessibleCommand = new SqlCommand(accessibleQuery, connection))
                    {
                        accessibleCommand.Parameters.AddWithValue("@DatabaseName", databaseName);
                        string? state = (string?)accessibleCommand.ExecuteScalar();
                        
                        if (state != "ONLINE")
                        {
                            return $"Error: Database '{databaseName}' is not online (current state: {state}). Cannot access its tables.";
                        }
                    }

                    // Get table information using three-part naming
                    string tableQuery = $@"
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
                            (SELECT COUNT(*) FROM [{databaseName}].sys.indexes i WHERE i.object_id = t.object_id) AS IndexCount,
                            (SELECT COUNT(*) FROM [{databaseName}].sys.foreign_keys fk WHERE fk.parent_object_id = t.object_id) AS ForeignKeyCount
                        FROM 
                            [{databaseName}].sys.tables t
                        JOIN 
                            [{databaseName}].sys.schemas s ON t.schema_id = s.schema_id
                        JOIN 
                            [{databaseName}].sys.indexes i ON t.object_id = i.object_id
                        JOIN 
                            [{databaseName}].sys.partitions p ON i.object_id = p.object_id AND i.index_id = p.index_id
                        JOIN 
                            [{databaseName}].sys.allocation_units a ON p.partition_id = a.container_id
                        WHERE 
                            t.is_ms_shipped = 0
                        GROUP BY 
                            s.name, t.name, p.rows, t.create_date, t.modify_date, t.temporal_type, t.object_id
                        ORDER BY 
                            s.name, t.name";

                    using (var tableCommand = new SqlCommand(tableQuery, connection))
                    {
                        using (var reader = tableCommand.ExecuteReader())
                        {
                            // If no tables found
                            if (!reader.HasRows)
                            {
                                sb.AppendLine($"No user tables found in database '{databaseName}'.");
                                return sb.ToString();
                            }

                            // Create table header
                            sb.AppendLine("| Schema | Table Name | Row Count | Size (MB) | Indexes | Foreign Keys | Created | Last Modified | Type |");
                            sb.AppendLine("|--------|------------|-----------|-----------|---------|--------------|---------|---------------|------|");

                            // Add table rows
                            while (reader.Read())
                            {
                                string schema = reader["SchemaName"].ToString() ?? "";
                                string tableName = reader["TableName"].ToString() ?? "";
                                long rowCount = reader["RowCount"] != DBNull.Value ? Convert.ToInt64(reader["RowCount"]) : 0;
                                double totalSizeMB = reader["TotalSizeMB"] != DBNull.Value ? Convert.ToDouble(reader["TotalSizeMB"]) : 0;
                                DateTime createDate = Convert.ToDateTime(reader["CreateDate"]);
                                DateTime modifyDate = Convert.ToDateTime(reader["ModifyDate"]);
                                string tableType = reader["TableType"].ToString() ?? "Normal";
                                int indexCount = Convert.ToInt32(reader["IndexCount"]);
                                int foreignKeyCount = Convert.ToInt32(reader["ForeignKeyCount"]);

                                sb.AppendLine($"| {schema} | {tableName} | {rowCount:#,0} | {totalSizeMB:F2} | {indexCount} | {foreignKeyCount} | {createDate:yyyy-MM-dd} | {modifyDate:yyyy-MM-dd} | {tableType} |");
                            }
                        }
                    }

                    // Add a summary section
                    sb.AppendLine();
                    sb.AppendLine("## Summary");
                    sb.AppendLine();

                    string summaryQuery = $@"
                        SELECT 
                            COUNT(*) AS TableCount,
                            SUM(p.rows) AS TotalRows,
                            SUM(a.total_pages) * 8 / 1024 AS TotalSizeMB,
                            COUNT(DISTINCT s.name) AS SchemaCount
                        FROM 
                            [{databaseName}].sys.tables t
                        JOIN 
                            [{databaseName}].sys.schemas s ON t.schema_id = s.schema_id
                        JOIN 
                            [{databaseName}].sys.indexes i ON t.object_id = i.object_id
                        JOIN 
                            [{databaseName}].sys.partitions p ON i.object_id = p.object_id AND i.index_id = p.index_id
                        JOIN 
                            [{databaseName}].sys.allocation_units a ON p.partition_id = a.container_id
                        WHERE 
                            t.is_ms_shipped = 0";

                    using (var summaryCommand = new SqlCommand(summaryQuery, connection))
                    {
                        using (var reader = summaryCommand.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int tableCount = Convert.ToInt32(reader["TableCount"]);
                                long totalRows = reader["TotalRows"] != DBNull.Value ? Convert.ToInt64(reader["TotalRows"]) : 0;
                                double totalSizeMB = reader["TotalSizeMB"] != DBNull.Value ? Convert.ToDouble(reader["TotalSizeMB"]) : 0;
                                int schemaCount = Convert.ToInt32(reader["SchemaCount"]);

                                sb.AppendLine($"- **Total Tables**: {tableCount}");
                                sb.AppendLine($"- **Total Rows**: {totalRows:#,0}");
                                sb.AppendLine($"- **Total Size**: {totalSizeMB:F2} MB");
                                sb.AppendLine($"- **Schemas**: {schemaCount}");
                            }
                        }
                    }

                    // Add schema distribution
                    sb.AppendLine();
                    sb.AppendLine("## Tables by Schema");
                    sb.AppendLine();

                    string schemaQuery = $@"
                        SELECT 
                            s.name AS SchemaName,
                            COUNT(*) AS TableCount
                        FROM 
                            [{databaseName}].sys.tables t
                        JOIN 
                            [{databaseName}].sys.schemas s ON t.schema_id = s.schema_id
                        WHERE 
                            t.is_ms_shipped = 0
                        GROUP BY 
                            s.name
                        ORDER BY 
                            COUNT(*) DESC";

                    using (var schemaCommand = new SqlCommand(schemaQuery, connection))
                    {
                        using (var reader = schemaCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                sb.AppendLine("| Schema | Table Count |");
                                sb.AppendLine("|--------|-------------|");

                                while (reader.Read())
                                {
                                    string schemaName = reader["SchemaName"].ToString() ?? "";
                                    int tableCount = Convert.ToInt32(reader["TableCount"]);

                                    sb.AppendLine($"| {schemaName} | {tableCount} |");
                                }
                            }
                        }
                    }
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Error listing tables: {ex.Message}\n\n{ex.StackTrace}";
            }
        }
    }
}
