using Core.Application.Interfaces;
using Core.Application.Models;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Core.Infrastructure.McpServer.Extensions;
using System.Text;
using Microsoft.Data.SqlClient;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class ListStoredProceduresTool
    {
        private readonly IDatabaseContext _databaseContext;
        private readonly DatabaseConfiguration _configuration;

        public ListStoredProceduresTool(IDatabaseContext databaseContext, IOptions<DatabaseConfiguration> configuration)
        {
            _databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
            _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
            Console.Error.WriteLine("ListStoredProceduresTool constructed with database context service");
        }

        /// <summary>
        /// Lists all stored procedures in the connected SQL Server database
        /// </summary>
        /// <param name="timeoutSeconds">The timeout in seconds for the operation (optional)</param>
        /// <returns>Formatted string with stored procedures information</returns>
        [McpServerTool(Name = "list_stored_procedures"), Description("List all stored procedures in the connected SQL Server database.")]
        public async Task<string> ListStoredProcedures(int? timeoutSeconds = null)
        {
            Console.Error.WriteLine($"ListStoredProcedures called with timeoutSeconds: {timeoutSeconds}");
            
            // Create timeout context
            var (timeoutContext, tokenSource) = ToolCallTimeoutFactory.CreateTimeout(_configuration);
            
            try
            {
                // Use the DatabaseContext service to get the stored procedures
                var procedures = await _databaseContext.ListStoredProceduresAsync(timeoutContext, timeoutSeconds);
                
                // No stored procedures found
                if (!procedures.Any())
                {
                    return "No stored procedures found in the database.";
                }
                
                // Format results into a readable table
                var sb = new StringBuilder();
                sb.AppendLine("Available Stored Procedures:");
                sb.AppendLine();
                
                // Column headers
                sb.AppendLine("Schema   | Procedure Name                  | Parameters | Last Execution    | Execution Count | Created Date");
                sb.AppendLine("-------- | ------------------------------- | ---------- | ----------------- | --------------- | -------------------");
                
                // Rows
                foreach (var proc in procedures)
                {
                    var schemaName = proc.SchemaName.PadRight(8);
                    var procName = proc.Name.PadRight(31);
                    var paramCount = proc.Parameters.Count.ToString().PadRight(10);
                    var lastExecution = proc.LastExecutionTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A".PadRight(17);
                    var execCount = proc.ExecutionCount?.ToString() ?? "N/A";
                    var createDate = proc.CreateDate.ToString("yyyy-MM-dd HH:mm:ss");
                    
                    sb.AppendLine($"{schemaName} | {procName} | {paramCount} | {lastExecution} | {execCount.PadRight(15)} | {createDate}");
                }
                
                return sb.ToString();
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
                return ex.ToSqlErrorResult("listing stored procedures");
            }
            finally
            {
                tokenSource?.Dispose();
            }
        }    }
}