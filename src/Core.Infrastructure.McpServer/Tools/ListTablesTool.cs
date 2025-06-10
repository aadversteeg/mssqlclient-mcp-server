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

        /// <summary>
        /// Lists all tables in the connected SQL Server database.
        /// </summary>
        /// <param name="timeoutSeconds">Optional timeout in seconds for the operation</param>
        /// <returns>Table information in markdown format</returns>
        [McpServerTool(Name = "list_tables"), Description("List all tables in the connected SQL Server database.")]
        public async Task<string> ListTables(int? timeoutSeconds = null)
        {
            Console.Error.WriteLine($"ListTables called with timeoutSeconds: {timeoutSeconds}");

            try
            {
                var tables = await _databaseContext.ListTablesAsync(timeoutSeconds);
                return tables.ToToolResult();
            }
            catch (Exception ex)
            {
                return ex.ToSqlErrorResult("listing tables");
            }
        }
    }
}