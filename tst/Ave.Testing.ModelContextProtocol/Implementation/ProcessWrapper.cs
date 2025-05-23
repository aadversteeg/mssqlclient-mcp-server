using System.Diagnostics;
using System.Text;
using Ave.Testing.ModelContextProtocol.Interfaces;
using Microsoft.Extensions.Logging;

namespace Ave.Testing.ModelContextProtocol.Implementation
{
    /// <summary>
    /// Wraps a process for communication via standard input/output
    /// </summary>
    public class ProcessWrapper : IProcessWrapper
    {
        private readonly Process _process;
        private readonly ILogger? _logger;
        private StreamWriter? _inputWriter;
        private StreamReader? _outputReader;
        private bool _disposed;

        /// <summary>
        /// Gets or sets the working directory for the process
        /// </summary>
        public string? WorkingDirectory
        {
            get => _process.StartInfo.WorkingDirectory;
            set => _process.StartInfo.WorkingDirectory = value ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the arguments for the process
        /// </summary>
        public string? Arguments
        {
            get => _process.StartInfo.Arguments;
            set => _process.StartInfo.Arguments = value ?? string.Empty;
        }

        /// <summary>
        /// Creates a new process wrapper
        /// </summary>
        /// <param name="executablePath">Path to the executable</param>
        /// <param name="environmentVariables">Optional environment variables</param>
        /// <param name="logger">Optional logger</param>
        public ProcessWrapper(string executablePath, Dictionary<string, string>? environmentVariables = null, ILogger? logger = null)
        {
            _logger = logger;
            
            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            // Log executable path
            _logger?.LogInformation("Creating process for executable: {ExecutablePath}", executablePath);

            // Set environment variables
            if (environmentVariables != null)
            {
                foreach (var kvp in environmentVariables)
                {
                    // Log sanitized environment variables (mask sensitive data)
                    if (kvp.Key.Contains("PASSWORD", StringComparison.OrdinalIgnoreCase) ||
                        kvp.Key.Contains("SECRET", StringComparison.OrdinalIgnoreCase) ||
                        kvp.Key.Contains("KEY", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger?.LogInformation("Setting environment variable: {Key}=********", kvp.Key);
                    }
                    else if (kvp.Key.Contains("CONNECTION", StringComparison.OrdinalIgnoreCase))
                    {
                        // Mask password in connection strings
                        string maskedValue = kvp.Value;
                        if (maskedValue.Contains("Password=", StringComparison.OrdinalIgnoreCase))
                        {
                            maskedValue = maskedValue.Replace("Password=", "Password=********", 
                                StringComparison.OrdinalIgnoreCase);
                        }
                        _logger?.LogInformation("Setting environment variable: {Key}={Value}", kvp.Key, maskedValue);
                    }
                    else
                    {
                        _logger?.LogInformation("Setting environment variable: {Key}={Value}", kvp.Key, kvp.Value);
                    }
                    
                    _process.StartInfo.EnvironmentVariables[kvp.Key] = kvp.Value;
                }
            }
        }

        /// <summary>
        /// Starts the process
        /// </summary>
        public void Start()
        {
            _logger?.LogInformation("Starting process: {FileName}", _process.StartInfo.FileName);
            
            _process.Start();
            
            _logger?.LogInformation("Process started with ID: {ProcessId}", _process.Id);
            
            _inputWriter = new StreamWriter(_process.StandardInput.BaseStream, Encoding.UTF8)
            {
                AutoFlush = true
            };
            _outputReader = new StreamReader(_process.StandardOutput.BaseStream, Encoding.UTF8);
            
            // Log stderr output using a separate thread
            new Thread(() => 
            {
                using var errorReader = new StreamReader(_process.StandardError.BaseStream, Encoding.UTF8);
                string? line;
                while ((line = errorReader.ReadLine()) != null)
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        _logger?.LogWarning("Process error output: {ErrorData}", line);
                    }
                    else
                    {
                        _logger?.LogInformation("Received empty line from stderr");
                    }
                }
            }).Start();
        }

        /// <summary>
        /// Gets a value indicating whether the process has exited
        /// </summary>
        public bool HasExited => _process.HasExited;

        /// <summary>
        /// Reads a line from the process's standard output
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>The line read, or null if the end of the stream was reached</returns>
        public async Task<string?> ReadLineAsync(CancellationToken cancellationToken = default)
        {
            if (_outputReader == null)
                throw new InvalidOperationException("Process has not been started");
            
            return await _outputReader.ReadLineAsync(cancellationToken);
        }

        /// <summary>
        /// Writes a line to the process's standard input
        /// </summary>
        /// <param name="line">The line to write</param>
        /// <param name="cancellationToken">A cancellation token</param>
        public async Task WriteLineAsync(string line, CancellationToken cancellationToken = default)
        {
            if (_inputWriter == null)
                throw new InvalidOperationException("Process has not been started");
            
            await _inputWriter.WriteLineAsync(line.AsMemory(), cancellationToken);
        }

        /// <summary>
        /// Flushes the standard input
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            if (_inputWriter == null)
                throw new InvalidOperationException("Process has not been started");
            
            await _inputWriter.FlushAsync();
        }

        /// <summary>
        /// Disposes the process wrapper
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the process wrapper
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    try
                    {
                        _inputWriter?.Dispose();
                        _outputReader?.Dispose();
                        
                        if (!_process.HasExited)
                        {
                            _process.Kill();
                        }
                        _process.Dispose();
                    }
                    catch (Exception)
                    {
                        // Ignore exceptions during cleanup
                    }
                }

                _disposed = true;
            }
        }
    }
}