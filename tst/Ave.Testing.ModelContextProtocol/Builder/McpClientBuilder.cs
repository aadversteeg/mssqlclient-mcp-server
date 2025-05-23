using System;
using System.Collections.Generic;
using Ave.Testing.ModelContextProtocol.Interfaces;
using Microsoft.Extensions.Logging;

namespace Ave.Testing.ModelContextProtocol.Builder
{
    /// <summary>
    /// Builder for creating MCP clients with various configurations
    /// </summary>
    public class McpClientBuilder
    {
        private readonly ILogger? _logger;

        /// <summary>
        /// Creates a new MCP client builder
        /// </summary>
        public McpClientBuilder()
        {
        }

        /// <summary>
        /// Creates a new MCP client builder with a logger
        /// </summary>
        /// <param name="logger">The logger to use</param>
        public McpClientBuilder(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Configures the builder to use a logger
        /// </summary>
        /// <param name="logger">The logger to use</param>
        /// <returns>The builder instance for method chaining</returns>
        public McpClientBuilder WithLogger(ILogger logger)
        {
            return new McpClientBuilder(logger);
        }

        /// <summary>
        /// Configures the builder to use stdio transport
        /// </summary>
        /// <returns>A stdio transport builder for further configuration</returns>
        public StdioTransportBuilder WithStdioTransport()
        {
            return new StdioTransportBuilder(_logger);
        }

        /// <summary>
        /// Configures the builder to use SSE transport
        /// </summary>
        /// <returns>An SSE transport builder for further configuration</returns>
        public SseTransportBuilder WithSseTransport()
        {
            return new SseTransportBuilder(_logger);
        }
    }
}