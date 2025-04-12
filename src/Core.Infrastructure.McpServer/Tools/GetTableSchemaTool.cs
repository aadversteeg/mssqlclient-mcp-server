using Core.Infrastructure.McpServer.Configuration;
using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Data;
using System.Text;

namespace Ave.McpServer.MSSQL.Tools
{
    [McpServerToolType]
    public class GetTableSchemaTool
    {
        private readonly string? _connectionString;

        public GetTableSchemaTool(DatabaseConfiguration dbConfig)
        {
            _connectionString = dbConfig.ConnectionString;
            Console.Error.WriteLine($"GetTableSchemaTool constructed with connection string: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
        }

        [McpServerTool(Name = "get_table_schema"), Description("Get the schema of a table from the connected SQL Server database.")]
        public string GetTableSchema(string tableName)
        {
            Console.Error.WriteLine($"GetTableSchema called with tableName: {tableName}");
            Console.Error.WriteLine($"Connection string is: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
            
            if (string.IsNullOrEmpty(_connectionString))
            {
                return "Error: No connection string provided. Set the MSSQL_CONNECTIONSTRING environment variable.";
            }

            if (string.IsNullOrWhiteSpace(tableName))
            {
                return "Error: Table name cannot be empty";
            }

            try
            {
                using SqlConnection connection = new SqlConnection(_connectionString);
                connection.Open();
                
                // Get schema information for the table
                var schemaTable = connection.GetSchema("Columns", new[] { null, null, tableName });
                
                StringBuilder schemaInfo = new StringBuilder();
                schemaInfo.AppendLine($"Schema for table: {tableName}");
                schemaInfo.AppendLine();
                schemaInfo.AppendLine("Column Name | Data Type | Max Length | Is Nullable");
                schemaInfo.AppendLine("----------- | --------- | ---------- | -----------");
                
                foreach (DataRow row in schemaTable.Rows)
                {
                    string columnName = row["COLUMN_NAME"].ToString() ?? "";
                    string dataType = row["DATA_TYPE"].ToString() ?? "";
                    string maxLength = row["CHARACTER_MAXIMUM_LENGTH"].ToString() ?? "-";
                    string isNullable = row["IS_NULLABLE"].ToString() ?? "";
                    
                    schemaInfo.AppendLine($"{columnName} | {dataType} | {maxLength} | {isNullable}");
                }
                
                return schemaInfo.ToString();
            }
            catch (Exception ex)
            {
                return $"Error: SQL error: {ex.Message}";
            }
        }
    }
}