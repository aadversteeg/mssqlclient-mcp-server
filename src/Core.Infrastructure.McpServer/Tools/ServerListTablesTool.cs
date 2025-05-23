using ModelContextProtocol.Server;
using System.ComponentModel;
using Core.Application.Interfaces;
using Core.Infrastructure.McpServer.Extensions;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class ServerListTablesTool
    {
        private readonly IServerDatabase _serverDatabase;

        public ServerListTablesTool(IServerDatabase serverDatabase)
        {
            _serverDatabase = serverDatabase ?? throw new ArgumentNullException(nameof(serverDatabase));
            Console.Error.WriteLine($"ServerListTablesTool constructed with server database service");
        }

        [McpServerTool(Name = "list_tables_in_database"), Description("List tables in the specified database (requires server mode).")]
        public async Task<string> ListTablesInDatabase(string databaseName)
        {
            Console.Error.WriteLine($"ListTablesInDatabase called with databaseName: {databaseName}");

            if (string.IsNullOrWhiteSpace(databaseName))
            {
                return "Error: Database name cannot be empty.";
            }

            try
            {
                var tables = await _serverDatabase.ListTablesAsync(databaseName);
                return tables.ToToolResult(databaseName);
            }
            catch (Exception ex)
            {
                return ex.ToSqlErrorResult("listing tables");
            }
        }
    }
}
