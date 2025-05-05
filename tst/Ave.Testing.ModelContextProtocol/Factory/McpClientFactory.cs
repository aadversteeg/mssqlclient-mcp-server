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
    }
}