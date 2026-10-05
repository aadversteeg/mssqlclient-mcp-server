using System.Text;
using Microsoft.Data.SqlClient;

namespace Core.Infrastructure.McpServer.Extensions
{
    /// <summary>
    /// Extension methods for formatting exceptions into user-friendly tool results
    /// </summary>
    public static class ExceptionExtensions
    {
        /// <summary>
        /// Maps SQL Server error numbers to safe, user-friendly messages.
        /// This prevents leaking internal database structure in error responses.
        /// </summary>
        private static string GetSafeErrorMessage(SqlException ex)
        {
            // Map common SQL error numbers to generic messages
            // Full list: https://docs.microsoft.com/en-us/sql/relational-databases/errors-events/database-engine-events-and-errors
            return ex.Number switch
            {
                -2 => "The operation timed out. Try reducing the scope of your query or increasing the timeout.",
                -1 => "The connection was lost. Please try again.",
                0 => "The operation was cancelled.",
                4060 => "Cannot access the specified database. Check that the database exists and you have permission.",
                18456 => "Authentication failed. Check your credentials.",
                229 => "Permission denied for this operation.",
                230 => "Permission denied for this operation.",
                262 => "Permission denied. You may not have the required database permissions.",
                297 => "The user does not have permission to perform this action.",
                4063 => "Cannot access the specified database.",
                547 => "The operation conflicts with a database constraint.",
                2601 => "Cannot insert duplicate key.",
                2627 => "Cannot insert duplicate key. A record with this key already exists.",
                515 => "Cannot insert NULL value into a required field.",
                544 => "Cannot insert explicit value into an identity column.",
                8152 => "String or binary data would be truncated.",
                207 => "Invalid column name in query.",
                208 => "Invalid object name in query.",
                102 => "Syntax error in SQL statement.",
                156 => "Syntax error in SQL statement.",
                _ => "A database error occurred. Please check your query and try again."
            };
        }

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
        /// Formats a SQL operation error into a user-friendly message.
        /// Sanitizes error messages to prevent leaking internal database structure.
        /// </summary>
        /// <param name="exception">The exception to format</param>
        /// <param name="operationType">The type of operation that failed (e.g., "listing tables", "querying database")</param>
        /// <returns>A formatted error message for tool output</returns>
        public static string ToSqlErrorResult(this Exception exception, string operationType)
        {
            string safeMessage;

            if (exception is SqlException sqlEx)
            {
                // Use safe, mapped error message for SQL exceptions
                safeMessage = GetSafeErrorMessage(sqlEx);
            }
            else
            {
                // For non-SQL exceptions, use a generic message
                safeMessage = "An unexpected error occurred while processing your request.";
            }

            var operationContext = string.IsNullOrEmpty(operationType) ? "" : $" while {operationType}";
            return $"Error{operationContext}: {safeMessage}";
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