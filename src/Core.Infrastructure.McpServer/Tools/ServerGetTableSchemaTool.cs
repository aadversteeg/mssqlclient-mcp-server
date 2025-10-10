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
    public class ServerGetTableSchemaTool
    {
        private readonly IServerDatabase _serverDatabase;
        private readonly DatabaseConfiguration _configuration;

        public ServerGetTableSchemaTool(IServerDatabase serverDatabase, IOptions<DatabaseConfiguration> configuration)
        {
            _serverDatabase = serverDatabase ?? throw new ArgumentNullException(nameof(serverDatabase));
            _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
            Console.Error.WriteLine("ServerGetTableSchemaTool constructed with server database service");
        }

        /// <summary>
        /// Get the schema of a table in the specified database (requires server mode).
        /// </summary>
        /// <param name="databaseName">The name of the database containing the table</param>
        /// <param name="tableName">The name of the table to get schema for</param>
        /// <param name="timeoutSeconds">Optional timeout in seconds. If null, uses default timeout.</param>
        /// <returns>A formatted string containing the table schema information</returns>
        [McpServerTool(Name = "get_table_schema_in_database"), Description("Get the schema of a table in the specified database (requires server mode).")]
        public async Task<string> GetTableSchemaInDatabase(string databaseName, string tableName, int? timeoutSeconds = null)
        {
            Console.Error.WriteLine($"GetTableSchemaInDatabase called with databaseName: {databaseName}, tableName: {tableName}, timeoutSeconds: {timeoutSeconds}");

            if (string.IsNullOrWhiteSpace(databaseName))
            {
                return "Error: Database name cannot be empty.";
            }

            if (string.IsNullOrWhiteSpace(tableName))
            {
                return "Error: Table name cannot be empty.";
            }

            // Create timeout context and cancellation token source if total timeout is configured
            var (timeoutContext, tokenSource) = ToolCallTimeoutFactory.CreateTimeout(_configuration);

            try
            {
                // Get schema information for the table using the server database service
                Core.Application.Models.TableSchemaInfo tableSchema = await _serverDatabase.GetTableSchemaAsync(databaseName, tableName, timeoutContext, timeoutSeconds);
                return tableSchema.ToToolResult();
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
                return ex.ToSqlErrorResult("getting table schema");
            }
            finally
            {
                tokenSource?.Dispose();
            }
        }    }
}
