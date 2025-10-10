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
    public class ExecuteStoredProcedureTool
    {
        private readonly IDatabaseContext _databaseContext;
        private readonly DatabaseConfiguration _configuration;

        public ExecuteStoredProcedureTool(IDatabaseContext databaseContext, IOptions<DatabaseConfiguration> configuration)
        {
            _databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
            _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
            Console.Error.WriteLine("ExecuteStoredProcedureTool constructed with database context service");
        }

        [McpServerTool(Name = "execute_stored_procedure"), 
         Description(@"Execute a stored procedure on the connected SQL Server database and wait for results. Best for procedures that complete quickly.

Parameters should be provided as a JSON object with parameter names as keys.
Both '@ParameterName' and 'ParameterName' formats are accepted.

Examples:
- Simple parameters: {""CustomerID"": 123, ""OrderDate"": ""2024-01-01""}
- With @ prefix: {""@CustomerID"": 123, ""@OrderDate"": ""2024-01-01""}
- Mixed types: {""ID"": 123, ""Name"": ""Test"", ""IsActive"": true, ""Price"": 99.99}
- Null values: {""CustomerID"": 123, ""Notes"": null}

The tool will automatically convert JSON values to the appropriate SQL types based on the stored procedure's parameter definitions.
Use 'get_stored_procedure_parameters' tool first to see what parameters are expected.")]
        public async Task<string> ExecuteStoredProcedure(
            [Description("The name of the stored procedure to execute")]
            string procedureName, 
            [Description("JSON object containing the parameters for the stored procedure")]
            string parameters,
            [Description("Optional timeout in seconds. If not specified, uses the default timeout")]
            int? timeoutSeconds = null)
        {
            Console.Error.WriteLine($"ExecuteStoredProcedure called with stored procedure: {procedureName}");
            
            if (string.IsNullOrWhiteSpace(procedureName))
            {
                return "Error: Procedure name cannot be empty";
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
                
                var reader = await _databaseContext.ExecuteStoredProcedureAsync(procedureName, paramDict, timeoutContext, timeoutSeconds);
                
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