using Core.Application.Models;

namespace Core.Application.Interfaces
{
    /// <summary>
    /// Interface for managing query and stored procedure sessions.
    /// </summary>
    public interface IQuerySessionManager
    {
        /// <summary>
        /// Starts a new query execution session in the background.
        /// </summary>
        /// <param name="query">The SQL query to execute</param>
        /// <param name="databaseName">Optional database name to execute the query in</param>
        /// <param name="timeoutSeconds">Timeout in seconds for the query execution</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>The created query session</returns>
        Task<QuerySession> StartQueryAsync(string query, string? databaseName, int timeoutSeconds, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Starts a new stored procedure execution session in the background.
        /// </summary>
        /// <param name="procedureName">The name of the stored procedure to execute</param>
        /// <param name="parameters">Parameters for the stored procedure</param>
        /// <param name="databaseName">Optional database name to execute the procedure in</param>
        /// <param name="timeoutSeconds">Timeout in seconds for the procedure execution</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>The created query session</returns>
        Task<QuerySession> StartStoredProcedureAsync(string procedureName, Dictionary<string, object?>? parameters, string? databaseName, int timeoutSeconds, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets a session by its ID.
        /// </summary>
        /// <param name="sessionId">The session ID</param>
        /// <returns>The session if found, null otherwise</returns>
        QuerySession? GetSession(int sessionId);
        
        /// <summary>
        /// Cancels a running session.
        /// </summary>
        /// <param name="sessionId">The session ID to cancel</param>
        /// <returns>True if the session was cancelled, false if not found or already completed</returns>
        bool CancelSession(int sessionId);
        
        /// <summary>
        /// Gets all sessions, optionally including completed ones.
        /// </summary>
        /// <param name="includeCompleted">Whether to include completed sessions</param>
        /// <returns>Collection of query sessions</returns>
        IEnumerable<QuerySession> GetAllSessions(bool includeCompleted = false);
        
        /// <summary>
        /// Cleans up completed sessions that are older than the cleanup interval.
        /// </summary>
        /// <returns>A task representing the cleanup operation</returns>
        Task CleanupCompletedSessions();
    }
}