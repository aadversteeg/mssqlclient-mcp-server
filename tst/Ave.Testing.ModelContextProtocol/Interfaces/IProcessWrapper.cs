namespace Ave.Testing.ModelContextProtocol.Interfaces
{
    /// <summary>
    /// Wraps a process for communication via standard input/output
    /// </summary>
    public interface IProcessWrapper : IDisposable
    {
        /// <summary>
        /// Starts the process
        /// </summary>
        void Start();

        /// <summary>
        /// Gets a value indicating whether the process has exited
        /// </summary>
        bool HasExited { get; }

        /// <summary>
        /// Gets or sets the working directory for the process
        /// </summary>
        string? WorkingDirectory { get; set; }

        /// <summary>
        /// Gets or sets the arguments for the process
        /// </summary>
        string? Arguments { get; set; }

        /// <summary>
        /// Reads a line from the process's standard output
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>The line read, or null if the end of the stream was reached</returns>
        Task<string?> ReadLineAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Writes a line to the process's standard input
        /// </summary>
        /// <param name="line">The line to write</param>
        /// <param name="cancellationToken">A cancellation token</param>
        Task WriteLineAsync(string line, CancellationToken cancellationToken = default);

        /// <summary>
        /// Flushes the standard input
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        Task FlushAsync(CancellationToken cancellationToken = default);
    }
}