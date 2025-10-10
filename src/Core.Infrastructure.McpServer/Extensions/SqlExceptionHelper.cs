using Microsoft.Data.SqlClient;

namespace Core.Infrastructure.McpServer.Extensions
{
    /// <summary>
    /// Helper methods for working with SqlException objects.
    /// </summary>
    public static class SqlExceptionHelper
    {
        /// <summary>
        /// Determines if a SqlException is a timeout or cancellation error.
        /// </summary>
        /// <param name="ex">The SQL exception to check</param>
        /// <returns>True if the exception indicates a timeout or cancellation</returns>
        public static bool IsTimeoutError(SqlException ex)
        {
            // SQL Server error codes for timeout/cancellation:
            // -2 = Timeout expired
            // -1 = Connection broken (can occur during cancellation)
            //  0 = Operation cancelled by user
            return ex.Number == -2 || ex.Number == -1 || ex.Number == 0;
        }
    }
}
