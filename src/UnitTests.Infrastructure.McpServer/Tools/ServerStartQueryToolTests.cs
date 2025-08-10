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
    public class ServerStartQueryToolTests
    {
        [Fact(DisplayName = "SSQT-001: ServerStartQueryTool constructor with null session manager throws ArgumentNullException")]
        public void SSQT001()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<ServerStartQueryTool>>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration());
            
            // Act
            IQuerySessionManager? nullSessionManager = null;
            Action act = () => new ServerStartQueryTool(nullSessionManager, mockLogger.Object, mockConfiguration.Object);
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("sessionManager");
        }
        
        [Fact(DisplayName = "SSQT-002: ServerStartQueryTool constructor with null logger throws ArgumentNullException")]
        public void SSQT002()
        {
            // Arrange
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration());
            
            // Act
            ILogger<ServerStartQueryTool>? nullLogger = null;
            Action act = () => new ServerStartQueryTool(mockSessionManager.Object, nullLogger, mockConfiguration.Object);
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }
        
        [Fact(DisplayName = "SSQT-003: ServerStartQueryTool constructor with null configuration throws ArgumentNullException")]
        public void SSQT003()
        {
            // Arrange
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<ServerStartQueryTool>>();
            
            // Act
            IOptions<DatabaseConfiguration>? nullConfiguration = null;
            Action act = () => new ServerStartQueryTool(mockSessionManager.Object, mockLogger.Object, nullConfiguration);
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("configuration");
        }
        
        [Fact(DisplayName = "SSQT-004: StartQueryInDatabase returns error for empty database name")]
        public async Task SSQT004()
        {
            // Arrange
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<ServerStartQueryTool>>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration { DefaultCommandTimeoutSeconds = 30 });
            
            var tool = new ServerStartQueryTool(mockSessionManager.Object, mockLogger.Object, mockConfiguration.Object);
            
            // Act
            var result = await tool.StartQueryInDatabase(string.Empty, "SELECT 1");
            
            // Assert
            result.Should().Contain("error");
            
            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("Database name cannot be empty");
        }
        
        [Fact(DisplayName = "SSQT-005: StartQueryInDatabase returns error for null database name")]
        public async Task SSQT005()
        {
            // Arrange
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<ServerStartQueryTool>>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration { DefaultCommandTimeoutSeconds = 30 });
            
            var tool = new ServerStartQueryTool(mockSessionManager.Object, mockLogger.Object, mockConfiguration.Object);
            
            // Act
            var result = await tool.StartQueryInDatabase(null, "SELECT 1");
            
            // Assert
            result.Should().Contain("error");
            
            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("Database name cannot be empty");
        }
        
        [Fact(DisplayName = "SSQT-006: StartQueryInDatabase returns error for whitespace database name")]
        public async Task SSQT006()
        {
            // Arrange
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<ServerStartQueryTool>>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration { DefaultCommandTimeoutSeconds = 30 });
            
            var tool = new ServerStartQueryTool(mockSessionManager.Object, mockLogger.Object, mockConfiguration.Object);
            
            // Act
            var result = await tool.StartQueryInDatabase("   ", "SELECT 1");
            
            // Assert
            result.Should().Contain("error");
            
            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("Database name cannot be empty");
        }
        
        [Fact(DisplayName = "SSQT-007: StartQueryInDatabase returns error for empty query")]
        public async Task SSQT007()
        {
            // Arrange
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<ServerStartQueryTool>>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration { DefaultCommandTimeoutSeconds = 30 });
            
            var tool = new ServerStartQueryTool(mockSessionManager.Object, mockLogger.Object, mockConfiguration.Object);
            
            // Act
            var result = await tool.StartQueryInDatabase("TestDB", string.Empty);
            
            // Assert
            result.Should().Contain("error");
            
            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("Query cannot be empty");
        }
        
        [Fact(DisplayName = "SSQT-008: StartQueryInDatabase returns error for null query")]
        public async Task SSQT008()
        {
            // Arrange
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<ServerStartQueryTool>>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration { DefaultCommandTimeoutSeconds = 30 });
            
            var tool = new ServerStartQueryTool(mockSessionManager.Object, mockLogger.Object, mockConfiguration.Object);
            
            // Act
            var result = await tool.StartQueryInDatabase("TestDB", null);
            
            // Assert
            result.Should().Contain("error");
            
            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("Query cannot be empty");
        }
        
        [Fact(DisplayName = "SSQT-009: StartQueryInDatabase returns error for whitespace query")]
        public async Task SSQT009()
        {
            // Arrange
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<ServerStartQueryTool>>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration { DefaultCommandTimeoutSeconds = 30 });
            
            var tool = new ServerStartQueryTool(mockSessionManager.Object, mockLogger.Object, mockConfiguration.Object);
            
            // Act
            var result = await tool.StartQueryInDatabase("TestDB", "   ");
            
            // Assert
            result.Should().Contain("error");
            
            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("Query cannot be empty");
        }
        
        [Fact(DisplayName = "SSQT-010: StartQueryInDatabase starts session successfully with default timeout")]
        public async Task SSQT010()
        {
            // Arrange
            var databaseName = "TestDB";
            var query = "SELECT * FROM Users";
            var sessionId = 123;
            var defaultTimeout = 30;
            
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<ServerStartQueryTool>>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration { DefaultCommandTimeoutSeconds = defaultTimeout });
            
            var session = new QuerySession
            {
                SessionId = sessionId,
                Type = QuerySessionType.Query,
                Query = query,
                DatabaseName = databaseName,
                StartTime = DateTime.UtcNow,
                EndTime = null,
                IsRunning = true,
                RowCount = 0,
                Error = null,
                TimeoutSeconds = defaultTimeout,
                Results = new StringBuilder(),
                Parameters = new Dictionary<string, object?>()
            };
            
            mockSessionManager.Setup(x => x.StartQueryAsync(
                query, 
                databaseName, 
                defaultTimeout, 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            
            var tool = new ServerStartQueryTool(mockSessionManager.Object, mockLogger.Object, mockConfiguration.Object);
            
            // Act
            var result = await tool.StartQueryInDatabase(databaseName, query);
            
            // Assert
            result.Should().NotBeNull();
            
            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("sessionId").GetInt32().Should().Be(sessionId);
            jsonDoc.RootElement.GetProperty("query").GetString().Should().Be(query);
            jsonDoc.RootElement.GetProperty("databaseName").GetString().Should().Be(databaseName);
            jsonDoc.RootElement.GetProperty("timeoutSeconds").GetInt32().Should().Be(defaultTimeout);
            jsonDoc.RootElement.GetProperty("status").GetString().Should().Be("running");
            
            mockSessionManager.Verify(x => x.StartQueryAsync(
                query, 
                databaseName, 
                defaultTimeout, 
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }
        
        [Fact(DisplayName = "SSQT-011: StartQueryInDatabase starts session successfully with custom timeout")]
        public async Task SSQT011()
        {
            // Arrange
            var databaseName = "TestDB";
            var query = "SELECT COUNT(*) FROM BigTable";
            var sessionId = 456;
            var customTimeout = 120;
            var defaultTimeout = 30;
            
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<ServerStartQueryTool>>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration { DefaultCommandTimeoutSeconds = defaultTimeout });
            
            var session = new QuerySession
            {
                SessionId = sessionId,
                Type = QuerySessionType.Query,
                Query = query,
                DatabaseName = databaseName,
                StartTime = DateTime.UtcNow,
                EndTime = null,
                IsRunning = true,
                RowCount = 0,
                Error = null,
                TimeoutSeconds = customTimeout,
                Results = new StringBuilder(),
                Parameters = new Dictionary<string, object?>()
            };
            
            mockSessionManager.Setup(x => x.StartQueryAsync(
                query, 
                databaseName, 
                customTimeout, 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            
            var tool = new ServerStartQueryTool(mockSessionManager.Object, mockLogger.Object, mockConfiguration.Object);
            
            // Act
            var result = await tool.StartQueryInDatabase(databaseName, query, customTimeout);
            
            // Assert
            result.Should().NotBeNull();
            
            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("sessionId").GetInt32().Should().Be(sessionId);
            jsonDoc.RootElement.GetProperty("query").GetString().Should().Be(query);
            jsonDoc.RootElement.GetProperty("databaseName").GetString().Should().Be(databaseName);
            jsonDoc.RootElement.GetProperty("timeoutSeconds").GetInt32().Should().Be(customTimeout);
            jsonDoc.RootElement.GetProperty("status").GetString().Should().Be("running");
            
            mockSessionManager.Verify(x => x.StartQueryAsync(
                query, 
                databaseName, 
                customTimeout, 
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }
        
        [Fact(DisplayName = "SSQT-012: StartQueryInDatabase handles exception from session manager")]
        public async Task SSQT012()
        {
            // Arrange
            var databaseName = "TestDB";
            var query = "SELECT * FROM NonExistentTable";
            var expectedErrorMessage = "Table 'NonExistentTable' doesn't exist";
            
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<ServerStartQueryTool>>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration { DefaultCommandTimeoutSeconds = 30 });
            
            mockSessionManager.Setup(x => x.StartQueryAsync(
                query, 
                databaseName, 
                It.IsAny<int>(), 
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(expectedErrorMessage));
            
            var tool = new ServerStartQueryTool(mockSessionManager.Object, mockLogger.Object, mockConfiguration.Object);
            
            // Act
            var result = await tool.StartQueryInDatabase(databaseName, query);
            
            // Assert
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
        
        [Fact(DisplayName = "SSQT-013: StartQueryInDatabase logs information when starting session")]
        public async Task SSQT013()
        {
            // Arrange
            var databaseName = "TestDB";
            var query = "SELECT 1";
            var sessionId = 789;
            var defaultTimeout = 60;
            
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<ServerStartQueryTool>>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration { DefaultCommandTimeoutSeconds = defaultTimeout });
            
            var session = new QuerySession
            {
                SessionId = sessionId,
                Type = QuerySessionType.Query,
                Query = query,
                DatabaseName = databaseName,
                StartTime = DateTime.UtcNow,
                EndTime = null,
                IsRunning = true,
                RowCount = 0,
                Error = null,
                TimeoutSeconds = defaultTimeout,
                Results = new StringBuilder(),
                Parameters = new Dictionary<string, object?>()
            };
            
            mockSessionManager.Setup(x => x.StartQueryAsync(
                query, 
                databaseName, 
                defaultTimeout, 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            
            var tool = new ServerStartQueryTool(mockSessionManager.Object, mockLogger.Object, mockConfiguration.Object);
            
            // Act
            var result = await tool.StartQueryInDatabase(databaseName, query);
            
            // Assert
            result.Should().NotBeNull();
            
            // Verify information logging occurred
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Starting query session")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        
        [Fact(DisplayName = "SSQT-014: StartQueryInDatabase includes correct message in response")]
        public async Task SSQT014()
        {
            // Arrange
            var databaseName = "TestDB";
            var query = "SELECT TOP 100 * FROM Orders";
            var sessionId = 999;
            var defaultTimeout = 45;
            
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<ServerStartQueryTool>>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration { DefaultCommandTimeoutSeconds = defaultTimeout });
            
            var session = new QuerySession
            {
                SessionId = sessionId,
                Type = QuerySessionType.Query,
                Query = query,
                DatabaseName = databaseName,
                StartTime = DateTime.UtcNow,
                EndTime = null,
                IsRunning = true,
                RowCount = 0,
                Error = null,
                TimeoutSeconds = defaultTimeout,
                Results = new StringBuilder(),
                Parameters = new Dictionary<string, object?>()
            };
            
            mockSessionManager.Setup(x => x.StartQueryAsync(
                query, 
                databaseName, 
                defaultTimeout, 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            
            var tool = new ServerStartQueryTool(mockSessionManager.Object, mockLogger.Object, mockConfiguration.Object);
            
            // Act
            var result = await tool.StartQueryInDatabase(databaseName, query);
            
            // Assert
            result.Should().NotBeNull();
            
            // Verify it's valid JSON and contains expected message
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("message").GetString().Should().Be("Query started successfully. Use get_session_status to check progress.");
        }
    }
}


