using System;
using System.Collections.Generic;
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
    public class StartStoredProcedureToolTests
    {
        [Fact(DisplayName = "SSPT-001: StartStoredProcedureTool constructor with null session manager throws ArgumentNullException")]
        public void SSPT001()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<StartStoredProcedureTool>>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration());
            
            // Act
            IQuerySessionManager? nullSessionManager = null;
            Action act = () => new StartStoredProcedureTool(nullSessionManager, mockLogger.Object, mockConfiguration.Object);
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("sessionManager");
        }
        
        [Fact(DisplayName = "SSPT-002: StartStoredProcedureTool constructor with null logger throws ArgumentNullException")]
        public void SSPT002()
        {
            // Arrange
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration());
            
            // Act
            ILogger<StartStoredProcedureTool>? nullLogger = null;
            Action act = () => new StartStoredProcedureTool(mockSessionManager.Object, nullLogger, mockConfiguration.Object);
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }
        
        [Fact(DisplayName = "SSPT-003: StartStoredProcedureTool constructor with null configuration throws ArgumentNullException")]
        public void SSPT003()
        {
            // Arrange
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<StartStoredProcedureTool>>();
            
            // Act
            IOptions<DatabaseConfiguration>? nullConfiguration = null;
            Action act = () => new StartStoredProcedureTool(mockSessionManager.Object, mockLogger.Object, nullConfiguration);
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("configuration");
        }
        
        [Fact(DisplayName = "SSPT-004: StartStoredProcedure returns error for empty procedure name")]
        public async Task SSPT004()
        {
            // Arrange
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<StartStoredProcedureTool>>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration { DefaultCommandTimeoutSeconds = 30 });
            
            var tool = new StartStoredProcedureTool(mockSessionManager.Object, mockLogger.Object, mockConfiguration.Object);
            
            // Act
            var result = await tool.StartStoredProcedure(string.Empty);
            
            // Assert
            result.Should().Contain("error");
            
            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("Procedure name cannot be empty");
        }
        
        [Fact(DisplayName = "SSPT-005: StartStoredProcedure returns error for null procedure name")]
        public async Task SSPT005()
        {
            // Arrange
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<StartStoredProcedureTool>>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration { DefaultCommandTimeoutSeconds = 30 });
            
            var tool = new StartStoredProcedureTool(mockSessionManager.Object, mockLogger.Object, mockConfiguration.Object);
            
            // Act
            var result = await tool.StartStoredProcedure(null);
            
            // Assert
            result.Should().Contain("error");
            
            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("Procedure name cannot be empty");
        }
        
        [Fact(DisplayName = "SSPT-006: StartStoredProcedure returns error for whitespace procedure name")]
        public async Task SSPT006()
        {
            // Arrange
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<StartStoredProcedureTool>>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration { DefaultCommandTimeoutSeconds = 30 });
            
            var tool = new StartStoredProcedureTool(mockSessionManager.Object, mockLogger.Object, mockConfiguration.Object);
            
            // Act
            var result = await tool.StartStoredProcedure("   ");
            
            // Assert
            result.Should().Contain("error");
            
            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("Procedure name cannot be empty");
        }
        
        [Fact(DisplayName = "SSPT-007: StartStoredProcedure starts session successfully with empty parameters")]
        public async Task SSPT007()
        {
            // Arrange
            var procedureName = "dbo.GetAllUsers";
            var sessionId = 123;
            var defaultTimeout = 30;
            
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<StartStoredProcedureTool>>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration { DefaultCommandTimeoutSeconds = defaultTimeout });
            
            var session = new QuerySession
            {
                SessionId = sessionId,
                Type = QuerySessionType.StoredProcedure,
                Query = procedureName,
                DatabaseName = null,
                StartTime = DateTime.UtcNow,
                EndTime = null,
                IsRunning = true,
                RowCount = 0,
                Error = null,
                TimeoutSeconds = defaultTimeout,
                Results = new StringBuilder(),
                Parameters = new Dictionary<string, object?>()
            };
            
            mockSessionManager.Setup(x => x.StartStoredProcedureAsync(
                procedureName, 
                It.IsAny<Dictionary<string, object?>>(),
                null, 
                defaultTimeout, 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            
            var tool = new StartStoredProcedureTool(mockSessionManager.Object, mockLogger.Object, mockConfiguration.Object);
            
            // Act
            var result = await tool.StartStoredProcedure(procedureName);
            
            // Assert
            result.Should().NotBeNull();
            
            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("sessionId").GetInt32().Should().Be(sessionId);
            jsonDoc.RootElement.GetProperty("procedureName").GetString().Should().Be(procedureName);
            jsonDoc.RootElement.GetProperty("timeoutSeconds").GetInt32().Should().Be(defaultTimeout);
            jsonDoc.RootElement.GetProperty("status").GetString().Should().Be("running");
            
            mockSessionManager.Verify(x => x.StartStoredProcedureAsync(
                procedureName, 
                It.IsAny<Dictionary<string, object?>>(),
                null, 
                defaultTimeout, 
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }
        
        [Fact(DisplayName = "SSPT-008: StartStoredProcedure starts session successfully with parameters")]
        public async Task SSPT008()
        {
            // Arrange
            var procedureName = "dbo.GetUserById";
            var sessionId = 456;
            var customTimeout = 120;
            var defaultTimeout = 30;
            var parametersJson = "{\"UserId\": 123, \"IncludeDetails\": true}";
            
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<StartStoredProcedureTool>>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration { DefaultCommandTimeoutSeconds = defaultTimeout });
            
            var session = new QuerySession
            {
                SessionId = sessionId,
                Type = QuerySessionType.StoredProcedure,
                Query = procedureName,
                DatabaseName = null,
                StartTime = DateTime.UtcNow,
                EndTime = null,
                IsRunning = true,
                RowCount = 0,
                Error = null,
                TimeoutSeconds = customTimeout,
                Results = new StringBuilder(),
                Parameters = new Dictionary<string, object?> { { "UserId", 123 }, { "IncludeDetails", true } }
            };
            
            mockSessionManager.Setup(x => x.StartStoredProcedureAsync(
                procedureName, 
                It.Is<Dictionary<string, object?>>(p => p.ContainsKey("UserId") && p.ContainsKey("IncludeDetails")),
                null, 
                customTimeout, 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            
            var tool = new StartStoredProcedureTool(mockSessionManager.Object, mockLogger.Object, mockConfiguration.Object);
            
            // Act
            var result = await tool.StartStoredProcedure(procedureName, parametersJson, null, customTimeout);
            
            // Assert
            result.Should().NotBeNull();
            
            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("sessionId").GetInt32().Should().Be(sessionId);
            jsonDoc.RootElement.GetProperty("procedureName").GetString().Should().Be(procedureName);
            jsonDoc.RootElement.GetProperty("timeoutSeconds").GetInt32().Should().Be(customTimeout);
            jsonDoc.RootElement.GetProperty("status").GetString().Should().Be("running");
            
            mockSessionManager.Verify(x => x.StartStoredProcedureAsync(
                procedureName, 
                It.IsAny<Dictionary<string, object?>>(),
                null, 
                customTimeout, 
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }
        
        [Fact(DisplayName = "SSPT-009: StartStoredProcedure returns error for invalid JSON parameters")]
        public async Task SSPT009()
        {
            // Arrange
            var procedureName = "dbo.TestProcedure";
            var invalidJson = "{invalid json";
            
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<StartStoredProcedureTool>>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration { DefaultCommandTimeoutSeconds = 30 });
            
            var tool = new StartStoredProcedureTool(mockSessionManager.Object, mockLogger.Object, mockConfiguration.Object);
            
            // Act
            var result = await tool.StartStoredProcedure(procedureName, invalidJson);
            
            // Assert
            result.Should().Contain("error");
            
            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("Invalid parameters JSON");
        }
        
        [Fact(DisplayName = "SSPT-010: StartStoredProcedure handles exception from session manager")]
        public async Task SSPT010()
        {
            // Arrange
            var procedureName = "dbo.TestProcedure";
            var expectedErrorMessage = "Database connection failed";
            
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<StartStoredProcedureTool>>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration { DefaultCommandTimeoutSeconds = 30 });
            
            mockSessionManager.Setup(x => x.StartStoredProcedureAsync(
                procedureName, 
                It.IsAny<Dictionary<string, object?>>(),
                null, 
                It.IsAny<int>(), 
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(expectedErrorMessage));
            
            var tool = new StartStoredProcedureTool(mockSessionManager.Object, mockLogger.Object, mockConfiguration.Object);
            
            // Act
            var result = await tool.StartStoredProcedure(procedureName);
            
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
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to start stored procedure session")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}