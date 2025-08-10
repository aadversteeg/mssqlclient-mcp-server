using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Infrastructure.SqlClient;
using Core.Infrastructure.SqlClient.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace UnitTests.Infrastructure.SqlClient
{
    public class ServerDatabaseServiceTests
    {
        private readonly Mock<IDatabaseService> _mockDatabaseService;
        private readonly ServerDatabaseService _serverDatabaseService;
        
        public ServerDatabaseServiceTests()
        {
            _mockDatabaseService = new Mock<IDatabaseService>();
            _serverDatabaseService = new ServerDatabaseService(_mockDatabaseService.Object);
        }
        
        [Fact(DisplayName = "SDS-001: Constructor with null database service throws ArgumentNullException")]
        public void SDS001()
        {
            // Act
            IDatabaseService? nullService = null;
            Action act = () => new ServerDatabaseService(nullService);
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("databaseService");
        }
        
        [Fact(DisplayName = "SDS-002: Constructor works with any database, not just master")]
        public void SDS002()
        {
            // Arrange
            var mockNonMasterService = new Mock<IDatabaseService>();
            mockNonMasterService.Setup(x => x.GetCurrentDatabaseName())
                .Returns("NotMaster");
            
            // Act - Should not throw an exception
            var serverDbService = new ServerDatabaseService(mockNonMasterService.Object);
            
            // Assert
            serverDbService.Should().NotBeNull();
        }
        
        [Fact(DisplayName = "SDS-003: ListTablesAsync delegates to database service with provided database name")]
        public async Task SDS003()
        {
            // Arrange
            var databaseName = "TestDb";
            var expectedTables = new List<TableInfo>
            {
                new TableInfo("dbo", "Table1", 10, 1.5, DateTime.Now, DateTime.Now, 2, 1, "Normal"),
                new TableInfo("dbo", "Table2", 5, 0.5, DateTime.Now, DateTime.Now, 1, 0, "Normal")
            };
            
            _mockDatabaseService.Setup(x => x.ListTablesAsync(databaseName, It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedTables);
            
            _mockDatabaseService.Setup(x => x.DoesDatabaseExistAsync(databaseName, It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            
            // Act
            var result = await _serverDatabaseService.ListTablesAsync(databaseName, null);
            
            // Assert
            result.Should().BeEquivalentTo(expectedTables);
            _mockDatabaseService.Verify(x => x.ListTablesAsync(databaseName, It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "SDS-004: ListTablesAsync with empty database name throws ArgumentException")]
        public async Task SDS004()
        {
            // Act
            Func<Task> act = async () => await _serverDatabaseService.ListTablesAsync(string.Empty, null);
            
            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Database name cannot be empty*");
        }
        
        [Fact(DisplayName = "SDS-005: ListTablesAsync with non-existent database throws InvalidOperationException")]
        public async Task SDS005()
        {
            // Arrange
            var databaseName = "NonExistentDb";
            
            _mockDatabaseService.Setup(x => x.DoesDatabaseExistAsync(databaseName, It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            
            // Act
            Func<Task> act = async () => await _serverDatabaseService.ListTablesAsync(databaseName, null);
            
            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage($"*Database '{databaseName}' does not exist*");
        }
        
        [Fact(DisplayName = "SDS-006: ListTablesAsync passes cancellation token to database service")]
        public async Task SDS006()
        {
            // Arrange
            var databaseName = "TestDb";
            var cancellationToken = new CancellationToken();
            
            _mockDatabaseService.Setup(x => x.DoesDatabaseExistAsync(databaseName, It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), cancellationToken))
                .ReturnsAsync(true);
            
            // Act
            await _serverDatabaseService.ListTablesAsync(databaseName, null, null, cancellationToken);
            
            // Assert
            _mockDatabaseService.Verify(x => x.DoesDatabaseExistAsync(databaseName, It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), cancellationToken), Times.Once);
            _mockDatabaseService.Verify(x => x.ListTablesAsync(databaseName, It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), cancellationToken), Times.Once);
        }
        
        [Fact(DisplayName = "SDS-007: ListDatabasesAsync delegates to database service")]
        public async Task SDS007()
        {
            // Arrange
            var expectedDatabases = new List<DatabaseInfo>
            {
                new DatabaseInfo("master", "ONLINE", 100.5, "sa", "150", "SQL_Latin1_General_CP1_CI_AS", DateTime.Now, "SIMPLE", false),
                new DatabaseInfo("TestDb", "ONLINE", 50.2, "sa", "150", "SQL_Latin1_General_CP1_CI_AS", DateTime.Now, "SIMPLE", false)
            };
            
            _mockDatabaseService.Setup(x => x.ListDatabasesAsync(It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedDatabases);
            
            // Act
            var result = await _serverDatabaseService.ListDatabasesAsync(null);
            
            // Assert
            result.Should().BeEquivalentTo(expectedDatabases);
            _mockDatabaseService.Verify(x => x.ListDatabasesAsync(It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "SDS-008: ListDatabasesAsync passes cancellation token to database service")]
        public async Task SDS008()
        {
            // Arrange
            var cancellationToken = new CancellationToken();
            
            // Act
            await _serverDatabaseService.ListDatabasesAsync(null, null, cancellationToken);
            
            // Assert
            _mockDatabaseService.Verify(x => x.ListDatabasesAsync(It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), cancellationToken), Times.Once);
        }
        
        // New tests for stored procedure functionality
        
        [Fact(DisplayName = "SDS-009: ListStoredProceduresAsync delegates to database service with provided database name")]
        public async Task SDS009()
        {
            // Arrange
            var databaseName = "TestDb";
            var expectedProcs = new List<StoredProcedureInfo> 
            {
                new StoredProcedureInfo(
                    SchemaName: "dbo",
                    Name: "TestProc1",
                    CreateDate: DateTime.Now,
                    ModifyDate: DateTime.Now,
                    Owner: "dbo",
                    Parameters: new List<StoredProcedureParameterInfo>(),
                    IsFunction: false,
                    LastExecutionTime: null,
                    ExecutionCount: null,
                    AverageDurationMs: null),
                new StoredProcedureInfo(
                    SchemaName: "dbo",
                    Name: "TestProc2",
                    CreateDate: DateTime.Now,
                    ModifyDate: DateTime.Now,
                    Owner: "dbo",
                    Parameters: new List<StoredProcedureParameterInfo>(),
                    IsFunction: false,
                    LastExecutionTime: null,
                    ExecutionCount: null,
                    AverageDurationMs: null)
            };
            
            _mockDatabaseService.Setup(x => x.ListStoredProceduresAsync(databaseName, It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedProcs);
            
            _mockDatabaseService.Setup(x => x.DoesDatabaseExistAsync(databaseName, It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            
            // Act
            var result = await _serverDatabaseService.ListStoredProceduresAsync(databaseName, null);
            
            // Assert
            result.Should().BeEquivalentTo(expectedProcs);
            _mockDatabaseService.Verify(x => x.ListStoredProceduresAsync(databaseName, It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "SDS-010: ListStoredProceduresAsync with empty database name throws ArgumentException")]
        public async Task SDS010()
        {
            // Act
            Func<Task> act = async () => await _serverDatabaseService.ListStoredProceduresAsync(string.Empty, null);
            
            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Database name cannot be empty*");
        }
        
        [Fact(DisplayName = "SDS-011: ListStoredProceduresAsync with non-existent database throws InvalidOperationException")]
        public async Task SDS011()
        {
            // Arrange
            var databaseName = "NonExistentDb";
            
            _mockDatabaseService.Setup(x => x.DoesDatabaseExistAsync(databaseName, It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            
            // Act
            Func<Task> act = async () => await _serverDatabaseService.ListStoredProceduresAsync(databaseName, null);
            
            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage($"*Database '{databaseName}' does not exist*");
        }
        
        [Fact(DisplayName = "SDS-012: GetStoredProcedureDefinitionAsync delegates to database service")]
        public async Task SDS012()
        {
            // Arrange
            var databaseName = "TestDb";
            var procedureName = "TestProc";
            var expectedDefinition = "CREATE PROCEDURE TestProc AS SELECT 1;";
            
            _mockDatabaseService.Setup(x => x.GetStoredProcedureDefinitionAsync(procedureName, databaseName, It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedDefinition);
            
            _mockDatabaseService.Setup(x => x.DoesDatabaseExistAsync(databaseName, It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            
            // Act
            var result = await _serverDatabaseService.GetStoredProcedureDefinitionAsync(databaseName, procedureName, null);
            
            // Assert
            result.Should().Be(expectedDefinition);
            _mockDatabaseService.Verify(x => x.GetStoredProcedureDefinitionAsync(procedureName, databaseName, It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "SDS-013: GetStoredProcedureDefinitionAsync with empty database name throws ArgumentException")]
        public async Task SDS013()
        {
            // Act
            Func<Task> act = async () => await _serverDatabaseService.GetStoredProcedureDefinitionAsync(string.Empty, "TestProc", null);
            
            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Database name cannot be empty*");
        }
        
        [Fact(DisplayName = "SDS-014: GetStoredProcedureDefinitionAsync with empty procedure name throws ArgumentException")]
        public async Task SDS014()
        {
            // Act
            Func<Task> act = async () => await _serverDatabaseService.GetStoredProcedureDefinitionAsync("TestDb", string.Empty, null);
            
            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Procedure name cannot be empty*");
        }
        
        [Fact(DisplayName = "SDS-015: ExecuteStoredProcedureAsync delegates to database service")]
        public async Task SDS015()
        {
            // Arrange
            var databaseName = "TestDb";
            var procedureName = "TestProc";
            var parameters = new Dictionary<string, object?>
            {
                { "Param1", 123 },
                { "Param2", "test" }
            };
            var expectedReader = new Mock<IAsyncDataReader>().Object;
            
            _mockDatabaseService.Setup(x => x.ExecuteStoredProcedureAsync(procedureName, parameters, databaseName, It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedReader);
            
            _mockDatabaseService.Setup(x => x.DoesDatabaseExistAsync(databaseName, It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            
            // Act
            var result = await _serverDatabaseService.ExecuteStoredProcedureAsync(databaseName, procedureName, parameters, null);
            
            // Assert
            result.Should().Be(expectedReader);
            _mockDatabaseService.Verify(x => x.ExecuteStoredProcedureAsync(procedureName, parameters, databaseName, It.IsAny<ToolCallTimeoutContext?>(), null, It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "SDS-016: ExecuteStoredProcedureAsync with empty database name throws ArgumentException")]
        public async Task SDS016()
        {
            // Arrange
            var parameters = new Dictionary<string, object?>();
            
            // Act
            Func<Task> act = async () => await _serverDatabaseService.ExecuteStoredProcedureAsync(string.Empty, "TestProc", parameters, null);
            
            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Database name cannot be empty*");
        }
        
        [Fact(DisplayName = "SDS-017: ExecuteStoredProcedureAsync with empty procedure name throws ArgumentException")]
        public async Task SDS017()
        {
            // Arrange
            var parameters = new Dictionary<string, object?>();
            
            // Act
            Func<Task> act = async () => await _serverDatabaseService.ExecuteStoredProcedureAsync("TestDb", string.Empty, parameters, null);
            
            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Procedure name cannot be empty*");
        }
        
        [Fact(DisplayName = "SDS-018: ExecuteStoredProcedureAsync with null parameters throws ArgumentNullException")]
        public async Task SDS018()
        {
            // Act
            Func<Task> act = async () => await _serverDatabaseService.ExecuteStoredProcedureAsync("TestDb", "TestProc", null, null);
            
            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("parameters");
        }
        
        [Fact(DisplayName = "SDS-019: ExecuteStoredProcedureAsync with non-existent database throws InvalidOperationException")]
        public async Task SDS019()
        {
            // Arrange
            var databaseName = "NonExistentDb";
            var procedureName = "TestProc";
            var parameters = new Dictionary<string, object?>();
            
            _mockDatabaseService.Setup(x => x.DoesDatabaseExistAsync(databaseName, It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            
            // Act
            Func<Task> act = async () => await _serverDatabaseService.ExecuteStoredProcedureAsync(databaseName, procedureName, parameters, null);
            
            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage($"*Database '{databaseName}' does not exist*");
        }
        
        [Fact(DisplayName = "SDS-020: ExecuteStoredProcedureAsync with timeout passes timeout to database service")]
        public async Task SDS020()
        {
            // Arrange
            var databaseName = "TestDb";
            var procedureName = "TestProc";
            var parameters = new Dictionary<string, object?>
            {
                { "Param1", 123 },
                { "Param2", "test" }
            };
            int timeoutSeconds = 180;
            var expectedReader = new Mock<IAsyncDataReader>().Object;
            
            _mockDatabaseService.Setup(x => x.ExecuteStoredProcedureAsync(procedureName, parameters, databaseName, It.IsAny<ToolCallTimeoutContext?>(), timeoutSeconds, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedReader);
            
            _mockDatabaseService.Setup(x => x.DoesDatabaseExistAsync(databaseName, It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            
            // Act
            var result = await _serverDatabaseService.ExecuteStoredProcedureAsync(databaseName, procedureName, parameters, null, timeoutSeconds);
            
            // Assert
            result.Should().Be(expectedReader);
            _mockDatabaseService.Verify(x => x.ExecuteStoredProcedureAsync(procedureName, parameters, databaseName, It.IsAny<ToolCallTimeoutContext?>(), timeoutSeconds, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}