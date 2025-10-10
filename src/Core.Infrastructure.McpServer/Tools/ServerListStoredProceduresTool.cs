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
    public class ServerListStoredProceduresTool
    {
        private readonly IServerDatabase _serverDatabase;
        private readonly DatabaseConfiguration _configuration;

        public ServerListStoredProceduresTool(IServerDatabase serverDatabase, IOptions<DatabaseConfiguration> configuration)
        {
            _serverDatabase = serverDatabase ?? throw new ArgumentNullException(nameof(serverDatabase));
            _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
            Console.Error.WriteLine("ServerListStoredProceduresTool constructed with server database service");
        }

        /// <summary>
        /// List all stored procedures in the specified SQL Server database.
        /// </summary>
        /// <param name="databaseName">The name of the database to list stored procedures from</param>
        /// <param name="timeoutSeconds">Optional timeout in seconds. If null, uses default timeout.</param>
        /// <returns>A formatted string containing the list of stored procedures</returns>
        [McpServerTool(Name = "list_stored_procedures_in_database"), Description("List all stored procedures in the specified SQL Server database.")]
        public async Task<string> ListStoredProceduresInDatabase(string databaseName, int? timeoutSeconds = null)
        {
            Console.Error.WriteLine($"ListStoredProceduresInDatabase called with database: {databaseName}, timeoutSeconds: {timeoutSeconds}");
            
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                return "Error: Database name cannot be empty";
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
                
                // Use the ServerDatabase service to get the stored procedures in the specified database
                IEnumerable<StoredProcedureInfo> procedures = await _serverDatabase.ListStoredProceduresAsync(databaseName, timeoutContext, timeoutSeconds);
                
                // No stored procedures found
                if (!procedures.Any())
                {
                    return $"No stored procedures found in the database '{databaseName}'.";
                }
                
                // Format results into a readable table
                var sb = new StringBuilder();
                sb.AppendLine($"Available Stored Procedures in '{databaseName}':");
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
                return ex.ToSqlErrorResult($"listing stored procedures in database '{databaseName}'");
            }
            finally
            {
                tokenSource?.Dispose();
            }
        }    }
}