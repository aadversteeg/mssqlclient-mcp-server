using Core.Application.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Core.Infrastructure.McpServer.Extensions;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class ServerGetTableSchemaTool
    {
        private readonly IServerDatabase _serverDatabase;

        public ServerGetTableSchemaTool(IServerDatabase serverDatabase)
        {
            _serverDatabase = serverDatabase ?? throw new ArgumentNullException(nameof(serverDatabase));
            Console.Error.WriteLine("ServerGetTableSchemaTool constructed with server database service");
        }

        /// <summary>
        /// Get the schema of a table in the specified database (requires server mode).
        /// </summary>
        /// <param name="databaseName">The name of the database containing the table</param>
        /// <param name="tableName">The name of the table to get schema for</param>
        /// <param name="timeoutSeconds">Optional timeout in seconds. If null, uses default timeout.</param>
        /// <returns>A formatted string containing the table schema information</returns>
        [McpServerTool(Name = "get_table_schema_in_database"), Description("Get the schema of a table in the specified database (requires server mode).")]
        public async Task<string> GetTableSchemaInDatabase(string databaseName, string tableName, int? timeoutSeconds = null)
        {
            Console.Error.WriteLine($"GetTableSchemaInDatabase called with databaseName: {databaseName}, tableName: {tableName}, timeoutSeconds: {timeoutSeconds}");

            if (string.IsNullOrWhiteSpace(databaseName))
            {
                return "Error: Database name cannot be empty.";
            }

            if (string.IsNullOrWhiteSpace(tableName))
            {
                return "Error: Table name cannot be empty.";
            }

            try
            {
                // Get schema information for the table using the server database service
                var tableSchema = await _serverDatabase.GetTableSchemaAsync(databaseName, tableName, timeoutSeconds);
                return tableSchema.ToToolResult();
            }
            catch (Exception ex)
            {
                return ex.ToSqlErrorResult("getting table schema");
            }
        }
    }
}