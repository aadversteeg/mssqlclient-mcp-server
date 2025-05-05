using Core.Application.Models;
using Core.Application.Interfaces;

namespace Core.Application.Interfaces
{
    /// <summary>
    /// Interface for user database operations.
    /// Provides access to tables in the currently connected database.
    /// </summary>
    public interface IUserDatabase
    {
        /// <summary>
        /// Lists all tables in the current database.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A collection of table information</returns>
        Task<IEnumerable<TableInfo>> ListTablesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the schema information for a specific table in the current database.
        /// </summary>
        /// <param name="tableName">The name of the table to get schema for</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Table schema information</returns>
        Task<TableSchemaInfo> GetTableSchemaAsync(string tableName, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Executes a SQL query in the current database.
        /// </summary>
        /// <param name="query">The SQL query to execute</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>An IAsyncDataReader with the results of the query</returns>
        Task<IAsyncDataReader> ExecuteQueryAsync(string query, CancellationToken cancellationToken = default);
    }
}