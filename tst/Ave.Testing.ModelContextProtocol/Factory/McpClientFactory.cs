using Ave.Testing.ModelContextProtocol.Helpers;
using Ave.Testing.ModelContextProtocol.Implementation;
using Ave.Testing.ModelContextProtocol.Interfaces;
using Microsoft.Extensions.Logging;

namespace Ave.Testing.ModelContextProtocol.Factory
{
    /// <summary>
    /// Factory for creating MCP clients
    /// </summary>
    public static class McpClientFactory
    {
        /// <summary>
        /// Creates a new MCP client
        /// </summary>
        /// <param name="executablePath">Path to the MCP server executable</param>
        /// <param name="environmentVariables">Optional environment variables</param>
        /// <param name="logger">Optional logger</param>
        /// <returns>An MCP client</returns>
        public static IMcpClient Create(
            string executablePath, 
            Dictionary<string, string>? environmentVariables = null,
            ILogger? logger = null)
        {
            var process = new ProcessWrapper(executablePath, environmentVariables, logger);
            return new McpClient(process, logger);
        }
        
        /// <summary>
        /// Creates an MCP client using automatic executable resolution
        /// </summary>
        /// <param name="environmentVariables">Optional environment variables</param>
        /// <param name="logger">Optional logger</param>
        /// <returns>An MCP client</returns>
        public static IMcpClient CreateWithAutoExe(
            Dictionary<string, string>? environmentVariables = null,
            ILogger? logger = null)
        {
            // Resolve executable path
            string executablePath = BuildArtifactHelper.ResolveMCPServerExecutablePath(logger);
            
            // Create client
            return Create(executablePath, environmentVariables, logger);
        }
        
        /// <summary>
        /// Creates an MCP client by building the MCP server if needed
        /// </summary>
        /// <param name="forceBuild">Whether to force rebuilding the server</param>
        /// <param name="environmentVariables">Optional environment variables</param>
        /// <param name="logger">Optional logger</param>
        /// <returns>An MCP client</returns>
        public static IMcpClient CreateWithBuild(
            bool forceBuild = false,
            Dictionary<string, string>? environmentVariables = null,
            ILogger? logger = null)
        {
            // Build and get executable path
            string executablePath = BuildArtifactHelper.EnsureMCPServerBuiltAndGetPath(forceBuild, logger);
            
            // Create client
            return Create(executablePath, environmentVariables, logger);
        }
    }
}