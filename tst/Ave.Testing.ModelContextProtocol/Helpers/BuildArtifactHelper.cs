using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Ave.Testing.ModelContextProtocol.Helpers
{
    /// <summary>
    /// Helper to build and locate MCP server executables
    /// </summary>
    public static class BuildArtifactHelper
    {
        /// <summary>
        /// Builds the mssqlclient-mcp-server project if needed and returns the path to the executable
        /// </summary>
        public static string EnsureMCPServerBuiltAndGetPath(
            bool forceBuild = false, 
            ILogger? logger = null)
        {
            // Get the root of the repository (from tst/IntegrationTests/bin/Debug/net9.0 to the repo root)
            var basePath = Path.GetFullPath(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."));
            
            logger?.LogInformation("Base path for project: {BasePath}", basePath);
            
            // Find the path to the mssqlclient-mcp-server project
            string mcpProjectPath = Path.Combine(
                basePath, 
                "src", 
                "Core.Infrastructure.McpServer", 
                "Core.Infrastructure.McpServer.csproj");
            
            logger?.LogInformation("Looking for MCP Server project at {ProjectPath}", mcpProjectPath);
            
            // Check if the file exists
            if (!File.Exists(mcpProjectPath))
            {
                var message = $"MCP Server project not found at {mcpProjectPath}";
                logger?.LogError(message);
                throw new FileNotFoundException(message);
            }
            
            // Get the current configuration (Debug or Release)
            string configuration = 
                #if DEBUG
                "Debug";
                #else
                "Release";
                #endif
            
            logger?.LogInformation("Using {Configuration} configuration", configuration);
            
            // Expected executable path
            string executablePath = Path.Combine(
                basePath,
                "src",
                "Core.Infrastructure.McpServer",
                "bin",
                configuration,
                "net9.0",
                "Core.Infrastructure.McpServer.exe");
            
            // On Linux/WSL the executable name doesn't have .exe
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                executablePath = Path.Combine(
                    basePath,
                    "src",
                    "Core.Infrastructure.McpServer",
                    "bin",
                    configuration,
                    "net9.0",
                    "Core.Infrastructure.McpServer");
            }
            
            logger?.LogInformation("Expected executable path: {ExecutablePath}", executablePath);
            
            // Check if the executable exists or if we need to force build
            if (forceBuild || !File.Exists(executablePath))
            {
                logger?.LogInformation("Building MCP Server project");
                
                // Build the project
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = $"build \"{mcpProjectPath}\" -c {configuration}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                
                if (process.ExitCode != 0)
                {
                    var message = $"Failed to build MCP Server project: {error}";
                    logger?.LogError(message);
                    throw new Exception(message);
                }
                
                logger?.LogInformation("MCP Server project built successfully");
                
                // Verify the executable exists after building
                if (!File.Exists(executablePath))
                {
                    var message = $"MCP Server executable not found at {executablePath} after building";
                    logger?.LogError(message);
                    throw new FileNotFoundException(message);
                }
            }
            
            return executablePath;
        }
        
        /// <summary>
        /// Resolves the path to the MCP server executable by finding the latest build artifact
        /// </summary>
        public static string ResolveMCPServerExecutablePath(ILogger? logger = null)
        {
            // Base path to the solution's build artifacts
            var basePath = Path.GetFullPath(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."));
            
            logger?.LogInformation("Base path for artifact resolution: {BasePath}", basePath);
            
            string executableName = "Core.Infrastructure.McpServer.exe";
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                executableName = "Core.Infrastructure.McpServer";
            }
            
            // Try to find the executable in the output directories
            string[] searchPaths = new[]
            {
                Path.Combine(basePath, "src", "Core.Infrastructure.McpServer", "bin", "Debug", "net9.0", executableName),
                Path.Combine(basePath, "src", "Core.Infrastructure.McpServer", "bin", "Release", "net9.0", executableName)
            };
            
            // Return the first path that exists
            foreach (var path in searchPaths)
            {
                logger?.LogInformation("Checking for executable at {Path}", path);
                if (File.Exists(path))
                {
                    logger?.LogInformation("Found executable at {Path}", path);
                    return path;
                }
            }
            
            // If neither exists, try to find the most recent build
            var srcDir = new DirectoryInfo(Path.Combine(basePath, "src"));
            if (srcDir.Exists)
            {
                logger?.LogInformation("Searching for executable in {SrcDir}", srcDir.FullName);
                
                // Look for the most recently modified executable
                var executableFiles = srcDir.GetFiles(executableName, SearchOption.AllDirectories);
                if (executableFiles.Length > 0)
                {
                    var latestFile = executableFiles
                        .OrderByDescending(f => f.LastWriteTime)
                        .First();
                    
                    logger?.LogInformation("Found latest executable at {Path}", latestFile.FullName);
                    return latestFile.FullName;
                }
            }
            
            // If we didn't find any executable, build it
            logger?.LogInformation("No executable found, building project");
            return EnsureMCPServerBuiltAndGetPath(true, logger);
        }
    }
}