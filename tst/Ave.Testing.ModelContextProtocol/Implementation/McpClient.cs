using Ave.Testing.ModelContextProtocol.Interfaces;
using Ave.Testing.ModelContextProtocol.Models;
using Microsoft.Extensions.Logging;

namespace Ave.Testing.ModelContextProtocol.Implementation
{
    /// <summary>
    /// Client for interacting with an MCP server
    /// </summary>
    public class McpClient : IMcpClient
    {
        private readonly IProcessWrapper _process;
        private readonly ILogger? _logger;
        private bool _disposed;

        /// <summary>
        /// Creates a new MCP client
        /// </summary>
        /// <param name="process">The process wrapper</param>
        /// <param name="logger">Optional logger</param>
        public McpClient(IProcessWrapper process, ILogger? logger = null)
        {
            _process = process;
            _logger = logger;
        }

        /// <summary>
        /// Starts the MCP client
        /// </summary>
        public void Start()
        {
            _logger?.LogInformation("Starting MCP client");
            _process.Start();
            _logger?.LogInformation("MCP client started");
        }

        /// <summary>
        /// Gets a value indicating whether the client is running
        /// </summary>
        public bool IsRunning => !_process.HasExited;

        /// <summary>
        /// Sends a request to the MCP server and awaits a response
        /// </summary>
        /// <param name="request">The request to send</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>The response from the server, or null if no response was received</returns>
        public async Task<McpResponse?> SendRequestAsync(McpRequest request, CancellationToken cancellationToken = default)
        {
            if (!IsRunning)
            {
                _logger?.LogError("Cannot send request - process is not running");
                throw new InvalidOperationException("Process is not running.");
            }

            // Create a timeout token source
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                timeoutCts.Token, cancellationToken);
            
            try
            {
                // Serialize and send the request
                string requestJson = request.Serialize();
                _logger?.LogInformation("Sending request: {RequestJson}", requestJson);
                await _process.WriteLineAsync(requestJson, linkedCts.Token);
                await _process.FlushAsync(linkedCts.Token);

                // Read the response
                string? responseLine = await _process.ReadLineAsync(linkedCts.Token);
                _logger?.LogInformation("Received response: {ResponseLine}", responseLine);
                
                if (string.IsNullOrEmpty(responseLine))
                {
                    _logger?.LogWarning("Received empty response");
                    return null;
                }

                // Deserialize the response
                var response = McpMessage.Deserialize<McpResponse>(responseLine);
                
                // Log more details about the response
                if (response != null)
                {
                    if (response.Error != null)
                    {
                        _logger?.LogWarning("Error in response: {ErrorCode} - {ErrorMessage}", 
                            response.Error.Code, response.Error.Message);
                    }
                    else if (response.Result != null)
                    {
                        _logger?.LogInformation("Response result type: {ResultType}", 
                            response.Result.GetType().Name);
                    }
                    else
                    {
                        _logger?.LogInformation("Response has no result (null)");
                    }
                }
                else
                {
                    _logger?.LogWarning("Failed to deserialize response");
                }
                
                return response;
            }
            catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
            {
                _logger?.LogWarning("Request timed out after 5 seconds");
                return new McpResponse
                {
                    Id = request.Id,
                    Error = new McpError
                    {
                        Code = -32000,
                        Message = "Request timed out"
                    }
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error sending request: {ErrorMessage}", ex.Message);
                return new McpResponse
                {
                    Id = request.Id,
                    Error = new McpError
                    {
                        Code = -32000,
                        Message = $"Error: {ex.Message}"
                    }
                };
            }
        }

        /// <summary>
        /// Disposes the MCP client
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the MCP client
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _logger?.LogInformation("Disposing MCP client");
                    _process.Dispose();
                }

                _disposed = true;
            }
        }
    }
}