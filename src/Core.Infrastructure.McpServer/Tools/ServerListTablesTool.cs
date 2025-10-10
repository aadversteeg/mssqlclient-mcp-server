using ModelContextProtocol.Server;
using System.ComponentModel;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Infrastructure.McpServer.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Data.SqlClient;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class ServerListTablesTool
    {
        private readonly IServerDatabase _serverDatabase;
        private readonly DatabaseConfiguration _configuration;

        public ServerListTablesTool(IServerDatabase serverDatabase, IOptions<DatabaseConfiguration> configuration)
        {
            _serverDatabase = serverDatabase ?? throw new ArgumentNullException(nameof(serverDatabase));
            _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
            Console.Error.WriteLine($"ServerListTablesTool constructed with server database service");
        }

        /// <summary>
        /// Lists tables in the specified database (requires server mode).
        /// </summary>
        /// <param name="databaseName">Name of the database to list tables from</param>
        /// <param name="timeoutSeconds">Optional timeout in seconds for the operation</param>
        /// <returns>Table information in markdown format</returns>
        [McpServerTool(Name = "list_tables_in_database"), Description("List tables in the specified database (requires server mode).")]
        public async Task<string> ListTablesInDatabase(string databaseName, int? timeoutSeconds = null)
        {
            Console.Error.WriteLine($"ListTablesInDatabase called with databaseName: {databaseName}, timeoutSeconds: {timeoutSeconds}");

            if (string.IsNullOrWhiteSpace(databaseName))
            {
                return "Error: Database name cannot be empty.";
            }

            // Create timeout context and cancellation token source if total timeout is configured
            var (timeoutContext, tokenSource) = ToolCallTimeoutFactory.CreateTimeout(_configuration);

            try
            {
                // Use server database service with timeout context
                var tables = await _serverDatabase.ListTablesAsync(databaseName, timeoutContext, timeoutSeconds);
                    
                return tables.ToToolResult(databaseName);
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
                return ex.ToSqlErrorResult("listing tables");
            }
            finally
            {
                tokenSource?.Dispose();
            }
        }    }
}