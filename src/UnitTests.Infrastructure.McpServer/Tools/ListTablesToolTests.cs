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
    public class ListTablesToolTests
    {
        [Fact(DisplayName = "LTT-001: ListTablesTool constructor with null database context throws ArgumentNullException")]
        public void LTT001()
        {
            // Act
            IDatabaseContext? nullContext = null;
            Action act = () => new ListTablesTool(nullContext);
            
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
            
            mockDatabaseContext.Setup(x => x.ListTablesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(emptyTableList);
            
            var tool = new ListTablesTool(mockDatabaseContext.Object);
            
            // Act
            var result = await tool.ListTables();
            
            // Assert
            result.Should().NotBeNull();
            mockDatabaseContext.Verify(x => x.ListTablesAsync(It.IsAny<CancellationToken>()), Times.Once);
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
            
            mockDatabaseContext.Setup(x => x.ListTablesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(tableList);
            
            var tool = new ListTablesTool(mockDatabaseContext.Object);
            
            // Act
            var result = await tool.ListTables();
            
            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("Users");
            result.Should().Contain("Orders");
            mockDatabaseContext.Verify(x => x.ListTablesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "LTT-004: ListTables handles exception from database context")]
        public async Task LTT004()
        {
            // Arrange
            var expectedErrorMessage = "Database connection failed";
            
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            mockDatabaseContext.Setup(x => x.ListTablesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(expectedErrorMessage));
            
            var tool = new ListTablesTool(mockDatabaseContext.Object);
            
            // Act
            var result = await tool.ListTables();
            
            // Assert
            result.Should().Contain(expectedErrorMessage);
        }
    }
}