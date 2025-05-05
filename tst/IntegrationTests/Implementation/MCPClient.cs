using IntegrationTests.Interfaces;
using IntegrationTests.Models;
using Microsoft.Extensions.Logging;

namespace IntegrationTests.Implementation
{
    public class MCPClient : IMCPClient
    {
        private readonly IProcessWrapper _process;
        private readonly ILogger? _logger;
        private bool _disposed;

        public MCPClient(IProcessWrapper process, ILogger? logger = null)
        {
            _process = process;
            _logger = logger;
        }

        public void Start()
        {
            _logger?.LogInformation("Starting MCP client");
            _process.Start();
            _logger?.LogInformation("MCP client started");
        }

        public bool IsRunning => !_process.HasExited;

        public async Task<MCPResponse?> SendRequestAsync(MCPRequest request, CancellationToken cancellationToken = default)
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
                var response = MCPMessage.Deserialize<MCPResponse>(responseLine);
                
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
                return new MCPResponse
                {
                    Id = request.Id,
                    Error = new MCPError
                    {
                        Code = -32000,
                        Message = "Request timed out"
                    }
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error sending request: {ErrorMessage}", ex.Message);
                return new MCPResponse
                {
                    Id = request.Id,
                    Error = new MCPError
                    {
                        Code = -32000,
                        Message = $"Error: {ex.Message}"
                    }
                };
            }
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
                    _logger?.LogInformation("Disposing MCP client");
                    _process.Dispose();
                }

                _disposed = true;
            }
        }
    }
}