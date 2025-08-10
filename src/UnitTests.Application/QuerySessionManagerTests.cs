using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using Core.Application;
using Core.Application.Interfaces;
using Core.Application.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace UnitTests.Application
{
    public class QuerySessionManagerTests
    {
        private readonly Mock<IDatabaseService> _mockDatabaseService;
        private readonly Mock<ILogger<QuerySessionManager>> _mockLogger;
        private readonly DatabaseConfiguration _configuration;
        private readonly QuerySessionManager _sessionManager;
        private readonly Fixture _fixture;

        public QuerySessionManagerTests()
        {
            _fixture = new Fixture();
            _mockDatabaseService = new Mock<IDatabaseService>();
            _mockLogger = new Mock<ILogger<QuerySessionManager>>();
            _configuration = new DatabaseConfiguration 
            { 
                MaxConcurrentSessions = 5,
                SessionCleanupIntervalMinutes = 30
            };
            
            var optionsWrapper = new OptionsWrapper<DatabaseConfiguration>(_configuration);
            _sessionManager = new QuerySessionManager(_mockDatabaseService.Object, _mockLogger.Object, optionsWrapper);
        }

        [Fact(DisplayName = "QSM-001: StartQueryAsync creates new session with correct properties")]
        public async Task QSM001()
        {
            // Arrange
            var query = "SELECT * FROM Users";
            var databaseName = "TestDB";
            var timeoutSeconds = 60;
            
            // Set up mock to return a mock data reader that will block execution
            var mockDataReader = new Mock<IAsyncDataReader>();
            mockDataReader.Setup(x => x.ReadAsync(It.IsAny<CancellationToken>()))
                          .Returns(Task.FromResult(false)); // No data to read
            mockDataReader.Setup(x => x.FieldCount).Returns(0);
            
            _mockDatabaseService.Setup(x => x.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(mockDataReader.Object);

            // Act
            var session = await _sessionManager.StartQueryAsync(query, databaseName, timeoutSeconds);
            
            // Give a small delay to allow background task to start
            await Task.Delay(10);

            // Assert
            session.Should().NotBeNull();
            session.SessionId.Should().BeGreaterThan(0);
            session.Query.Should().Be(query);
            session.DatabaseName.Should().Be(databaseName);
            session.TimeoutSeconds.Should().Be(timeoutSeconds);
            session.Type.Should().Be(QuerySessionType.Query);
            session.StartTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact(DisplayName = "QSM-002: StartStoredProcedureAsync creates new session with correct properties")]
        public async Task QSM002()
        {
            // Arrange
            var procedureName = "sp_GetUserData";
            var parameters = new Dictionary<string, object?> { { "UserId", 123 } };
            var databaseName = "TestDB";
            var timeoutSeconds = 30;
            
            // Set up mock to return a mock data reader
            var mockDataReader = new Mock<IAsyncDataReader>();
            mockDataReader.Setup(x => x.ReadAsync(It.IsAny<CancellationToken>()))
                          .Returns(Task.FromResult(false));
            mockDataReader.Setup(x => x.FieldCount).Returns(0);
            
            _mockDatabaseService.Setup(x => x.ExecuteStoredProcedureAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>(), It.IsAny<string>(), It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(mockDataReader.Object);

            // Act
            var session = await _sessionManager.StartStoredProcedureAsync(procedureName, parameters, databaseName, timeoutSeconds);
            
            // Give a small delay to allow background task to start
            await Task.Delay(10);

            // Assert
            session.Should().NotBeNull();
            session.SessionId.Should().BeGreaterThan(0);
            session.Query.Should().Be(procedureName);
            session.DatabaseName.Should().Be(databaseName);
            session.Parameters.Should().BeEquivalentTo(parameters);
            session.TimeoutSeconds.Should().Be(timeoutSeconds);
            session.Type.Should().Be(QuerySessionType.StoredProcedure);
            session.StartTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact(DisplayName = "QSM-003: StartQueryAsync throws exception when query is null or empty")]
        public async Task QSM003()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _sessionManager.StartQueryAsync("", "TestDB", 30));
            
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _sessionManager.StartQueryAsync(null!, "TestDB", 30));
            
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _sessionManager.StartQueryAsync("   ", "TestDB", 30));
        }

        [Fact(DisplayName = "QSM-004: StartStoredProcedureAsync throws exception when procedure name is null or empty")]
        public async Task QSM004()
        {
            // Arrange
            var parameters = new Dictionary<string, object?>();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _sessionManager.StartStoredProcedureAsync("", parameters, "TestDB", 30));
            
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _sessionManager.StartStoredProcedureAsync(null!, parameters, "TestDB", 30));
            
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _sessionManager.StartStoredProcedureAsync("   ", parameters, "TestDB", 30));
        }

        [Fact(DisplayName = "QSM-005: GetSession returns correct session by ID")]
        public async Task QSM005()
        {
            // Arrange
            var query = "SELECT * FROM Products";
            var session = await _sessionManager.StartQueryAsync(query, "TestDB", 30);

            // Act
            var retrievedSession = _sessionManager.GetSession(session.SessionId);

            // Assert
            retrievedSession.Should().NotBeNull();
            retrievedSession!.SessionId.Should().Be(session.SessionId);
            retrievedSession.Query.Should().Be(query);
        }

        [Fact(DisplayName = "QSM-006: GetSession returns null for non-existent session ID")]
        public void QSM006()
        {
            // Act
            var session = _sessionManager.GetSession(999);

            // Assert
            session.Should().BeNull();
        }

        [Fact(DisplayName = "QSM-007: CancelSession cancels running session successfully")]
        public async Task QSM007()
        {
            // Arrange - Set up mock to keep session running
            var tcs = new TaskCompletionSource<bool>();
            var mockDataReader = new Mock<IAsyncDataReader>();
            mockDataReader.Setup(x => x.ReadAsync(It.IsAny<CancellationToken>()))
                          .Returns(tcs.Task);
            mockDataReader.Setup(x => x.FieldCount).Returns(0);
            
            _mockDatabaseService.Setup(x => x.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(mockDataReader.Object);

            var session = await _sessionManager.StartQueryAsync("SELECT * FROM Orders", "TestDB", 30);
            
            // Give time for background task to start
            await Task.Delay(50);

            // Act
            var result = _sessionManager.CancelSession(session.SessionId);

            // Assert
            result.Should().BeTrue();
            session.IsRunning.Should().BeFalse();
            session.EndTime.Should().NotBeNull();
            session.Error.Should().Be("Session was cancelled by user");
            
            // Clean up
            tcs.SetResult(false);
        }

        [Fact(DisplayName = "QSM-008: CancelSession returns false for non-existent session")]
        public void QSM008()
        {
            // Act
            var result = _sessionManager.CancelSession(999);

            // Assert
            result.Should().BeFalse();
        }

        [Fact(DisplayName = "QSM-009: GetAllSessions returns running sessions by default")]
        public async Task QSM009()
        {
            // Arrange - Set up mocks to keep sessions running longer
            var tcs1 = new TaskCompletionSource<bool>();
            var tcs2 = new TaskCompletionSource<bool>();
            
            var mockDataReader1 = new Mock<IAsyncDataReader>();
            mockDataReader1.Setup(x => x.ReadAsync(It.IsAny<CancellationToken>()))
                          .Returns(tcs1.Task); // This will keep the session running
            mockDataReader1.Setup(x => x.FieldCount).Returns(0);
            
            var mockDataReader2 = new Mock<IAsyncDataReader>();
            mockDataReader2.Setup(x => x.ReadAsync(It.IsAny<CancellationToken>()))
                          .Returns(tcs2.Task); // This will keep the session running
            mockDataReader2.Setup(x => x.FieldCount).Returns(0);
            
            _mockDatabaseService.SetupSequence(x => x.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(mockDataReader1.Object)
                               .ReturnsAsync(mockDataReader2.Object);

            var session1 = await _sessionManager.StartQueryAsync("SELECT 1", "TestDB", 30);
            var session2 = await _sessionManager.StartQueryAsync("SELECT 2", "TestDB", 30);
            
            // Give time for background tasks to start
            await Task.Delay(50);
            
            _sessionManager.CancelSession(session2.SessionId); // Cancel one session

            // Act
            var runningSessions = _sessionManager.GetAllSessions(includeCompleted: false);

            // Assert
            runningSessions.Should().HaveCount(1);
            runningSessions.Should().Contain(s => s.SessionId == session1.SessionId);
            runningSessions.Should().NotContain(s => s.SessionId == session2.SessionId);
            
            // Clean up - Complete the remaining task
            tcs1.SetResult(false);
        }

        [Fact(DisplayName = "QSM-010: GetAllSessions includes completed sessions when requested")]
        public async Task QSM010()
        {
            // Arrange - Set up mocks to return immediately so sessions complete quickly
            var mockDataReader = new Mock<IAsyncDataReader>();
            mockDataReader.Setup(x => x.ReadAsync(It.IsAny<CancellationToken>()))
                          .Returns(Task.FromResult(false));
            mockDataReader.Setup(x => x.FieldCount).Returns(0);
            
            _mockDatabaseService.Setup(x => x.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(mockDataReader.Object);

            var session1 = await _sessionManager.StartQueryAsync("SELECT 1", "TestDB", 30);
            var session2 = await _sessionManager.StartQueryAsync("SELECT 2", "TestDB", 30);
            
            // Give time for background tasks to complete
            await Task.Delay(100);

            // Act
            var allSessions = _sessionManager.GetAllSessions(includeCompleted: true);

            // Assert
            allSessions.Should().HaveCount(2);
            allSessions.Should().Contain(s => s.SessionId == session1.SessionId);
            allSessions.Should().Contain(s => s.SessionId == session2.SessionId);
        }

        [Fact(DisplayName = "QSM-011: Starting sessions beyond max concurrent limit throws exception")]
        public async Task QSM011()
        {
            // Arrange - Set up mocks to keep sessions running by returning an incomplete task
            var tcs = new TaskCompletionSource<bool>();
            var mockDataReader = new Mock<IAsyncDataReader>();
            mockDataReader.Setup(x => x.ReadAsync(It.IsAny<CancellationToken>()))
                          .Returns(tcs.Task); // This will keep all sessions running
            mockDataReader.Setup(x => x.FieldCount).Returns(0);
            
            _mockDatabaseService.Setup(x => x.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(mockDataReader.Object);
            
            // Create maximum number of sessions
            for (int i = 0; i < _configuration.MaxConcurrentSessions; i++)
            {
                await _sessionManager.StartQueryAsync($"SELECT {i}", "TestDB", 30);
            }
            
            // Give time for background tasks to start
            await Task.Delay(100);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sessionManager.StartQueryAsync("SELECT overflow", "TestDB", 30));
                
            // Clean up - Complete the task to allow sessions to finish
            tcs.SetResult(false);
        }

        [Fact(DisplayName = "QSM-012: Constructor throws exception for null dependencies")]
        public void QSM012()
        {
            // Arrange
            var options = new OptionsWrapper<DatabaseConfiguration>(_configuration);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new QuerySessionManager(null!, _mockLogger.Object, options));
            
            Assert.Throws<ArgumentNullException>(() => 
                new QuerySessionManager(_mockDatabaseService.Object, null!, options));
            
            Assert.Throws<ArgumentNullException>(() => 
                new QuerySessionManager(_mockDatabaseService.Object, _mockLogger.Object, null!));
        }

        [Fact(DisplayName = "QSM-013: CleanupCompletedSessions removes old completed sessions")]
        public async Task QSM013()
        {
            // Arrange - Set up mocks to complete immediately
            var mockDataReader = new Mock<IAsyncDataReader>();
            mockDataReader.Setup(x => x.ReadAsync(It.IsAny<CancellationToken>()))
                          .Returns(Task.FromResult(false));
            mockDataReader.Setup(x => x.FieldCount).Returns(0);
            
            _mockDatabaseService.Setup(x => x.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(mockDataReader.Object);

            var session1 = await _sessionManager.StartQueryAsync("SELECT 1", "TestDB", 30);
            var session2 = await _sessionManager.StartQueryAsync("SELECT 2", "TestDB", 30);
            
            // Give time for background tasks to complete
            await Task.Delay(100);
            
            // Use reflection to set EndTime to simulate old sessions
            var endTime = DateTime.UtcNow.AddMinutes(-_configuration.SessionCleanupIntervalMinutes - 1);
            session1.EndTime = endTime;
            session2.EndTime = DateTime.UtcNow; // Recent completion

            // Act
            await _sessionManager.CleanupCompletedSessions();

            // Assert
            _sessionManager.GetSession(session1.SessionId).Should().BeNull();
            _sessionManager.GetSession(session2.SessionId).Should().NotBeNull();
        }

        [Fact(DisplayName = "QSM-014: Sessions have unique sequential IDs")]
        public async Task QSM014()
        {
            // Arrange - Set up mocks
            var mockDataReader = new Mock<IAsyncDataReader>();
            mockDataReader.Setup(x => x.ReadAsync(It.IsAny<CancellationToken>()))
                          .Returns(Task.FromResult(false));
            mockDataReader.Setup(x => x.FieldCount).Returns(0);
            
            _mockDatabaseService.Setup(x => x.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(mockDataReader.Object);
                               
            _mockDatabaseService.Setup(x => x.ExecuteStoredProcedureAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>(), It.IsAny<string>(), It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(mockDataReader.Object);

            // Act
            var session1 = await _sessionManager.StartQueryAsync("SELECT 1", "TestDB", 30);
            var session2 = await _sessionManager.StartQueryAsync("SELECT 2", "TestDB", 30);
            var session3 = await _sessionManager.StartStoredProcedureAsync("sp_Test", null, "TestDB", 30);

            // Assert
            session1.SessionId.Should().BeLessThan(session2.SessionId);
            session2.SessionId.Should().BeLessThan(session3.SessionId);
            
            // All IDs should be unique
            new[] { session1.SessionId, session2.SessionId, session3.SessionId }
                .Should().OnlyHaveUniqueItems();
        }
    }
}