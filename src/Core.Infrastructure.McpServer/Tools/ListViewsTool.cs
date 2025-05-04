using Core.Infrastructure.McpServer.Configuration;
using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

namespace Ave.McpServer.MSSQL.Tools
{
    [McpServerToolType]
    public class ListViewsTool
    {
        private readonly string? _connectionString;

        public ListViewsTool(DatabaseConfiguration dbConfig)
        {
            _connectionString = dbConfig.ConnectionString;
            Console.Error.WriteLine($"ListViewsTool constructed with connection string: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
        }

        [McpServerTool(Name = "list_views"), Description("List all views in the connected SQL Server database.")]
        public string ListViews()
        {
            Console.Error.WriteLine($"ListViews called");
            Console.Error.WriteLine($"Connection string is: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
            
            if (string.IsNullOrEmpty(_connectionString))
            {
                return "Error: No connection string provided. Set the MSSQL_CONNECTIONSTRING environment variable.";
            }

            try
            {
                using SqlConnection connection = new SqlConnection(_connectionString);
                connection.Open();
                
                // Query to get all views
                string query = @"
                    SELECT 
                        v.name AS ViewName,
                        s.name AS SchemaName,
                        CASE
                            WHEN v.is_indexed = 1 THEN 'Yes'
                            ELSE 'No'
                        END AS IsIndexed,
                        CASE
                            WHEN v.is_replicated = 1 THEN 'Yes'
                            ELSE 'No'
                        END AS IsReplicated,
                        CASE
                            WHEN ep.value IS NOT NULL THEN ep.value
                            ELSE ''
                        END AS Description,
                        CASE
                            WHEN v.is_schema_bound = 1 THEN 'Yes'
                            ELSE 'No'
                        END AS IsSchemaBound,
                        CONVERT(VARCHAR(20), v.create_date, 120) AS CreatedDate,
                        CONVERT(VARCHAR(20), v.modify_date, 120) AS ModifiedDate
                    FROM 
                        sys.views v
                    INNER JOIN 
                        sys.schemas s ON v.schema_id = s.schema_id
                    LEFT JOIN 
                        sys.extended_properties ep ON v.object_id = ep.major_id AND ep.minor_id = 0 AND ep.name = 'MS_Description'
                    ORDER BY 
                        s.name, v.name";
                
                using SqlCommand command = new SqlCommand(query, connection);
                using SqlDataReader reader = command.ExecuteReader();
                
                StringBuilder viewList = new StringBuilder();
                viewList.AppendLine("Database Views:");
                viewList.AppendLine();
                viewList.AppendLine("Schema | View Name | Indexed | Schema Bound | Created Date | Modified Date | Description");
                viewList.AppendLine("------ | --------- | ------- | ------------ | ------------ | ------------- | -----------");
                
                while (reader.Read())
                {
                    string schemaName = reader["SchemaName"].ToString() ?? "";
                    string viewName = reader["ViewName"].ToString() ?? "";
                    string isIndexed = reader["IsIndexed"].ToString() ?? "No";
                    string isSchemaBound = reader["IsSchemaBound"].ToString() ?? "No";
                    string createdDate = reader["CreatedDate"].ToString() ?? "";
                    string modifiedDate = reader["ModifiedDate"].ToString() ?? "";
                    string description = reader["Description"].ToString() ?? "";
                    
                    viewList.AppendLine($"{schemaName} | {viewName} | {isIndexed} | {isSchemaBound} | {createdDate} | {modifiedDate} | {description}");
                }
                
                return viewList.ToString();
            }
            catch (Exception ex)
            {
                return $"Error: SQL error: {ex.Message}";
            }
        }
    }
}