using System;
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
    public class ExecuteQueryToolTests
    {
        [Fact(DisplayName = "EQT-001: ExecuteQueryTool constructor with null database context throws ArgumentNullException")]
        public void EQT001()
        {
            // Arrange
            var mockOptions = new Mock<IOptions<DatabaseConfiguration>>();
            mockOptions.Setup(o => o.Value).Returns(new DatabaseConfiguration { TotalToolCallTimeoutSeconds = null });
            
            // Act
            IDatabaseContext? nullContext = null;
            Action act = () => new ExecuteQueryTool(nullContext, mockOptions.Object);
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("databaseContext");
        }
        
        [Fact(DisplayName = "EQT-002: ExecuteQueryTool constructor with null configuration throws ArgumentNullException")]
        public void EQT002()
        {
            // Arrange
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            
            // Act
            IOptions<DatabaseConfiguration>? nullOptions = null;
            Action act = () => new ExecuteQueryTool(mockDatabaseContext.Object, nullOptions);
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("configuration");
        }
        
        [Fact(DisplayName = "EQT-003: ExecuteQueryTool returns error for empty query")]
        public async Task EQT003()
        {
            // Arrange
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            var mockOptions = new Mock<IOptions<DatabaseConfiguration>>();
            mockOptions.Setup(o => o.Value).Returns(new DatabaseConfiguration { TotalToolCallTimeoutSeconds = null });
            
            var tool = new ExecuteQueryTool(mockDatabaseContext.Object, mockOptions.Object);
            
            // Act
            var result = await tool.ExecuteQuery(string.Empty);
            
            // Assert
            result.Should().Contain("Error: Query cannot be empty");
        }
        
        [Fact(DisplayName = "EQT-004: ExecuteQueryTool returns error for null query")]
        public async Task EQT004()
        {
            // Arrange
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            var mockOptions = new Mock<IOptions<DatabaseConfiguration>>();
            mockOptions.Setup(o => o.Value).Returns(new DatabaseConfiguration { TotalToolCallTimeoutSeconds = null });
            var tool = new ExecuteQueryTool(mockDatabaseContext.Object, mockOptions.Object);
            
            // Act
            var result = await tool.ExecuteQuery(null);
            
            // Assert
            result.Should().Contain("Error: Query cannot be empty");
        }
        
        [Fact(DisplayName = "EQT-005: ExecuteQueryTool returns error for whitespace query")]
        public async Task EQT005()
        {
            // Arrange
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            var mockOptions = new Mock<IOptions<DatabaseConfiguration>>();
            mockOptions.Setup(o => o.Value).Returns(new DatabaseConfiguration { TotalToolCallTimeoutSeconds = null });
            var tool = new ExecuteQueryTool(mockDatabaseContext.Object, mockOptions.Object);
            
            // Act
            var result = await tool.ExecuteQuery("   ");
            
            // Assert
            result.Should().Contain("Error: Query cannot be empty");
        }
        
        [Fact(DisplayName = "EQT-006: ExecuteQueryTool executes query successfully")]
        public async Task EQT006()
        {
            // Arrange
            var query = "SELECT 1";
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            var mockReader = new Mock<IAsyncDataReader>();
            
            // Setup reader to return a simple result
            mockReader.Setup(x => x.ReadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => false); // No rows to read
            mockReader.Setup(x => x.FieldCount)
                .Returns(0);
            
            mockDatabaseContext.Setup(x => x.ExecuteQueryAsync(
                query, 
                It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockReader.Object);
            
            var mockOptions = new Mock<IOptions<DatabaseConfiguration>>();
            mockOptions.Setup(o => o.Value).Returns(new DatabaseConfiguration { TotalToolCallTimeoutSeconds = null });
            var tool = new ExecuteQueryTool(mockDatabaseContext.Object, mockOptions.Object);
            
            // Act
            var result = await tool.ExecuteQuery(query);
            
            // Assert
            result.Should().NotBeNull();
            mockDatabaseContext.Verify(x => x.ExecuteQueryAsync(
                query,
                It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }
        
        [Fact(DisplayName = "EQT-007: ExecuteQueryTool executes query with timeout")]
        public async Task EQT007()
        {
            // Arrange
            var query = "SELECT 1";
            var timeout = 30;
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            var mockReader = new Mock<IAsyncDataReader>();
            
            mockReader.Setup(x => x.ReadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => false);
            mockReader.Setup(x => x.FieldCount)
                .Returns(0);
            
            mockDatabaseContext.Setup(x => x.ExecuteQueryAsync(
                query, 
                It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(),
                timeout,
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockReader.Object);
            
            var mockOptions = new Mock<IOptions<DatabaseConfiguration>>();
            mockOptions.Setup(o => o.Value).Returns(new DatabaseConfiguration { TotalToolCallTimeoutSeconds = null });
            var tool = new ExecuteQueryTool(mockDatabaseContext.Object, mockOptions.Object);
            
            // Act
            var result = await tool.ExecuteQuery(query, timeout);
            
            // Assert
            result.Should().NotBeNull();
            mockDatabaseContext.Verify(x => x.ExecuteQueryAsync(
                query,
                It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(),
                timeout,
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }
        
        [Fact(DisplayName = "EQT-008: ExecuteQueryTool handles exception from database context")]
        public async Task EQT008()
        {
            // Arrange
            var query = "SELECT 1";
            var expectedErrorMessage = "Database connection failed";
            
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            mockDatabaseContext.Setup(x => x.ExecuteQueryAsync(
                query, 
                It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(expectedErrorMessage));
            
            var mockOptions = new Mock<IOptions<DatabaseConfiguration>>();
            mockOptions.Setup(o => o.Value).Returns(new DatabaseConfiguration { TotalToolCallTimeoutSeconds = null });
            var tool = new ExecuteQueryTool(mockDatabaseContext.Object, mockOptions.Object);
            
            // Act
            var result = await tool.ExecuteQuery(query);
            
            // Assert
            result.Should().Contain(expectedErrorMessage);
        }
    }
}