using Core.Application.Interfaces;
using Core.Application.Models;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Core.Infrastructure.McpServer.Extensions;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Microsoft.Data.SqlClient;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class ServerExecuteStoredProcedureTool
    {
        private readonly IServerDatabase _serverDatabase;
        private readonly DatabaseConfiguration _configuration;

        public ServerExecuteStoredProcedureTool(IServerDatabase serverDatabase, IOptions<DatabaseConfiguration> configuration)
        {
            _serverDatabase = serverDatabase ?? throw new ArgumentNullException(nameof(serverDatabase));
            _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
            Console.Error.WriteLine("ServerExecuteStoredProcedureTool constructed with server database service");
        }

        [McpServerTool(Name = "execute_stored_procedure_in_database"), Description("Execute a stored procedure in the specified database (requires server mode).")]
        public async Task<string> ExecuteStoredProcedureInDatabase(
            [Description("The name of the database to execute the stored procedure in")]
            string databaseName, 
            [Description("The name of the stored procedure to execute")]
            string procedureName, 
            [Description("JSON object containing the parameters for the stored procedure")]
            string parameters,
            [Description("Optional timeout in seconds. If not specified, uses the default timeout")]
            int? timeoutSeconds = null)
        {
            Console.Error.WriteLine($"ExecuteStoredProcedureInDatabase called with databaseName: {databaseName}, stored procedure: {procedureName}");

            if (string.IsNullOrWhiteSpace(databaseName))
            {
                return "Error: Database name cannot be empty.";
            }

            if (string.IsNullOrWhiteSpace(procedureName))
            {
                return "Error: Procedure name cannot be empty.";
            }

            // Create timeout context and cancellation token source if total timeout is configured
            var (timeoutContext, tokenSource) = ToolCallTimeoutFactory.CreateTimeout(_configuration);

            try
            {
                // Parse the parameters from JSON
                Dictionary<string, object?> paramDict;
                try
                {
                    paramDict = !string.IsNullOrWhiteSpace(parameters) 
                        ? JsonSerializer.Deserialize<Dictionary<string, object?>>(parameters, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                        : new Dictionary<string, object?>();
                    
                    if (paramDict == null)
                    {
                        paramDict = new Dictionary<string, object?>();
                    }
                }
                catch (JsonException ex)
                {
                    return $"Error parsing parameters: {ex.Message}. Parameters must be a valid JSON object with parameter names as keys.";
                }

                // Use server database service with timeout context
                IAsyncDataReader reader = await _serverDatabase.ExecuteStoredProcedureAsync(databaseName, procedureName, paramDict, timeoutContext, timeoutSeconds);
                
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
                return ex.ToSqlErrorResult("executing stored procedure");
            }
            finally
            {
                tokenSource?.Dispose();
            }
        }
    }
}