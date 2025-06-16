using Core.Application.Interfaces;
using Core.Application.Models;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Core.Infrastructure.McpServer.Extensions;
using Microsoft.Extensions.Options;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class GetTableSchemaTool
    {
        private readonly IDatabaseContext _databaseContext;
        private readonly DatabaseConfiguration _configuration;

        public GetTableSchemaTool(IDatabaseContext databaseContext, IOptions<DatabaseConfiguration> configuration)
        {
            _databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
            _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
            Console.Error.WriteLine("GetTableSchemaTool constructed with database context service");
        }

        /// <summary>
        /// Gets the schema of a table from the connected SQL Server database
        /// </summary>
        /// <param name="tableName">The name of the table to get schema for</param>
        /// <param name="timeoutSeconds">The timeout in seconds for the operation (optional)</param>
        /// <returns>Formatted string with table schema information</returns>
        [McpServerTool(Name = "get_table_schema"), Description("Get the schema of a table from the connected SQL Server database.")]
        public async Task<string> GetTableSchema(string tableName, int? timeoutSeconds = null)
        {
            Console.Error.WriteLine($"GetTableSchema called with tableName: {tableName}, timeoutSeconds: {timeoutSeconds}");
            
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return "Error: Table name cannot be empty";
            }

            // Create timeout context and cancellation token source if total timeout is configured
            var (timeoutContext, tokenSource) = ToolCallTimeoutFactory.CreateTimeout(_configuration);

            try
            {
                // Use timeout context if available, otherwise fall back to legacy behavior
                var tableSchema = timeoutContext != null
                    ? await _databaseContext.GetTableSchemaAsync(tableName, timeoutContext, timeoutSeconds)
                    : await _databaseContext.GetTableSchemaAsync(tableName, timeoutSeconds);
                    
                return tableSchema.ToToolResult();
            }
            catch (OperationCanceledException ex) when (timeoutContext != null && timeoutContext.IsTimeoutExceeded)
            {
                // Return timeout error message instead of generic cancellation error
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
        }
    }
}