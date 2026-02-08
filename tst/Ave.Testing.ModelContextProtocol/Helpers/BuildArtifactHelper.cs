using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Ave.Testing.ModelContextProtocol.Helpers
{
    /// <summary>
    /// Helper to build and locate MCP server executables
    /// </summary>
    public static class BuildArtifactHelper
    {
        private static string GetRepositoryBasePath()
        {
            return Path.GetFullPath(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."));
        }

        private static string GetExecutableName()
        {
            return Environment.OSVersion.Platform == PlatformID.Unix
                ? "Core.Infrastructure.McpServer"
                : "Core.Infrastructure.McpServer.exe";
        }

        private static string GetMcpProjectPath(string basePath)
        {
            return Path.Combine(
                basePath,
                "src",
                "Core.Infrastructure.McpServer",
                "Core.Infrastructure.McpServer.csproj");
        }

        private static FileInfo? FindLatestExecutable(string basePath, ILogger? logger = null)
        {
            var binDir = new DirectoryInfo(Path.Combine(
                basePath, "src", "Core.Infrastructure.McpServer", "bin"));

            if (!binDir.Exists)
                return null;

            var executableName = GetExecutableName();
            var executables = binDir.GetFiles(executableName, SearchOption.AllDirectories);

            if (executables.Length == 0)
                return null;

            var latest = executables
                .OrderByDescending(f => f.LastWriteTime)
                .First();

            logger?.LogInformation("Found executable at {Path}", latest.FullName);
            return latest;
        }

        /// <summary>
        /// Builds the mssqlclient-mcp-server project if needed and returns the path to the executable
        /// </summary>
        public static string EnsureMCPServerBuiltAndGetPath(
            bool forceBuild = false,
            ILogger? logger = null)
        {
            var basePath = GetRepositoryBasePath();
            logger?.LogInformation("Base path for project: {BasePath}", basePath);

            string mcpProjectPath = GetMcpProjectPath(basePath);
            logger?.LogInformation("Looking for MCP Server project at {ProjectPath}", mcpProjectPath);

            if (!File.Exists(mcpProjectPath))
            {
                var message = $"MCP Server project not found at {mcpProjectPath}";
                logger?.LogError(message);
                throw new FileNotFoundException(message);
            }

            // Check if we already have a built executable
            if (!forceBuild)
            {
                var existing = FindLatestExecutable(basePath, logger);
                if (existing != null)
                    return existing.FullName;
            }

            // Get the current configuration (Debug or Release)
            string configuration =
                #if DEBUG
                "Debug";
                #else
                "Release";
                #endif

            logger?.LogInformation("Building MCP Server project with {Configuration} configuration", configuration);

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

            var built = FindLatestExecutable(basePath, logger);
            if (built == null)
            {
                var message = "MCP Server executable not found after building";
                logger?.LogError(message);
                throw new FileNotFoundException(message);
            }

            return built.FullName;
        }

        /// <summary>
        /// Resolves the path to the MCP server executable by finding the latest build artifact
        /// </summary>
        public static string ResolveMCPServerExecutablePath(ILogger? logger = null)
        {
            var basePath = GetRepositoryBasePath();
            logger?.LogInformation("Base path for artifact resolution: {BasePath}", basePath);

            var existing = FindLatestExecutable(basePath, logger);
            if (existing != null)
                return existing.FullName;

            logger?.LogInformation("No executable found, building project");
            return EnsureMCPServerBuiltAndGetPath(true, logger);
        }
    }
}
