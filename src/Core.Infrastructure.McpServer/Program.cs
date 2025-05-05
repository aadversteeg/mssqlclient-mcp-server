using Core.Application.Interfaces;
using Core.Infrastructure.McpServer.Configuration;
using Core.Infrastructure.McpServer.Tools;
using Core.Infrastructure.SqlClient;
using Core.Infrastructure.SqlClient.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Core.Infrastructure.McpServer
{
    internal class Program
    {
        /// <summary>
        /// Gets the version from the assembly's informational version attribute,
        /// which is set from the Version property in the project file.
        /// </summary>
        /// <returns>The version string, or "0.0.0" if not available</returns>
        private static string GetServerVersion()
        {
            return Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion ?? "0.0.0";
        }
        
        /// <summary>
        /// Determines if the database connection is to the master database by examining the connection string
        /// without establishing an actual database connection
        /// </summary>
        /// <param name="connectionString">SQL Server connection string</param>
        /// <returns>True if the database in the connection string is "master", false otherwise</returns>
        public static bool IsMasterDb(string connectionString)
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(connectionString);
                string databaseName = builder.InitialCatalog;

                if (string.IsNullOrEmpty(databaseName) && connectionString.Contains("Database=", StringComparison.OrdinalIgnoreCase))
                {
                    // Parse the Database parameter directly if SqlConnectionStringBuilder didn't work
                    var dbParamStart = connectionString.IndexOf("Database=", StringComparison.OrdinalIgnoreCase);
                    if (dbParamStart >= 0)
                    {
                        dbParamStart += "Database=".Length;
                        var dbParamEnd = connectionString.IndexOf(';', dbParamStart);
                        if (dbParamEnd < 0)
                            dbParamEnd = connectionString.Length;

                        databaseName = connectionString.Substring(dbParamStart, dbParamEnd - dbParamStart);
                    }
                }

                // Also check for Initial Catalog which is an alternative to Database
                if (string.IsNullOrEmpty(databaseName) && connectionString.Contains("Initial Catalog=", StringComparison.OrdinalIgnoreCase))
                {
                    var dbParamStart = connectionString.IndexOf("Initial Catalog=", StringComparison.OrdinalIgnoreCase);
                    if (dbParamStart >= 0)
                    {
                        dbParamStart += "Initial Catalog=".Length;
                        var dbParamEnd = connectionString.IndexOf(';', dbParamStart);
                        if (dbParamEnd < 0)
                            dbParamEnd = connectionString.Length;

                        databaseName = connectionString.Substring(dbParamStart, dbParamEnd - dbParamStart);
                    }
                }

                Console.Error.WriteLine($"Database name from connection string: {databaseName}");
                return string.Equals(databaseName, "master", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error checking database name in connection string: {ex.Message}");
                return false; // Default to false if there's an error
            }
        }
        
        static async Task Main(string[] args)
        {
            Console.Error.WriteLine("Starting MCP MSSQLClient Server...");
            var builder = Host.CreateApplicationBuilder(args);

            // Add appsettings.json configuration, use full path in case working folder is different
            string? basePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            basePath ??= Directory.GetCurrentDirectory();

            builder.Configuration.AddJsonFile(
                Path.Combine(basePath, "appsettings.json"), 
                optional: true, 
                reloadOnChange: true);

            builder.Configuration.AddJsonFile(
                Path.Combine(basePath, $"appsettings.{builder.Environment.EnvironmentName}.json"), 
                optional: true, 
                reloadOnChange: true);

            builder.Configuration.AddUserSecrets(
                Assembly.GetExecutingAssembly(),
                optional: true,
                reloadOnChange: true);

            builder.Configuration.AddEnvironmentVariables();

            // Configure logging
            builder.Logging.AddConsole(consoleLogOptions =>
            {
                // Configure all logs to go to stderr
                consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
            });
            
            // Get connection string from config 
            string? connectionString = builder.Configuration.GetValue<string>("MSSQL_CONNECTIONSTRING");
            if(connectionString == null)
            {
                Console.Error.WriteLine("MSSQL_CONNECTIONSTRING is not set in appsettings.json or environment variables.");
                return;
            }

            // Check if the connection string specifies the master database
            bool isConnectedToMaster = IsMasterDb(connectionString);
            Console.Error.WriteLine($"Using master database mode: {isConnectedToMaster}");

            // Register the database configuration
            var dbConfig = new DatabaseConfiguration { ConnectionString = connectionString };
            builder.Services.AddSingleton(dbConfig);

            // Register our database services
            
            // First register the core database service that both user and master services will use
            builder.Services.AddSingleton<IDatabaseService>(provider => new DatabaseService(connectionString));
            
            if (isConnectedToMaster)
            {
                // When connected to master, we need both user and master database services
                builder.Services.AddSingleton<IUserDatabase>(provider => 
                    new UserDatabaseService(provider.GetRequiredService<IDatabaseService>()));
                
                builder.Services.AddSingleton<IMasterDatabase>(provider => 
                    new MasterDatabaseService(provider.GetRequiredService<IDatabaseService>()));
            }
            else
            {
                // When connected to user database, we only need user database service
                builder.Services.AddSingleton<IUserDatabase>(provider => 
                    new UserDatabaseService(provider.GetRequiredService<IDatabaseService>()));
            }

            // Register MCP server
            var mcpServerBuilder = builder.Services
                .AddMcpServer(options =>
                {
                    options.ServerInfo = new()
                    {
                        Name = "MSSQLClient",
                        Version = GetServerVersion()
                    };
                })
                .WithStdioServerTransport();

            Console.Error.WriteLine("Registering MCP tools...");

            if (isConnectedToMaster)
            {
                // Master database mode tools
                Console.Error.WriteLine("Registering master database tools...");
                mcpServerBuilder.WithTools<MasterListTablesTool>();
                Console.Error.WriteLine("Registered MasterListTablesTool");
                mcpServerBuilder.WithTools<MasterListDatabasesTool>();
                Console.Error.WriteLine("Registered MasterListDatabasesTool");
                mcpServerBuilder.WithTools<MasterExecuteQueryTool>();
                Console.Error.WriteLine("Registered MasterExecuteQueryTool");
                mcpServerBuilder.WithTools<MasterGetTableSchemaTool>();
                Console.Error.WriteLine("Registered MasterGetTableSchemaTool");
            }
            else
            {
                // User database mode tools
                Console.Error.WriteLine("Registering user database tools...");
                mcpServerBuilder.WithTools<ListTablesTool>();
                Console.Error.WriteLine("Registered ListTablesTool");
                mcpServerBuilder.WithTools<ExecuteQueryTool>();
                Console.Error.WriteLine("Registered ExecuteQueryTool");
                mcpServerBuilder.WithTools<GetTableSchemaTool>();
                Console.Error.WriteLine("Registered GetTableSchemaTool");
            }
            
            Console.Error.WriteLine("All tools registered. Building MCP server..."); 

            await builder.Build().RunAsync();
        }
    }

}