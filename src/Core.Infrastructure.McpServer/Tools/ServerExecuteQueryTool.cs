using Core.Application.Interfaces;
using Core.Application.Models;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Core.Infrastructure.McpServer.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Data.SqlClient;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class ServerExecuteQueryTool
    {
        private readonly IServerDatabase _serverDatabase;
        private readonly DatabaseConfiguration _configuration;

        public ServerExecuteQueryTool(IServerDatabase serverDatabase, IOptions<DatabaseConfiguration> configuration)
        {
            _serverDatabase = serverDatabase ?? throw new ArgumentNullException(nameof(serverDatabase));
            _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
            Console.Error.WriteLine("ServerExecuteQueryTool constructed with server database service");
        }

        [McpServerTool(Name = "execute_query_in_database"), Description("Execute a SQL query in the specified database (requires server mode).")]
        public async Task<string> ExecuteQueryInDatabase(
            [Description("The name of the database to execute the query in")]
            string databaseName, 
            [Description("The SQL query to execute")]
            string query,
            [Description("Optional timeout in seconds. If not specified, uses the default timeout")]
            int? timeoutSeconds = null)
        {
            Console.Error.WriteLine($"ExecuteQueryInDatabase called with databaseName: {databaseName}, query: {query}");

            if (string.IsNullOrWhiteSpace(databaseName))
            {
                return "Error: Database name cannot be empty.";
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                return "Error: Query cannot be empty.";
            }

            // Create timeout context and cancellation token source if total timeout is configured
            var (timeoutContext, tokenSource) = ToolCallTimeoutFactory.CreateTimeout(_configuration);

            try
            {
                // Use timeout context if available, otherwise fall back to legacy behavior
                IAsyncDataReader reader = await _serverDatabase.ExecuteQueryInDatabaseAsync(databaseName, query, timeoutContext, timeoutSeconds);
                
                // Format results into a readable table
                return await reader.ToToolResult();
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
                return ex.ToSqlErrorResult("executing query");
            }
            finally
            {
                tokenSource?.Dispose();
            }
        }
    }
}