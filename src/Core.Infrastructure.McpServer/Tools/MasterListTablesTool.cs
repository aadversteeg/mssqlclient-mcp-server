using Core.Infrastructure.McpServer.Configuration;
using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class MasterListTablesTool
    {
        private readonly string? _connectionString;
        private readonly SqlConnectionContext _connectionContext;
        private readonly ListTablesTool _listTablesTool;

        public MasterListTablesTool(DatabaseConfiguration dbConfig, SqlConnectionContext connectionContext, ListTablesTool listTablesTool)
        {
            _connectionString = dbConfig.ConnectionString;
            _connectionContext = connectionContext ?? throw new ArgumentNullException(nameof(connectionContext));
            _listTablesTool = listTablesTool ?? throw new ArgumentNullException(nameof(listTablesTool));
            Console.Error.WriteLine($"MasterListTablesTool constructed with connection string: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
            Console.Error.WriteLine($"Connected to master: {_connectionContext.IsConnectedToMaster}");
        }

        [McpServerTool(Name = "list_tables_in_database"), Description("List tables in the specified database (requires master database connection).")]
        public string ListTablesInDatabase(string databaseName)
        {
            Console.Error.WriteLine($"ListTablesInDatabase called with databaseName: {databaseName}");
            Console.Error.WriteLine($"Connection string is: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
            
            if (string.IsNullOrEmpty(_connectionString))
            {
                return "Error: No connection string provided. Set the MSSQL_CONNECTIONSTRING environment variable.";
            }

            if (!_connectionContext.IsConnectedToMaster)
            {
                return "Error: This tool can only be used when connected to the master database. Please reconnect to the master database and try again.";
            }

            if (string.IsNullOrWhiteSpace(databaseName))
            {
                return "Error: Database name cannot be empty.";
            }

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"# Tables in Database: {databaseName}");
                sb.AppendLine();

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // First, verify the database exists
                    string checkDbQuery = "SELECT COUNT(*) FROM sys.databases WHERE name = @DatabaseName";
                    using (var checkDbCommand = new SqlCommand(checkDbQuery, connection))
                    {
                        checkDbCommand.Parameters.AddWithValue("@DatabaseName", databaseName);
                        int dbCount = (int)checkDbCommand.ExecuteScalar();
                        
                        if (dbCount == 0)
                        {
                            return $"Error: Database '{databaseName}' does not exist on this server.";
                        }
                    }

                    // Check if the database is accessible
                    string accessibleQuery = "SELECT state_desc FROM sys.databases WHERE name = @DatabaseName";
                    using (var accessibleCommand = new SqlCommand(accessibleQuery, connection))
                    {
                        accessibleCommand.Parameters.AddWithValue("@DatabaseName", databaseName);
                        string? state = (string?)accessibleCommand.ExecuteScalar();
                        
                        if (state != "ONLINE")
                        {
                            return $"Error: Database '{databaseName}' is not online (current state: {state}). Cannot access its tables.";
                        }
                    }

                    // Create a new connection string to the specific database
                    SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(_connectionString);
                    string originalDatabase = builder.InitialCatalog; // Save the original database name
                    builder.InitialCatalog = databaseName;
                    string databaseConnectionString = builder.ConnectionString;

                    // Create a temporary DatabaseConfiguration to pass to ListTablesTool
                    DatabaseConfiguration tempConfig = new DatabaseConfiguration
                    {
                        ConnectionString = databaseConnectionString
                    };

                    // Create a new instance of ListTablesTool with the database-specific connection string
                    ListTablesTool dbSpecificListTablesTool = new ListTablesTool(tempConfig);
                    
                    // Get the table listing from ListTablesTool
                    string tableListingResult = dbSpecificListTablesTool.ListTables();
                    
                    // Add the result to our output
                    sb.AppendLine(tableListingResult);
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Error listing tables: {ex.Message}\n\n{ex.StackTrace}";
            }
        }
    }
}
