using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Infrastructure.SqlClient.Interfaces;

namespace Core.Infrastructure.SqlClient
{
    /// <summary>
    /// Implementation of the IServerDatabase interface for SQL Server operations at server level.
    /// </summary>
    public class ServerDatabaseService : IServerDatabase
    {
        private readonly IDatabaseService _databaseService;
        private readonly DatabaseConfiguration? _configuration;
        
        /// <summary>
        /// Initializes a new instance of the ServerDatabaseService class with an existing database service.
        /// </summary>
        /// <param name="databaseService">The database service to use</param>
        public ServerDatabaseService(IDatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _configuration = null; // Configuration not available in this constructor
            
            // Server mode works with any database connection, not just master database
            // This allows using server mode with any database name in connection string
        }
        
        /// <summary>
        /// Initializes a new instance of the ServerDatabaseService class with an existing database service and configuration.
        /// </summary>
        /// <param name="databaseService">The database service to use</param>
        /// <param name="configuration">Database configuration with timeout settings</param>
        public ServerDatabaseService(IDatabaseService databaseService, DatabaseConfiguration configuration)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        
        /// <summary>
        /// Lists all tables in the specified database.
        /// </summary>
        /// <param name="databaseName">Name of the database</param>
        /// <param name="timeoutContext">Tool call timeout context for calculating remaining time</param>
        /// <param name="timeoutSeconds">Optional timeout in seconds. If null, uses timeout context or default timeout.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A collection of table information</returns>
        public async Task<IEnumerable<TableInfo>> ListTablesAsync(string databaseName, ToolCallTimeoutContext? timeoutContext, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Database name cannot be empty", nameof(databaseName));

            var effectiveTimeout = CalculateEffectiveTimeout(timeoutContext, timeoutSeconds);
            var combinedToken = ToolCallTimeoutFactory.CombineTokens(timeoutContext, cancellationToken);
            
            // Check if timeout already exceeded
            if (timeoutContext?.IsTimeoutExceeded == true)
                throw new OperationCanceledException(timeoutContext.CreateTimeoutExceededMessage());

            // First verify the database exists and is accessible
            if (!await DoesDatabaseExistAsync(databaseName, timeoutContext, effectiveTimeout, combinedToken))
                throw new InvalidOperationException($"Database '{databaseName}' does not exist or is not accessible");

            // Use the database service with the specified database name to change context
            return await _databaseService.ListTablesAsync(databaseName, null, effectiveTimeout, combinedToken);
        }
        
