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

        [McpServerTool(Name = "get_table_schema_in_database"), Description("Get the schema of a table in the specified database (requires server mode).")]
        public async Task<string> GetTableSchemaInDatabase(string databaseName, string tableName)
        {
            Console.Error.WriteLine($"GetTableSchemaInDatabase called with databaseName: {databaseName}, tableName: {tableName}");

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
                var tableSchema = await _serverDatabase.GetTableSchemaAsync(databaseName, tableName);
                return tableSchema.ToToolResult();
            }
            catch (Exception ex)
            {
                return ex.ToSqlErrorResult("getting table schema");
            }
        }
    }
}