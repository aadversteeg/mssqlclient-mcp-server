using Core.Application.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Core.Infrastructure.McpServer.Extensions;
using System.Text;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class ServerListStoredProceduresTool
    {
        private readonly IServerDatabase _serverDatabase;

        public ServerListStoredProceduresTool(IServerDatabase serverDatabase)
        {
            _serverDatabase = serverDatabase ?? throw new ArgumentNullException(nameof(serverDatabase));
            Console.Error.WriteLine("ServerListStoredProceduresTool constructed with server database service");
        }

        [McpServerTool(Name = "list_stored_procedures_in_database"), Description("List all stored procedures in the specified SQL Server database.")]
        public async Task<string> ListStoredProceduresInDatabase(string databaseName)
        {
            Console.Error.WriteLine($"ListStoredProceduresInDatabase called with database: {databaseName}");
            
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                return "Error: Database name cannot be empty";
            }
            
            try
            {
                // First check if the database exists
                if (!await _serverDatabase.DoesDatabaseExistAsync(databaseName))
                {
                    return $"Error: Database '{databaseName}' does not exist or is not accessible";
                }
                
                // Use the ServerDatabase service to get the stored procedures in the specified database
                var procedures = await _serverDatabase.ListStoredProceduresAsync(databaseName);
                
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
            catch (Exception ex)
            {
                return ex.ToSqlErrorResult($"listing stored procedures in database '{databaseName}'");
            }
        }
    }
}