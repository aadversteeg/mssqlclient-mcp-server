using Core.Infrastructure.McpServer.Configuration;
using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

namespace Ave.McpServer.MSSQL.Tools
{
    [McpServerToolType]
    public class ListStoredProceduresTool
    {
        private readonly string? _connectionString;

        public ListStoredProceduresTool(DatabaseConfiguration dbConfig)
        {
            _connectionString = dbConfig.ConnectionString;
            Console.Error.WriteLine($"ListStoredProceduresTool constructed with connection string: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
        }

        [McpServerTool(Name = "list_stored_procedures"), Description("List all stored procedures in the connected SQL Server database.")]
        public string ListStoredProcedures()
        {
            Console.Error.WriteLine($"ListStoredProcedures called");
            Console.Error.WriteLine($"Connection string is: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
            
            if (string.IsNullOrEmpty(_connectionString))
            {
                return "Error: No connection string provided. Set the MSSQL_CONNECTIONSTRING environment variable.";
            }

            try
            {
                using SqlConnection connection = new SqlConnection(_connectionString);
                connection.Open();
                
                // Query to get all stored procedures
                string query = @"
                    SELECT 
                        p.name AS ProcedureName,
                        s.name AS SchemaName,
                        CONVERT(VARCHAR(20), p.create_date, 120) AS CreatedDate,
                        CONVERT(VARCHAR(20), p.modify_date, 120) AS ModifiedDate,
                        CASE
                            WHEN p.is_recompiled = 1 THEN 'Yes'
                            ELSE 'No'
                        END AS IsRecompiled,
                        CASE
                            WHEN p.is_encrypted = 1 THEN 'Yes'
                            ELSE 'No'
                        END AS IsEncrypted,
                        CASE
                            WHEN ep.value IS NOT NULL THEN ep.value
                            ELSE ''
                        END AS Description,
                        (SELECT COUNT(*) FROM sys.parameters param WHERE param.object_id = p.object_id) AS ParameterCount
                    FROM 
                        sys.procedures p
                    INNER JOIN 
                        sys.schemas s ON p.schema_id = s.schema_id
                    LEFT JOIN 
                        sys.extended_properties ep ON p.object_id = ep.major_id AND ep.minor_id = 0 AND ep.name = 'MS_Description'
                    ORDER BY 
                        s.name, p.name";
                
                using SqlCommand command = new SqlCommand(query, connection);
                using SqlDataReader reader = command.ExecuteReader();
                
                StringBuilder procList = new StringBuilder();
                procList.AppendLine("Stored Procedures:");
                procList.AppendLine();
                procList.AppendLine("Schema | Procedure Name | Parameters | Encrypted | Created Date | Modified Date | Description");
                procList.AppendLine("------ | -------------- | ---------- | --------- | ------------ | ------------- | -----------");
                
                while (reader.Read())
                {
                    string schemaName = reader["SchemaName"].ToString() ?? "";
                    string procedureName = reader["ProcedureName"].ToString() ?? "";
                    string parameterCount = reader["ParameterCount"].ToString() ?? "0";
                    string isEncrypted = reader["IsEncrypted"].ToString() ?? "No";
                    string createdDate = reader["CreatedDate"].ToString() ?? "";
                    string modifiedDate = reader["ModifiedDate"].ToString() ?? "";
                    string description = reader["Description"].ToString() ?? "";
                    
                    procList.AppendLine($"{schemaName} | {procedureName} | {parameterCount} | {isEncrypted} | {createdDate} | {modifiedDate} | {description}");
                }
                
                return procList.ToString();
            }
            catch (Exception ex)
            {
                return $"Error: SQL error: {ex.Message}";
            }
        }
    }
}