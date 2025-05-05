using IntegrationTests.Models;

namespace IntegrationTests.Interfaces
{
    public interface IMCPClient : IDisposable
    {
        void Start();
        Task<MCPResponse?> SendRequestAsync(MCPRequest request, CancellationToken cancellationToken = default);
        bool IsRunning { get; }
    }
}