using Core.Application.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Core.Infrastructure.McpServer.Extensions;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class ExecuteQueryTool
    {
        private readonly IDatabaseContext _databaseContext;

        public ExecuteQueryTool(IDatabaseContext databaseContext)
        {
            _databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
            Console.Error.WriteLine("ExecuteQueryTool constructed with database context service");
        }

        [McpServerTool(Name = "execute_query"), Description("Execute a SQL query on the connected SQL Server database and wait for results. Best for queries that complete quickly.")]
        public async Task<string> ExecuteQuery(
            [Description("The SQL query to execute")]
            string query,
            [Description("Optional timeout in seconds. If not specified, uses the default timeout")]
            int? timeoutSeconds = null)
        {
            Console.Error.WriteLine($"ExecuteQuery called with query: {query}");
            
            if (string.IsNullOrWhiteSpace(query))
            {
                return "Error: Query cannot be empty";
            }

            try
            {
                // Use the DatabaseContext service to execute the query
                IAsyncDataReader reader = await _databaseContext.ExecuteQueryAsync(query, timeoutSeconds);
                
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