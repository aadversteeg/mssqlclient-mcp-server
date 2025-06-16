using Core.Application.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Core.Infrastructure.McpServer.Extensions;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class ServerGetStoredProcedureDefinitionTool
    {
        private readonly IServerDatabase _serverDatabase;

        public ServerGetStoredProcedureDefinitionTool(IServerDatabase serverDatabase)
        {
            _serverDatabase = serverDatabase ?? throw new ArgumentNullException(nameof(serverDatabase));
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
            
            try
            {
                // First check if the database exists
                if (!await _serverDatabase.DoesDatabaseExistAsync(databaseName, timeoutSeconds))
                {
                    return $"Error: Database '{databaseName}' does not exist or is not accessible";
                }
                
                // Use the ServerDatabase service to get the stored procedure definition in the specified database
                string definition = await _serverDatabase.GetStoredProcedureDefinitionAsync(databaseName, procedureName, timeoutSeconds);
                
                // If the definition is empty, return a helpful message
                if (string.IsNullOrWhiteSpace(definition))
                {
                    return $"No definition found for stored procedure '{procedureName}' in database '{databaseName}'. The procedure might not exist or you don't have permission to view its definition.";
                }
                
                // Return the definition with a header
                return $"Definition for stored procedure '{procedureName}' in database '{databaseName}':\n\n{definition}";
            }
            catch (Exception ex)
            {
                return ex.ToSqlErrorResult($"getting definition for stored procedure '{procedureName}' in database '{databaseName}'");
            }
        }
    }
}