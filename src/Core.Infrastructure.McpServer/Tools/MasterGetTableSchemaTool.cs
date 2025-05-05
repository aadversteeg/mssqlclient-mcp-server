using Core.Application.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Core.Infrastructure.McpServer.Extensions;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class MasterGetTableSchemaTool
    {
        private readonly IMasterDatabase _masterDatabase;

        public MasterGetTableSchemaTool(IMasterDatabase masterDatabase)
        {
            _masterDatabase = masterDatabase ?? throw new ArgumentNullException(nameof(masterDatabase));
            Console.Error.WriteLine("MasterGetTableSchemaTool constructed with master database service");
        }

        [McpServerTool(Name = "get_table_schema_in_database"), Description("Get the schema of a table in the specified database (requires master database connection).")]
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
                // Get schema information for the table using the master database service
                var tableSchema = await _masterDatabase.GetTableSchemaAsync(databaseName, tableName);
                return tableSchema.ToToolResult();
            }
            catch (Exception ex)
            {
                return ex.ToSqlErrorResult("getting table schema");
            }
        }
    }
}