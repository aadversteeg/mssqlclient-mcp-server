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

        /// <summary>
        /// Lists tables in the specified database (requires server mode).
        /// </summary>
        /// <param name="databaseName">Name of the database to list tables from</param>
        /// <param name="timeoutSeconds">Optional timeout in seconds for the operation</param>
        /// <returns>Table information in markdown format</returns>
        [McpServerTool(Name = "list_tables_in_database"), Description("List tables in the specified database (requires server mode).")]
        public async Task<string> ListTablesInDatabase(string databaseName, int? timeoutSeconds = null)
        {
            Console.Error.WriteLine($"ListTablesInDatabase called with databaseName: {databaseName}, timeoutSeconds: {timeoutSeconds}");

            if (string.IsNullOrWhiteSpace(databaseName))
            {
                return "Error: Database name cannot be empty.";
            }

            try
            {
                var tables = await _serverDatabase.ListTablesAsync(databaseName, timeoutSeconds);
                return tables.ToToolResult(databaseName);
            }
            catch (Exception ex)
            {
                return ex.ToSqlErrorResult("listing tables");
            }
        }
    }
}
