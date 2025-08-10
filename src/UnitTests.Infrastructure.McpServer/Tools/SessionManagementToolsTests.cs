using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Infrastructure.McpServer.Tools;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace UnitTests.Infrastructure.McpServer.Tools
{
    public class SessionManagementToolsTests
    {
        [Fact(DisplayName = "SMT-001: SessionManagementTools constructor with null session manager throws ArgumentNullException")]
        public void SMT001()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<SessionManagementTools>>();
            
            // Act
            IQuerySessionManager? nullSessionManager = null;
            Action act = () => new SessionManagementTools(nullSessionManager, mockLogger.Object);
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("sessionManager");
        }
        
        [Fact(DisplayName = "SMT-002: SessionManagementTools constructor with null logger throws ArgumentNullException")]
        public void SMT002()
        {
            // Arrange
            var mockSessionManager = new Mock<IQuerySessionManager>();
            
            // Act
            ILogger<SessionManagementTools>? nullLogger = null;
            Action act = () => new SessionManagementTools(mockSessionManager.Object, nullLogger);
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }
        
        [Fact(DisplayName = "SMT-003: GetSessionStatus returns not found error for non-existent session")]
        public void SMT003()
        {
            // Arrange
            var sessionId = 123;
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<SessionManagementTools>>();
            
            mockSessionManager.Setup(x => x.GetSession(sessionId))
                .Returns((QuerySession?)null);
            
            var tool = new SessionManagementTools(mockSessionManager.Object, mockLogger.Object);
            
            // Act
            var result = tool.GetSessionStatus(sessionId);
            
            // Assert
            result.Should().Contain("not found");
            result.Should().Contain(sessionId.ToString());
            
            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("not found");
        }
        
        [Fact(DisplayName = "SMT-004: GetSessionStatus returns session information for existing session")]
        public void SMT004()
        {
            // Arrange
            var sessionId = 123;
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<SessionManagementTools>>();
            
            var session = new QuerySession
            {
                SessionId = sessionId,
                Type = QuerySessionType.Query,
                Query = "SELECT * FROM Users",
                DatabaseName = "TestDB",
                StartTime = DateTime.UtcNow.AddMinutes(-5),
                EndTime = DateTime.UtcNow,
                IsRunning = false,
                RowCount = 100,
                Error = null,
                TimeoutSeconds = 30,
                Results = new StringBuilder("User results")
            };
            
            mockSessionManager.Setup(x => x.GetSession(sessionId))
                .Returns(session);
            
            var tool = new SessionManagementTools(mockSessionManager.Object, mockLogger.Object);
            
            // Act
            var result = tool.GetSessionStatus(sessionId);
            
            // Assert
            result.Should().NotBeNull();
            
            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("sessionId").GetInt32().Should().Be(sessionId);
            jsonDoc.RootElement.GetProperty("type").GetString().Should().Be("query");
            jsonDoc.RootElement.GetProperty("status").GetString().Should().Be("completed");
            jsonDoc.RootElement.GetProperty("isRunning").GetBoolean().Should().BeFalse();
        }
        
        [Fact(DisplayName = "SMT-005: GetSessionResults returns not found error for non-existent session")]
        public void SMT005()
        {
            // Arrange
            var sessionId = 456;
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<SessionManagementTools>>();
            
            mockSessionManager.Setup(x => x.GetSession(sessionId))
                .Returns((QuerySession?)null);
            
            var tool = new SessionManagementTools(mockSessionManager.Object, mockLogger.Object);
            
            // Act
            var result = tool.GetSessionResults(sessionId);
            
            // Assert
            result.Should().Contain("not found");
            result.Should().Contain(sessionId.ToString());
            
            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("not found");
        }
        
        [Fact(DisplayName = "SMT-006: GetSessionResults returns session results with row limit")]
        public void SMT006()
        {
            // Arrange
            var sessionId = 456;
            var maxRows = 10;
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<SessionManagementTools>>();
            
            var session = new QuerySession
            {
                SessionId = sessionId,
                Type = QuerySessionType.Query,
                Query = "SELECT * FROM Users",
                DatabaseName = "TestDB",
                StartTime = DateTime.UtcNow.AddMinutes(-2),
                EndTime = DateTime.UtcNow,
                IsRunning = false,
                RowCount = 100,
                Error = null,
                TimeoutSeconds = 30,
                Results = new StringBuilder("Header\nRow1\nRow2\nRow3\nRow4\nRow5\nRow6\nRow7\nRow8\nRow9\nRow10\nRow11\nRow12\nRow13\nRow14\nRow15")
            };
            
            mockSessionManager.Setup(x => x.GetSession(sessionId))
                .Returns(session);
            
            var tool = new SessionManagementTools(mockSessionManager.Object, mockLogger.Object);
            
            // Act
            var result = tool.GetSessionResults(sessionId, maxRows);
            
            // Assert
            result.Should().NotBeNull();
            
            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("sessionId").GetInt32().Should().Be(sessionId);
            jsonDoc.RootElement.GetProperty("maxRowsApplied").GetInt32().Should().Be(maxRows);
            jsonDoc.RootElement.GetProperty("results").GetString().Should().Contain("showing first");
        }
        
        [Fact(DisplayName = "SMT-007: StopSession returns success when session is cancelled")]
        public void SMT007()
        {
            // Arrange
            var sessionId = 789;
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<SessionManagementTools>>();
            
            mockSessionManager.Setup(x => x.CancelSession(sessionId))
                .Returns(true);
            
            var tool = new SessionManagementTools(mockSessionManager.Object, mockLogger.Object);
            
            // Act
            var result = tool.StopSession(sessionId);
            
            // Assert
            result.Should().NotBeNull();
            
            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("sessionId").GetInt32().Should().Be(sessionId);
            jsonDoc.RootElement.GetProperty("status").GetString().Should().Be("cancelled");
            jsonDoc.RootElement.GetProperty("message").GetString().Should().Contain("successfully");
        }
        
        [Fact(DisplayName = "SMT-008: StopSession returns error when session not found")]
        public void SMT008()
        {
            // Arrange
            var sessionId = 999;
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<SessionManagementTools>>();
            
            mockSessionManager.Setup(x => x.CancelSession(sessionId))
                .Returns(false);
            
            var tool = new SessionManagementTools(mockSessionManager.Object, mockLogger.Object);
            
            // Act
            var result = tool.StopSession(sessionId);
            
            // Assert
            result.Should().NotBeNull();
            
            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("not found");
        }
        
        [Fact(DisplayName = "SMT-009: ListSessions returns all sessions by default")]
        public void SMT009()
        {
            // Arrange
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<SessionManagementTools>>();
            
            var sessions = new List<QuerySession>
            {
                new QuerySession
                {
                    SessionId = 1,
                    Type = QuerySessionType.Query,
                    Query = "SELECT * FROM Users",
                    StartTime = DateTime.UtcNow.AddMinutes(-10),
                    EndTime = DateTime.UtcNow.AddMinutes(-5),
                    IsRunning = false,
                    RowCount = 50,
                    Results = new StringBuilder("Results")
                },
                new QuerySession
                {
                    SessionId = 2,
                    Type = QuerySessionType.StoredProcedure,
                    Query = "EXEC GetUserData",
                    StartTime = DateTime.UtcNow.AddMinutes(-2),
                    EndTime = null,
                    IsRunning = true,
                    RowCount = 0,
                    Results = new StringBuilder()
                }
            };
            
            mockSessionManager.Setup(x => x.GetAllSessions(true))
                .Returns(sessions);
            
            var tool = new SessionManagementTools(mockSessionManager.Object, mockLogger.Object);
            
            // Act
            var result = tool.ListSessions();
            
            // Assert
            result.Should().NotBeNull();
            
            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("filter").GetString().Should().Be("all");
            jsonDoc.RootElement.GetProperty("totalSessions").GetInt32().Should().Be(2);
            
            var sessionsArray = jsonDoc.RootElement.GetProperty("sessions");
            sessionsArray.GetArrayLength().Should().Be(2);
        }
        
        [Fact(DisplayName = "SMT-010: ListSessions filters running sessions correctly")]
        public void SMT010()
        {
            // Arrange
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<SessionManagementTools>>();
            
            var sessions = new List<QuerySession>
            {
                new QuerySession
                {
                    SessionId = 1,
                    Type = QuerySessionType.Query,
                    Query = "SELECT * FROM Users",
                    StartTime = DateTime.UtcNow.AddMinutes(-10),
                    EndTime = DateTime.UtcNow.AddMinutes(-5),
                    IsRunning = false,
                    RowCount = 50,
                    Results = new StringBuilder("Results")
                },
                new QuerySession
                {
                    SessionId = 2,
                    Type = QuerySessionType.StoredProcedure,
                    Query = "EXEC GetUserData",
                    StartTime = DateTime.UtcNow.AddMinutes(-2),
                    EndTime = null,
                    IsRunning = true,
                    RowCount = 0,
                    Results = new StringBuilder()
                }
            };
            
            mockSessionManager.Setup(x => x.GetAllSessions(false))
                .Returns(sessions.Where(s => s.IsRunning).ToList());
            
            var tool = new SessionManagementTools(mockSessionManager.Object, mockLogger.Object);
            
            // Act
            var result = tool.ListSessions("running");
            
            // Assert
            result.Should().NotBeNull();
            
            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("filter").GetString().Should().Be("running");
            jsonDoc.RootElement.GetProperty("totalSessions").GetInt32().Should().Be(1);
            
            var sessionsArray = jsonDoc.RootElement.GetProperty("sessions");
            sessionsArray.GetArrayLength().Should().Be(1);
        }
        
        [Fact(DisplayName = "SMT-011: GetSessionStatus handles exception and logs error")]
        public void SMT011()
        {
            // Arrange
            var sessionId = 123;
            var mockSessionManager = new Mock<IQuerySessionManager>();
            var mockLogger = new Mock<ILogger<SessionManagementTools>>();
            var expectedErrorMessage = "Database error occurred";
            
            mockSessionManager.Setup(x => x.GetSession(sessionId))
                .Throws(new InvalidOperationException(expectedErrorMessage));
            
            var tool = new SessionManagementTools(mockSessionManager.Object, mockLogger.Object);
            
            // Act
            var result = tool.GetSessionStatus(sessionId);
            
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
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to get session status")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}