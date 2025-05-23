using ModelContextProtocol.Server;
using System.ComponentModel;
using Core.Application.Interfaces;
using Core.Infrastructure.McpServer.Extensions;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class ListTablesTool
    {
        private readonly IDatabaseContext _databaseContext;

        public ListTablesTool(IDatabaseContext databaseContext)
        {
            _databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
            Console.Error.WriteLine("ListTablesTool constructed with database context service");
        }

        [McpServerTool(Name = "list_tables"), Description("List all tables in the connected SQL Server database.")]
        public async Task<string> ListTables()
        {
            Console.Error.WriteLine("ListTables called");

            try
            {
                var tables = await _databaseContext.ListTablesAsync();
                return tables.ToToolResult();
            }
            catch (Exception ex)
            {
                return ex.ToSqlErrorResult("listing tables");
            }
        }
    }
}