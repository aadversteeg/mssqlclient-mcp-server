using System.Text;
using Core.Application.Interfaces;

namespace Core.Infrastructure.McpServer.Extensions
{
    /// <summary>
    /// Extension methods for IAsyncDataReader to produce formatted tool results
    /// </summary>
    public static class AsyncDataReaderExtensions
    {
        /// <summary>
        /// Converts an IAsyncDataReader to a formatted tool result string
        /// </summary>
        /// <param name="reader">The IAsyncDataReader to format</param>
        /// <returns>A formatted string for tool output</returns>
        public static async Task<string> ToToolResult(this IAsyncDataReader reader)
        {
            StringBuilder result = new StringBuilder();
            
            // Get column information
            int columnCount = reader.FieldCount;
            List<string> columnNames = reader.GetColumnNames().ToList();
            List<int> columnWidths = columnNames.Select(name => name.Length).ToList();
            
            // Create a list to store all rows for processing
            List<string[]> rows = new List<string[]>();
            
            // Process rows to determine optimal column widths
            while (await reader.ReadAsync())
            {
                string[] rowValues = new string[columnCount];
                
                for (int i = 0; i < columnCount; i++)
                {
                    bool isNull = await reader.IsDBNullAsync(i);
                    string value = isNull ? "NULL" : (await reader.GetFieldValueAsync<object>(i))?.ToString() ?? "";
                    rowValues[i] = value;
                    columnWidths[i] = Math.Max(columnWidths[i], value.Length);
                }
                
                rows.Add(rowValues);
            }
            
            // Check if no rows were returned
            if (rows.Count == 0)
            {
                return "Query executed successfully. No results returned.";
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
        
        /// <summary>
        /// Synchronous version of ToToolResult that internally calls the async version
        /// </summary>
        /// <param name="reader">The IAsyncDataReader to format</param>
        /// <returns>A formatted string for tool output</returns>
        public static string ToToolResultSync(this IAsyncDataReader reader)
        {
            // Since we're in a synchronous context but working with async methods,
            // we'll use Task.Run to execute an async method and wait for it to complete.
            return Task.Run(async () => await reader.ToToolResult()).GetAwaiter().GetResult();
        }
    }
}