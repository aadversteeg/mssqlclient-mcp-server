using Core.Application.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Core.Infrastructure.McpServer.Extensions;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class ExecuteQueryTool
    {
        private readonly IUserDatabase _userDatabase;

        public ExecuteQueryTool(IUserDatabase userDatabase)
        {
            _userDatabase = userDatabase ?? throw new ArgumentNullException(nameof(userDatabase));
            Console.Error.WriteLine("ExecuteQueryTool constructed with user database service");
        }

        [McpServerTool(Name = "execute_query"), Description("Execute a SQL query on the connected SQL Server database.")]
        public async Task<string> ExecuteQuery(string query)
        {
            Console.Error.WriteLine($"ExecuteQuery called with query: {query}");
            
            if (string.IsNullOrWhiteSpace(query))
            {
                return "Error: Query cannot be empty";
            }

            try
            {
                // Use the UserDatabase service to execute the query
                IAsyncDataReader reader = await _userDatabase.ExecuteQueryAsync(query);
                
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