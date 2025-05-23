using Core.Application.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Core.Infrastructure.McpServer.Extensions;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class GetTableSchemaTool
    {
        private readonly IDatabaseContext _databaseContext;

        public GetTableSchemaTool(IDatabaseContext databaseContext)
        {
            _databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
            Console.Error.WriteLine("GetTableSchemaTool constructed with database context service");
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
                // Get schema information for the table using the database context service
                var tableSchema = await _databaseContext.GetTableSchemaAsync(tableName);
                return tableSchema.ToToolResult();
            }
            catch (Exception ex)
            {
                return ex.ToSqlErrorResult("getting table schema");
            }
        }
    }
}