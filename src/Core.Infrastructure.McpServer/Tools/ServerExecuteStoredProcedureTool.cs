using Core.Application.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Core.Infrastructure.McpServer.Extensions;
using System.Text.Json;
using System.Collections.Generic;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class ServerExecuteStoredProcedureTool
    {
        private readonly IServerDatabase _serverDatabase;

        public ServerExecuteStoredProcedureTool(IServerDatabase serverDatabase)
        {
            _serverDatabase = serverDatabase ?? throw new ArgumentNullException(nameof(serverDatabase));
            Console.Error.WriteLine("ServerExecuteStoredProcedureTool constructed with server database service");
        }

        [McpServerTool(Name = "execute_stored_procedure_in_database"), Description("Execute a stored procedure in the specified database (requires server mode).")]
        public async Task<string> ExecuteStoredProcedureInDatabase(string databaseName, string procedureName, string parameters)
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

                // Use the ServerDatabase service to execute the stored procedure in the specified database
                IAsyncDataReader reader = await _serverDatabase.ExecuteStoredProcedureAsync(databaseName, procedureName, paramDict);
                
                // Format results into a readable table
                return await reader.ToToolResult();
            }
            catch (Exception ex)
            {
                return ex.ToSqlErrorResult("executing stored procedure");
            }
        }
    }
}