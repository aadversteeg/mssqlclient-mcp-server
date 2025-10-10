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
    public class ListTablesTool
    {
        private readonly IDatabaseContext _databaseContext;
        private readonly DatabaseConfiguration _configuration;

        public ListTablesTool(IDatabaseContext databaseContext, IOptions<DatabaseConfiguration> configuration)
        {
            _databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
            _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
            Console.Error.WriteLine("ListTablesTool constructed with database context service");
        }

        /// <summary>
        /// Lists all tables in the connected SQL Server database.
        /// </summary>
        /// <param name="timeoutSeconds">Optional timeout in seconds for the operation</param>
        /// <returns>Table information in markdown format</returns>
        [McpServerTool(Name = "list_tables"), Description("List all tables in the connected SQL Server database.")]
        public async Task<string> ListTables(int? timeoutSeconds = null)
        {
            Console.Error.WriteLine($"ListTables called with timeoutSeconds: {timeoutSeconds}");

            // Create timeout context and cancellation token source if total timeout is configured
            var (timeoutContext, tokenSource) = ToolCallTimeoutFactory.CreateTimeout(_configuration);

            try
            {
                var tables = await _databaseContext.ListTablesAsync(timeoutContext, timeoutSeconds);
                    
                return tables.ToToolResult();
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