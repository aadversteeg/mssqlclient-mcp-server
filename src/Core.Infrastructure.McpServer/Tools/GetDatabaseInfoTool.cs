using Core.Infrastructure.McpServer.Configuration;
using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

namespace Ave.McpServer.MSSQL.Tools
{
    [McpServerToolType]
    public class GetDatabaseInfoTool
    {
        private readonly string? _connectionString;

        public GetDatabaseInfoTool(DatabaseConfiguration dbConfig)
        {
            _connectionString = dbConfig.ConnectionString;
            Console.Error.WriteLine($"GetDatabaseInfoTool constructed with connection string: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
        }

        [McpServerTool(Name = "get_database_info"), Description("Get general information about the connected SQL Server database.")]
        public string GetDatabaseInfo()
        {
            Console.Error.WriteLine($"GetDatabaseInfo called");
            Console.Error.WriteLine($"Connection string is: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
            
            if (string.IsNullOrEmpty(_connectionString))
            {
                return "Error: No connection string provided. Set the MSSQL_CONNECTIONSTRING environment variable.";
            }

            try
            {
                using SqlConnection connection = new SqlConnection(_connectionString);
                connection.Open();
                
                string dbName = connection.Database;
                
                // Query to get database details
                string query = @"
                    SELECT 
                        SERVERPROPERTY('ProductVersion') AS SQLServerVersion,
                        SERVERPROPERTY('ProductLevel') AS ServicePack,
                        SERVERPROPERTY('Edition') AS Edition,
                        DB_NAME() AS DatabaseName,
                        DATABASEPROPERTYEX(DB_NAME(), 'Collation') AS Collation,
                        DATABASEPROPERTYEX(DB_NAME(), 'Recovery') AS RecoveryModel,
                        CONVERT(VARCHAR(25), create_date, 120) AS CreateDate,
                        compatibility_level AS CompatibilityLevel,
                        page_verify_option_desc AS PageVerify,
                        is_auto_shrink_on AS AutoShrink,
                        CASE is_read_only WHEN 1 THEN 'Yes' ELSE 'No' END AS IsReadOnly,
                        CASE is_auto_create_stats_on WHEN 1 THEN 'Yes' ELSE 'No' END AS AutoCreateStats,
                        CASE is_auto_update_stats_on WHEN 1 THEN 'Yes' ELSE 'No' END AS AutoUpdateStats
                    FROM 
                        sys.databases
                    WHERE 
                        name = DB_NAME()";
                
                using SqlCommand command = new SqlCommand(query, connection);
                using SqlDataReader reader = command.ExecuteReader();
                
                StringBuilder dbInfo = new StringBuilder();
                dbInfo.AppendLine("Database Information:");
                dbInfo.AppendLine();
                
                if (reader.Read())
                {
                    dbInfo.AppendLine($"Database Name: {reader["DatabaseName"]}");
                    dbInfo.AppendLine($"SQL Server Version: {reader["SQLServerVersion"]} {reader["ServicePack"]}");
                    dbInfo.AppendLine($"Edition: {reader["Edition"]}");
                    dbInfo.AppendLine($"Collation: {reader["Collation"]}");
                    dbInfo.AppendLine($"Recovery Model: {reader["RecoveryModel"]}");
                    dbInfo.AppendLine($"Compatibility Level: {reader["CompatibilityLevel"]}");
                    dbInfo.AppendLine($"Creation Date: {reader["CreateDate"]}");
                    dbInfo.AppendLine($"Page Verify: {reader["PageVerify"]}");
                    dbInfo.AppendLine($"Read Only: {reader["IsReadOnly"]}");
                    dbInfo.AppendLine($"Auto Shrink: {reader["AutoShrink"]}");
                    dbInfo.AppendLine($"Auto Create Statistics: {reader["AutoCreateStats"]}");
                    dbInfo.AppendLine($"Auto Update Statistics: {reader["AutoUpdateStats"]}");
                }
                
                // Get database size
                reader.Close();
                query = @"
                    SELECT 
                        SUM(size * 8.0 / 1024) AS DatabaseSizeMB
                    FROM 
                        sys.database_files
                    WHERE 
                        type IN (0, 1)";
                
                command.CommandText = query;
                var dbSize = command.ExecuteScalar();
                
                dbInfo.AppendLine($"Database Size: {dbSize} MB");
                
                // Get object counts
                reader.Close();
                query = @"
                    SELECT 
                        (SELECT COUNT(*) FROM sys.tables) AS TableCount,
                        (SELECT COUNT(*) FROM sys.views) AS ViewCount,
                        (SELECT COUNT(*) FROM sys.procedures) AS ProcedureCount,
                        (SELECT COUNT(*) FROM sys.triggers) AS TriggerCount,
                        (SELECT COUNT(*) FROM sys.objects WHERE type IN ('FN', 'IF', 'TF')) AS FunctionCount
                    ";
                
                command.CommandText = query;
                // Create a new reader instead of reassigning the using variable
                using SqlDataReader objectCountReader = command.ExecuteReader();
                
                if (objectCountReader.Read())
                {
                    dbInfo.AppendLine();
                    dbInfo.AppendLine("Object Counts:");
                    dbInfo.AppendLine($"Tables: {objectCountReader["TableCount"]}");
                    dbInfo.AppendLine($"Views: {objectCountReader["ViewCount"]}");
                    dbInfo.AppendLine($"Stored Procedures: {objectCountReader["ProcedureCount"]}");
                    dbInfo.AppendLine($"Triggers: {objectCountReader["TriggerCount"]}");
                    dbInfo.AppendLine($"Functions: {objectCountReader["FunctionCount"]}");
                }
                
                return dbInfo.ToString();
            }
            catch (Exception ex)
            {
                return $"Error: SQL error: {ex.Message}";
            }
        }
    }
}