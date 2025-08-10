using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Infrastructure.McpServer.Tools;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace UnitTests.Infrastructure.McpServer.Tools
{
    public class GetTableSchemaToolTests
    {
        [Fact(DisplayName = "GTST-001: GetTableSchemaTool constructor with null database context throws ArgumentNullException")]
        public void GTST001()
        {
            // Arrange
            var mockOptions = new Mock<IOptions<DatabaseConfiguration>>();
            mockOptions.Setup(o => o.Value).Returns(new DatabaseConfiguration { TotalToolCallTimeoutSeconds = null });
            
            // Act
            IDatabaseContext? nullContext = null;
            Action act = () => new GetTableSchemaTool(nullContext, mockOptions.Object);
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("databaseContext");
        }
        
        [Fact(DisplayName = "GTST-002: GetTableSchema returns error for empty table name")]
        public async Task GTST002()
        {
            // Arrange
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            var mockOptions = new Mock<IOptions<DatabaseConfiguration>>();
            mockOptions.Setup(o => o.Value).Returns(new DatabaseConfiguration { TotalToolCallTimeoutSeconds = null });
            var tool = new GetTableSchemaTool(mockDatabaseContext.Object, mockOptions.Object);
            
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
            var mockOptions = new Mock<IOptions<DatabaseConfiguration>>();
            mockOptions.Setup(o => o.Value).Returns(new DatabaseConfiguration { TotalToolCallTimeoutSeconds = null });
            var tool = new GetTableSchemaTool(mockDatabaseContext.Object, mockOptions.Object);
            
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
            var mockOptions = new Mock<IOptions<DatabaseConfiguration>>();
            mockOptions.Setup(o => o.Value).Returns(new DatabaseConfiguration { TotalToolCallTimeoutSeconds = null });
            var tool = new GetTableSchemaTool(mockDatabaseContext.Object, mockOptions.Object);
            
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
                It.IsAny<ToolCallTimeoutContext?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(tableSchema);
            
            var mockOptions = new Mock<IOptions<DatabaseConfiguration>>();
            mockOptions.Setup(o => o.Value).Returns(new DatabaseConfiguration { TotalToolCallTimeoutSeconds = null });
            var tool = new GetTableSchemaTool(mockDatabaseContext.Object, mockOptions.Object);
            
            // Act
            var result = await tool.GetTableSchema(tableName);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("Users");
            mockDatabaseContext.Verify(x => x.GetTableSchemaAsync(
                tableName,
                It.IsAny<ToolCallTimeoutContext?>(),
                It.IsAny<int?>(),
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
                It.IsAny<ToolCallTimeoutContext?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(expectedErrorMessage));
            
            var mockOptions = new Mock<IOptions<DatabaseConfiguration>>();
            mockOptions.Setup(o => o.Value).Returns(new DatabaseConfiguration { TotalToolCallTimeoutSeconds = null });
            var tool = new GetTableSchemaTool(mockDatabaseContext.Object, mockOptions.Object);
            
            // Act
            var result = await tool.GetTableSchema(tableName);
            
            // Assert
            result.Should().Contain(expectedErrorMessage);
        }
        
        [Fact(DisplayName = "GTST-007: GetTableSchema with timeout parameter passes through correctly")]
        public async Task GTST007()
        {
            // Arrange
            var tableName = "Users";
            var timeoutSeconds = 45;
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            var columns = new List<TableColumnInfo>
            {
                new TableColumnInfo("Id", "int", "4", "NO", "Primary key"),
                new TableColumnInfo("Name", "varchar", "255", "YES", "User name")
            };
            
            var tableSchema = new TableSchemaInfo(tableName, "TestDB", "User information table", columns);
            
            mockDatabaseContext.Setup(x => x.GetTableSchemaAsync(
                tableName,
                It.IsAny<ToolCallTimeoutContext?>(),
                timeoutSeconds,
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(tableSchema);
            
            var mockOptions = new Mock<IOptions<DatabaseConfiguration>>();
            mockOptions.Setup(o => o.Value).Returns(new DatabaseConfiguration { TotalToolCallTimeoutSeconds = null });
            var tool = new GetTableSchemaTool(mockDatabaseContext.Object, mockOptions.Object);
            
            // Act
            var result = await tool.GetTableSchema(tableName, timeoutSeconds);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("Users");
            
            // Verify the timeout parameter was passed through correctly
            mockDatabaseContext.Verify(x => x.GetTableSchemaAsync(
                tableName,
                It.IsAny<ToolCallTimeoutContext?>(),
                timeoutSeconds,
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }
        
        [Fact(DisplayName = "GTST-008: GetTableSchema with null timeout uses default")]
        public async Task GTST008()
        {
            // Arrange
            var tableName = "Products";
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            var columns = new List<TableColumnInfo>
            {
                new TableColumnInfo("ProductId", "int", "4", "NO", "Product identifier")
            };
            
            var tableSchema = new TableSchemaInfo(tableName, "TestDB", "Product information", columns);
            
            mockDatabaseContext.Setup(x => x.GetTableSchemaAsync(
                tableName,
                It.IsAny<ToolCallTimeoutContext?>(),
                null,
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(tableSchema);
            
            var mockOptions = new Mock<IOptions<DatabaseConfiguration>>();
            mockOptions.Setup(o => o.Value).Returns(new DatabaseConfiguration { TotalToolCallTimeoutSeconds = null });
            var tool = new GetTableSchemaTool(mockDatabaseContext.Object, mockOptions.Object);
            
            // Act
            var result = await tool.GetTableSchema(tableName, null);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("Products");
            
            // Verify null timeout was passed through
            mockDatabaseContext.Verify(x => x.GetTableSchemaAsync(
                tableName,
                It.IsAny<ToolCallTimeoutContext?>(),
                null,
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }
        
        [Fact(DisplayName = "GTST-009: GetTableSchema verifies timeout parameter is passed through correctly")]
        public async Task GTST009()
        {
            // Arrange
            var tableName = "Orders";
            var specificTimeout = 90;
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            var columns = new List<TableColumnInfo>
            {
                new TableColumnInfo("OrderId", "int", "4", "NO", "Order identifier"),
                new TableColumnInfo("CustomerId", "int", "4", "NO", "Customer identifier"),
                new TableColumnInfo("OrderDate", "datetime", "8", "NO", "Order date")
            };
            
            var tableSchema = new TableSchemaInfo(tableName, "TestDB", "Order information", columns);
            
            mockDatabaseContext.Setup(x => x.GetTableSchemaAsync(
                tableName,
                It.IsAny<ToolCallTimeoutContext?>(),
                specificTimeout,
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(tableSchema);
            
            var mockOptions = new Mock<IOptions<DatabaseConfiguration>>();
            mockOptions.Setup(o => o.Value).Returns(new DatabaseConfiguration { TotalToolCallTimeoutSeconds = null });
            var tool = new GetTableSchemaTool(mockDatabaseContext.Object, mockOptions.Object);
            
            // Act
            var result = await tool.GetTableSchema(tableName, specificTimeout);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("Orders");
            
            // Verify the exact timeout value was passed
            mockDatabaseContext.Verify(x => x.GetTableSchemaAsync(
                tableName,
                It.IsAny<ToolCallTimeoutContext?>(),
                specificTimeout,
                It.IsAny<CancellationToken>()), 
                Times.Once);
            
            // Verify it was not called with any other timeout value
            mockDatabaseContext.Verify(x => x.GetTableSchemaAsync(
                tableName,
                It.IsAny<ToolCallTimeoutContext?>(),
                It.Is<int?>(t => t != specificTimeout),
                It.IsAny<CancellationToken>()), 
                Times.Never);
        }
    }
}