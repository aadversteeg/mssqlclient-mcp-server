using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Infrastructure.McpServer.Tools;
using FluentAssertions;
using Moq;
using Xunit;

namespace UnitTests.Infrastructure.McpServer.Tools
{
    public class GetTableSchemaToolTests
    {
        [Fact(DisplayName = "GTST-001: GetTableSchemaTool constructor with null database context throws ArgumentNullException")]
        public void GTST001()
        {
            // Act
            IDatabaseContext? nullContext = null;
            Action act = () => new GetTableSchemaTool(nullContext);
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("databaseContext");
        }
        
        [Fact(DisplayName = "GTST-002: GetTableSchema returns error for empty table name")]
        public async Task GTST002()
        {
            // Arrange
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            var tool = new GetTableSchemaTool(mockDatabaseContext.Object);
            
            // Act
            var result = await tool.GetTableSchema(string.Empty);
            
            // Assert
            result.Should().Contain("Error: Table name cannot be empty");
        }
        
        [Fact(DisplayName = "GTST-003: GetTableSchema returns error for null table name")]
        public async Task GTST003()
        {
            // Arrange
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            var tool = new GetTableSchemaTool(mockDatabaseContext.Object);
            
            // Act
            var result = await tool.GetTableSchema(null);
            
            // Assert
            result.Should().Contain("Error: Table name cannot be empty");
        }
        
        [Fact(DisplayName = "GTST-004: GetTableSchema returns error for whitespace table name")]
        public async Task GTST004()
        {
            // Arrange
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            var tool = new GetTableSchemaTool(mockDatabaseContext.Object);
            
            // Act
            var result = await tool.GetTableSchema("   ");
            
            // Assert
            result.Should().Contain("Error: Table name cannot be empty");
        }
        
        [Fact(DisplayName = "GTST-005: GetTableSchema returns formatted schema information")]
        public async Task GTST005()
        {
            // Arrange
            var tableName = "Users";
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            var columns = new List<TableColumnInfo>
            {
                new TableColumnInfo("Id", "int", "4", "NO", "Primary key"),
                new TableColumnInfo("Name", "varchar", "255", "YES", "User name"),
                new TableColumnInfo("Email", "varchar", "255", "NO", "User email")
            };
            
            var tableSchema = new TableSchemaInfo(tableName, "TestDB", "User information table", columns);
            
            mockDatabaseContext.Setup(x => x.GetTableSchemaAsync(
                tableName,
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(tableSchema);
            
            var tool = new GetTableSchemaTool(mockDatabaseContext.Object);
            
            // Act
            var result = await tool.GetTableSchema(tableName);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("Users");
            mockDatabaseContext.Verify(x => x.GetTableSchemaAsync(
                tableName,
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }
        
        [Fact(DisplayName = "GTST-006: GetTableSchema handles exception from database context")]
        public async Task GTST006()
        {
            // Arrange
            var tableName = "NonExistentTable";
            var expectedErrorMessage = "Table does not exist";
            
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            mockDatabaseContext.Setup(x => x.GetTableSchemaAsync(
                tableName,
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(expectedErrorMessage));
            
            var tool = new GetTableSchemaTool(mockDatabaseContext.Object);
            
            // Act
            var result = await tool.GetTableSchema(tableName);
            
            // Assert
            result.Should().Contain(expectedErrorMessage);
        }
    }
}