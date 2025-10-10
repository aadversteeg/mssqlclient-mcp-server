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
            // Create a test database service with a dummy connection string and a mock capability detector
            // Just to verify that it implements the interface
            var dummyConnectionString = "Server=test;Database=dummy;Trusted_Connection=True;";
            var configuration = new DatabaseConfiguration { DefaultCommandTimeoutSeconds = 30 };
            var mockCapabilityDetector = new Mock<ISqlServerCapabilityDetector>();
            mockCapabilityDetector.Setup(x => x.DetectCapabilitiesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SqlServerCapability
                {
                    MajorVersion = 14, // SQL Server 2017
                    SupportsExactRowCount = true,
                    SupportsDetailedIndexMetadata = true
                });
                
            var service = new DatabaseService(dummyConnectionString, mockCapabilityDetector.Object, configuration);
            
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
        
        // New properties for stored procedure methods
        public bool ListStoredProceduresAsyncCalled { get; private set; }
        public string? DatabaseNamePassedToListStoredProcedures { get; private set; }
        public CancellationToken TokenPassedToListStoredProcedures { get; private set; }
        
        public bool GetStoredProcedureDefinitionAsyncCalled { get; private set; }
        public string? ProcedureNamePassedToGetStoredProcedureDefinition { get; private set; }
        public string? DatabaseNamePassedToGetStoredProcedureDefinition { get; private set; }
        public CancellationToken TokenPassedToGetStoredProcedureDefinition { get; private set; }
        
        public bool ExecuteStoredProcedureAsyncCalled { get; private set; }
        public string? ProcedureNamePassedToExecuteStoredProcedure { get; private set; }
        public Dictionary<string, object?>? ParametersPassedToExecuteStoredProcedure { get; private set; }
        public string? DatabaseNamePassedToExecuteStoredProcedure { get; private set; }
        public CancellationToken TokenPassedToExecuteStoredProcedure { get; private set; }
        
        // Mock responses
        public List<TableInfo> TablesResponse { get; set; } = new List<TableInfo>();
        public List<DatabaseInfo> DatabasesResponse { get; set; } = new List<DatabaseInfo>();
        public bool DatabaseExistsResponse { get; set; } = true;
        public string CurrentDatabaseNameResponse { get; set; } = "TestDb";
        public TableSchemaInfo TableSchemaResponse { get; set; } = new TableSchemaInfo("TestTable", "TestDb", string.Empty, new List<TableColumnInfo>());
        public IAsyncDataReader? ExecuteQueryResponse { get; set; } = null;
        
        // New mock responses for stored procedures
        public List<StoredProcedureInfo> StoredProceduresResponse { get; set; } = new List<StoredProcedureInfo>();
        public string StoredProcedureDefinitionResponse { get; set; } = "CREATE PROCEDURE Test_Proc AS SELECT 1;";
        public IAsyncDataReader? ExecuteStoredProcedureResponse { get; set; } = null;
        
        public Task<IEnumerable<TableInfo>> ListTablesAsync(string? databaseName = null, ToolCallTimeoutContext? timeoutContext = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        {
            ListTablesAsyncCalled = true;
            DatabaseNamePassedToListTables = databaseName;
            TokenPassedToListTables = cancellationToken;
            return Task.FromResult<IEnumerable<TableInfo>>(TablesResponse);
        }
        
        public Task<IEnumerable<DatabaseInfo>> ListDatabasesAsync(ToolCallTimeoutContext? timeoutContext = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        {
            ListDatabasesAsyncCalled = true;
            TokenPassedToListDatabases = cancellationToken;
            return Task.FromResult<IEnumerable<DatabaseInfo>>(DatabasesResponse);
        }
        
        public Task<bool> DoesDatabaseExistAsync(string databaseName, ToolCallTimeoutContext? timeoutContext = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
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
        
        
        public Task<TableSchemaInfo> GetTableSchemaAsync(string tableName, string? databaseName = null, ToolCallTimeoutContext? timeoutContext = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        {
            GetTableSchemaAsyncCalled = true;
            TableNamePassedToGetTableSchema = tableName;
            DatabaseNamePassedToGetTableSchema = databaseName;
            TokenPassedToGetTableSchema = cancellationToken;
            return Task.FromResult(TableSchemaResponse);
        }
        
        public Task<IAsyncDataReader> ExecuteQueryAsync(string query, string? databaseName = null, ToolCallTimeoutContext? timeoutContext = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        {
            ExecuteQueryAsyncCalled = true;
            QueryPassedToExecuteQuery = query;
            DatabaseNamePassedToExecuteQuery = databaseName;
            TokenPassedToExecuteQuery = cancellationToken;
            return Task.FromResult(ExecuteQueryResponse ?? throw new System.InvalidOperationException("ExecuteQueryResponse is not set"));
        }
        
        // New methods for stored procedures
        public Task<IEnumerable<StoredProcedureInfo>> ListStoredProceduresAsync(string? databaseName = null, ToolCallTimeoutContext? timeoutContext = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        {
            ListStoredProceduresAsyncCalled = true;
            DatabaseNamePassedToListStoredProcedures = databaseName;
            TokenPassedToListStoredProcedures = cancellationToken;
            return Task.FromResult<IEnumerable<StoredProcedureInfo>>(StoredProceduresResponse);
        }
        
        public Task<string> GetStoredProcedureDefinitionAsync(string procedureName, string? databaseName = null, ToolCallTimeoutContext? timeoutContext = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        {
            GetStoredProcedureDefinitionAsyncCalled = true;
            ProcedureNamePassedToGetStoredProcedureDefinition = procedureName;
            DatabaseNamePassedToGetStoredProcedureDefinition = databaseName;
            TokenPassedToGetStoredProcedureDefinition = cancellationToken;
            return Task.FromResult(StoredProcedureDefinitionResponse);
        }
        
        public Task<IAsyncDataReader> ExecuteStoredProcedureAsync(string procedureName, Dictionary<string, object?> parameters, string? databaseName = null, ToolCallTimeoutContext? timeoutContext = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        {
            ExecuteStoredProcedureAsyncCalled = true;
            ProcedureNamePassedToExecuteStoredProcedure = procedureName;
            ParametersPassedToExecuteStoredProcedure = parameters;
            DatabaseNamePassedToExecuteStoredProcedure = databaseName;
            TokenPassedToExecuteStoredProcedure = cancellationToken;
            return Task.FromResult(ExecuteStoredProcedureResponse ?? throw new System.InvalidOperationException("ExecuteStoredProcedureResponse is not set"));
        }
    }
}