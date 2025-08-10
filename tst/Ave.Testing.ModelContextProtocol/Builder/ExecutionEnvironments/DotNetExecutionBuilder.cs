using System;
using System.Collections.Generic;
using System.IO;
using Ave.Testing.ModelContextProtocol.Factory;
using Ave.Testing.ModelContextProtocol.Interfaces;
using Microsoft.Extensions.Logging;

namespace Ave.Testing.ModelContextProtocol.Builder.ExecutionEnvironments
{
    /// <summary>
    /// Builder for configuring MCP clients using .NET execution
    /// </summary>
    public class DotNetExecutionBuilder
    {
        private readonly ILogger? _logger;
        private readonly string _assemblyPath;
        private readonly Dictionary<string, string> _environmentVariables = new Dictionary<string, string>();
        private string? _workingDirectory;
        private string _arguments = string.Empty;

        /// <summary>
        /// Creates a new .NET execution builder
        /// </summary>
        /// <param name="logger">Optional logger</param>
        /// <param name="assemblyPath">Path to the .NET assembly</param>
        internal DotNetExecutionBuilder(ILogger? logger, string assemblyPath)
        {
            _logger = logger;
            _assemblyPath = assemblyPath;
        }

        /// <summary>
        /// Adds an environment variable to the execution environment
        /// </summary>
        /// <param name="name">Name of the environment variable</param>
        /// <param name="value">Value of the environment variable</param>
        /// <returns>The builder instance for method chaining</returns>
        public DotNetExecutionBuilder WithEnvironmentVariable(string name, string value)
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
        public DotNetExecutionBuilder WithEnvironmentVariables(Dictionary<string, string> variables)
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
        /// Sets the working directory for the .NET execution
        /// </summary>
        /// <param name="workingDirectory">The working directory</param>
        /// <returns>The builder instance for method chaining</returns>
        public DotNetExecutionBuilder WithWorkingDirectory(string workingDirectory)
        {
            if (string.IsNullOrEmpty(workingDirectory))
                throw new ArgumentException("Working directory cannot be null or empty", nameof(workingDirectory));

            if (!Directory.Exists(workingDirectory))
                throw new DirectoryNotFoundException($"Working directory does not exist: {workingDirectory}");

            _workingDirectory = workingDirectory;
            return this;
        }

        /// <summary>
        /// Sets additional arguments for the dotnet command
        /// </summary>
        /// <param name="arguments">Command-line arguments</param>
        /// <returns>The builder instance for method chaining</returns>
        public DotNetExecutionBuilder WithArguments(string arguments)
        {
            _arguments = arguments ?? string.Empty;
            return this;
        }

        /// <summary>
        /// Builds the MCP client with the configured settings
        /// </summary>
        /// <returns>The configured MCP client</returns>
        public IMcpClient Build()
        {
            // Validate configuration
            if (!File.Exists(_assemblyPath))
                throw new FileNotFoundException($"Assembly not found: {_assemblyPath}");

            // Prepare the command
            string dotnetCommand = $"dotnet \"{_assemblyPath}\"";
            if (!string.IsNullOrEmpty(_arguments))
                dotnetCommand += $" {_arguments}";

            // Create environment variables
            var env = new Dictionary<string, string>(_environmentVariables);

            // Create the process wrapper
            var processWrapper = new Implementation.ProcessWrapper(
                "dotnet",
                env,
                _logger);

            // Set working directory if specified
            if (!string.IsNullOrEmpty(_workingDirectory))
                processWrapper.WorkingDirectory = _workingDirectory;

            // Create the MCP client
            return new Implementation.McpClient(processWrapper, _logger);
        }
    }
}