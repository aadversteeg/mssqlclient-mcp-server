using System;
using System.Collections.Generic;
using System.IO;
using Ave.Testing.ModelContextProtocol.Builder;
using Ave.Testing.ModelContextProtocol.Interfaces;
using Microsoft.Extensions.Logging;

namespace Ave.Testing.ModelContextProtocol.Examples
{
    /// <summary>
    /// Examples demonstrating the McpClientBuilder usage patterns
    /// </summary>
    public static class BuilderUsageExamples
    {
        /// <summary>
        /// Example of creating an MCP client with .NET execution
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <returns>Configured MCP client</returns>
        public static IMcpClient CreateDotNetClient(ILogger logger)
        {
            // Define paths
            string dotnetAssemblyPath = Path.Combine(
                AppContext.BaseDirectory, "mssqlclient-mcp-server.dll");
            
            // Define environment variables
            Dictionary<string, string> environmentVariables = new Dictionary<string, string>
            {
                ["MSSQL_CONNECTIONSTRING"] = "Server=localhost;Database=test;User Id=sa;Password=Password123;TrustServerCertificate=True;"
            };
            
            // Create the client using the builder pattern
            return new McpClientBuilder()
                .WithLogger(logger)
                .WithStdioTransport()
                    .UsingDotNet(dotnetAssemblyPath)
                    .WithEnvironmentVariables(environmentVariables)
                    .WithWorkingDirectory(AppContext.BaseDirectory)
                    .WithArguments("--urls=http://localhost:5000")
                .Build();
        }

        /// <summary>
        /// Example of creating an MCP client with Docker execution
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <returns>Configured MCP client</returns>
        public static IMcpClient CreateDockerClient(ILogger logger)
        {
            // Connection string (with password masking in logs)
            string connectionString = "Server=host.docker.internal;Database=test;User Id=sa;Password=Password123;TrustServerCertificate=True;";
            
            // Create the client using the builder pattern
            return new McpClientBuilder()
                .WithLogger(logger)
                .WithStdioTransport()
                    .UsingDocker("localhost:5000/mssqlclient-mcp-server:latest")
                    .WithEnvironmentVariable("MSSQL_CONNECTIONSTRING", connectionString)
                    .WithContainerName("mssql-mcp-client")
                    .WithPortMapping("8080", "80")
                    .WithVolumeMapping("/tmp/config", "/app/config", true)
                    .WithNetwork("my-network")
                    .RemoveWhenExited(true)
                .Build();
        }

        /// <summary>
        /// Example of creating an MCP client with automatic executable resolution
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <returns>Configured MCP client</returns>
        public static IMcpClient CreateAutoExeClient(ILogger logger)
        {
            return new McpClientBuilder()
                .WithLogger(logger)
                .WithStdioTransport()
                    .UsingAutoExe()
                    .WithEnvironmentVariable("MSSQL_CONNECTIONSTRING", 
                        "Server=localhost;Database=test;User Id=sa;Password=Password123;TrustServerCertificate=True;")
                .Build();
        }

        /// <summary>
        /// Example of creating an MCP client that builds the server if needed
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <returns>Configured MCP client</returns>
        public static IMcpClient CreateBuildClient(ILogger logger)
        {
            return new McpClientBuilder()
                .WithLogger(logger)
                .WithStdioTransport()
                    .UsingBuild(forceBuild: false)
                    .WithEnvironmentVariable("MSSQL_CONNECTIONSTRING", 
                        "Server=localhost;Database=test;User Id=sa;Password=Password123;TrustServerCertificate=True;")
                .Build();
        }

        /// <summary>
        /// Example of sending a request using the configured client
        /// </summary>
        /// <param name="client">The MCP client</param>
        public static async Task SendRequestExampleAsync(IMcpClient client)
        {
            // Start the client first
            client.Start();

            try
            {
                // Create a request to execute a SQL query
                var request = new Models.McpRequest
                {
                    Method = "execute_query",
                    Params = new
                    {
                        query = "SELECT TOP 5 * FROM Customers"
                    }
                };

                // Send the request
                var response = await client.SendRequestAsync(request);

                // Check the response
                if (response?.IsSuccess == true)
                {
                    Console.WriteLine("Query executed successfully:");
                    Console.WriteLine(response.Result?.ToString());
                }
                else
                {
                    Console.WriteLine($"Error: {response?.Error?.Message}");
                }
            }
            finally
            {
                // Dispose the client when done
                client.Dispose();
            }
        }
    }
}