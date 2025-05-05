using ModelContextProtocol.Server;
using System.ComponentModel;
using Core.Application.Interfaces;
using Core.Infrastructure.McpServer.Extensions;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class ListTablesTool
    {
        private readonly IUserDatabase _userDatabase;

        public ListTablesTool(IUserDatabase userDatabase)
        {
            _userDatabase = userDatabase ?? throw new ArgumentNullException(nameof(userDatabase));
            Console.Error.WriteLine("ListTablesTool constructed with user database service");
        }

        [McpServerTool(Name = "list_tables"), Description("List all tables in the connected SQL Server database.")]
        public async Task<string> ListTables()
        {
            Console.Error.WriteLine("ListTables called");

            try
            {
                var tables = await _userDatabase.ListTablesAsync();
                return tables.ToToolResult();
            }
            catch (Exception ex)
            {
                return ex.ToSqlErrorResult("listing tables");
            }
        }
    }
}