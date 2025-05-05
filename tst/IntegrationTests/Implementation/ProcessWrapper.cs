using System.Diagnostics;
using System.Text;
using IntegrationTests.Interfaces;
using Microsoft.Extensions.Logging;

namespace IntegrationTests.Implementation
{
    public class ProcessWrapper : IProcessWrapper
    {
        private readonly Process _process;
        private readonly ILogger? _logger;
        private StreamWriter? _inputWriter;
        private StreamReader? _outputReader;
        private StreamReader? _errorReader;
        private bool _disposed;

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

        public bool HasExited => _process.HasExited;

        public async Task<string?> ReadLineAsync(CancellationToken cancellationToken = default)
        {
            if (_outputReader == null)
                throw new InvalidOperationException("Process has not been started");
            
            return await _outputReader.ReadLineAsync(cancellationToken);
        }

        public async Task WriteLineAsync(string line, CancellationToken cancellationToken = default)
        {
            if (_inputWriter == null)
                throw new InvalidOperationException("Process has not been started");
            
            await _inputWriter.WriteLineAsync(line.AsMemory(), cancellationToken);
        }

        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            if (_inputWriter == null)
                throw new InvalidOperationException("Process has not been started");
            
            await _inputWriter.FlushAsync();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

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