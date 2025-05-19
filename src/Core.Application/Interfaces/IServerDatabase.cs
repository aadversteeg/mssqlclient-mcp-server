using Core.Application.Models;
using Core.Application.Interfaces;

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
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A collection of table information</returns>
        Task<IEnumerable<TableInfo>> ListTablesAsync(string databaseName, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Lists all databases on the server.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A collection of database information</returns>
        Task<IEnumerable<DatabaseInfo>> ListDatabasesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the schema information for a specific table in the specified database.
        /// </summary>
        /// <param name="databaseName">Name of the database containing the table</param>
        /// <param name="tableName">The name of the table to get schema for</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Table schema information</returns>
        Task<TableSchemaInfo> GetTableSchemaAsync(string databaseName, string tableName, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Checks if a database exists and is accessible.
        /// </summary>
        /// <param name="databaseName">Name of the database to check</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>True if the database exists and is accessible, otherwise false</returns>
        Task<bool> DoesDatabaseExistAsync(string databaseName, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Executes a SQL query in the specified database.
        /// </summary>
        /// <param name="databaseName">Name of the database to execute the query in</param>
        /// <param name="query">The SQL query to execute</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>An IAsyncDataReader with the results of the query</returns>
        Task<IAsyncDataReader> ExecuteQueryInDatabaseAsync(string databaseName, string query, CancellationToken cancellationToken = default);
    }
}