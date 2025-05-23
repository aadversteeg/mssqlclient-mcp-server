using System.Text;
using System.Linq;
using Core.Application.Models;

namespace Core.Infrastructure.McpServer.Extensions
{
    /// <summary>
    /// Extension methods for TableInfo to produce formatted tool results
    /// </summary>
    public static class TableInfoExtensions
    {
        /// <summary>
        /// Converts a collection of TableInfo objects to a formatted tool result string
        /// </summary>
        /// <param name="tables">The collection of TableInfo objects</param>
        /// <param name="databaseName">Optional database name for context</param>
        /// <returns>A formatted string for tool output</returns>
        public static string ToToolResult(this IEnumerable<TableInfo> tables, string? databaseName = null)
        {
            var sb = new StringBuilder();
            var tablesList = tables.ToList();
            
            // Add title with optional database name
            if (!string.IsNullOrEmpty(databaseName))
            {
                sb.AppendLine($"# Tables in Database: {databaseName}");
            }
            else
            {
                sb.AppendLine("Available Tables:");
            }
            
            sb.AppendLine();
            
            // Check if all tables have null or zero values for specific columns
            bool allRowCountsZero = tablesList.All(t => t.RowCount == null || t.RowCount == 0);
            bool allSizesZero = tablesList.All(t => t.SizeMB == null || t.SizeMB == 0);
            bool allIndexCountsZero = tablesList.All(t => t.IndexCount == null || t.IndexCount == 0);
            bool allForeignKeyCountsZero = tablesList.All(t => t.ForeignKeyCount == null || t.ForeignKeyCount == 0);
            
            // Build header based on values
            List<string> headerParts = new() { "Schema", "Table Name" };
            List<string> separatorParts = new() { "------", "----------" };
            
            if (!allRowCountsZero)
            {
                headerParts.Add("Row Count");
                separatorParts.Add("---------");
            }
            
            if (!allSizesZero)
            {
                headerParts.Add("Size (MB)");
                separatorParts.Add("---------");
            }
            
            headerParts.Add("Type");
            separatorParts.Add("----");
            
            if (!allIndexCountsZero)
            {
                headerParts.Add("Indexes");
                separatorParts.Add("-------");
            }
            
            if (!allForeignKeyCountsZero)
            {
                headerParts.Add("Foreign Keys");
                separatorParts.Add("------------");
            }
            
            // Write headers
            sb.AppendLine(string.Join(" | ", headerParts));
            sb.AppendLine(string.Join(" | ", separatorParts));
            
            // Write table data
            foreach (var table in tablesList)
            {
                List<string> rowParts = new() { table.Schema, table.Name };
                
                if (!allRowCountsZero)
                {
                    rowParts.Add(table.RowCount?.ToString() ?? "N/A");
                }
                
                if (!allSizesZero)
                {
                    rowParts.Add(table.SizeMB.HasValue ? table.SizeMB.Value.ToString("F2") : "N/A");
                }
                
                rowParts.Add(table.TableType);
                
                if (!allIndexCountsZero)
                {
                    rowParts.Add(table.IndexCount?.ToString() ?? "N/A");
                }
                
                if (!allForeignKeyCountsZero)
                {
                    rowParts.Add(table.ForeignKeyCount?.ToString() ?? "N/A");
                }
                
                sb.AppendLine(string.Join(" | ", rowParts));
            }
            
            return sb.ToString();
        }
    }
}