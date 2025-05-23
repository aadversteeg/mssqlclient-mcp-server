using Core.Application.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Core.Infrastructure.McpServer.Extensions;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class ServerExecuteQueryTool
    {
        private readonly IServerDatabase _serverDatabase;

        public ServerExecuteQueryTool(IServerDatabase serverDatabase)
        {
            _serverDatabase = serverDatabase ?? throw new ArgumentNullException(nameof(serverDatabase));
            Console.Error.WriteLine("ServerExecuteQueryTool constructed with server database service");
        }

        [McpServerTool(Name = "execute_query_in_database"), Description("Execute a SQL query in the specified database (requires server mode).")]
        public async Task<string> ExecuteQueryInDatabase(string databaseName, string query)
        {
            Console.Error.WriteLine($"ExecuteQueryInDatabase called with databaseName: {databaseName}, query: {query}");

            if (string.IsNullOrWhiteSpace(databaseName))
            {
                return "Error: Database name cannot be empty.";
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                return "Error: Query cannot be empty.";
            }

            try
            {
                // Use the ServerDatabase service to execute the query in the specified database
                IAsyncDataReader reader = await _serverDatabase.ExecuteQueryInDatabaseAsync(databaseName, query);
                
                // Format results into a readable table
                return await reader.ToToolResult();
            }
            catch (Exception ex)
            {
                return ex.ToSqlErrorResult("executing query");
            }
        }
    }
}