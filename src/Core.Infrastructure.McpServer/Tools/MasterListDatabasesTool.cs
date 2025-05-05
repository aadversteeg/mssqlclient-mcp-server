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
    public class MasterListDatabasesTool
    {
        private readonly IMasterDatabase _masterDatabase;

        public MasterListDatabasesTool(IMasterDatabase masterDatabase)
        {
            _masterDatabase = masterDatabase ?? throw new ArgumentNullException(nameof(masterDatabase));
        }

        /// <summary>
        /// Gets a list of all databases on the SQL Server instance with their properties
        /// </summary>
        /// <returns>Markdown formatted string with database information</returns>
        [McpServerTool(Name = "list_databases"), Description("List all databases on the SQL Server instance.")]
        public async Task<string> GetDatabases()
        {
            try
            {
                var databases = await _masterDatabase.ListDatabasesAsync();
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
