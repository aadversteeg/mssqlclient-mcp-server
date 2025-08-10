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
    public class ListTablesToolTests
    {
        [Fact(DisplayName = "LTT-001: ListTablesTool constructor with null database context throws ArgumentNullException")]
        public void LTT001()
        {
            // Arrange
            var mockOptions = new Mock<IOptions<DatabaseConfiguration>>();
            mockOptions.Setup(o => o.Value).Returns(new DatabaseConfiguration { TotalToolCallTimeoutSeconds = null });
            
            // Act
            IDatabaseContext? nullContext = null;
            Action act = () => new ListTablesTool(nullContext, mockOptions.Object);
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("databaseContext");
        }
        
        [Fact(DisplayName = "LTT-002: ListTables returns empty list when no tables exist")]
        public async Task LTT002()
        {
            // Arrange
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            var emptyTableList = new List<TableInfo>();
            
            mockDatabaseContext.Setup(x => x.ListTablesAsync(It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(emptyTableList);
            
            var mockOptions = new Mock<IOptions<DatabaseConfiguration>>();
            mockOptions.Setup(o => o.Value).Returns(new DatabaseConfiguration { TotalToolCallTimeoutSeconds = null }); // Disable timeout
            var tool = new ListTablesTool(mockDatabaseContext.Object, mockOptions.Object);
            
            // Act
            var result = await tool.ListTables();
            
            // Assert
            result.Should().NotBeNull();
            mockDatabaseContext.Verify(x => x.ListTablesAsync(It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "LTT-003: ListTables returns formatted table list")]
        public async Task LTT003()
        {
            // Arrange
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            var tableList = new List<TableInfo>
            {
                new TableInfo(
                    Schema: "dbo",
                    Name: "Users",
                    CreateDate: DateTime.Now,
                    ModifyDate: DateTime.Now,
                    RowCount: 100,
                    SizeMB: 5.0,
                    IndexCount: 2,
                    ForeignKeyCount: 1,
                    TableType: "Normal"
                ),
                new TableInfo(
                    Schema: "dbo", 
                    Name: "Orders",
                    CreateDate: DateTime.Now,
                    ModifyDate: DateTime.Now,
                    RowCount: 500,
                    SizeMB: 10.0,
                    IndexCount: 3,
                    ForeignKeyCount: 2,
                    TableType: "Normal"
                )
            };
            
            mockDatabaseContext.Setup(x => x.ListTablesAsync(It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(tableList);
            
            var mockOptions = new Mock<IOptions<DatabaseConfiguration>>();
            mockOptions.Setup(o => o.Value).Returns(new DatabaseConfiguration { TotalToolCallTimeoutSeconds = null });
            var tool = new ListTablesTool(mockDatabaseContext.Object, mockOptions.Object);
            
            // Act
            var result = await tool.ListTables();
            
            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("Users");
            result.Should().Contain("Orders");
            mockDatabaseContext.Verify(x => x.ListTablesAsync(It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "LTT-004: ListTables handles exception from database context")]
        public async Task LTT004()
        {
            // Arrange
            var expectedErrorMessage = "Database connection failed";
            
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            mockDatabaseContext.Setup(x => x.ListTablesAsync(It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(expectedErrorMessage));
            
            var mockOptions = new Mock<IOptions<DatabaseConfiguration>>();
            mockOptions.Setup(o => o.Value).Returns(new DatabaseConfiguration { TotalToolCallTimeoutSeconds = null }); // Disable timeout
            var tool = new ListTablesTool(mockDatabaseContext.Object, mockOptions.Object);
            
            // Act
            var result = await tool.ListTables();
            
            // Assert
            result.Should().Contain(expectedErrorMessage);
        }
        
        [Fact(DisplayName = "LTT-005: ListTables with timeout parameter passes through correctly")]
        public async Task LTT005()
        {
            // Arrange
            var timeoutSeconds = 30;
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            var tableList = new List<TableInfo>();
            
            mockDatabaseContext.Setup(x => x.ListTablesAsync(It.IsAny<ToolCallTimeoutContext?>(), timeoutSeconds, It.IsAny<CancellationToken>()))
                .ReturnsAsync(tableList);
            
            var mockOptions = new Mock<IOptions<DatabaseConfiguration>>();
            mockOptions.Setup(o => o.Value).Returns(new DatabaseConfiguration { TotalToolCallTimeoutSeconds = null });
            var tool = new ListTablesTool(mockDatabaseContext.Object, mockOptions.Object);
            
            // Act
            var result = await tool.ListTables(timeoutSeconds);
            
            // Assert
            result.Should().NotBeNull();
            mockDatabaseContext.Verify(x => x.ListTablesAsync(It.IsAny<ToolCallTimeoutContext?>(), timeoutSeconds, It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "LTT-006: ListTables with null timeout uses default")]
        public async Task LTT006()
        {
            // Arrange
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            var tableList = new List<TableInfo>();
            
            mockDatabaseContext.Setup(x => x.ListTablesAsync(It.IsAny<ToolCallTimeoutContext?>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(tableList);
            
            var mockOptions = new Mock<IOptions<DatabaseConfiguration>>();
            mockOptions.Setup(o => o.Value).Returns(new DatabaseConfiguration { TotalToolCallTimeoutSeconds = null });
            var tool = new ListTablesTool(mockDatabaseContext.Object, mockOptions.Object);
            
            // Act
            var result = await tool.ListTables(null);
            
            // Assert
            result.Should().NotBeNull();
            mockDatabaseContext.Verify(x => x.ListTablesAsync(It.IsAny<ToolCallTimeoutContext?>(), null, It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "LTT-007: ListTables verifies timeout parameter is passed through correctly")]
        public async Task LTT007()
        {
            // Arrange
            var specificTimeout = 60;
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            var tableList = new List<TableInfo>
            {
                new TableInfo(
                    Schema: "dbo",
                    Name: "TestTable",
                    CreateDate: DateTime.Now,
                    ModifyDate: DateTime.Now,
                    RowCount: 10,
                    SizeMB: 1.0,
                    IndexCount: 1,
                    ForeignKeyCount: 0,
                    TableType: "Normal"
                )
            };
            
            mockDatabaseContext.Setup(x => x.ListTablesAsync(It.IsAny<ToolCallTimeoutContext?>(), specificTimeout, It.IsAny<CancellationToken>()))
                .ReturnsAsync(tableList);
            
            var mockOptions = new Mock<IOptions<DatabaseConfiguration>>();
            mockOptions.Setup(o => o.Value).Returns(new DatabaseConfiguration { TotalToolCallTimeoutSeconds = null });
            var tool = new ListTablesTool(mockDatabaseContext.Object, mockOptions.Object);
            
            // Act
            var result = await tool.ListTables(specificTimeout);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("TestTable");
            
            // Verify the exact timeout value was passed
            mockDatabaseContext.Verify(x => x.ListTablesAsync(It.IsAny<ToolCallTimeoutContext?>(), specificTimeout, It.IsAny<CancellationToken>()), Times.Once);
            
            // Verify it was not called with any other timeout value
            mockDatabaseContext.Verify(x => x.ListTablesAsync(It.IsAny<ToolCallTimeoutContext?>(), It.Is<int?>(t => t != specificTimeout), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}