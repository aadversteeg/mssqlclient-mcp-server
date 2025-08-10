using System.Text;
using Core.Application.Models;

namespace Core.Infrastructure.McpServer.Extensions
{
    /// <summary>
    /// Extension methods for TableSchemaInfo to produce formatted tool results
    /// </summary>
    public static class TableSchemaInfoExtensions
    {
        /// <summary>
        /// Converts a TableSchemaInfo object to a formatted tool result string
        /// </summary>
        /// <param name="tableSchema">The TableSchemaInfo object</param>
        /// <returns>A formatted string for tool output</returns>
        public static string ToToolResult(this TableSchemaInfo tableSchema)
        {
            var sb = new StringBuilder();
            
            if (!string.IsNullOrEmpty(tableSchema.DatabaseName))
            {
                sb.AppendLine($"# Schema for table `{tableSchema.TableName}` {tableSchema.GetMsDescriptionWithBrackets()} in database `{tableSchema.DatabaseName}`:");
            }
            else
            {
                sb.AppendLine($"Schema for table `{tableSchema.TableName}` {tableSchema.GetMsDescriptionWithBrackets()}:");
            }
            
            sb.AppendLine();
            sb.AppendLine("Column Name | Data Type | Max Length | Is Nullable | Description");
            sb.AppendLine("----------- | --------- | ---------- | ----------- | -----------");
            
            foreach (var column in tableSchema.Columns)
            {
                sb.AppendLine($"{column.ColumnName} | {column.DataType} | {column.MaxLength} | {column.IsNullable} | {column.MsDescription}");
            }
            
            return sb.ToString();
        }
    }
}