using System.Diagnostics;
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
    public class ExecuteQueryTool
    {
        private readonly IDatabaseContext _databaseContext;
        private readonly DatabaseConfiguration _configuration;

        public ExecuteQueryTool(IDatabaseContext databaseContext, IOptions<DatabaseConfiguration> configuration)
        {
            _databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
            _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
            Console.Error.WriteLine("ExecuteQueryTool constructed with database context service");
        }

        [McpServerTool(Name = "execute_query"), Description("Execute a SQL query on the connected SQL Server database and wait for results. Best for queries that complete quickly. Cell output is limited to {MaxCellOutputLength} characters per cell (0 = no limit).")]
        public async Task<string> ExecuteQuery(
            [Description("The SQL query to execute")]
            string query,
            [Description("Optional timeout in seconds. If not specified, uses the default timeout")]
            int? timeoutSeconds = null,
            [Description("Include per-table IO statistics (logical reads, physical reads, read-ahead reads). Default is false")]
            bool includeIoStats = false,
            [Description("Include the actual XML execution plan. Default is false")]
            bool includeExecutionPlan = false,
            [Description("Maximum number of characters to display per cell in the output. Values longer than this are truncated with '...'. Set to 0 to disable truncation. If not specified, uses the server default.")]
            int? maxCellOutputLength = null)
        {
            Console.Error.WriteLine($"ExecuteQuery called with query: {query}");
            
            if (string.IsNullOrWhiteSpace(query))
            {
                return "Error: Query cannot be empty";
            }

            // Create timeout context and cancellation token source if total timeout is configured
            var (timeoutContext, tokenSource) = ToolCallTimeoutFactory.CreateTimeout(_configuration);
            
            try
            {
                var statisticsOptions = new QueryStatisticsOptions(includeIoStats, includeExecutionPlan);
                var stopwatch = Stopwatch.StartNew();
                var reader = await _databaseContext.ExecuteQueryAsync(query, timeoutContext, timeoutSeconds, statisticsOptions);

                // Format results into a readable table
                return await reader.ToToolResult(stopwatch, maxCellOutputLength ?? _configuration.MaxCellOutputLength);
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