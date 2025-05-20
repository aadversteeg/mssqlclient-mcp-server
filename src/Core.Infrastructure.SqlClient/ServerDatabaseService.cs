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
        
        /// <summary>
        /// Initializes a new instance of the ServerDatabaseService class with an existing database service.
        /// </summary>
        /// <param name="databaseService">The database service to use</param>
        public ServerDatabaseService(IDatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            
            // Server mode works with any database connection, not just master database
            // This allows using server mode with any database name in connection string
        }

        /// <summary>
        /// Lists all tables in the specified database.
        /// </summary>
        /// <param name="databaseName">Name of the database</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A collection of table information</returns>
        public async Task<IEnumerable<TableInfo>> ListTablesAsync(string databaseName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Database name cannot be empty", nameof(databaseName));

            // First verify the database exists and is accessible
            if (!await DoesDatabaseExistAsync(databaseName, cancellationToken))
                throw new InvalidOperationException($"Database '{databaseName}' does not exist or is not accessible");

            // Use the database service with the specified database name to change context
            return await _databaseService.ListTablesAsync(databaseName, cancellationToken);
        }

        /// <summary>
        /// Lists all databases on the server.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A collection of database information</returns>
        public async Task<IEnumerable<DatabaseInfo>> ListDatabasesAsync(CancellationToken cancellationToken = default)
        {
            return await _databaseService.ListDatabasesAsync(cancellationToken);
        }

        /// <summary>
        /// Gets the schema information for a specific table in the specified database.
        /// </summary>
        /// <param name="databaseName">Name of the database containing the table</param>
        /// <param name="tableName">The name of the table to get schema for</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Table schema information</returns>
        public async Task<TableSchemaInfo> GetTableSchemaAsync(string databaseName, string tableName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Database name cannot be empty", nameof(databaseName));

            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be empty", nameof(tableName));

            // First verify the database exists and is accessible
            if (!await DoesDatabaseExistAsync(databaseName, cancellationToken))
                throw new InvalidOperationException($"Database '{databaseName}' does not exist or is not accessible");

            // Use the database service with the specified database name to get the table schema
            return await _databaseService.GetTableSchemaAsync(tableName, databaseName, cancellationToken);
        }
        
        /// <summary>
        /// Checks if a database exists and is accessible.
        /// </summary>
        /// <param name="databaseName">Name of the database to check</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>True if the database exists and is accessible, otherwise false</returns>
        public async Task<bool> DoesDatabaseExistAsync(string databaseName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Database name cannot be empty", nameof(databaseName));
                
            return await _databaseService.DoesDatabaseExistAsync(databaseName, cancellationToken);
        }
        
        /// <summary>
        /// Executes a SQL query in the specified database.
        /// </summary>
        /// <param name="databaseName">Name of the database to execute the query in</param>
        /// <param name="query">The SQL query to execute</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>An IAsyncDataReader with the results of the query</returns>
        public async Task<IAsyncDataReader> ExecuteQueryInDatabaseAsync(string databaseName, string query, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Database name cannot be empty", nameof(databaseName));
                
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query cannot be empty", nameof(query));
                
            // First verify the database exists and is accessible
            if (!await DoesDatabaseExistAsync(databaseName, cancellationToken))
                throw new InvalidOperationException($"Database '{databaseName}' does not exist or is not accessible");
                
            // Use the database service with the specified database name to execute the query
            return await _databaseService.ExecuteQueryAsync(query, databaseName, cancellationToken);
        }
    }
}