        /// <summary>
        /// Lists all databases on the server .
        /// </summary>
        /// <param name="timeoutContext">Tool call timeout context for calculating remaining time</param>
        /// <param name="timeoutSeconds">Optional timeout in seconds. If null, uses timeout context or default timeout.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A collection of database information</returns>
        public async Task<IEnumerable<DatabaseInfo>> ListDatabasesAsync(ToolCallTimeoutContext? timeoutContext, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        {
            var effectiveTimeout = CalculateEffectiveTimeout(timeoutContext, timeoutSeconds);
            var combinedToken = ToolCallTimeoutFactory.CombineTokens(timeoutContext, cancellationToken);
            
            // Check if timeout already exceeded
            if (timeoutContext?.IsTimeoutExceeded == true)
                throw new OperationCanceledException(timeoutContext.CreateTimeoutExceededMessage());
                
            return await _databaseService.ListDatabasesAsync(null, effectiveTimeout, combinedToken);
        }

        /// <summary>
        /// Gets the schema information for a specific table in the specified database .
        /// </summary>
        /// <param name="databaseName">Name of the database containing the table</param>
        /// <param name="tableName">The name of the table to get schema for</param>
        /// <param name="timeoutContext">Tool call timeout context for calculating remaining time</param>
        /// <param name="timeoutSeconds">Optional timeout in seconds. If null, uses timeout context or default timeout.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Table schema information</returns>
        public async Task<TableSchemaInfo> GetTableSchemaAsync(string databaseName, string tableName, ToolCallTimeoutContext? timeoutContext, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Database name cannot be empty", nameof(databaseName));

            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be empty", nameof(tableName));

            var effectiveTimeout = CalculateEffectiveTimeout(timeoutContext, timeoutSeconds);
            var combinedToken = ToolCallTimeoutFactory.CombineTokens(timeoutContext, cancellationToken);
            
            // Check if timeout already exceeded
            if (timeoutContext?.IsTimeoutExceeded == true)
                throw new OperationCanceledException(timeoutContext.CreateTimeoutExceededMessage());

            // First verify the database exists and is accessible
            if (!await DoesDatabaseExistAsync(databaseName, timeoutContext, effectiveTimeout, combinedToken))
                throw new InvalidOperationException($"Database '{databaseName}' does not exist or is not accessible");

            // Use the database service with the specified database name to get the table schema
            return await _databaseService.GetTableSchemaAsync(tableName, databaseName, null, effectiveTimeout, combinedToken);
        }
        
        /// <summary>
        /// Executes a SQL query in the specified database .
        /// </summary>
        /// <param name="databaseName">Name of the database to execute the query in</param>
        /// <param name="query">The SQL query to execute</param>
        /// <param name="timeoutContext">Tool call timeout context for calculating remaining time</param>
        /// <param name="timeoutSeconds">Optional timeout in seconds. If null, uses timeout context or default timeout.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>An IAsyncDataReader with the results of the query</returns>
        public async Task<IAsyncDataReader> ExecuteQueryInDatabaseAsync(string databaseName, string query, ToolCallTimeoutContext? timeoutContext, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Database name cannot be empty", nameof(databaseName));
                
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query cannot be empty", nameof(query));

            var effectiveTimeout = CalculateEffectiveTimeout(timeoutContext, timeoutSeconds);
            var combinedToken = ToolCallTimeoutFactory.CombineTokens(timeoutContext, cancellationToken);
            
            // Check if timeout already exceeded
            if (timeoutContext?.IsTimeoutExceeded == true)
                throw new OperationCanceledException(timeoutContext.CreateTimeoutExceededMessage());
                
            // First verify the database exists and is accessible
            if (!await DoesDatabaseExistAsync(databaseName, timeoutContext, effectiveTimeout, combinedToken))
                throw new InvalidOperationException($"Database '{databaseName}' does not exist or is not accessible");
                
            // Use the database service with the specified database name to execute the query
            return await _databaseService.ExecuteQueryAsync(query, databaseName, null, effectiveTimeout, combinedToken);
        }
        
        /// <summary>
        /// Lists all stored procedures in the specified database .
        /// </summary>
        /// <param name="databaseName">Name of the database</param>
        /// <param name="timeoutContext">Tool call timeout context for calculating remaining time</param>
        /// <param name="timeoutSeconds">Optional timeout in seconds. If null, uses timeout context or default timeout.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A collection of stored procedure information</returns>
        public async Task<IEnumerable<StoredProcedureInfo>> ListStoredProceduresAsync(string databaseName, ToolCallTimeoutContext? timeoutContext, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Database name cannot be empty", nameof(databaseName));

            var effectiveTimeout = CalculateEffectiveTimeout(timeoutContext, timeoutSeconds);
            var combinedToken = ToolCallTimeoutFactory.CombineTokens(timeoutContext, cancellationToken);
            
            // Check if timeout already exceeded
            if (timeoutContext?.IsTimeoutExceeded == true)
                throw new OperationCanceledException(timeoutContext.CreateTimeoutExceededMessage());

            // First verify the database exists and is accessible
            if (!await DoesDatabaseExistAsync(databaseName, timeoutContext, effectiveTimeout, combinedToken))
                throw new InvalidOperationException($"Database '{databaseName}' does not exist or is not accessible");

            // Use the database service with the specified database name to change context
            return await _databaseService.ListStoredProceduresAsync(databaseName, null, effectiveTimeout, combinedToken);
        }

        /// <summary>
        /// Gets the definition information for a specific stored procedure in the specified database .
        /// </summary>
        /// <param name="databaseName">Name of the database containing the stored procedure</param>
        /// <param name="procedureName">The name of the stored procedure</param>
        /// <param name="timeoutContext">Tool call timeout context for calculating remaining time</param>
        /// <param name="timeoutSeconds">Optional timeout in seconds. If null, uses timeout context or default timeout.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Stored procedure definition as SQL string</returns>
        public async Task<string> GetStoredProcedureDefinitionAsync(string databaseName, string procedureName, ToolCallTimeoutContext? timeoutContext, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Database name cannot be empty", nameof(databaseName));

            if (string.IsNullOrWhiteSpace(procedureName))
                throw new ArgumentException("Procedure name cannot be empty", nameof(procedureName));

            var effectiveTimeout = CalculateEffectiveTimeout(timeoutContext, timeoutSeconds);
            var combinedToken = ToolCallTimeoutFactory.CombineTokens(timeoutContext, cancellationToken);
            
            // Check if timeout already exceeded
            if (timeoutContext?.IsTimeoutExceeded == true)
                throw new OperationCanceledException(timeoutContext.CreateTimeoutExceededMessage());

            // First verify the database exists and is accessible
            if (!await DoesDatabaseExistAsync(databaseName, timeoutContext, effectiveTimeout, combinedToken))
                throw new InvalidOperationException($"Database '{databaseName}' does not exist or is not accessible");

            // Use the database service with the specified database name to get the stored procedure definition
            return await _databaseService.GetStoredProcedureDefinitionAsync(procedureName, databaseName, null, effectiveTimeout, combinedToken);
        }
        
        /// <summary>
        /// Executes a stored procedure in the specified database .
        /// </summary>
        /// <param name="databaseName">Name of the database to execute the stored procedure in</param>
        /// <param name="procedureName">The name of the stored procedure to execute</param>
        /// <param name="parameters">Dictionary of parameter names and values</param>
        /// <param name="timeoutContext">Tool call timeout context for calculating remaining time</param>
        /// <param name="timeoutSeconds">Optional timeout in seconds. If null, uses timeout context or default timeout.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>An IAsyncDataReader with the results of the stored procedure</returns>
        public async Task<IAsyncDataReader> ExecuteStoredProcedureAsync(string databaseName, string procedureName, Dictionary<string, object?> parameters, ToolCallTimeoutContext? timeoutContext, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Database name cannot be empty", nameof(databaseName));
                
            if (string.IsNullOrWhiteSpace(procedureName))
                throw new ArgumentException("Procedure name cannot be empty", nameof(procedureName));
                
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            var effectiveTimeout = CalculateEffectiveTimeout(timeoutContext, timeoutSeconds);
            var combinedToken = ToolCallTimeoutFactory.CombineTokens(timeoutContext, cancellationToken);
            
            // Check if timeout already exceeded
            if (timeoutContext?.IsTimeoutExceeded == true)
                throw new OperationCanceledException(timeoutContext.CreateTimeoutExceededMessage());
                
            // First verify the database exists and is accessible
            if (!await DoesDatabaseExistAsync(databaseName, timeoutContext, effectiveTimeout, combinedToken))
                throw new InvalidOperationException($"Database '{databaseName}' does not exist or is not accessible");
                
            // Use the database service with the specified database name to execute the stored procedure
            return await _databaseService.ExecuteStoredProcedureAsync(procedureName, parameters, databaseName, null, effectiveTimeout, combinedToken);
        }
        
        /// <summary>
        /// Checks if a database exists and is accessible .
        /// </summary>
        /// <param name="databaseName">Name of the database to check</param>
        /// <param name="timeoutContext">Tool call timeout context for calculating remaining time</param>
        /// <param name="timeoutSeconds">Optional timeout in seconds. If null, uses timeout context or default timeout.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>True if the database exists and is accessible, otherwise false</returns>
        public async Task<bool> DoesDatabaseExistAsync(string databaseName, ToolCallTimeoutContext? timeoutContext, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Database name cannot be empty", nameof(databaseName));

            var effectiveTimeout = CalculateEffectiveTimeout(timeoutContext, timeoutSeconds);
            var combinedToken = ToolCallTimeoutFactory.CombineTokens(timeoutContext, cancellationToken);
            
            // Check if timeout already exceeded
            if (timeoutContext?.IsTimeoutExceeded == true)
                throw new OperationCanceledException(timeoutContext.CreateTimeoutExceededMessage());
                
            return await _databaseService.DoesDatabaseExistAsync(databaseName, null, effectiveTimeout, combinedToken);
        }
        
        /// <summary>
        /// Calculates the effective timeout for a command based on the timeout context.
        /// </summary>
        /// <param name="timeoutContext">Tool call timeout context</param>
        /// <param name="timeoutSeconds">Optional explicit timeout</param>
        /// <returns>Effective timeout in seconds</returns>
        private int? CalculateEffectiveTimeout(ToolCallTimeoutContext? timeoutContext, int? timeoutSeconds)
        {
            // If explicit timeout is provided, use it
            if (timeoutSeconds.HasValue)
                return timeoutSeconds.Value;
                
            // If no timeout context, use null (will default to configuration)
            if (timeoutContext == null)
                return null;
                
            // Use timeout context to calculate effective timeout based on remaining time
            var defaultTimeout = _configuration?.DefaultCommandTimeoutSeconds ?? 30;
            return timeoutContext.GetEffectiveCommandTimeout(defaultTimeout);
        }
    }
}