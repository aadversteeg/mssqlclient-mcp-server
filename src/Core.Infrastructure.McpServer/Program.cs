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
        /// Determines if the database connection is to the master database
        /// </summary>
        /// <param name="connectionString">SQL Server connection string</param>
        /// <returns>True if connected to master, false otherwise</returns>
        public static bool IsMasterDb(string connectionString)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand("SELECT DB_NAME()", connection))
                    {
                        string? dbName = (string?)command.ExecuteScalar();
                        return string.Equals(dbName, "master", StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error checking database connection: {ex.Message}");
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

            // Check if we're connected to master database
            bool isConnectedToMaster = IsMasterDb(connectionString);
            Console.Error.WriteLine($"Connected to master database: {isConnectedToMaster}");

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

            if (isConnectedToMaster)
            {
                // Master database mode tools
                mcpServerBuilder.WithTools<MasterListTablesTool>();
                mcpServerBuilder.WithTools<MasterListDatabasesTool>();
                mcpServerBuilder.WithTools<MasterExecuteQueryTool>();
                mcpServerBuilder.WithTools<MasterGetTableSchemaTool>();
            }
            else
            {
                // User database mode tools
                mcpServerBuilder.WithTools<ListTablesTool>();
                mcpServerBuilder.WithTools<ExecuteQueryTool>();
                mcpServerBuilder.WithTools<GetTableSchemaTool>();
            } 

            await builder.Build().RunAsync();
        }
    }

}