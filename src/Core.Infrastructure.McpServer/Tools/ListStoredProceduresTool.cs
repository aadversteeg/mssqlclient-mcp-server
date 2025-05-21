using Core.Application.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Core.Infrastructure.McpServer.Extensions;
using System.Text;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class ListStoredProceduresTool
    {
        private readonly IDatabaseContext _databaseContext;

        public ListStoredProceduresTool(IDatabaseContext databaseContext)
        {
            _databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
            Console.Error.WriteLine("ListStoredProceduresTool constructed with database context service");
        }

        [McpServerTool(Name = "list_stored_procedures"), Description("List all stored procedures in the connected SQL Server database.")]
        public async Task<string> ListStoredProcedures()
        {
            Console.Error.WriteLine("ListStoredProcedures called");
            
            try
            {
                // Use the DatabaseContext service to get the stored procedures
                var procedures = await _databaseContext.ListStoredProceduresAsync();
                
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
            catch (Exception ex)
            {
                return ex.ToSqlErrorResult("listing stored procedures");
            }
        }
    }
}