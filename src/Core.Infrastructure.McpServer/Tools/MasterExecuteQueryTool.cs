using Core.Application.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Core.Infrastructure.McpServer.Extensions;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class MasterExecuteQueryTool
    {
        private readonly IMasterDatabase _masterDatabase;

        public MasterExecuteQueryTool(IMasterDatabase masterDatabase)
        {
            _masterDatabase = masterDatabase ?? throw new ArgumentNullException(nameof(masterDatabase));
            Console.Error.WriteLine("MasterExecuteQueryTool constructed with master database service");
        }

        [McpServerTool(Name = "execute_query_in_database"), Description("Execute a SQL query in the specified database (requires master database connection).")]
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
                // Use the MasterDatabase service to execute the query in the specified database
                IAsyncDataReader reader = await _masterDatabase.ExecuteQueryInDatabaseAsync(databaseName, query);
                
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