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
    public class ServerStartStoredProcedureToolTests
    {
        [Fact(DisplayName = "SSPRT-001: ServerStartStoredProcedureTool constructor with null session manager throws ArgumentNullException")]
        public void SSPRT001()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<ServerStartStoredProcedureTool>>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration());
            
            // Act
            IQuerySessionManager? nullSessionManager = null;
            Action act = () => new ServerStartStoredProcedureTool(nullSessionManager, mockLogger.Object, mockConfiguration.Object);
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("sessionManager");
        }
        
        [Fact(DisplayName = "SSPRT-002: ServerStartStoredProcedureTool constructor with null logger throws ArgumentNullException")]
        public void SSPRT002()
        {
            // Arrange
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration());
            
            // Act
            ILogger<ServerStartStoredProcedureTool>? nullLogger = null;
            Action act = () => new ServerStartStoredProcedureTool(mockSessionManager.Object, nullLogger, mockConfiguration.Object);
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }
        
        [Fact(DisplayName = "SSPRT-003: ServerStartStoredProcedureTool constructor with null configuration throws ArgumentNullException")]
        public void SSPRT003()
        {
            // Arrange
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<ServerStartStoredProcedureTool>>();
            
            // Act
            IOptions<DatabaseConfiguration>? nullConfiguration = null;
            Action act = () => new ServerStartStoredProcedureTool(mockSessionManager.Object, mockLogger.Object, nullConfiguration);
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("configuration");
        }
        
        [Fact(DisplayName = "SSPRT-004: StartStoredProcedureInDatabase returns error for empty database name")]
        public async Task SSPRT004()
        {
            // Arrange
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<ServerStartStoredProcedureTool>>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration { DefaultCommandTimeoutSeconds = 30 });
            
            var tool = new ServerStartStoredProcedureTool(mockSessionManager.Object, mockLogger.Object, mockConfiguration.Object);
            
            // Act
            var result = await tool.StartStoredProcedureInDatabase(string.Empty, "TestProcedure");
            
            // Assert
            result.Should().Contain("error");
            
            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("Database name cannot be empty");
        }
        
        [Fact(DisplayName = "SSPRT-005: StartStoredProcedureInDatabase returns error for null database name")]
        public async Task SSPRT005()
        {
            // Arrange
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<ServerStartStoredProcedureTool>>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration { DefaultCommandTimeoutSeconds = 30 });
            
            var tool = new ServerStartStoredProcedureTool(mockSessionManager.Object, mockLogger.Object, mockConfiguration.Object);
            
            // Act
            var result = await tool.StartStoredProcedureInDatabase(null, "TestProcedure");
            
            // Assert
            result.Should().Contain("error");
            
            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("Database name cannot be empty");
        }
        
        [Fact(DisplayName = "SSPRT-006: StartStoredProcedureInDatabase returns error for whitespace database name")]
        public async Task SSPRT006()
        {
            // Arrange
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<ServerStartStoredProcedureTool>>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration { DefaultCommandTimeoutSeconds = 30 });
            
            var tool = new ServerStartStoredProcedureTool(mockSessionManager.Object, mockLogger.Object, mockConfiguration.Object);
            
            // Act
            var result = await tool.StartStoredProcedureInDatabase("   ", "TestProcedure");
            
            // Assert
            result.Should().Contain("error");
            
            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("Database name cannot be empty");
        }
        
        [Fact(DisplayName = "SSPRT-007: StartStoredProcedureInDatabase returns error for empty procedure name")]
        public async Task SSPRT007()
        {
            // Arrange
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<ServerStartStoredProcedureTool>>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration { DefaultCommandTimeoutSeconds = 30 });
            
            var tool = new ServerStartStoredProcedureTool(mockSessionManager.Object, mockLogger.Object, mockConfiguration.Object);
            
            // Act
            var result = await tool.StartStoredProcedureInDatabase("TestDB", string.Empty);
            
            // Assert
            result.Should().Contain("error");
            
            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("Procedure name cannot be empty");
        }
        
        [Fact(DisplayName = "SSPRT-008: StartStoredProcedureInDatabase returns error for null procedure name")]
        public async Task SSPRT008()
        {
            // Arrange
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<ServerStartStoredProcedureTool>>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration { DefaultCommandTimeoutSeconds = 30 });
            
            var tool = new ServerStartStoredProcedureTool(mockSessionManager.Object, mockLogger.Object, mockConfiguration.Object);
            
            // Act
            var result = await tool.StartStoredProcedureInDatabase("TestDB", null);
            
            // Assert
            result.Should().Contain("error");
            
            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("Procedure name cannot be empty");
        }
        
        [Fact(DisplayName = "SSPRT-009: StartStoredProcedureInDatabase returns error for whitespace procedure name")]
        public async Task SSPRT009()
        {
            // Arrange
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<ServerStartStoredProcedureTool>>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration { DefaultCommandTimeoutSeconds = 30 });
            
            var tool = new ServerStartStoredProcedureTool(mockSessionManager.Object, mockLogger.Object, mockConfiguration.Object);
            
            // Act
            var result = await tool.StartStoredProcedureInDatabase("TestDB", "   ");
            
            // Assert
            result.Should().Contain("error");
            
            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("Procedure name cannot be empty");
        }
        
        [Fact(DisplayName = "SSPRT-010: StartStoredProcedureInDatabase starts session successfully with default parameters")]
        public async Task SSPRT010()
        {
            // Arrange
            var databaseName = "TestDB";
            var procedureName = "dbo.GetAllUsers";
            var sessionId = 123;
            var defaultTimeout = 30;
            
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<ServerStartStoredProcedureTool>>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration { DefaultCommandTimeoutSeconds = defaultTimeout });
            
            var session = new QuerySession
            {
                SessionId = sessionId,
                Type = QuerySessionType.StoredProcedure,
                Query = procedureName,
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
            
            mockSessionManager.Setup(x => x.StartStoredProcedureAsync(
                procedureName, 
                It.IsAny<Dictionary<string, object?>>(),
                databaseName, 
                defaultTimeout, 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            
            var tool = new ServerStartStoredProcedureTool(mockSessionManager.Object, mockLogger.Object, mockConfiguration.Object);
            
            // Act
            var result = await tool.StartStoredProcedureInDatabase(databaseName, procedureName);
            
            // Assert
            result.Should().NotBeNull();
            
            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("sessionId").GetInt32().Should().Be(sessionId);
            jsonDoc.RootElement.GetProperty("procedureName").GetString().Should().Be(procedureName);
            jsonDoc.RootElement.GetProperty("databaseName").GetString().Should().Be(databaseName);
            jsonDoc.RootElement.GetProperty("timeoutSeconds").GetInt32().Should().Be(defaultTimeout);
            jsonDoc.RootElement.GetProperty("status").GetString().Should().Be("running");
            
            mockSessionManager.Verify(x => x.StartStoredProcedureAsync(
                procedureName, 
                It.IsAny<Dictionary<string, object?>>(),
                databaseName, 
                defaultTimeout, 
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }
        
        [Fact(DisplayName = "SSPRT-011: StartStoredProcedureInDatabase starts session successfully with custom parameters and timeout")]
        public async Task SSPRT011()
        {
            // Arrange
            var databaseName = "TestDB";
            var procedureName = "dbo.GetUserById";
            var sessionId = 456;
            var customTimeout = 120;
            var defaultTimeout = 30;
            var parametersJson = "{\"UserId\": 123, \"IncludeDetails\": true}";
            
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<ServerStartStoredProcedureTool>>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration { DefaultCommandTimeoutSeconds = defaultTimeout });
            
            var session = new QuerySession
            {
                SessionId = sessionId,
                Type = QuerySessionType.StoredProcedure,
                Query = procedureName,
                DatabaseName = databaseName,
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
                databaseName, 
                customTimeout, 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            
            var tool = new ServerStartStoredProcedureTool(mockSessionManager.Object, mockLogger.Object, mockConfiguration.Object);
            
            // Act
            var result = await tool.StartStoredProcedureInDatabase(databaseName, procedureName, parametersJson, customTimeout);
            
            // Assert
            result.Should().NotBeNull();
            
            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("sessionId").GetInt32().Should().Be(sessionId);
            jsonDoc.RootElement.GetProperty("procedureName").GetString().Should().Be(procedureName);
            jsonDoc.RootElement.GetProperty("databaseName").GetString().Should().Be(databaseName);
            jsonDoc.RootElement.GetProperty("timeoutSeconds").GetInt32().Should().Be(customTimeout);
            jsonDoc.RootElement.GetProperty("status").GetString().Should().Be("running");
            
            // Verify parameters are included in response
            var parametersElement = jsonDoc.RootElement.GetProperty("parameters");
            parametersElement.GetProperty("UserId").GetInt32().Should().Be(123);
            parametersElement.GetProperty("IncludeDetails").GetBoolean().Should().Be(true);
            
            mockSessionManager.Verify(x => x.StartStoredProcedureAsync(
                procedureName, 
                It.IsAny<Dictionary<string, object?>>(),
                databaseName, 
                customTimeout, 
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }
        
        [Fact(DisplayName = "SSPRT-012: StartStoredProcedureInDatabase returns error for invalid JSON parameters")]
        public async Task SSPRT012()
        {
            // Arrange
            var databaseName = "TestDB";
            var procedureName = "dbo.TestProcedure";
            var invalidJson = "{invalid json";
            
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<ServerStartStoredProcedureTool>>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration { DefaultCommandTimeoutSeconds = 30 });
            
            var tool = new ServerStartStoredProcedureTool(mockSessionManager.Object, mockLogger.Object, mockConfiguration.Object);
            
            // Act
            var result = await tool.StartStoredProcedureInDatabase(databaseName, procedureName, invalidJson);
            
            // Assert
            result.Should().Contain("error");
            
            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("Error parsing parameters");
        }
        
        [Fact(DisplayName = "SSPRT-013: StartStoredProcedureInDatabase handles exception from session manager")]
        public async Task SSPRT013()
        {
            // Arrange
            var databaseName = "TestDB";
            var procedureName = "dbo.TestProcedure";
            var expectedErrorMessage = "Stored procedure not found";
            
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<ServerStartStoredProcedureTool>>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration { DefaultCommandTimeoutSeconds = 30 });
            
            mockSessionManager.Setup(x => x.StartStoredProcedureAsync(
                procedureName, 
                It.IsAny<Dictionary<string, object?>>(),
                databaseName, 
                It.IsAny<int>(), 
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(expectedErrorMessage));
            
            var tool = new ServerStartStoredProcedureTool(mockSessionManager.Object, mockLogger.Object, mockConfiguration.Object);
            
            // Act
            var result = await tool.StartStoredProcedureInDatabase(databaseName, procedureName);
            
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
        
        [Fact(DisplayName = "SSPRT-014: StartStoredProcedureInDatabase logs information when starting session")]
        public async Task SSPRT014()
        {
            // Arrange
            var databaseName = "TestDB";
            var procedureName = "dbo.LogTestProcedure";
            var sessionId = 789;
            var defaultTimeout = 60;
            
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<ServerStartStoredProcedureTool>>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration { DefaultCommandTimeoutSeconds = defaultTimeout });
            
            var session = new QuerySession
            {
                SessionId = sessionId,
                Type = QuerySessionType.StoredProcedure,
                Query = procedureName,
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
            
            mockSessionManager.Setup(x => x.StartStoredProcedureAsync(
                procedureName, 
                It.IsAny<Dictionary<string, object?>>(),
                databaseName, 
                defaultTimeout, 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            
            var tool = new ServerStartStoredProcedureTool(mockSessionManager.Object, mockLogger.Object, mockConfiguration.Object);
            
            // Act
            var result = await tool.StartStoredProcedureInDatabase(databaseName, procedureName);
            
            // Assert
            result.Should().NotBeNull();
            
            // Verify information logging occurred
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Starting stored procedure session")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        
        [Fact(DisplayName = "SSPRT-015: StartStoredProcedureInDatabase includes correct message in response")]
        public async Task SSPRT015()
        {
            // Arrange
            var databaseName = "TestDB";
            var procedureName = "dbo.ProcessOrders";
            var sessionId = 999;
            var defaultTimeout = 45;
            
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<ServerStartStoredProcedureTool>>();
            var mockConfiguration = new Mock<IOptions<DatabaseConfiguration>>();
            mockConfiguration.Setup(x => x.Value).Returns(new DatabaseConfiguration { DefaultCommandTimeoutSeconds = defaultTimeout });
            
            var session = new QuerySession
            {
                SessionId = sessionId,
                Type = QuerySessionType.StoredProcedure,
                Query = procedureName,
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
            
            mockSessionManager.Setup(x => x.StartStoredProcedureAsync(
                procedureName, 
                It.IsAny<Dictionary<string, object?>>(),
                databaseName, 
                defaultTimeout, 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            
            var tool = new ServerStartStoredProcedureTool(mockSessionManager.Object, mockLogger.Object, mockConfiguration.Object);
            
            // Act
            var result = await tool.StartStoredProcedureInDatabase(databaseName, procedureName);
            
            // Assert
            result.Should().NotBeNull();
            
            // Verify it's valid JSON and contains expected message
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("message").GetString().Should().Be("Stored procedure started successfully. Use get_session_status to check progress.");
        }
    }
}


