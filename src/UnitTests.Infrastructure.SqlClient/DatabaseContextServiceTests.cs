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
    public class DatabaseContextServiceTests
    {
        private readonly Mock<IDatabaseService> _mockDatabaseService;
        private readonly DatabaseContextService _databaseContextService;
        
        public DatabaseContextServiceTests()
        {
            _mockDatabaseService = new Mock<IDatabaseService>();
            _databaseContextService = new DatabaseContextService(_mockDatabaseService.Object);
        }
        
        [Fact(DisplayName = "DCS-001: Constructor with null database service throws ArgumentNullException")]
        public void DCS001()
        {
            // Act
            IDatabaseService? nullService = null;
            Action act = () => new DatabaseContextService(nullService);
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("databaseService");
        }
        
        [Fact(DisplayName = "DCS-001a: Constructor with null connection string throws ArgumentNullException")]
        public void DCS001a()
        {
            // Arrange
            var configuration = new DatabaseConfiguration { DefaultCommandTimeoutSeconds = 30 };
            
            // Act
            string? nullConnectionString = null;
            Action act = () => new DatabaseContextService(nullConnectionString, configuration);
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("connectionString");
        }
        
        [Fact(DisplayName = "DCS-002: ListTablesAsync delegates to database service with null database name")]
        public async Task DCS002()
        {
            // Arrange
            var expectedTables = new List<TableInfo>
            {
                new TableInfo("dbo", "Table1", 10, 1.5, DateTime.Now, DateTime.Now, 2, 1, "Normal"),
                new TableInfo("dbo", "Table2", 5, 0.5, DateTime.Now, DateTime.Now, 1, 0, "Normal")
            };
            
            _mockDatabaseService.Setup(x => x.ListTablesAsync(null, It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedTables);
            
            // Act
            var result = await _databaseContextService.ListTablesAsync(null);
            
            // Assert
            result.Should().BeEquivalentTo(expectedTables);
            _mockDatabaseService.Verify(x => x.ListTablesAsync(null, It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "DCS-003: GetTableSchemaAsync delegates to database service with null database name")]
        public async Task DCS003()
        {
            // Arrange
            var tableName = "TestTable";
            var expectedSchema = new TableSchemaInfo(tableName, "TestDb", string.Empty, new List<TableColumnInfo>());
            
            _mockDatabaseService.Setup(x => x.GetTableSchemaAsync(tableName, null, It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedSchema);
            
            // Act
            var result = await _databaseContextService.GetTableSchemaAsync(tableName, null);
            
            // Assert
            result.Should().BeEquivalentTo(expectedSchema);
            _mockDatabaseService.Verify(x => x.GetTableSchemaAsync(tableName, null, It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "DCS-004: GetTableSchemaAsync with empty table name throws ArgumentException")]
        public async Task DCS004()
        {
            // Act
            Func<Task> act = async () => await _databaseContextService.GetTableSchemaAsync(string.Empty, null);
            
            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Table name cannot be empty*");
        }
        
        [Fact(DisplayName = "DCS-005: ExecuteQueryAsync delegates to database service with null database name")]
        public async Task DCS005()
        {
            // Arrange
            var query = "SELECT * FROM TestTable";
            var expectedReader = new Mock<IAsyncDataReader>().Object;
            
            _mockDatabaseService.Setup(x => x.ExecuteQueryAsync(query, null, It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedReader);
            
            // Act
            var result = await _databaseContextService.ExecuteQueryAsync(query, null);
            
            // Assert
            result.Should().Be(expectedReader);
            _mockDatabaseService.Verify(x => x.ExecuteQueryAsync(query, null, It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "DCS-006: ExecuteQueryAsync with empty query throws ArgumentException")]
        public async Task DCS006()
        {
            // Act
            Func<Task> act = async () => await _databaseContextService.ExecuteQueryAsync(string.Empty, null);
            
            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Query cannot be empty*");
        }
        
        // New tests for stored procedure functionality
        
        [Fact(DisplayName = "DCS-007: ListStoredProceduresAsync delegates to database service with null database name")]
        public async Task DCS007()
        {
            // Arrange
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
            
            _mockDatabaseService.Setup(x => x.ListStoredProceduresAsync(null, It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedProcs);
            
            // Act
            var result = await _databaseContextService.ListStoredProceduresAsync(null);
            
            // Assert
            result.Should().BeEquivalentTo(expectedProcs);
            _mockDatabaseService.Verify(x => x.ListStoredProceduresAsync(null, It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "DCS-008: GetStoredProcedureDefinitionAsync delegates to database service with null database name")]
        public async Task DCS008()
        {
            // Arrange
            var procedureName = "TestProc";
            var expectedDefinition = "CREATE PROCEDURE TestProc AS SELECT 1;";
            
            _mockDatabaseService.Setup(x => x.GetStoredProcedureDefinitionAsync(procedureName, null, It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedDefinition);
            
            // Act
            var result = await _databaseContextService.GetStoredProcedureDefinitionAsync(procedureName, null);
            
            // Assert
            result.Should().Be(expectedDefinition);
            _mockDatabaseService.Verify(x => x.GetStoredProcedureDefinitionAsync(procedureName, null, It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "DCS-009: GetStoredProcedureDefinitionAsync with empty procedure name throws ArgumentException")]
        public async Task DCS009()
        {
            // Act
            Func<Task> act = async () => await _databaseContextService.GetStoredProcedureDefinitionAsync(string.Empty, null);
            
            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Procedure name cannot be empty*");
        }
        
        [Fact(DisplayName = "DCS-010: ExecuteStoredProcedureAsync delegates to database service with null database name")]
        public async Task DCS010()
        {
            // Arrange
            var procedureName = "TestProc";
            var parameters = new Dictionary<string, object?>
            {
                { "Param1", 123 },
                { "Param2", "test" }
            };
            var expectedReader = new Mock<IAsyncDataReader>().Object;
            
            _mockDatabaseService.Setup(x => x.ExecuteStoredProcedureAsync(procedureName, parameters, null, It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedReader);
            
            // Act
            var result = await _databaseContextService.ExecuteStoredProcedureAsync(procedureName, parameters, null);
            
            // Assert
            result.Should().Be(expectedReader);
            _mockDatabaseService.Verify(x => x.ExecuteStoredProcedureAsync(procedureName, parameters, null, It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "DCS-011: ExecuteStoredProcedureAsync with empty procedure name throws ArgumentException")]
        public async Task DCS011()
        {
            // Arrange
            var parameters = new Dictionary<string, object?>();
            
            // Act
            Func<Task> act = async () => await _databaseContextService.ExecuteStoredProcedureAsync(string.Empty, parameters, null);
            
            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Procedure name cannot be empty*");
        }
    }
}