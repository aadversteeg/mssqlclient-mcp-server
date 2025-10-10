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
    public class GetStoredProcedureDefinitionTool
    {
        private readonly IDatabaseContext _databaseContext;
        private readonly DatabaseConfiguration _configuration;

        public GetStoredProcedureDefinitionTool(IDatabaseContext databaseContext, IOptions<DatabaseConfiguration> configuration)
        {
            _databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
            _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
            Console.Error.WriteLine("GetStoredProcedureDefinitionTool constructed with database context service");
        }

        /// <summary>
        /// Gets the definition of a stored procedure in the connected SQL Server database
        /// </summary>
        /// <param name="procedureName">The name of the stored procedure to get definition for</param>
        /// <param name="timeoutSeconds">The timeout in seconds for the operation (optional)</param>
        /// <returns>Formatted string with stored procedure definition</returns>
        [McpServerTool(Name = "get_stored_procedure_definition"), Description("Get the definition of a stored procedure in the connected SQL Server database.")]
        public async Task<string> GetStoredProcedureDefinition(string procedureName, int? timeoutSeconds = null)
        {
            Console.Error.WriteLine($"GetStoredProcedureDefinition called with procedure: {procedureName}, timeoutSeconds: {timeoutSeconds}");
            
            if (string.IsNullOrWhiteSpace(procedureName))
            {
                return "Error: Procedure name cannot be empty";
            }
            
            // Create timeout context
            var (timeoutContext, tokenSource) = ToolCallTimeoutFactory.CreateTimeout(_configuration);
            
            try
            {
                // Use the DatabaseContext service to get the stored procedure definition
                string definition = await _databaseContext.GetStoredProcedureDefinitionAsync(procedureName, timeoutContext, timeoutSeconds);
                
                // If the definition is empty, return a helpful message
                if (string.IsNullOrWhiteSpace(definition))
                {
                    return $"No definition found for stored procedure '{procedureName}'. The procedure might not exist or you don't have permission to view its definition.";
                }
                
                // Return the definition with a header
                return $"Definition for stored procedure '{procedureName}':\n\n{definition}";
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
                return ex.ToSqlErrorResult($"getting definition for stored procedure '{procedureName}'");
            }
            finally
            {
                tokenSource?.Dispose();
            }
        }
    }
}