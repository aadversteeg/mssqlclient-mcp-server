using Core.Application.Models;

namespace Core.Application.Interfaces
{
    /// <summary>
    /// Interface for core database operations with context switching capability.
    /// </summary>
    public interface IDatabaseService
    {
        /// <summary>
        /// Lists all tables in the database with optional database context switching.
        /// </summary>
        /// <param name="databaseName">Optional database name to switch context. If null, uses current database.</param>
        /// <param name="timeoutContext">Tool call timeout context for calculating remaining time</param>
        /// <param name="timeoutSeconds">Optional timeout in seconds. If null, uses default timeout.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A collection of table information</returns>
        Task<IEnumerable<TableInfo>> ListTablesAsync(string? databaseName = null, ToolCallTimeoutContext? timeoutContext = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists all databases on the server.
        /// </summary>
        /// <param name="timeoutContext">Tool call timeout context for calculating remaining time</param>
        /// <param name="timeoutSeconds">Optional timeout in seconds. If null, uses default timeout.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A collection of database information</returns>
        Task<IEnumerable<DatabaseInfo>> ListDatabasesAsync(ToolCallTimeoutContext? timeoutContext = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the schema information for a specific table with optional database context switching.
        /// </summary>
        /// <param name="tableName">The name of the table to get schema for</param>
        /// <param name="databaseName">Optional database name to switch context. If null, uses current database.</param>
        /// <param name="timeoutContext">Tool call timeout context for calculating remaining time</param>
        /// <param name="timeoutSeconds">Optional timeout in seconds. If null, uses default timeout.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Table schema information</returns>
        Task<TableSchemaInfo> GetTableSchemaAsync(string tableName, string? databaseName = null, ToolCallTimeoutContext? timeoutContext = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a database exists and is accessible.
        /// </summary>
        /// <param name="databaseName">Name of the database to check</param>
        /// <param name="timeoutContext">Tool call timeout context for calculating remaining time</param>
        /// <param name="timeoutSeconds">Optional timeout in seconds. If null, uses default timeout.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>True if the database exists and is accessible, otherwise false</returns>
        Task<bool> DoesDatabaseExistAsync(string databaseName, ToolCallTimeoutContext? timeoutContext = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current database name from the connection string.
        /// </summary>
        /// <returns>The current database name</returns>
        string GetCurrentDatabaseName();
        
        /// <summary>
        /// Executes a database query with optional database context switching.
        /// </summary>
        /// <param name="query">The database query to execute</param>
        /// <param name="databaseName">Optional database name to switch context. If null, uses current database.</param>
        /// <param name="timeoutContext">Tool call timeout context for calculating remaining time</param>
        /// <param name="timeoutSeconds">Optional timeout in seconds. If null, uses default timeout.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>An IAsyncDataReader with the results of the query</returns>
        Task<IAsyncDataReader> ExecuteQueryAsync(string query, string? databaseName = null, ToolCallTimeoutContext? timeoutContext = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Lists all stored procedures with optional database context switching.
        /// </summary>
        /// <param name="databaseName">Optional database name to switch context. If null, uses current database.</param>
        /// <param name="timeoutContext">Tool call timeout context for calculating remaining time</param>
        /// <param name="timeoutSeconds">Optional timeout in seconds. If null, uses default timeout.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A collection of stored procedure information</returns>
        Task<IEnumerable<StoredProcedureInfo>> ListStoredProceduresAsync(string? databaseName = null, ToolCallTimeoutContext? timeoutContext = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the definition information for a specific stored procedure with optional database context switching.
        /// </summary>
        /// <param name="procedureName">The name of the stored procedure</param>
        /// <param name="databaseName">Optional database name to switch context. If null, uses current database.</param>
        /// <param name="timeoutContext">Tool call timeout context for calculating remaining time</param>
        /// <param name="timeoutSeconds">Optional timeout in seconds. If null, uses default timeout.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Stored procedure definition as text</returns>
        Task<string> GetStoredProcedureDefinitionAsync(string procedureName, string? databaseName = null, ToolCallTimeoutContext? timeoutContext = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Executes a stored procedure with optional database context switching.
        /// </summary>
        /// <param name="procedureName">The name of the stored procedure to execute</param>
        /// <param name="parameters">Dictionary of parameter names and values</param>
        /// <param name="databaseName">Optional database name to switch context. If null, uses current database.</param>
        /// <param name="timeoutContext">Tool call timeout context for calculating remaining time</param>
        /// <param name="timeoutSeconds">Optional timeout in seconds. If null, uses default timeout.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>An IAsyncDataReader with the results of the stored procedure</returns>
        Task<IAsyncDataReader> ExecuteStoredProcedureAsync(string procedureName, Dictionary<string, object?> parameters, string? databaseName = null, ToolCallTimeoutContext? timeoutContext = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
    }
}