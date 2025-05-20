using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Infrastructure.SqlClient;
using Core.Infrastructure.SqlClient.Interfaces;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Moq;
using Xunit;

namespace UnitTests.Infrastructure.SqlClient
{
    public class DatabaseServiceMockTests
    {
        // We're not testing DatabaseService directly since it has direct SQL dependencies
        // Instead, we'll verify behavior through the SpyDatabaseService implementation
        
        [Fact(DisplayName = "DBS-001: SpyDatabaseService implements IDatabaseService correctly")]
        public void DBS001()
        {
            // Arrange
            var service = new SpyDatabaseService();
            
            // Act & Assert
            service.Should().BeAssignableTo<IDatabaseService>();
        }
        
        // Note: For the following tests we would normally mock SqlConnection, SqlCommand, and SqlDataReader
        // However, these classes are not easily mockable due to being sealed classes.
        // In a real scenario, you might use a library like System.Data.SqlClient.TestDouble
        // or create a wrapper/adapter around these classes that can be mocked.
        
        [Fact(DisplayName = "DBS-002: IDatabaseService implementation")]
        public void DBS002()
        {
            // Arrange
            // Create a test database service with a dummy connection string
            // Just to verify that it implements the interface
            var dummyConnectionString = "Server=test;Database=dummy;Trusted_Connection=True;";
            var service = new DatabaseService(dummyConnectionString);
            
            // Act & Assert
            service.Should().BeAssignableTo<IDatabaseService>();
        }
    }
    
    // This is a spy implementation that records calls but doesn't execute real SQL
    public class SpyDatabaseService : IDatabaseService
    {
        public bool ListTablesAsyncCalled { get; private set; }
        public string? DatabaseNamePassedToListTables { get; private set; }
        public CancellationToken TokenPassedToListTables { get; private set; }
        
        public bool ListDatabasesAsyncCalled { get; private set; }
        public CancellationToken TokenPassedToListDatabases { get; private set; }
        
        public bool DoesDatabaseExistAsyncCalled { get; private set; }
        public string? DatabaseNamePassedToDatabaseExists { get; private set; }
        public CancellationToken TokenPassedToDatabaseExists { get; private set; }
        
        public bool GetCurrentDatabaseNameCalled { get; private set; }
        
        public bool GetTableSchemaAsyncCalled { get; private set; }
        public string? TableNamePassedToGetTableSchema { get; private set; }
        public string? DatabaseNamePassedToGetTableSchema { get; private set; }
        public CancellationToken TokenPassedToGetTableSchema { get; private set; }
        
        public bool ExecuteQueryAsyncCalled { get; private set; }
        public string? QueryPassedToExecuteQuery { get; private set; }
        public string? DatabaseNamePassedToExecuteQuery { get; private set; }
        public CancellationToken TokenPassedToExecuteQuery { get; private set; }
        
        // Mock responses
        public List<TableInfo> TablesResponse { get; set; } = new List<TableInfo>();
        public List<DatabaseInfo> DatabasesResponse { get; set; } = new List<DatabaseInfo>();
        public bool DatabaseExistsResponse { get; set; } = true;
        public string CurrentDatabaseNameResponse { get; set; } = "TestDb";
        public TableSchemaInfo TableSchemaResponse { get; set; } = new TableSchemaInfo("TestTable", "TestDb", new List<TableColumnInfo>());
        public IAsyncDataReader? ExecuteQueryResponse { get; set; } = null;
        
        public Task<IEnumerable<TableInfo>> ListTablesAsync(string? databaseName = null, CancellationToken cancellationToken = default)
        {
            ListTablesAsyncCalled = true;
            DatabaseNamePassedToListTables = databaseName;
            TokenPassedToListTables = cancellationToken;
            return Task.FromResult<IEnumerable<TableInfo>>(TablesResponse);
        }
        
        public Task<IEnumerable<DatabaseInfo>> ListDatabasesAsync(CancellationToken cancellationToken = default)
        {
            ListDatabasesAsyncCalled = true;
            TokenPassedToListDatabases = cancellationToken;
            return Task.FromResult<IEnumerable<DatabaseInfo>>(DatabasesResponse);
        }
        
        public Task<bool> DoesDatabaseExistAsync(string databaseName, CancellationToken cancellationToken = default)
        {
            DoesDatabaseExistAsyncCalled = true;
            DatabaseNamePassedToDatabaseExists = databaseName;
            TokenPassedToDatabaseExists = cancellationToken;
            return Task.FromResult(DatabaseExistsResponse);
        }
        
        public string GetCurrentDatabaseName()
        {
            GetCurrentDatabaseNameCalled = true;
            return CurrentDatabaseNameResponse;
        }
        
        
        public Task<TableSchemaInfo> GetTableSchemaAsync(string tableName, string? databaseName = null, CancellationToken cancellationToken = default)
        {
            GetTableSchemaAsyncCalled = true;
            TableNamePassedToGetTableSchema = tableName;
            DatabaseNamePassedToGetTableSchema = databaseName;
            TokenPassedToGetTableSchema = cancellationToken;
            return Task.FromResult(TableSchemaResponse);
        }
        
        public Task<IAsyncDataReader> ExecuteQueryAsync(string query, string? databaseName = null, CancellationToken cancellationToken = default)
        {
            ExecuteQueryAsyncCalled = true;
            QueryPassedToExecuteQuery = query;
            DatabaseNamePassedToExecuteQuery = databaseName;
            TokenPassedToExecuteQuery = cancellationToken;
            return Task.FromResult(ExecuteQueryResponse ?? throw new System.InvalidOperationException("ExecuteQueryResponse is not set"));
        }
    }
}