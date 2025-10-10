using System;
using System.Collections.Generic;
using System.Text;
using Ave.Testing.ModelContextProtocol.Interfaces;
using Microsoft.Extensions.Logging;

namespace Ave.Testing.ModelContextProtocol.Builder.ExecutionEnvironments
{
    /// <summary>
    /// Builder for configuring MCP clients using Docker execution
    /// </summary>
    public class DockerExecutionBuilder
    {
        private readonly ILogger? _logger;
        private readonly string _imageNameOrId;
        private readonly Dictionary<string, string> _environmentVariables = new Dictionary<string, string>();
        private readonly List<string> _portMappings = new List<string>();
        private readonly List<string> _volumeMappings = new List<string>();
        private string? _containerName;
        private string? _networkName;
        private string? _command;
        private bool _removeWhenExited = true;

        /// <summary>
        /// Creates a new Docker execution builder
        /// </summary>
        /// <param name="logger">Optional logger</param>
        /// <param name="imageNameOrId">Docker image name or ID</param>
        internal DockerExecutionBuilder(ILogger? logger, string imageNameOrId)
        {
            _logger = logger;
            _imageNameOrId = imageNameOrId;
        }

        /// <summary>
        /// Adds an environment variable to the Docker container
        /// </summary>
        /// <param name="name">Name of the environment variable</param>
        /// <param name="value">Value of the environment variable</param>
        /// <returns>The builder instance for method chaining</returns>
        public DockerExecutionBuilder WithEnvironmentVariable(string name, string value)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Environment variable name cannot be null or empty", nameof(name));

            _environmentVariables[name] = value;
            return this;
        }

        /// <summary>
        /// Adds multiple environment variables to the Docker container
        /// </summary>
        /// <param name="variables">Dictionary of environment variables</param>
        /// <returns>The builder instance for method chaining</returns>
        public DockerExecutionBuilder WithEnvironmentVariables(Dictionary<string, string> variables)
        {
            if (variables == null)
                throw new ArgumentNullException(nameof(variables));

            foreach (var kvp in variables)
            {
                _environmentVariables[kvp.Key] = kvp.Value;
            }
            return this;
        }

        /// <summary>
        /// Sets the container name
        /// </summary>
        /// <param name="containerName">Name for the container</param>
        /// <returns>The builder instance for method chaining</returns>
        public DockerExecutionBuilder WithContainerName(string containerName)
        {
            if (string.IsNullOrEmpty(containerName))
                throw new ArgumentException("Container name cannot be null or empty", nameof(containerName));

            _containerName = containerName;
            return this;
        }

        /// <summary>
        /// Adds a port mapping to the Docker container
        /// </summary>
        /// <param name="hostPort">Port on the host</param>
        /// <param name="containerPort">Port in the container</param>
        /// <returns>The builder instance for method chaining</returns>
        public DockerExecutionBuilder WithPortMapping(string hostPort, string containerPort)
        {
            if (string.IsNullOrEmpty(hostPort))
                throw new ArgumentException("Host port cannot be null or empty", nameof(hostPort));
            if (string.IsNullOrEmpty(containerPort))
                throw new ArgumentException("Container port cannot be null or empty", nameof(containerPort));

            _portMappings.Add($"{hostPort}:{containerPort}");
            return this;
        }

        /// <summary>
        /// Adds a volume mapping to the Docker container
        /// </summary>
        /// <param name="hostPath">Path on the host</param>
        /// <param name="containerPath">Path in the container</param>
        /// <param name="readOnly">Whether the volume should be read-only</param>
        /// <returns>The builder instance for method chaining</returns>
        public DockerExecutionBuilder WithVolumeMapping(string hostPath, string containerPath, bool readOnly = false)
        {
            if (string.IsNullOrEmpty(hostPath))
                throw new ArgumentException("Host path cannot be null or empty", nameof(hostPath));
            if (string.IsNullOrEmpty(containerPath))
                throw new ArgumentException("Container path cannot be null or empty", nameof(containerPath));

            string mapping = $"{hostPath}:{containerPath}";
            if (readOnly)
                mapping += ":ro";

            _volumeMappings.Add(mapping);
            return this;
        }

