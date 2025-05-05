using ModelContextProtocol.Server;
using System.ComponentModel;
using Core.Application.Interfaces;
using Core.Infrastructure.McpServer.Extensions;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class MasterListTablesTool
    {
        private readonly IMasterDatabase _masterDatabase;

        public MasterListTablesTool(IMasterDatabase masterDatabase)
        {
            _masterDatabase = masterDatabase ?? throw new ArgumentNullException(nameof(masterDatabase));
            Console.Error.WriteLine($"MasterListTablesTool constructed with master database service");
        }

        [McpServerTool(Name = "list_tables_in_database"), Description("List tables in the specified database (requires master database connection).")]
        public async Task<string> ListTablesInDatabase(string databaseName)
        {
            Console.Error.WriteLine($"ListTablesInDatabase called with databaseName: {databaseName}");

            if (string.IsNullOrWhiteSpace(databaseName))
            {
                return "Error: Database name cannot be empty.";
            }

            try
            {
                var tables = await _masterDatabase.ListTablesAsync(databaseName);
                return tables.ToToolResult(databaseName);
            }
            catch (Exception ex)
            {
                return ex.ToSqlErrorResult("listing tables");
            }
        }
    }
}
