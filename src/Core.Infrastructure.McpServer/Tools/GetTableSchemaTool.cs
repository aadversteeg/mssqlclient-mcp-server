using Core.Application.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Core.Infrastructure.McpServer.Extensions;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class GetTableSchemaTool
    {
        private readonly IUserDatabase _userDatabase;

        public GetTableSchemaTool(IUserDatabase userDatabase)
        {
            _userDatabase = userDatabase ?? throw new ArgumentNullException(nameof(userDatabase));
            Console.Error.WriteLine("GetTableSchemaTool constructed with user database service");
        }

        [McpServerTool(Name = "get_table_schema"), Description("Get the schema of a table from the connected SQL Server database.")]
        public async Task<string> GetTableSchema(string tableName)
        {
            Console.Error.WriteLine($"GetTableSchema called with tableName: {tableName}");
            
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return "Error: Table name cannot be empty";
            }

            try
            {
                // Get schema information for the table using the user database service
                var tableSchema = await _userDatabase.GetTableSchemaAsync(tableName);
                return tableSchema.ToToolResult();
            }
            catch (Exception ex)
            {
                return ex.ToSqlErrorResult("getting table schema");
            }
        }
    }
}