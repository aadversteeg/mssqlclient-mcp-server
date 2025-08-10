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
    public class ServerListTablesToolTests
    {
        [Fact(DisplayName = "SLTT-001: ServerListTablesTool constructor with null server database throws ArgumentNullException")]
        public void SLTT001()
        {
            // Act
            IServerDatabase? nullServerDatabase = null;
            var configuration = TestHelpers.CreateConfiguration();
            Action act = () => new ServerListTablesTool(nullServerDatabase, configuration);
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("serverDatabase");
        }
        
        [Fact(DisplayName = "SLTT-002: ListTablesInDatabase returns error for empty database name")]
        public async Task SLTT002()
        {
            // Arrange
            var mockServerDatabase = new Mock<IServerDatabase>();
            var configuration = TestHelpers.CreateConfiguration();
            var tool = new ServerListTablesTool(mockServerDatabase.Object, configuration);
            
            // Act
            var result = await tool.ListTablesInDatabase(string.Empty);
            
            // Assert
            result.Should().Be("Error: Database name cannot be empty.");
        }
        
        [Fact(DisplayName = "SLTT-003: ListTablesInDatabase returns error for null database name")]
        public async Task SLTT003()
        {
            // Arrange
            var mockServerDatabase = new Mock<IServerDatabase>();
            var configuration = TestHelpers.CreateConfiguration();
            var tool = new ServerListTablesTool(mockServerDatabase.Object, configuration);
            
            // Act
            var result = await tool.ListTablesInDatabase(null);
            
            // Assert
            result.Should().Be("Error: Database name cannot be empty.");
        }
        
        [Fact(DisplayName = "SLTT-004: ListTablesInDatabase returns error for whitespace database name")]
        public async Task SLTT004()
        {
            // Arrange
            var mockServerDatabase = new Mock<IServerDatabase>();
            var configuration = TestHelpers.CreateConfiguration();
            var tool = new ServerListTablesTool(mockServerDatabase.Object, configuration);
            
            // Act
            var result = await tool.ListTablesInDatabase("   ");
            
            // Assert
            result.Should().Be("Error: Database name cannot be empty.");
        }
        
        [Fact(DisplayName = "SLTT-005: ListTablesInDatabase returns formatted table list when tables exist")]
        public async Task SLTT005()
        {
            // Arrange
            var databaseName = "TestDB";
            var tableList = new List<TableInfo>
            {
                new TableInfo(
                    Schema: "dbo",
                    Name: "Users",
                    RowCount: 1000,
                    SizeMB: 5.2,
                    CreateDate: new DateTime(2023, 1, 1),
                    ModifyDate: new DateTime(2023, 6, 1),
                    IndexCount: 3,
                    ForeignKeyCount: 1,
                    TableType: "BASE TABLE"
                ),
                new TableInfo(
                    Schema: "sales",
                    Name: "Orders",
                    RowCount: 5000,
                    SizeMB: 12.5,
                    CreateDate: new DateTime(2023, 2, 15),
                    ModifyDate: new DateTime(2023, 8, 20),
                    IndexCount: 5,
                    ForeignKeyCount: 2,
                    TableType: "BASE TABLE"
                )
            };
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            mockServerDatabase.Setup(x => x.ListTablesAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(tableList);
            
            // Mock the ToToolResult extension method behavior
            var expectedResult = $"Tables in database '{databaseName}':\ndbo.Users (1000 rows)\nsales.Orders (5000 rows)";
            
            var configuration = TestHelpers.CreateConfiguration();
            var tool = new ServerListTablesTool(mockServerDatabase.Object, configuration);
            
            // Act
            var result = await tool.ListTablesInDatabase(databaseName);
            
            // Assert
            result.Should().NotBeNull();
            
            // Verify the server database was called with correct parameters
            mockServerDatabase.Verify(x => x.ListTablesAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "SLTT-006: ListTablesInDatabase returns empty list message when no tables exist")]
        public async Task SLTT006()
        {
            // Arrange
            var databaseName = "EmptyDB";
            var emptyTableList = new List<TableInfo>();
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            mockServerDatabase.Setup(x => x.ListTablesAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(emptyTableList);
            
            var configuration = TestHelpers.CreateConfiguration();
            var tool = new ServerListTablesTool(mockServerDatabase.Object, configuration);
            
            // Act
            var result = await tool.ListTablesInDatabase(databaseName);
            
            // Assert
            result.Should().NotBeNull();
            
            // Verify the server database was called
            mockServerDatabase.Verify(x => x.ListTablesAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "SLTT-007: ListTablesInDatabase handles exception from server database")]
        public async Task SLTT007()
        {
            // Arrange
            var databaseName = "TestDB";
            var expectedErrorMessage = "Database not found";
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            mockServerDatabase.Setup(x => x.ListTablesAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(expectedErrorMessage));
            
            var configuration = TestHelpers.CreateConfiguration();
            var tool = new ServerListTablesTool(mockServerDatabase.Object, configuration);
            
            // Act
            var result = await tool.ListTablesInDatabase(databaseName);
            
            // Assert
            result.Should().Contain(expectedErrorMessage);
            result.Should().Contain("listing tables");
            
            // Verify the server database was called
            mockServerDatabase.Verify(x => x.ListTablesAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "SLTT-008: ListTablesInDatabase handles database access exception")]
        public async Task SLTT008()
        {
            // Arrange
            var databaseName = "RestrictedDB";
            var expectedErrorMessage = "Access denied to database";
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            mockServerDatabase.Setup(x => x.ListTablesAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UnauthorizedAccessException(expectedErrorMessage));
            
            var configuration = TestHelpers.CreateConfiguration();
            var tool = new ServerListTablesTool(mockServerDatabase.Object, configuration);
            
            // Act
            var result = await tool.ListTablesInDatabase(databaseName);
            
            // Assert
            result.Should().Contain(expectedErrorMessage);
            result.Should().Contain("listing tables");
            
            // Verify the server database was called
            mockServerDatabase.Verify(x => x.ListTablesAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "SLTT-009: ListTablesInDatabase handles SQL connection timeout")]
        public async Task SLTT009()
        {
            // Arrange
            var databaseName = "TestDB";
            var expectedErrorMessage = "Connection timeout";
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            mockServerDatabase.Setup(x => x.ListTablesAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TimeoutException(expectedErrorMessage));
            
            var configuration = TestHelpers.CreateConfiguration();
            var tool = new ServerListTablesTool(mockServerDatabase.Object, configuration);
            
            // Act
            var result = await tool.ListTablesInDatabase(databaseName);
            
            // Assert
            result.Should().Contain(expectedErrorMessage);
            result.Should().Contain("listing tables");
            
            // Verify the server database was called
            mockServerDatabase.Verify(x => x.ListTablesAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "SLTT-010: ListTablesInDatabase works with different database names")]
        public async Task SLTT010()
        {
            // Arrange
            var databaseName = "CustomDatabaseName";
            var tableList = new List<TableInfo>
            {
                new TableInfo(
                    Schema: "custom",
                    Name: "CustomTable",
                    RowCount: 100,
                    SizeMB: 1.0,
                    CreateDate: DateTime.Now,
                    ModifyDate: DateTime.Now,
                    IndexCount: 1,
                    ForeignKeyCount: 0,
                    TableType: "BASE TABLE"
                )
            };
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            mockServerDatabase.Setup(x => x.ListTablesAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(tableList);
            
            var configuration = TestHelpers.CreateConfiguration();
            var tool = new ServerListTablesTool(mockServerDatabase.Object, configuration);
            
            // Act
            var result = await tool.ListTablesInDatabase(databaseName);
            
            // Assert
            result.Should().NotBeNull();
            
            // Verify the server database was called with the custom database name
            mockServerDatabase.Verify(x => x.ListTablesAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "SLTT-011: ListTablesInDatabase handles tables with special characters in names")]
        public async Task SLTT011()
        {
            // Arrange
            var databaseName = "TestDB";
            var tableList = new List<TableInfo>
            {
                new TableInfo(
                    Schema: "dbo",
                    Name: "Table With Spaces",
                    RowCount: 50,
                    SizeMB: 0.5,
                    CreateDate: DateTime.Now,
                    ModifyDate: DateTime.Now,
                    IndexCount: 1,
                    ForeignKeyCount: 0,
                    TableType: "BASE TABLE"
                ),
                new TableInfo(
                    Schema: "test",
                    Name: "Table_With_Underscores",
                    RowCount: 75,
                    SizeMB: 1.2,
                    CreateDate: DateTime.Now,
                    ModifyDate: DateTime.Now,
                    IndexCount: 2,
                    ForeignKeyCount: 1,
                    TableType: "BASE TABLE"
                )
            };
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            mockServerDatabase.Setup(x => x.ListTablesAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(tableList);
            
            var configuration = TestHelpers.CreateConfiguration();
            var tool = new ServerListTablesTool(mockServerDatabase.Object, configuration);
            
            // Act
            var result = await tool.ListTablesInDatabase(databaseName);
            
            // Assert
            result.Should().NotBeNull();
            
            // Verify the server database was called
            mockServerDatabase.Verify(x => x.ListTablesAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "SLTT-012: ListTablesInDatabase handles tables with zero rows")]
        public async Task SLTT012()
        {
            // Arrange
            var databaseName = "TestDB";
            var tableList = new List<TableInfo>
            {
                new TableInfo(
                    Schema: "dbo",
                    Name: "EmptyTable",
                    RowCount: 0,
                    SizeMB: 0.0,
                    CreateDate: DateTime.Now,
                    ModifyDate: DateTime.Now,
                    IndexCount: 1,
                    ForeignKeyCount: 0,
                    TableType: "BASE TABLE"
                )
            };
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            mockServerDatabase.Setup(x => x.ListTablesAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(tableList);
            
            var configuration = TestHelpers.CreateConfiguration();
            var tool = new ServerListTablesTool(mockServerDatabase.Object, configuration);
            
            // Act
            var result = await tool.ListTablesInDatabase(databaseName);
            
            // Assert
            result.Should().NotBeNull();
            
            // Verify the server database was called
            mockServerDatabase.Verify(x => x.ListTablesAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "SLTT-013: ListTablesInDatabase with timeout parameter passes through correctly")]
        public async Task SLTT013()
        {
            // Arrange
            var databaseName = "TestDB";
            var timeoutSeconds = 45;
            var tableList = new List<TableInfo>
            {
                new TableInfo(
                    Schema: "dbo",
                    Name: "TestTable",
                    RowCount: 100,
                    SizeMB: 2.0,
                    CreateDate: DateTime.Now,
                    ModifyDate: DateTime.Now,
                    IndexCount: 1,
                    ForeignKeyCount: 0,
                    TableType: "BASE TABLE"
                )
            };
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            mockServerDatabase.Setup(x => x.ListTablesAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), timeoutSeconds, It.IsAny<CancellationToken>()))
                .ReturnsAsync(tableList);
            
            var configuration = TestHelpers.CreateConfiguration();
            var tool = new ServerListTablesTool(mockServerDatabase.Object, configuration);
            
            // Act
            var result = await tool.ListTablesInDatabase(databaseName, timeoutSeconds);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("TestTable");
            
            // Verify the timeout parameter was passed through correctly
            mockServerDatabase.Verify(x => x.ListTablesAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), timeoutSeconds, It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "SLTT-014: ListTablesInDatabase with null timeout uses default")]
        public async Task SLTT014()
        {
            // Arrange
            var databaseName = "TestDB";
            var tableList = new List<TableInfo>();
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            mockServerDatabase.Setup(x => x.ListTablesAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(tableList);
            
            var configuration = TestHelpers.CreateConfiguration();
            var tool = new ServerListTablesTool(mockServerDatabase.Object, configuration);
            
            // Act
            var result = await tool.ListTablesInDatabase(databaseName, null);
            
            // Assert
            result.Should().NotBeNull();
            
            // Verify null timeout was passed through
            mockServerDatabase.Verify(x => x.ListTablesAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), null, It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "SLTT-015: ListTablesInDatabase verifies timeout parameter is passed through correctly")]
        public async Task SLTT015()
        {
            // Arrange
            var databaseName = "TestDB";
            var specificTimeout = 120;
            var tableList = new List<TableInfo>
            {
                new TableInfo(
                    Schema: "dbo",
                    Name: "TimeoutTestTable",
                    RowCount: 200,
                    SizeMB: 3.5,
                    CreateDate: DateTime.Now,
                    ModifyDate: DateTime.Now,
                    IndexCount: 2,
                    ForeignKeyCount: 1,
                    TableType: "BASE TABLE"
                )
            };
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            mockServerDatabase.Setup(x => x.ListTablesAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), specificTimeout, It.IsAny<CancellationToken>()))
                .ReturnsAsync(tableList);
            
            var configuration = TestHelpers.CreateConfiguration();
            var tool = new ServerListTablesTool(mockServerDatabase.Object, configuration);
            
            // Act
            var result = await tool.ListTablesInDatabase(databaseName, specificTimeout);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("TimeoutTestTable");
            
            // Verify the exact timeout value was passed
            mockServerDatabase.Verify(x => x.ListTablesAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), specificTimeout, It.IsAny<CancellationToken>()), Times.Once);
            
            // Verify it was not called with any other timeout value
            mockServerDatabase.Verify(x => x.ListTablesAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.Is<int?>(t => t != specificTimeout), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}


