using System;
using System.Collections.Generic;
using Ave.Testing.ModelContextProtocol.Builder.ExecutionEnvironments;
using Microsoft.Extensions.Logging;

namespace Ave.Testing.ModelContextProtocol.Builder
{
    /// <summary>
    /// Builder for configuring MCP clients using stdio transport
    /// </summary>
    public class StdioTransportBuilder
    {
        private readonly ILogger? _logger;

        /// <summary>
        /// Creates a new stdio transport builder
        /// </summary>
        /// <param name="logger">Optional logger</param>
        internal StdioTransportBuilder(ILogger? logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Configures the builder to use a .NET assembly for execution
        /// </summary>
        /// <param name="assemblyPath">Path to the .NET assembly</param>
        /// <returns>A .NET execution builder for further configuration</returns>
        public DotNetExecutionBuilder UsingDotNet(string assemblyPath)
        {
            if (string.IsNullOrEmpty(assemblyPath))
                throw new ArgumentException("Assembly path cannot be null or empty", nameof(assemblyPath));

            return new DotNetExecutionBuilder(_logger, assemblyPath);
        }

        /// <summary>
        /// Configures the builder to use a Docker container for execution
        /// </summary>
        /// <param name="imageNameOrId">Docker image name or ID</param>
        /// <returns>A Docker execution builder for further configuration</returns>
        public DockerExecutionBuilder UsingDocker(string imageNameOrId)
        {
            if (string.IsNullOrEmpty(imageNameOrId))
                throw new ArgumentException("Image name/ID cannot be null or empty", nameof(imageNameOrId));

            return new DockerExecutionBuilder(_logger, imageNameOrId);
        }

        /// <summary>
        /// Configures the builder to use a custom command for execution
        /// </summary>
        /// <param name="executablePath">Path to the executable</param>
        /// <returns>A command execution builder for further configuration</returns>
        public CommandExecutionBuilder UsingCommand(string executablePath)
        {
            if (string.IsNullOrEmpty(executablePath))
                throw new ArgumentException("Executable path cannot be null or empty", nameof(executablePath));

            return new CommandExecutionBuilder(_logger, executablePath);
        }

        /// <summary>
        /// Configures the builder to automatically find and use the MCP server executable
        /// </summary>
        /// <returns>A command execution builder for further configuration</returns>
        public CommandExecutionBuilder UsingAutoExe()
        {
            return new CommandExecutionBuilder(_logger, true);
        }

        /// <summary>
        /// Configures the builder to build the MCP server if needed and use it
        /// </summary>
        /// <param name="forceBuild">Whether to force rebuilding the server</param>
        /// <returns>A command execution builder for further configuration</returns>
        public CommandExecutionBuilder UsingBuild(bool forceBuild = false)
        {
            return new CommandExecutionBuilder(_logger, false, forceBuild);
        }
    }
}