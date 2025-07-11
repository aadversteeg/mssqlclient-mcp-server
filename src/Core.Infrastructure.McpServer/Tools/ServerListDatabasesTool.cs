using Core.Application.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Core.Infrastructure.McpServer.Extensions;

namespace Core.Infrastructure.McpServer.Tools
{
    /// <summary>
    /// Tool to list all databases on a SQL Server instance with their properties
    /// </summary>
    [McpServerToolType]
    public class ServerListDatabasesTool
    {
        private readonly IServerDatabase _serverDatabase;

        public ServerListDatabasesTool(IServerDatabase serverDatabase)
        {
            _serverDatabase = serverDatabase ?? throw new ArgumentNullException(nameof(serverDatabase));
        }

        /// <summary>
        /// Gets a list of all databases on the SQL Server instance with their properties
        /// </summary>
        /// <param name="timeoutSeconds">The timeout in seconds for the operation (optional)</param>
        /// <returns>Markdown formatted string with database information</returns>
        [McpServerTool(Name = "list_databases"), Description("List all databases on the SQL Server instance.")]
        public async Task<string> GetDatabases(int? timeoutSeconds = null)
        {
            Console.Error.WriteLine($"GetDatabases called with timeoutSeconds: {timeoutSeconds}");
            
            try
            {
                var databases = await _serverDatabase.ListDatabasesAsync(timeoutSeconds);
                return databases.ToToolResult();
            }
            catch (Exception ex)
            {
                // Using the detailed error format for listing databases since it provides a richer UI
                return ex.ToSimpleDatabaseErrorResult();
            }
        }
    }
}
