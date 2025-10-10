using Core.Application.Interfaces;
using Core.Application.Models;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Core.Infrastructure.McpServer.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Data.SqlClient;

namespace Core.Infrastructure.McpServer.Tools
{
    /// <summary>
    /// Tool to list all databases on a SQL Server instance with their properties
    /// </summary>
    [McpServerToolType]
    public class ServerListDatabasesTool
    {
        private readonly IServerDatabase _serverDatabase;
        private readonly DatabaseConfiguration _configuration;

        public ServerListDatabasesTool(IServerDatabase serverDatabase, IOptions<DatabaseConfiguration> configuration)
        {
            _serverDatabase = serverDatabase ?? throw new ArgumentNullException(nameof(serverDatabase));
            _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Gets a list of all databases on the SQL Server instance with their properties
        /// </summary>
        /// <param name="timeoutSeconds">The timeout in seconds for the operation (optional)</param>
        /// <returns>Markdown formatted string with database information</returns>
        [McpServerTool(Name = "list_databases"), Description("List all databases on the SQL Server instance.")]
        public async Task<string> GetDatabases(int? timeoutSeconds = null)
        {
            Console.Error.WriteLine($"GetDatabases called with timeoutSeconds: {timeoutSeconds}");
            
            // Create timeout context and cancellation token source if total timeout is configured
            var (timeoutContext, tokenSource) = ToolCallTimeoutFactory.CreateTimeout(_configuration);
            
            try
            {
                var databases = await _serverDatabase.ListDatabasesAsync(timeoutContext, timeoutSeconds);
                return databases.ToToolResult();
            }
            catch (OperationCanceledException ex) when (timeoutContext != null && timeoutContext.IsTimeoutExceeded)
            {
                // Return timeout error message instead of generic cancellation error
                return $"Error: {timeoutContext.CreateTimeoutExceededMessage()}";
            }
            catch (SqlException ex) when (timeoutContext != null && timeoutContext.IsTimeoutExceeded && SqlExceptionHelper.IsTimeoutError(ex))
            {
                // SQL Server throws SqlException when cancelled - show custom timeout message
                return $"Error: {timeoutContext.CreateTimeoutExceededMessage()}";
            }
            catch (Exception ex)
            {
                // Using the detailed error format for listing databases since it provides a richer UI
                return ex.ToSimpleDatabaseErrorResult();
            }
            finally
            {
                tokenSource?.Dispose();
            }
        }    }
}
