using System.Diagnostics;
using BuildArtifactHelper = Ave.Testing.ModelContextProtocol.Helpers.BuildArtifactHelper;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IntegrationTests.Fixtures
{
    /// <summary>
    /// Fixture for MCP integration tests that manages MCP server process
    /// </summary>
    public class McpFixture : IAsyncLifetime
    {
        private readonly ILogger<McpFixture> _logger;
        private readonly List<Process> _processesToCleanUp = new();
        
        public string McpServerExecutablePath { get; private set; }
        
        public McpFixture()
        {
            // Create logger
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            
            _logger = loggerFactory.CreateLogger<McpFixture>();
            
            // Initialize with default path that will be resolved during initialization
            McpServerExecutablePath = string.Empty;
        }
        
        public void RegisterProcess(Process process)
        {
            _processesToCleanUp.Add(process);
        }
        
        public async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing MCP fixture");
            
            try
            {
                // Try to resolve MCP server executable path
                McpServerExecutablePath = BuildArtifactHelper.ResolveMCPServerExecutablePath(_logger);
                _logger.LogInformation("MCP server executable path: {Path}", McpServerExecutablePath);
                
                // Verify that the executable exists
                if (!File.Exists(McpServerExecutablePath))
                {
                    _logger.LogWarning("MCP server executable not found at resolved path: {Path}", McpServerExecutablePath);
                    
                    // Build the MCP server if it doesn't exist
                    _logger.LogInformation("Attempting to build MCP server...");
                    
                    // Determine the src directory (2 levels up from tst)
                    var testDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    var repositoryRoot = Path.GetFullPath(Path.Combine(testDirectory, "..", "..", "..", "..", ".."));
                    var srcDirectory = Path.Combine(repositoryRoot, "src");
                    
                    _logger.LogInformation("Building MCP server in directory: {SrcDirectory}", srcDirectory);
                    
                    // Build the MCP server project
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = "build",
                        WorkingDirectory = srcDirectory,
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
                    
                    if (process.ExitCode == 0)
                    {
                        _logger.LogInformation("MCP server built successfully");
                        // Try to resolve the path again
                        McpServerExecutablePath = BuildArtifactHelper.ResolveMCPServerExecutablePath(_logger);
                        _logger.LogInformation("MCP server executable path after build: {Path}", McpServerExecutablePath);
                    }
                    else
                    {
                        _logger.LogError("Failed to build MCP server: {Error}", error);
                        throw new InvalidOperationException($"Failed to build MCP server: {error}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resolve or build MCP server");
                throw new InvalidOperationException("Failed to initialize MCP server. Ensure you can build the project with 'dotnet build' in the src directory.", ex);
            }
        }
        
        public async Task DisposeAsync()
        {
            _logger.LogInformation("Cleaning up MCP fixture");
            
            foreach (var process in _processesToCleanUp)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                        _logger.LogInformation("Killed process {ProcessId}", process.Id);
                    }
                    process.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error cleaning up process");
                }
            }
        }
    }
    
    [CollectionDefinition("MCP Tests")]
    public class McpTestCollection : ICollectionFixture<McpFixture>
    {
        // This class has no code, and is never created.
        // Its purpose is to be the place to apply [CollectionDefinition] and
        // all the ICollectionFixture<> interfaces.
    }
}