using Core.Application.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Core.Infrastructure.McpServer.Extensions;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class GetStoredProcedureDefinitionTool
    {
        private readonly IDatabaseContext _databaseContext;

        public GetStoredProcedureDefinitionTool(IDatabaseContext databaseContext)
        {
            _databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
            Console.Error.WriteLine("GetStoredProcedureDefinitionTool constructed with database context service");
        }

        [McpServerTool(Name = "get_stored_procedure_definition"), Description("Get the definition of a stored procedure in the connected SQL Server database.")]
        public async Task<string> GetStoredProcedureDefinition(string procedureName)
        {
            Console.Error.WriteLine($"GetStoredProcedureDefinition called with procedure: {procedureName}");
            
            if (string.IsNullOrWhiteSpace(procedureName))
            {
                return "Error: Procedure name cannot be empty";
            }
            
            try
            {
                // Use the DatabaseContext service to get the stored procedure definition
                string definition = await _databaseContext.GetStoredProcedureDefinitionAsync(procedureName);
                
                // If the definition is empty, return a helpful message
                if (string.IsNullOrWhiteSpace(definition))
                {
                    return $"No definition found for stored procedure '{procedureName}'. The procedure might not exist or you don't have permission to view its definition.";
                }
                
                // Return the definition with a header
                return $"Definition for stored procedure '{procedureName}':\n\n{definition}";
            }
            catch (Exception ex)
            {
                return ex.ToSqlErrorResult($"getting definition for stored procedure '{procedureName}'");
            }
        }
    }
}