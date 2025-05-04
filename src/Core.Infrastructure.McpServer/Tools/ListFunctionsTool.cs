using Core.Infrastructure.McpServer.Configuration;
using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

namespace Ave.McpServer.MSSQL.Tools
{
    [McpServerToolType]
    public class ListFunctionsTool
    {
        private readonly string? _connectionString;

        public ListFunctionsTool(DatabaseConfiguration dbConfig)
        {
            _connectionString = dbConfig.ConnectionString;
            Console.Error.WriteLine($"ListFunctionsTool constructed with connection string: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
        }

        [McpServerTool(Name = "list_functions"), Description("List all user-defined functions in the connected SQL Server database.")]
        public string ListFunctions()
        {
            Console.Error.WriteLine($"ListFunctions called");
            Console.Error.WriteLine($"Connection string is: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
            
            if (string.IsNullOrEmpty(_connectionString))
            {
                return "Error: No connection string provided. Set the MSSQL_CONNECTIONSTRING environment variable.";
            }

            try
            {
                using SqlConnection connection = new SqlConnection(_connectionString);
                connection.Open();
                
                // Query to get all user-defined functions
                string query = @"
                    SELECT 
                        o.name AS FunctionName,
                        s.name AS SchemaName,
                        CASE o.type
                            WHEN 'FN' THEN 'Scalar Function'
                            WHEN 'IF' THEN 'Inline Table-Valued Function'
                            WHEN 'TF' THEN 'Table-Valued Function'
                            ELSE 'Unknown'
                        END AS FunctionType,
                        CONVERT(VARCHAR(20), o.create_date, 120) AS CreatedDate,
                        CONVERT(VARCHAR(20), o.modify_date, 120) AS ModifiedDate,
                        CASE
                            WHEN o.is_ms_shipped = 1 THEN 'Yes'
                            ELSE 'No'
                        END AS IsSystemFunction,
                        CASE
                            WHEN m.is_encrypted = 1 THEN 'Yes'
                            ELSE 'No'
                        END AS IsEncrypted,
                        CASE
                            WHEN ep.value IS NOT NULL THEN ep.value
                            ELSE ''
                        END AS Description,
                        (SELECT COUNT(*) FROM sys.parameters param WHERE param.object_id = o.object_id) AS ParameterCount
                    FROM 
                        sys.objects o
                    INNER JOIN 
                        sys.schemas s ON o.schema_id = s.schema_id
                    LEFT JOIN 
                        sys.sql_modules m ON o.object_id = m.object_id
                    LEFT JOIN 
                        sys.extended_properties ep ON o.object_id = ep.major_id AND ep.minor_id = 0 AND ep.name = 'MS_Description'
                    WHERE 
                        o.type IN ('FN', 'IF', 'TF')
                    ORDER BY 
                        s.name, o.name";
                
                using SqlCommand command = new SqlCommand(query, connection);
                using SqlDataReader reader = command.ExecuteReader();
                
                StringBuilder functionList = new StringBuilder();
                functionList.AppendLine("User-Defined Functions:");
                functionList.AppendLine();
                functionList.AppendLine("Schema | Function Name | Type | Parameters | Encrypted | Created Date | Modified Date | Description");
                functionList.AppendLine("------ | ------------- | ---- | ---------- | --------- | ------------ | ------------- | -----------");
                
                while (reader.Read())
                {
                    string schemaName = reader["SchemaName"].ToString() ?? "";
                    string functionName = reader["FunctionName"].ToString() ?? "";
                    string functionType = reader["FunctionType"].ToString() ?? "";
                    string parameterCount = reader["ParameterCount"].ToString() ?? "0";
                    string isEncrypted = reader["IsEncrypted"].ToString() ?? "No";
                    string createdDate = reader["CreatedDate"].ToString() ?? "";
                    string modifiedDate = reader["ModifiedDate"].ToString() ?? "";
                    string description = reader["Description"].ToString() ?? "";
                    
                    functionList.AppendLine($"{schemaName} | {functionName} | {functionType} | {parameterCount} | {isEncrypted} | {createdDate} | {modifiedDate} | {description}");
                }
                
                return functionList.ToString();
            }
            catch (Exception ex)
            {
                return $"Error: SQL error: {ex.Message}";
            }
        }
    }
}