        /// <summary>
        /// Sets the network for the Docker container
        /// </summary>
        /// <param name="networkName">Name of the network</param>
        /// <returns>The builder instance for method chaining</returns>
        public DockerExecutionBuilder WithNetwork(string networkName)
        {
            if (string.IsNullOrEmpty(networkName))
                throw new ArgumentException("Network name cannot be null or empty", nameof(networkName));

            _networkName = networkName;
            return this;
        }

        /// <summary>
        /// Sets the command to run in the Docker container
        /// </summary>
        /// <param name="command">Command to run</param>
        /// <param name="args">Command arguments</param>
        /// <returns>The builder instance for method chaining</returns>
        public DockerExecutionBuilder WithCommand(string command, params string[] args)
        {
            if (string.IsNullOrEmpty(command))
                throw new ArgumentException("Command cannot be null or empty", nameof(command));

            // Build the full command with proper escaping
            StringBuilder commandBuilder = new StringBuilder(command);
            foreach (var arg in args)
            {
                if (!string.IsNullOrEmpty(arg))
                {
                    commandBuilder.Append(' ');
                    // Check if we need to quote the argument
                    if (arg.Contains(' ') && !arg.StartsWith("\"") && !arg.EndsWith("\""))
                    {
                        commandBuilder.Append('"').Append(arg).Append('"');
                    }
                    else
                    {
                        commandBuilder.Append(arg);
                    }
                }
            }

            _command = commandBuilder.ToString();
            return this;
        }

        /// <summary>
        /// Sets whether to remove the container when exited
        /// </summary>
        /// <param name="remove">Whether to remove the container</param>
        /// <returns>The builder instance for method chaining</returns>
        public DockerExecutionBuilder RemoveWhenExited(bool remove)
        {
            _removeWhenExited = remove;
            return this;
        }

        /// <summary>
        /// Builds the MCP client with the configured settings
        /// </summary>
        /// <returns>The configured MCP client</returns>
        public IMcpClient Build()
        {
            // Build the docker run command
            var dockerRunArgs = new StringBuilder();

            // Add container name if specified
            if (!string.IsNullOrEmpty(_containerName))
            {
                dockerRunArgs.Append("--name ").Append(_containerName).Append(' ');
            }

            // Add environment variables
            foreach (var kvp in _environmentVariables)
            {
                // Escape any quotes in the value
                string escapedValue = kvp.Value.Replace("\"", "\\\"");
                dockerRunArgs.Append("-e ").Append(kvp.Key).Append("=\"").Append(escapedValue).Append("\" ");
            }

            // Add port mappings
            foreach (var mapping in _portMappings)
            {
                dockerRunArgs.Append("-p ").Append(mapping).Append(' ');
            }

            // Add volume mappings
            foreach (var mapping in _volumeMappings)
            {
                dockerRunArgs.Append("-v ").Append(mapping).Append(' ');
            }

            // Add network if specified
            if (!string.IsNullOrEmpty(_networkName))
            {
                dockerRunArgs.Append("--network ").Append(_networkName).Append(' ');
            }

            // Add remove flag if specified
            if (_removeWhenExited)
            {
                dockerRunArgs.Append("--rm ");
            }

            // Add interactive flags for stdin/stdout
            dockerRunArgs.Append("-i ");

            // Add image name
            dockerRunArgs.Append(_imageNameOrId).Append(' ');

            // Add command if specified
            if (!string.IsNullOrEmpty(_command))
            {
                dockerRunArgs.Append(_command);
            }

            // Create the process wrapper with docker run command
            var processWrapper = new Implementation.ProcessWrapper(
                "docker",
                null,  // No additional environment variables for docker host
                _logger);

            // Set docker run arguments
            processWrapper.Arguments = "run " + dockerRunArgs.ToString().TrimEnd();

            // Create the MCP client
            return new Implementation.McpClient(processWrapper, _logger);
        }
    }
}