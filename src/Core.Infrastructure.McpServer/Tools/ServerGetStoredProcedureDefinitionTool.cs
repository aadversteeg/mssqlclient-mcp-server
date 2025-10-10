using Core.Application.Interfaces;
using Core.Application.Models;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Core.Infrastructure.McpServer.Extensions;
using Microsoft.Data.SqlClient;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class ServerGetStoredProcedureDefinitionTool
    {
        private readonly IServerDatabase _serverDatabase;
        private readonly DatabaseConfiguration _configuration;

        public ServerGetStoredProcedureDefinitionTool(IServerDatabase serverDatabase, IOptions<DatabaseConfiguration> configuration)
        {
            _serverDatabase = serverDatabase ?? throw new ArgumentNullException(nameof(serverDatabase));
            _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
            Console.Error.WriteLine("ServerGetStoredProcedureDefinitionTool constructed with server database service");
        }

        /// <summary>
        /// Get the definition of a stored procedure in a specified SQL Server database.
        /// </summary>
        /// <param name="databaseName">The name of the database containing the stored procedure</param>
        /// <param name="procedureName">The name of the stored procedure to get the definition for</param>
        /// <param name="timeoutSeconds">Optional timeout in seconds. If null, uses default timeout.</param>
        /// <returns>A formatted string containing the stored procedure definition</returns>
        [McpServerTool(Name = "get_stored_procedure_definition_in_database"), Description("Get the definition of a stored procedure in a specified SQL Server database.")]
        public async Task<string> GetStoredProcedureDefinitionInDatabase(string databaseName, string procedureName, int? timeoutSeconds = null)
        {
            Console.Error.WriteLine($"GetStoredProcedureDefinitionInDatabase called with database: {databaseName}, procedure: {procedureName}, timeoutSeconds: {timeoutSeconds}");
            
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                return "Error: Database name cannot be empty";
            }
            
            if (string.IsNullOrWhiteSpace(procedureName))
            {
                return "Error: Procedure name cannot be empty";
            }
            
            // Create timeout context
            var (timeoutContext, tokenSource) = ToolCallTimeoutFactory.CreateTimeout(_configuration);
            
            try
            {
                // First check if the database exists
                bool databaseExists = await _serverDatabase.DoesDatabaseExistAsync(databaseName, timeoutContext, timeoutSeconds);
                
                if (!databaseExists)
                {
                    return $"Error: Database '{databaseName}' does not exist or is not accessible";
                }
                
                // Use the ServerDatabase service to get the stored procedure definition in the specified database
                string definition = await _serverDatabase.GetStoredProcedureDefinitionAsync(databaseName, procedureName, timeoutContext, timeoutSeconds);
                
                // If the definition is empty, return a helpful message
                if (string.IsNullOrWhiteSpace(definition))
                {
                    return $"No definition found for stored procedure '{procedureName}' in database '{databaseName}'. The procedure might not exist or you don't have permission to view its definition.";
                }
                
                // Return the definition with a header
                return $"Definition for stored procedure '{procedureName}' in database '{databaseName}':\n\n{definition}";
            }
            catch (OperationCanceledException ex) when (timeoutContext?.IsTimeoutExceeded == true)
            {
                return $"Error: {timeoutContext.CreateTimeoutExceededMessage()}";
            }
            catch (SqlException ex) when (timeoutContext?.IsTimeoutExceeded == true && SqlExceptionHelper.IsTimeoutError(ex))
            {
                // SQL Server throws SqlException when cancelled - show custom timeout message
                return $"Error: {timeoutContext.CreateTimeoutExceededMessage()}";
            }
            catch (Exception ex)
            {
                return ex.ToSqlErrorResult($"getting definition for stored procedure '{procedureName}' in database '{databaseName}'");
            }
            finally
            {
                tokenSource?.Dispose();
            }
        }    }
}