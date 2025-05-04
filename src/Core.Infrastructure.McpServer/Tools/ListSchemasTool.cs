using Core.Infrastructure.McpServer.Configuration;
using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

namespace Ave.McpServer.MSSQL.Tools
{
    [McpServerToolType]
    public class ListSchemasTool
    {
        private readonly string? _connectionString;

        public ListSchemasTool(DatabaseConfiguration dbConfig)
        {
            _connectionString = dbConfig.ConnectionString;
            Console.Error.WriteLine($"ListSchemasTool constructed with connection string: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
        }

        [McpServerTool(Name = "list_schemas"), Description("List all schemas in the connected SQL Server database.")]
        public string ListSchemas()
        {
            Console.Error.WriteLine($"ListSchemas called");
            Console.Error.WriteLine($"Connection string is: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
            
            if (string.IsNullOrEmpty(_connectionString))
            {
                return "Error: No connection string provided. Set the MSSQL_CONNECTIONSTRING environment variable.";
            }

            try
            {
                using SqlConnection connection = new SqlConnection(_connectionString);
                connection.Open();
                
                // Query to get all schemas
                string query = @"
                    SELECT 
                        s.schema_id,
                        s.name AS SchemaName,
                        CASE 
                            WHEN s.schema_id = 1 THEN 'System schema for database diagrams' 
                            WHEN s.schema_id = 2 THEN 'Owner of system objects'
                            WHEN s.schema_id = 3 THEN 'Owner of system catalog views'
                            WHEN s.schema_id = 4 THEN 'Information schema views'
                            ELSE COALESCE(p.name, 'No owner') 
                        END AS SchemaOwner,
                        (SELECT COUNT(*) FROM sys.objects o WHERE o.schema_id = s.schema_id AND o.type = 'U') AS TableCount,
                        (SELECT COUNT(*) FROM sys.objects o WHERE o.schema_id = s.schema_id AND o.type = 'V') AS ViewCount,
                        (SELECT COUNT(*) FROM sys.objects o WHERE o.schema_id = s.schema_id AND o.type = 'P') AS ProcedureCount,
                        (SELECT COUNT(*) FROM sys.objects o WHERE o.schema_id = s.schema_id AND o.type IN ('FN', 'IF', 'TF')) AS FunctionCount
                    FROM 
                        sys.schemas s
                    LEFT JOIN 
                        sys.database_principals p ON s.principal_id = p.principal_id
                    ORDER BY 
                        s.name";
                
                using SqlCommand command = new SqlCommand(query, connection);
                using SqlDataReader reader = command.ExecuteReader();
                
                StringBuilder schemaList = new StringBuilder();
                schemaList.AppendLine("Database Schemas:");
                schemaList.AppendLine();
                schemaList.AppendLine("Schema Name | Owner | Tables | Views | Procedures | Functions");
                schemaList.AppendLine("----------- | ----- | ------ | ----- | ---------- | ---------");
                
                while (reader.Read())
                {
                    string schemaName = reader["SchemaName"].ToString() ?? "";
                    string schemaOwner = reader["SchemaOwner"].ToString() ?? "";
                    string tableCount = reader["TableCount"].ToString() ?? "0";
                    string viewCount = reader["ViewCount"].ToString() ?? "0";
                    string procedureCount = reader["ProcedureCount"].ToString() ?? "0";
                    string functionCount = reader["FunctionCount"].ToString() ?? "0";
                    
                    schemaList.AppendLine($"{schemaName} | {schemaOwner} | {tableCount} | {viewCount} | {procedureCount} | {functionCount}");
                }
                
                return schemaList.ToString();
            }
            catch (Exception ex)
            {
                return $"Error: SQL error: {ex.Message}";
            }
        }
    }
}