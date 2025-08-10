using System;
using System.Collections.Generic;
using System.IO;
using Ave.Testing.ModelContextProtocol.Factory;
using Ave.Testing.ModelContextProtocol.Helpers;
using Ave.Testing.ModelContextProtocol.Interfaces;
using Microsoft.Extensions.Logging;

namespace Ave.Testing.ModelContextProtocol.Builder.ExecutionEnvironments
{
    /// <summary>
    /// Builder for configuring MCP clients using command execution
    /// </summary>
    public class CommandExecutionBuilder
    {
        private readonly ILogger? _logger;
        private readonly string? _executablePath;
        private readonly bool _useAutoExe;
        private readonly bool? _forceBuild;
        private readonly Dictionary<string, string> _environmentVariables = new Dictionary<string, string>();
        private string? _workingDirectory;
        private string? _arguments;

        /// <summary>
        /// Creates a new command execution builder for a specific executable
        /// </summary>
        /// <param name="logger">Optional logger</param>
        /// <param name="executablePath">Path to the executable</param>
        internal CommandExecutionBuilder(ILogger? logger, string executablePath)
        {
            _logger = logger;
            _executablePath = executablePath;
            _useAutoExe = false;
        }

        /// <summary>
        /// Creates a new command execution builder with auto-resolution of the executable
        /// </summary>
        /// <param name="logger">Optional logger</param>
        /// <param name="useAutoExe">Whether to use auto-exe resolution</param>
        /// <param name="forceBuild">Whether to force rebuilding the server (only used with build mode)</param>
        internal CommandExecutionBuilder(ILogger? logger, bool useAutoExe, bool? forceBuild = null)
        {
            _logger = logger;
            _useAutoExe = useAutoExe;
            _forceBuild = forceBuild;
        }

        /// <summary>
        /// Adds an environment variable to the execution environment
        /// </summary>
        /// <param name="name">Name of the environment variable</param>
        /// <param name="value">Value of the environment variable</param>
        /// <returns>The builder instance for method chaining</returns>
        public CommandExecutionBuilder WithEnvironmentVariable(string name, string value)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Environment variable name cannot be null or empty", nameof(name));

            _environmentVariables[name] = value;
            return this;
        }

        /// <summary>
        /// Adds multiple environment variables to the execution environment
        /// </summary>
        /// <param name="variables">Dictionary of environment variables</param>
        /// <returns>The builder instance for method chaining</returns>
        public CommandExecutionBuilder WithEnvironmentVariables(Dictionary<string, string> variables)
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
        /// Sets the working directory for the command execution
        /// </summary>
        /// <param name="workingDirectory">The working directory</param>
        /// <returns>The builder instance for method chaining</returns>
        public CommandExecutionBuilder WithWorkingDirectory(string workingDirectory)
        {
            if (string.IsNullOrEmpty(workingDirectory))
                throw new ArgumentException("Working directory cannot be null or empty", nameof(workingDirectory));

            if (!Directory.Exists(workingDirectory))
                throw new DirectoryNotFoundException($"Working directory does not exist: {workingDirectory}");

            _workingDirectory = workingDirectory;
            return this;
        }

        /// <summary>
        /// Sets arguments for the command
        /// </summary>
        /// <param name="arguments">Command-line arguments</param>
        /// <returns>The builder instance for method chaining</returns>
        public CommandExecutionBuilder WithArguments(string arguments)
        {
            _arguments = arguments;
            return this;
        }

        /// <summary>
        /// Builds the MCP client with the configured settings
        /// </summary>
        /// <returns>The configured MCP client</returns>
        public IMcpClient Build()
        {
            // Determine executable path based on configuration
            string executablePath;
            if (_useAutoExe)
            {
                executablePath = BuildArtifactHelper.ResolveMCPServerExecutablePath(_logger);
            }
            else if (_forceBuild.HasValue)
            {
                executablePath = BuildArtifactHelper.EnsureMCPServerBuiltAndGetPath(_forceBuild.Value, _logger);
            }
            else
            {
                if (string.IsNullOrEmpty(_executablePath))
                    throw new InvalidOperationException("Executable path is not specified");

                executablePath = _executablePath;
            }

            // Create the process wrapper
            var processWrapper = new Implementation.ProcessWrapper(
                executablePath,
                _environmentVariables,
                _logger);

            // Set working directory if specified
            if (!string.IsNullOrEmpty(_workingDirectory))
                processWrapper.WorkingDirectory = _workingDirectory;

            // Set arguments if specified
            if (!string.IsNullOrEmpty(_arguments))
                processWrapper.Arguments = _arguments;

            // Create the MCP client
            return new Implementation.McpClient(processWrapper, _logger);
        }
    }
}