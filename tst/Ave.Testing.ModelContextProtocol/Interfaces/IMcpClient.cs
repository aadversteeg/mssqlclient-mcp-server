using Ave.Testing.ModelContextProtocol.Models;

namespace Ave.Testing.ModelContextProtocol.Interfaces
{
    /// <summary>
    /// Represents a client for interacting with an MCP server
    /// </summary>
    public interface IMcpClient : IDisposable
    {
        /// <summary>
        /// Starts the MCP client
        /// </summary>
        void Start();

        /// <summary>
        /// Sends a request to the MCP server and awaits a response
        /// </summary>
        /// <param name="request">The request to send</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>The response from the server, or null if no response was received</returns>
        Task<McpResponse?> SendRequestAsync(McpRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a value indicating whether the client is running
        /// </summary>
        bool IsRunning { get; }
    }
}