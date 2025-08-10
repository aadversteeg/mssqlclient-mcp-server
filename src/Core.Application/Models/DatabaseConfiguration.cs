namespace Core.Application.Models
{
    public class DatabaseConfiguration
    {
        public string ConnectionString { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets whether the execute query tools should be enabled.
        /// When false, the execute_query and execute_query_in_database tools will not be registered.
        /// Default is false.
        /// </summary>
        public bool EnableExecuteQuery { get; set; } = false;
        
        /// <summary>
        /// Gets or sets whether the execute stored procedure tools should be enabled.
        /// When false, the execute_stored_procedure and execute_stored_procedure_in_database tools will not be registered.
        /// Default is false.
        /// </summary>
        public bool EnableExecuteStoredProcedure { get; set; } = false;
        
        /// <summary>
        /// Gets or sets whether the start query session tools should be enabled.
        /// When false, the start_query and start_query_in_database tools will not be registered.
        /// Default is false.
        /// </summary>
        public bool EnableStartQuery { get; set; } = false;
        
        /// <summary>
        /// Gets or sets whether the start stored procedure session tools should be enabled.
        /// When false, the start_stored_procedure and start_stored_procedure_in_database tools will not be registered.
        /// Default is false.
        /// </summary>
        public bool EnableStartStoredProcedure { get; set; } = false;
        
        /// <summary>
        /// Gets or sets the default command timeout in seconds for SQL operations.
        /// Default is 30 seconds.
        /// </summary>
        public int DefaultCommandTimeoutSeconds { get; set; } = 30;
        
        /// <summary>
        /// Gets or sets the connection timeout in seconds for SQL connections.
        /// Default is 15 seconds.
        /// </summary>
        public int ConnectionTimeoutSeconds { get; set; } = 15;
        
        /// <summary>
        /// Gets or sets the maximum number of concurrent query sessions allowed.
        /// Default is 10.
        /// </summary>
        public int MaxConcurrentSessions { get; set; } = 10;
        
        /// <summary>
        /// Gets or sets the interval in minutes for cleaning up completed sessions.
        /// Default is 60 minutes.
        /// </summary>
        public int SessionCleanupIntervalMinutes { get; set; } = 60;
        
        /// <summary>
        /// Gets or sets the total timeout in seconds for tool calls. When set, all operations
        /// within a tool call must complete within this time limit. The remaining time is used
        /// to calculate individual command timeouts. Default is 120 seconds. Set to null to
        /// disable total timeout (preserves backward compatibility).
        /// </summary>
        public int? TotalToolCallTimeoutSeconds { get; set; } = 120;
    }
}
