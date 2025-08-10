using System.Text;

namespace Core.Application.Models
{
    /// <summary>
    /// Represents a query or stored procedure session with its state and results.
    /// </summary>
    public class QuerySession
    {
        /// <summary>
        /// Unique identifier for the session.
        /// </summary>
        public int SessionId { get; set; }
        
        /// <summary>
        /// The SQL query or stored procedure name being executed.
        /// </summary>
        public string Query { get; set; } = string.Empty;
        
        /// <summary>
        /// The type of operation (Query or StoredProcedure).
        /// </summary>
        public QuerySessionType Type { get; set; }
        
        /// <summary>
        /// When the session was started.
        /// </summary>
        public DateTime StartTime { get; set; }
        
        /// <summary>
        /// When the session completed (null if still running).
        /// </summary>
        public DateTime? EndTime { get; set; }
        
        /// <summary>
        /// Whether the session is currently running.
        /// </summary>
        public bool IsRunning { get; set; }
        
        /// <summary>
        /// Error message if the session failed.
        /// </summary>
        public string? Error { get; set; }
        
        /// <summary>
        /// Number of rows processed.
        /// </summary>
        public long RowCount { get; set; }
        
        /// <summary>
        /// Accumulated results from the query.
        /// </summary>
        public StringBuilder Results { get; set; } = new();
        
        /// <summary>
        /// Cancellation token source for stopping the session.
        /// </summary>
        public CancellationTokenSource? CancellationToken { get; set; }
        
        /// <summary>
        /// Optional database name where the query is executed.
        /// </summary>
        public string? DatabaseName { get; set; }
        
        /// <summary>
        /// Parameters for stored procedure execution.
        /// </summary>
        public Dictionary<string, object?>? Parameters { get; set; }
        
        /// <summary>
        /// Command timeout for this specific session.
        /// </summary>
        public int TimeoutSeconds { get; set; }
    }
}