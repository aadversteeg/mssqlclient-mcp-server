using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Infrastructure.McpServer.Tools;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace UnitTests.Infrastructure.McpServer.Tools
{
    public class StartQueryToolTests
    {
        [Fact(DisplayName = "SQT-001: StartQueryTool constructor with null session manager throws ArgumentNullException")]
        public void SQT001()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<StartQueryTool>>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration());
            
            // Act
            IQuerySessionManager? nullSessionManager = null;
            Action act = () => new StartQueryTool(nullSessionManager, mockLogger.Object, mockConfiguration.Object);
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("sessionManager");
        }
        
        [Fact(DisplayName = "SQT-002: StartQueryTool constructor with null logger throws ArgumentNullException")]
        public void SQT002()
        {
            // Arrange
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration());
            
            // Act
            ILogger<StartQueryTool>? nullLogger = null;
            Action act = () => new StartQueryTool(mockSessionManager.Object, nullLogger, mockConfiguration.Object);
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }
        
        [Fact(DisplayName = "SQT-003: StartQueryTool constructor with null configuration throws ArgumentNullException")]
        public void SQT003()
        {
            // Arrange
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<StartQueryTool>>();
            
            // Act
            IOptions<DatabaseConfiguration>? nullConfiguration = null;
            Action act = () => new StartQueryTool(mockSessionManager.Object, mockLogger.Object, nullConfiguration);
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("configuration");
        }
        
        [Fact(DisplayName = "SQT-004: StartQuery returns error for empty query")]
        public async Task SQT004()
        {
            // Arrange
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<StartQueryTool>>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration { DefaultCommandTimeoutSeconds = 30 });
            
            var tool = new StartQueryTool(mockSessionManager.Object, mockLogger.Object, mockConfiguration.Object);
            
            // Act
            var result = await tool.StartQuery(string.Empty);
            
            // Assert
            result.Should().Contain("error");
            
            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("Query cannot be empty");
        }
        
        [Fact(DisplayName = "SQT-005: StartQuery returns error for null query")]
        public async Task SQT005()
        {
            // Arrange
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<StartQueryTool>>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration { DefaultCommandTimeoutSeconds = 30 });
            
            var tool = new StartQueryTool(mockSessionManager.Object, mockLogger.Object, mockConfiguration.Object);
            
            // Act
            var result = await tool.StartQuery(null);
            
            // Assert
            result.Should().Contain("error");
            
            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("Query cannot be empty");
        }
        
        [Fact(DisplayName = "SQT-006: StartQuery returns error for whitespace query")]
        public async Task SQT006()
        {
            // Arrange
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<StartQueryTool>>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration { DefaultCommandTimeoutSeconds = 30 });
            
            var tool = new StartQueryTool(mockSessionManager.Object, mockLogger.Object, mockConfiguration.Object);
            
            // Act
            var result = await tool.StartQuery("   ");
            
            // Assert
            result.Should().Contain("error");
            
            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("Query cannot be empty");
        }
        
        [Fact(DisplayName = "SQT-007: StartQuery starts session successfully with default timeout")]
        public async Task SQT007()
        {
            // Arrange
            var query = "SELECT * FROM Users";
            var sessionId = 123;
            var defaultTimeout = 30;
            
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<StartQueryTool>>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration { DefaultCommandTimeoutSeconds = defaultTimeout });
            
            var session = new QuerySession
            {
                SessionId = sessionId,
                Type = QuerySessionType.Query,
                Query = query,
                DatabaseName = null,
                StartTime = DateTime.UtcNow,
                EndTime = null,
                IsRunning = true,
                RowCount = 0,
                Error = null,
                TimeoutSeconds = defaultTimeout,
                Results = new StringBuilder()
            };
            
            mockSessionManager.Setup(x => x.StartQueryAsync(
                query, 
                null, 
                defaultTimeout, 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            
            var tool = new StartQueryTool(mockSessionManager.Object, mockLogger.Object, mockConfiguration.Object);
            
            // Act
            var result = await tool.StartQuery(query);
            
            // Assert
            result.Should().NotBeNull();
            
            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("sessionId").GetInt32().Should().Be(sessionId);
            jsonDoc.RootElement.GetProperty("query").GetString().Should().Be(query);
            jsonDoc.RootElement.GetProperty("timeoutSeconds").GetInt32().Should().Be(defaultTimeout);
            jsonDoc.RootElement.GetProperty("status").GetString().Should().Be("running");
            
            mockSessionManager.Verify(x => x.StartQueryAsync(
                query, 
                null, 
                defaultTimeout, 
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }
        
        [Fact(DisplayName = "SQT-008: StartQuery starts session successfully with custom timeout")]
        public async Task SQT008()
        {
            // Arrange
            var query = "SELECT * FROM LargeTable";
            var sessionId = 456;
            var customTimeout = 120;
            var defaultTimeout = 30;
            
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<StartQueryTool>>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration { DefaultCommandTimeoutSeconds = defaultTimeout });
            
            var session = new QuerySession
            {
                SessionId = sessionId,
                Type = QuerySessionType.Query,
                Query = query,
                DatabaseName = null,
                StartTime = DateTime.UtcNow,
                EndTime = null,
                IsRunning = true,
                RowCount = 0,
                Error = null,
                TimeoutSeconds = customTimeout,
                Results = new StringBuilder()
            };
            
            mockSessionManager.Setup(x => x.StartQueryAsync(
                query, 
                null, 
                customTimeout, 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            
            var tool = new StartQueryTool(mockSessionManager.Object, mockLogger.Object, mockConfiguration.Object);
            
            // Act
            var result = await tool.StartQuery(query, customTimeout);
            
            // Assert
            result.Should().NotBeNull();
            
            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("sessionId").GetInt32().Should().Be(sessionId);
            jsonDoc.RootElement.GetProperty("query").GetString().Should().Be(query);
            jsonDoc.RootElement.GetProperty("timeoutSeconds").GetInt32().Should().Be(customTimeout);
            jsonDoc.RootElement.GetProperty("status").GetString().Should().Be("running");
            
            mockSessionManager.Verify(x => x.StartQueryAsync(
                query, 
                null, 
                customTimeout, 
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }
        
        [Fact(DisplayName = "SQT-009: StartQuery handles exception from session manager")]
        public async Task SQT009()
        {
            // Arrange
            var query = "SELECT * FROM Users";
            var expectedErrorMessage = "Database connection failed";
            
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<StartQueryTool>>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration { DefaultCommandTimeoutSeconds = 30 });
            
            mockSessionManager.Setup(x => x.StartQueryAsync(
                query, 
                null, 
                It.IsAny<int>(), 
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(expectedErrorMessage));
            
            var tool = new StartQueryTool(mockSessionManager.Object, mockLogger.Object, mockConfiguration.Object);
            
            // Act
            var result = await tool.StartQuery(query);
            
            // Assert
            result.Should().Contain(expectedErrorMessage);
            
            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("error").GetString().Should().Be(expectedErrorMessage);
            
            // Verify logging occurred
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to start query session")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}