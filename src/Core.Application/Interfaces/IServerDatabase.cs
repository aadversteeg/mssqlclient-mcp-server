using Core.Application.Models;
using Core.Application.Interfaces;
using System.Collections.Generic;

namespace Core.Application.Interfaces
{
    /// <summary>
    /// Interface for server-level database operations.
    /// Provides access to server-wide operations and cross-database queries.
    /// </summary>
    public interface IServerDatabase
    {
        
        /// <summary>
        /// Lists all tables in the specified database.
        /// </summary>
        /// <param name="databaseName">Name of the database</param>
        /// <param name="timeoutContext">Tool call timeout context for calculating remaining time</param>
        /// <param name="timeoutSeconds">Optional timeout in seconds. If null, uses timeout context or default timeout.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A collection of table information</returns>
        Task<IEnumerable<TableInfo>> ListTablesAsync(string databaseName, ToolCallTimeoutContext? timeoutContext, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Lists all databases on the server.
        /// </summary>
        /// <param name="timeoutContext">Tool call timeout context for calculating remaining time</param>
        /// <param name="timeoutSeconds">Optional timeout in seconds. If null, uses timeout context or default timeout.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A collection of database information</returns>
        Task<IEnumerable<DatabaseInfo>> ListDatabasesAsync(ToolCallTimeoutContext? timeoutContext, int? timeoutSeconds = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the schema information for a specific table in the specified database.
        /// </summary>
        /// <param name="databaseName">Name of the database containing the table</param>
        /// <param name="tableName">The name of the table to get schema for</param>
        /// <param name="timeoutContext">Tool call timeout context for calculating remaining time</param>
        /// <param name="timeoutSeconds">Optional timeout in seconds. If null, uses timeout context or default timeout.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Table schema information</returns>
        Task<TableSchemaInfo> GetTableSchemaAsync(string databaseName, string tableName, ToolCallTimeoutContext? timeoutContext, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Executes a SQL query in the specified database.
        /// </summary>
        /// <param name="databaseName">Name of the database to execute the query in</param>
        /// <param name="query">The SQL query to execute</param>
        /// <param name="timeoutContext">Tool call timeout context for calculating remaining time</param>
        /// <param name="timeoutSeconds">Optional timeout in seconds. If null, uses timeout context or default timeout.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>An IAsyncDataReader with the results of the query</returns>
        Task<IAsyncDataReader> ExecuteQueryInDatabaseAsync(string databaseName, string query, ToolCallTimeoutContext? timeoutContext, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Lists all stored procedures in the specified database.
        /// </summary>
        /// <param name="databaseName">Name of the database</param>
        /// <param name="timeoutContext">Tool call timeout context for calculating remaining time</param>
        /// <param name="timeoutSeconds">Optional timeout in seconds. If null, uses timeout context or default timeout.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A collection of stored procedure information</returns>
        Task<IEnumerable<StoredProcedureInfo>> ListStoredProceduresAsync(string databaseName, ToolCallTimeoutContext? timeoutContext, int? timeoutSeconds = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the definition information for a specific stored procedure in the specified database.
        /// </summary>
        /// <param name="databaseName">Name of the database containing the stored procedure</param>
        /// <param name="procedureName">The name of the stored procedure</param>
        /// <param name="timeoutContext">Tool call timeout context for calculating remaining time</param>
        /// <param name="timeoutSeconds">Optional timeout in seconds. If null, uses timeout context or default timeout.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Stored procedure definition as SQL string</returns>
        Task<string> GetStoredProcedureDefinitionAsync(string databaseName, string procedureName, ToolCallTimeoutContext? timeoutContext, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Executes a stored procedure in the specified database.
        /// </summary>
        /// <param name="databaseName">Name of the database to execute the stored procedure in</param>
        /// <param name="procedureName">The name of the stored procedure to execute</param>
        /// <param name="parameters">Dictionary of parameter names and values</param>
        /// <param name="timeoutContext">Tool call timeout context for calculating remaining time</param>
        /// <param name="timeoutSeconds">Optional timeout in seconds. If null, uses timeout context or default timeout.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>An IAsyncDataReader with the results of the stored procedure</returns>
        Task<IAsyncDataReader> ExecuteStoredProcedureAsync(string databaseName, string procedureName, Dictionary<string, object?> parameters, ToolCallTimeoutContext? timeoutContext, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Checks if a database exists and is accessible.
        /// </summary>
        /// <param name="databaseName">Name of the database to check</param>
        /// <param name="timeoutContext">Tool call timeout context for calculating remaining time</param>
        /// <param name="timeoutSeconds">Optional timeout in seconds. If null, uses timeout context or default timeout.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>True if the database exists and is accessible, otherwise false</returns>
        Task<bool> DoesDatabaseExistAsync(string databaseName, ToolCallTimeoutContext? timeoutContext, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
    }
}
