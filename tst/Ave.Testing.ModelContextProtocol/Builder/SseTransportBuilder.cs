using System;
using Ave.Testing.ModelContextProtocol.Builder.ExecutionEnvironments;
using Microsoft.Extensions.Logging;

namespace Ave.Testing.ModelContextProtocol.Builder
{
    /// <summary>
    /// Builder for configuring MCP clients using Server-Sent Events (SSE) transport
    /// </summary>
    public class SseTransportBuilder
    {
        private readonly ILogger? _logger;

        /// <summary>
        /// Creates a new SSE transport builder
        /// </summary>
        /// <param name="logger">Optional logger</param>
        internal SseTransportBuilder(ILogger? logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Configures the builder to use HTTP client to connect to an MCP server
        /// </summary>
        /// <param name="serverUrl">URL of the MCP server</param>
        /// <returns>An HTTP client execution builder for further configuration</returns>
        public HttpClientExecutionBuilder UseHttpClient(string serverUrl)
        {
            if (serverUrl == null)
                throw new ArgumentNullException(nameof(serverUrl), "Server URL cannot be null");
                
            if (string.IsNullOrEmpty(serverUrl))
                throw new ArgumentException("Server URL cannot be empty", nameof(serverUrl));

            return new HttpClientExecutionBuilder(_logger, serverUrl);
        }
    }
}