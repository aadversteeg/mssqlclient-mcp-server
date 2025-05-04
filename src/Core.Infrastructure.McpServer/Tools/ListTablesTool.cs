using Core.Infrastructure.McpServer.Configuration;
using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class ListTablesTool
    {
        private readonly string? _connectionString;

        public ListTablesTool(DatabaseConfiguration dbConfig)
        {
            _connectionString = dbConfig.ConnectionString;
            Console.Error.WriteLine($"ListTablesTool constructed with connection string: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
        }

        [McpServerTool(Name = "list_tables"), Description("List all tables in the connected SQL Server database.")]
        public string ListTables()
        {
            Console.Error.WriteLine($"ListTables called");
            Console.Error.WriteLine($"Connection string is: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
            
            if (string.IsNullOrEmpty(_connectionString))
            {
                return "Error: No connection string provided. Set the MSSQL_CONNECTIONSTRING environment variable.";
            }

            try
            {
                using SqlConnection connection = new SqlConnection(_connectionString);
                connection.Open();
                
                // Query to get all user tables
                string query = @"
                    SELECT 
                        t.name AS TableName,
                        s.name AS SchemaName,
                        p.rows AS NumberOfRows
                    FROM 
                        sys.tables t
                    INNER JOIN 
                        sys.schemas s ON t.schema_id = s.schema_id
                    INNER JOIN 
                        sys.partitions p ON t.object_id = p.object_id
                    WHERE 
                        p.index_id IN (0, 1)
                    ORDER BY 
                        s.name, t.name";
                
                using SqlCommand command = new SqlCommand(query, connection);
                using SqlDataReader reader = command.ExecuteReader();
                
                StringBuilder tableList = new StringBuilder();
                tableList.AppendLine("Available Tables:");
                tableList.AppendLine();
                tableList.AppendLine("Schema | Table Name | Row Count");
                tableList.AppendLine("------ | ---------- | ---------");
                
                while (reader.Read())
                {
                    string schemaName = reader["SchemaName"].ToString() ?? "";
                    string tableName = reader["TableName"].ToString() ?? "";
                    string rowCount = reader["NumberOfRows"].ToString() ?? "0";
                    
                    tableList.AppendLine($"{schemaName} | {tableName} | {rowCount}");
                }
                
                return tableList.ToString();
            }
            catch (Exception ex)
            {
                return $"Error: SQL error: {ex.Message}";
            }
        }
    }
}