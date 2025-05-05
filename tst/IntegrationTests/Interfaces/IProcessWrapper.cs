namespace IntegrationTests.Interfaces
{
    public interface IProcessWrapper : IDisposable
    {
        void Start();
        bool HasExited { get; }
        Task<string?> ReadLineAsync(CancellationToken cancellationToken = default);
        Task WriteLineAsync(string line, CancellationToken cancellationToken = default);
        Task FlushAsync(CancellationToken cancellationToken = default);
    }
}