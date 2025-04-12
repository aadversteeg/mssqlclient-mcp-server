using Core.Infrastructure.McpServer.Configuration;
using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

namespace Ave.McpServer.MSSQL.Tools
{
    [McpServerToolType]
    public class ExecuteQueryTool
    {
        private readonly string? _connectionString;

        public ExecuteQueryTool(DatabaseConfiguration dbConfig)
        {
            _connectionString = dbConfig.ConnectionString;
            Console.Error.WriteLine($"ExecuteQueryTool constructed with connection string: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
        }

        [McpServerTool(Name = "execute_query"), Description("Execute a SQL query on the connected SQL Server database.")]
        public string ExecuteQuery(string query)
        {
            Console.Error.WriteLine($"ExecuteQuery called with query: {query}");
            Console.Error.WriteLine($"Connection string is: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
            
            if (string.IsNullOrEmpty(_connectionString))
            {
                return "Error: No connection string provided. Set the MSSQL_CONNECTIONSTRING environment variable.";
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                return "Error: Query cannot be empty";
            }

            try
            {
                using SqlConnection connection = new SqlConnection(_connectionString);
                connection.Open();
                
                using SqlCommand command = new SqlCommand(query, connection);
                using SqlDataReader reader = command.ExecuteReader();
                
                // Format results into a readable table
                var result = FormatQueryResults(reader);
                
                return result;
            }
            catch (Exception ex)
            {
                return $"Error: SQL error: {ex.Message}";
            }
        }

        private static string FormatQueryResults(SqlDataReader reader)
        {
            StringBuilder result = new StringBuilder();
            
            // Check if the reader has rows
            if (!reader.HasRows)
            {
                return "Query executed successfully. No results returned.";
            }
            
            // Get column information
            int columnCount = reader.FieldCount;
            List<string> columnNames = new List<string>();
            List<int> columnWidths = new List<int>();
            
            for (int i = 0; i < columnCount; i++)
            {
                string columnName = reader.GetName(i);
                columnNames.Add(columnName);
                columnWidths.Add(columnName.Length);
            }
            
            // Create a list to store all rows for processing
            List<string[]> rows = new List<string[]>();
            
            // Process rows to determine optimal column widths
            while (reader.Read())
            {
                string[] rowValues = new string[columnCount];
                
                for (int i = 0; i < columnCount; i++)
                {
                    string value = reader.IsDBNull(i) ? "NULL" : reader[i].ToString() ?? "";
                    rowValues[i] = value;
                    columnWidths[i] = Math.Max(columnWidths[i], value.Length);
                }
                
                rows.Add(rowValues);
            }
            
            // Limit column width to a reasonable size
            for (int i = 0; i < columnWidths.Count; i++)
            {
                columnWidths[i] = Math.Min(columnWidths[i], 40);
            }
            
            // Build header row
            for (int i = 0; i < columnCount; i++)
            {
                result.Append("| ");
                result.Append(columnNames[i].PadRight(columnWidths[i]));
                result.Append(" ");
            }
            result.AppendLine("|");
            
            // Build separator row
            for (int i = 0; i < columnCount; i++)
            {
                result.Append("| ");
                result.Append(new string('-', columnWidths[i]));
                result.Append(" ");
            }
            result.AppendLine("|");
            
            // Build data rows
            foreach (var rowValues in rows)
            {
                for (int i = 0; i < columnCount; i++)
                {
                    string displayValue = rowValues[i];
                    
                    // Truncate value if too long
                    if (displayValue.Length > columnWidths[i])
                    {
                        displayValue = displayValue.Substring(0, columnWidths[i] - 3) + "...";
                    }
                    
                    result.Append("| ");
                    result.Append(displayValue.PadRight(columnWidths[i]));
                    result.Append(" ");
                }
                result.AppendLine("|");
            }
            
            // Add row count
            result.AppendLine();
            result.AppendLine($"Total rows: {rows.Count}");
            
            return result.ToString();
        }
    }
}