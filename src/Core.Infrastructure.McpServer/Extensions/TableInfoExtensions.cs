using System.Text;
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
            sb.AppendLine("Schema | Table Name | Row Count | Size (MB) | Type | Indexes | Foreign Keys");
            sb.AppendLine("------ | ---------- | --------- | --------- | ---- | ------- | ------------");
            
            foreach (var table in tables)
            {
                sb.AppendLine($"{table.Schema} | {table.Name} | {table.RowCount} | {table.SizeMB:F2} | {table.TableType} | {table.IndexCount} | {table.ForeignKeyCount}");
            }
            
            return sb.ToString();
        }
    }
}