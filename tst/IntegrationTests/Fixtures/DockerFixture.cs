using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IntegrationTests.Fixtures
{
    /// <summary>
    /// Fixture for managing Docker containers during integration tests
    /// </summary>
    public class DockerFixture : IAsyncLifetime
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DockerFixture> _logger;
        private readonly PortManager _portManager;
        private readonly string _dockerComposeFilePath;
        private readonly string _projectName;
        private readonly string _containerPrefix;
        
        public int SqlServerPort { get; private set; }
        public int McpServerPort { get; private set; }
        public string SqlServerConnectionString { get; private set; }
        public string McpServerEndpoint { get; private set; }
        
        public DockerFixture()
        {
            // Build configuration
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.test.json", optional: false)
                .AddEnvironmentVariables()
                .Build();
                
            // Create logger factory and logger
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });
            
            _logger = loggerFactory.CreateLogger<DockerFixture>();
            
            // Get Docker configuration values
            var testConfig = _configuration.GetSection("IntegrationTests");
            // Get the directory of the current assembly and build the path to the docker-compose file
            string baseDirectory = Directory.GetCurrentDirectory();
            string relativeComposeFilePath = testConfig["Docker:ComposeFilePath"] ?? "Docker/docker-compose.yml";
            _dockerComposeFilePath = Path.GetFullPath(Path.Combine(baseDirectory, relativeComposeFilePath));
            _projectName = testConfig["Docker:ProjectName"];
            _containerPrefix = testConfig["Docker:ContainerPrefix"];
            
            // Log the docker compose file path for debugging
            Console.WriteLine($"Docker compose file path: {_dockerComposeFilePath}");
            if (!File.Exists(_dockerComposeFilePath))
            {
                Console.WriteLine($"WARNING: Docker compose file not found at path: {_dockerComposeFilePath}");
            }
            
            // Parse port ranges
            var sqlPortRange = ParsePortRange(testConfig["SqlServer:PortRange"], 14330, 14339);
            var mcpPortRange = ParsePortRange(testConfig["McpServer:PortRange"], 5100, 5110);
            
            // Create port manager
            _portManager = new PortManager(
                Math.Min(sqlPortRange.start, mcpPortRange.start),
                Math.Max(sqlPortRange.end, mcpPortRange.end));
        }
        
        public async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing Docker test environment");
            
            // Check if we should skip Docker tests
            if (Environment.GetEnvironmentVariable("SKIP_DOCKER_TESTS") == "true")
            {
                _logger.LogInformation("Skipping Docker initialization as SKIP_DOCKER_TESTS is set");
                // Set mock values for test
                SqlServerConnectionString = "Server=mock;Database=mock;User Id=sa;Password=mock;TrustServerCertificate=True;";
                McpServerEndpoint = "http://localhost:12345";
                return;
            }
            
            // Check if we should use existing containers
            if (_configuration.GetValue<bool>("IntegrationTests:UseExistingContainers"))
            {
                _logger.LogInformation("Using existing containers as specified in configuration");
                
                // Set connection details for existing configuration
                string password = _configuration["IntegrationTests:SqlServer:Password"] ?? "IntegrationTest!123";
                string database = _configuration["IntegrationTests:SqlServer:DatabaseName"] ?? "TestDb";
                
                if (_configuration.GetValue<bool>("IntegrationTests:UseLocalSqlServer"))
                {
                    SqlServerConnectionString = _configuration["IntegrationTests:LocalSqlServerConnectionString"] ?? 
                        $"Server=localhost;Database={database};User Id=sa;Password={password};TrustServerCertificate=True;";
                }
                else
                {
                    SqlServerConnectionString = $"Server=localhost,14330;Database={database};User Id=sa;Password={password};TrustServerCertificate=True;";
                }
                
                McpServerEndpoint = "http://localhost:5100";
                
                return;
            }
            
            // Allocate ports for services
            SqlServerPort = _portManager.GetAvailablePortWithRetry();
            McpServerPort = _portManager.GetAvailablePortWithRetry();
            
            _logger.LogInformation($"Allocated ports - SQL Server: {SqlServerPort}, MCP Server: {McpServerPort}");
            
            // Prepare environment variables for docker-compose
            var env = new Dictionary<string, string>
            {
                ["SQL_SERVER_PORT"] = SqlServerPort.ToString(),
                ["MCP_SERVER_PORT"] = McpServerPort.ToString(),
                ["SQL_SERVER_PASSWORD"] = _configuration["IntegrationTests:SqlServer:Password"],
                ["SQL_SERVER_IMAGE"] = _configuration["IntegrationTests:SqlServer:ImageName"],
                ["SQL_SERVER_DATABASE"] = _configuration["IntegrationTests:SqlServer:DatabaseName"],
                ["CONTAINER_PREFIX"] = $"{_containerPrefix}{Guid.NewGuid().ToString().Substring(0, 8)}-",
                ["NETWORK_NAME"] = $"{_projectName}-{Guid.NewGuid().ToString().Substring(0, 8)}"
            };
            
            // Try to start a SQL Server via docker-compose
            try
            {
                _logger.LogInformation("Starting Docker containers via docker-compose");
                await StartContainersAsync(env);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start containers via docker-compose: {Message}", ex.Message);
                
                try
                {
                    // Fallback: Start SQL Server directly with docker run
                    _logger.LogInformation("Trying fallback: Starting SQL Server directly with docker run");
                    
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "docker",
                        Arguments = $"run -e \"ACCEPT_EULA=Y\" -e \"SA_PASSWORD=IntegrationTest!123\" -p 14330:1433 -d mcr.microsoft.com/mssql/server:2022-latest",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    
                    using var process = new Process { StartInfo = startInfo };
                    process.Start();
                    
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    
                    await process.WaitForExitAsync();
                    
                    if (process.ExitCode != 0)
                    {
                        _logger.LogError($"Failed to start SQL Server directly: {error}");
                        throw new InvalidOperationException($"Failed to start SQL Server container: {error}");
                    }
                    
                    // If we got here, the container started successfully
                    _logger.LogInformation("SQL Server container started directly: {ContainerId}", output.Trim());
                    
                    // Wait for SQL Server to be ready (simple delay)
                    _logger.LogInformation("Waiting for SQL Server to start (15 seconds)...");
                    await Task.Delay(15000); // Wait 15 seconds for SQL Server to start
                    
                    // Using the direct container connection
                    SqlServerConnectionString = "Server=localhost,14330;Database=master;User Id=sa;Password=IntegrationTest!123;TrustServerCertificate=True;";
                    McpServerEndpoint = "http://localhost:5100"; // Dummy value as we're not starting MCP server
                    return;
                }
                catch (Exception directEx)
                {
                    _logger.LogError(directEx, "Failed to start SQL Server directly: {Message}", directEx.Message);
                    _logger.LogWarning("All Docker startup methods failed - using localhost connection");
                    
                    // Using fallback localhost connection
                    SqlServerConnectionString = "Server=localhost,14330;Database=master;User Id=sa;Password=IntegrationTest!123;TrustServerCertificate=True;";
                    McpServerEndpoint = "http://localhost:5100";
                    return;
                }
            }
            
            // Set connection details
            var dbPassword = _configuration["IntegrationTests:SqlServer:Password"];
            var dbName = _configuration["IntegrationTests:SqlServer:DatabaseName"];
            
            SqlServerConnectionString = $"Server=localhost,{SqlServerPort};Database={dbName};User Id=sa;Password={dbPassword};TrustServerCertificate=True;";
            McpServerEndpoint = $"http://localhost:{McpServerPort}";
            
            _logger.LogInformation("Docker test environment initialized successfully");
        }
        
        public async Task DisposeAsync()
        {
            _logger.LogInformation("Cleaning up Docker test environment");
            
            // Check if we should keep existing containers
            if (_configuration.GetValue<bool>("IntegrationTests:UseExistingContainers"))
            {
                _logger.LogInformation("Keeping existing containers as specified in configuration");
                return;
            }
            
            try
            {
                // Stop containers
                await StopContainersAsync();
                
                // Also look for directly started containers and clean them up
                try 
                {
                    // Find containers using the mssql image
                    var findInfo = new ProcessStartInfo
                    {
                        FileName = "docker",
                        Arguments = "ps -q --filter ancestor=mcr.microsoft.com/mssql/server:2022-latest",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    
                    using var findProcess = new Process { StartInfo = findInfo };
                    findProcess.Start();
                    string containerIds = await findProcess.StandardOutput.ReadToEndAsync();
                    await findProcess.WaitForExitAsync();
                    
                    // Kill each container
                    foreach (var containerId in containerIds.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        _logger.LogInformation("Stopping SQL Server container: {ContainerId}", containerId);
                        
                        var killInfo = new ProcessStartInfo
                        {
                            FileName = "docker",
                            Arguments = $"rm -f {containerId}",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        
                        using var killProcess = new Process { StartInfo = killInfo };
                        killProcess.Start();
                        await killProcess.WaitForExitAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to cleanup directly started containers");
                }
                
                // Release ports
                _portManager.ReleasePort(SqlServerPort);
                _portManager.ReleasePort(McpServerPort);
                
                _logger.LogInformation("Docker test environment cleaned up successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during container cleanup");
            }
        }
        
        private async Task StartContainersAsync(Dictionary<string, string> environment)
        {
            _logger.LogInformation("Starting Docker containers");
            
            try
            {
                // Prepare command
                var startInfo = new ProcessStartInfo
                {
                    FileName = "docker-compose",
                    Arguments = $"-f \"{_dockerComposeFilePath}\" -p {_projectName} up -d",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                // Add environment variables
                foreach (var kvp in environment)
                {
                    startInfo.EnvironmentVariables[kvp.Key] = kvp.Value;
                }
                
                // Start process
                using var process = new Process { StartInfo = startInfo };
                process.Start();
                
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                
                await process.WaitForExitAsync();
                
                if (process.ExitCode != 0)
                {
                    _logger.LogError($"Failed to start Docker containers. Exit code: {process.ExitCode}");
                    _logger.LogError($"Error: {error}");
                    throw new InvalidOperationException($"Failed to start Docker containers: {error}");
                }
                
                _logger.LogInformation("Docker containers started successfully");
                
                // Wait for services to be ready
                await WaitForServicesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting Docker containers");
                throw;
            }
        }
        
        private async Task StopContainersAsync()
        {
            _logger.LogInformation("Stopping Docker containers");
            
            try
            {
                // Prepare command
                var startInfo = new ProcessStartInfo
                {
                    FileName = "docker-compose",
                    Arguments = $"-f \"{_dockerComposeFilePath}\" -p {_projectName} down -v",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                // Start process
                using var process = new Process { StartInfo = startInfo };
                process.Start();
                
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                
                await process.WaitForExitAsync();
                
                if (process.ExitCode != 0)
                {
                    _logger.LogError($"Failed to stop Docker containers. Exit code: {process.ExitCode}");
                    _logger.LogError($"Error: {error}");
                    // Don't throw here, we're in cleanup
                }
                
                _logger.LogInformation("Docker containers stopped successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping Docker containers");
                // Don't throw here, we're in cleanup
            }
        }
        
        private async Task WaitForServicesAsync()
        {
            _logger.LogInformation("Waiting for services to be ready");
            
            // Wait for SQL Server
            bool sqlServerReady = false;
            int retries = 30;
            
            while (!sqlServerReady && retries > 0)
            {
                try
                {
                    // Use docker logs to check if SQL Server is ready
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "docker",
                        Arguments = $"logs {_containerPrefix}sql-server 2>&1 | grep 'SQL Server is now ready'",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    
                    using var process = new Process { StartInfo = startInfo };
                    process.Start();
                    
                    string output = await process.StandardOutput.ReadToEndAsync();
                    
                    await process.WaitForExitAsync();
                    
                    if (!string.IsNullOrEmpty(output))
                    {
                        sqlServerReady = true;
                        _logger.LogInformation("SQL Server is ready");
                    }
                    else
                    {
                        retries--;
                        await Task.Delay(2000);
                    }
                }
                catch
                {
                    retries--;
                    await Task.Delay(2000);
                }
            }
            
            if (!sqlServerReady)
            {
                throw new TimeoutException("SQL Server did not become ready in the allotted time");
            }
            
            // Wait for MCP Server
            bool mcpServerReady = false;
            retries = 30;
            
            while (!mcpServerReady && retries > 0)
            {
                try
                {
                    // Use HTTP request to check if MCP Server is ready
                    using var httpClient = new HttpClient();
                    httpClient.Timeout = TimeSpan.FromSeconds(5);
                    
                    var response = await httpClient.GetAsync($"http://localhost:{McpServerPort}/health");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        mcpServerReady = true;
                        _logger.LogInformation("MCP Server is ready");
                    }
                    else
                    {
                        retries--;
                        await Task.Delay(2000);
                    }
                }
                catch
                {
                    retries--;
                    await Task.Delay(2000);
                }
            }
            
            if (!mcpServerReady)
            {
                throw new TimeoutException("MCP Server did not become ready in the allotted time");
            }
            
            _logger.LogInformation("All services are ready");
        }
        
        private (int start, int end) ParsePortRange(string portRange, int defaultStart, int defaultEnd)
        {
            if (string.IsNullOrEmpty(portRange))
            {
                return (defaultStart, defaultEnd);
            }
            
            var parts = portRange.Split('-');
            if (parts.Length != 2)
            {
                return (defaultStart, defaultEnd);
            }
            
            if (!int.TryParse(parts[0], out int start) || !int.TryParse(parts[1], out int end))
            {
                return (defaultStart, defaultEnd);
            }
            
            return (start, end);
        }
    }
}