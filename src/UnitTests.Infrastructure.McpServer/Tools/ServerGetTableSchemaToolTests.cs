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
    public class ServerGetTableSchemaToolTests
    {
        [Fact(DisplayName = "SGTST-001: ServerGetTableSchemaTool constructor with null server database throws ArgumentNullException")]
        public void SGTST001()
        {
            // Act
            IServerDatabase? nullServerDatabase = null;
            Action act = () => new ServerGetTableSchemaTool(nullServerDatabase, TestHelpers.CreateConfiguration());
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("serverDatabase");
        }
        
        [Fact(DisplayName = "SGTST-002: GetTableSchemaInDatabase returns error for empty database name")]
        public async Task SGTST002()
        {
            // Arrange
            var mockServerDatabase = new Mock<IServerDatabase>();
            var tool = new ServerGetTableSchemaTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetTableSchemaInDatabase(string.Empty, "TestTable");
            
            // Assert
            result.Should().Be("Error: Database name cannot be empty.");
        }
        
        [Fact(DisplayName = "SGTST-003: GetTableSchemaInDatabase returns error for null database name")]
        public async Task SGTST003()
        {
            // Arrange
            var mockServerDatabase = new Mock<IServerDatabase>();
            var tool = new ServerGetTableSchemaTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetTableSchemaInDatabase(null, "TestTable");
            
            // Assert
            result.Should().Be("Error: Database name cannot be empty.");
        }
        
        [Fact(DisplayName = "SGTST-004: GetTableSchemaInDatabase returns error for whitespace database name")]
        public async Task SGTST004()
        {
            // Arrange
            var mockServerDatabase = new Mock<IServerDatabase>();
            var tool = new ServerGetTableSchemaTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetTableSchemaInDatabase("   ", "TestTable");
            
            // Assert
            result.Should().Be("Error: Database name cannot be empty.");
        }
        
        [Fact(DisplayName = "SGTST-005: GetTableSchemaInDatabase returns error for empty table name")]
        public async Task SGTST005()
        {
            // Arrange
            var mockServerDatabase = new Mock<IServerDatabase>();
            var tool = new ServerGetTableSchemaTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetTableSchemaInDatabase("TestDB", string.Empty);
            
            // Assert
            result.Should().Be("Error: Table name cannot be empty.");
        }
        
        [Fact(DisplayName = "SGTST-006: GetTableSchemaInDatabase returns error for null table name")]
        public async Task SGTST006()
        {
            // Arrange
            var mockServerDatabase = new Mock<IServerDatabase>();
            var tool = new ServerGetTableSchemaTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetTableSchemaInDatabase("TestDB", null);
            
            // Assert
            result.Should().Be("Error: Table name cannot be empty.");
        }
        
        [Fact(DisplayName = "SGTST-007: GetTableSchemaInDatabase returns error for whitespace table name")]
        public async Task SGTST007()
        {
            // Arrange
            var mockServerDatabase = new Mock<IServerDatabase>();
            var tool = new ServerGetTableSchemaTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetTableSchemaInDatabase("TestDB", "   ");
            
            // Assert
            result.Should().Be("Error: Table name cannot be empty.");
        }
        
        [Fact(DisplayName = "SGTST-008: GetTableSchemaInDatabase returns formatted schema when table exists")]
        public async Task SGTST008()
        {
            // Arrange
            var databaseName = "TestDB";
            var tableName = "Users";
            var schemaInfo = new TableSchemaInfo(
                TableName: tableName,
                DatabaseName: databaseName,
                MsDescription: "Test table",
                Columns: new List<TableColumnInfo>
                {
                    new TableColumnInfo("Column1", "int", "4", "NO", "Primary key"),
                    new TableColumnInfo("Column2", "varchar", "50", "YES", "Description")
                }
            );
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            mockServerDatabase.Setup(x => x.GetTableSchemaAsync(databaseName, tableName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(schemaInfo);
            
            var tool = new ServerGetTableSchemaTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetTableSchemaInDatabase(databaseName, tableName);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("Schema for table");
            result.Should().Contain(tableName);
            result.Should().Contain(databaseName);
            
            // Verify the server database was called with correct parameters
            mockServerDatabase.Verify(x => x.GetTableSchemaAsync(databaseName, tableName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "SGTST-009: GetTableSchemaInDatabase handles exception from server database")]
        public async Task SGTST009()
        {
            // Arrange
            var databaseName = "TestDB";
            var tableName = "NonExistentTable";
            var expectedErrorMessage = "Table not found";
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            mockServerDatabase.Setup(x => x.GetTableSchemaAsync(databaseName, tableName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(expectedErrorMessage));
            
            var tool = new ServerGetTableSchemaTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetTableSchemaInDatabase(databaseName, tableName);
            
            // Assert
            result.Should().Contain(expectedErrorMessage);
            result.Should().Contain("getting table schema");
            
            // Verify the server database was called
            mockServerDatabase.Verify(x => x.GetTableSchemaAsync(databaseName, tableName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "SGTST-010: GetTableSchemaInDatabase handles database access exception")]
        public async Task SGTST010()
        {
            // Arrange
            var databaseName = "RestrictedDB";
            var tableName = "TestTable";
            var expectedErrorMessage = "Access denied to database";
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            mockServerDatabase.Setup(x => x.GetTableSchemaAsync(databaseName, tableName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UnauthorizedAccessException(expectedErrorMessage));
            
            var tool = new ServerGetTableSchemaTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetTableSchemaInDatabase(databaseName, tableName);
            
            // Assert
            result.Should().Contain(expectedErrorMessage);
            result.Should().Contain("getting table schema");
            
            // Verify the server database was called
            mockServerDatabase.Verify(x => x.GetTableSchemaAsync(databaseName, tableName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "SGTST-011: GetTableSchemaInDatabase handles SQL connection exception")]
        public async Task SGTST011()
        {
            // Arrange
            var databaseName = "TestDB";
            var tableName = "TestTable";
            var expectedErrorMessage = "Database connection timeout";
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            mockServerDatabase.Setup(x => x.GetTableSchemaAsync(databaseName, tableName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TimeoutException(expectedErrorMessage));
            
            var tool = new ServerGetTableSchemaTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetTableSchemaInDatabase(databaseName, tableName);
            
            // Assert
            result.Should().Contain(expectedErrorMessage);
            result.Should().Contain("getting table schema");
            
            // Verify the server database was called
            mockServerDatabase.Verify(x => x.GetTableSchemaAsync(databaseName, tableName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "SGTST-012: GetTableSchemaInDatabase works with qualified table names")]
        public async Task SGTST012()
        {
            // Arrange
            var databaseName = "TestDB";
            var tableName = "dbo.Users"; // Schema-qualified table name
            var schemaInfo = new TableSchemaInfo(
                TableName: tableName,
                DatabaseName: databaseName,
                MsDescription: "User table",
                Columns: new List<TableColumnInfo>
                {
                    new TableColumnInfo("Id", "int", "4", "NO", "Primary key"),
                    new TableColumnInfo("Name", "varchar", "100", "YES", "User name")
                }
            );
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            mockServerDatabase.Setup(x => x.GetTableSchemaAsync(databaseName, tableName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(schemaInfo);
            
            var tool = new ServerGetTableSchemaTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetTableSchemaInDatabase(databaseName, tableName);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("Schema for table");
            result.Should().Contain(tableName);
            result.Should().Contain(databaseName);
            
            // Verify the server database was called with the qualified table name
            mockServerDatabase.Verify(x => x.GetTableSchemaAsync(databaseName, tableName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}


