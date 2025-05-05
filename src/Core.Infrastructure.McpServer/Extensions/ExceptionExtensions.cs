using System.Text;

namespace Core.Infrastructure.McpServer.Extensions
{
    /// <summary>
    /// Extension methods for formatting exceptions into user-friendly tool results
    /// </summary>
    public static class ExceptionExtensions
    {
        /// <summary>
        /// Formats an exception into a user-friendly database error message
        /// </summary>
        /// <param name="exception">The exception to format</param>
        /// <param name="title">Optional custom title for the error section</param>
        /// <param name="additionalCauses">Optional additional causes to include in the message</param>
        /// <returns>A formatted string for tool output</returns>
        public static string ToDatabaseErrorResult(
            this Exception exception, 
            string title = "Error Getting Database Information",
            params string[] additionalCauses)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"## {title}");
            sb.AppendLine();
            sb.AppendLine($"```");
            sb.AppendLine($"{exception.Message}");
            sb.AppendLine($"```");
            
            sb.AppendLine();
            sb.AppendLine("### Possible Causes");
            sb.AppendLine();

            // Add standard causes
            sb.AppendLine("- Insufficient permissions to perform the operation");
            sb.AppendLine("- Connection issues with the SQL Server");
            
            // Add any additional causes
            foreach (var cause in additionalCauses)
            {
                sb.AppendLine($"- {cause}");
            }
            
            return sb.ToString();
        }

        /// <summary>
        /// Formats a SQL operation error into a user-friendly message
        /// </summary>
        /// <param name="exception">The exception to format</param>
        /// <param name="operationType">The type of operation that failed (e.g., "listing tables", "querying database")</param>
        /// <returns>A formatted error message for tool output</returns>
        public static string ToSqlErrorResult(this Exception exception, string operationType)
        {
            return $"Error: SQL error {(string.IsNullOrEmpty(operationType) ? "" : $"while {operationType}")}: {exception.Message}";
        }

        /// <summary>
        /// Formats a database-related exception into a simplified error message
        /// </summary>
        /// <param name="exception">The exception to format</param>
        /// <returns>A formatted error message for tool output</returns>
        public static string ToSimpleDatabaseErrorResult(this Exception exception)
        {
            var sb = new StringBuilder();
            sb.AppendLine("## Error Getting Database Information");
            sb.AppendLine();
            sb.AppendLine($"```");
            sb.AppendLine($"{exception.Message}");
            sb.AppendLine($"```");
            
            sb.AppendLine();
            sb.AppendLine("### Possible Causes");
            sb.AppendLine();
            sb.AppendLine("- Insufficient permissions to view server-level information");
            sb.AppendLine("- Connection issues with the SQL Server");
            
            return sb.ToString();
        }
    }
}