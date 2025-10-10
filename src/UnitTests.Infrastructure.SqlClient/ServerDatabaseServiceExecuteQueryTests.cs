using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Infrastructure.SqlClient;
using Core.Infrastructure.SqlClient.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace UnitTests.Infrastructure.SqlClient
{
    public class ServerDatabaseServiceExecuteQueryTests
    {
        private readonly Mock<IDatabaseService> _mockDatabaseService;
        private readonly ServerDatabaseService _serverDatabaseService;
        
        public ServerDatabaseServiceExecuteQueryTests()
        {
            _mockDatabaseService = new Mock<IDatabaseService>();
            _serverDatabaseService = new ServerDatabaseService(_mockDatabaseService.Object);
        }
        
        [Fact(DisplayName = "SDSEQ-001: ExecuteQueryInDatabaseAsync delegates to database service with provided database name")]
        public async Task SDSEQ001()
        {
            // Arrange
            string databaseName = "TestDb";
            string query = "SELECT * FROM Users";
            var mockDataReader = new Mock<IAsyncDataReader>();
            
            _mockDatabaseService.Setup(x => x.DoesDatabaseExistAsync(databaseName, It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
                
            _mockDatabaseService.Setup(x => x.ExecuteQueryAsync(query, databaseName, It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockDataReader.Object);
            
            // Act
            var result = await _serverDatabaseService.ExecuteQueryInDatabaseAsync(databaseName, query, null);
            
            // Assert
            result.Should().Be(mockDataReader.Object);
            _mockDatabaseService.Verify(x => x.ExecuteQueryAsync(query, databaseName, It.IsAny<ToolCallTimeoutContext?>(), null, It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "SDSEQ-002: ExecuteQueryInDatabaseAsync with empty database name throws ArgumentException")]
        public async Task SDSEQ002()
        {
            // Arrange
            string query = "SELECT * FROM Users";
            
            // Act
            Func<Task> act = async () => await _serverDatabaseService.ExecuteQueryInDatabaseAsync(string.Empty, query, null);
            
            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Database name cannot be empty*");
        }
        
        [Fact(DisplayName = "SDSEQ-003: ExecuteQueryInDatabaseAsync with empty query throws ArgumentException")]
        public async Task SDSEQ003()
        {
            // Arrange
            string databaseName = "TestDb";
            
            // Act
            Func<Task> act = async () => await _serverDatabaseService.ExecuteQueryInDatabaseAsync(databaseName, string.Empty, null);
            
            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Query cannot be empty*");
        }
        
        [Fact(DisplayName = "SDSEQ-004: ExecuteQueryInDatabaseAsync with non-existent database throws InvalidOperationException")]
        public async Task SDSEQ004()
        {
            // Arrange
            string databaseName = "NonExistentDb";
            string query = "SELECT * FROM Users";
            
            _mockDatabaseService.Setup(x => x.DoesDatabaseExistAsync(databaseName, It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            
            // Act
            Func<Task> act = async () => await _serverDatabaseService.ExecuteQueryInDatabaseAsync(databaseName, query, null);
            
            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage($"*Database '{databaseName}' does not exist*");
        }
        
        [Fact(DisplayName = "SDSEQ-005: ExecuteQueryInDatabaseAsync passes cancellation token to database service")]
        public async Task SDSEQ005()
        {
            // Arrange
            string databaseName = "TestDb";
            string query = "SELECT * FROM Users";
            var cancellationToken = new CancellationToken();
            var mockDataReader = new Mock<IAsyncDataReader>();
            
            _mockDatabaseService.Setup(x => x.DoesDatabaseExistAsync(databaseName, It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), cancellationToken))
                .ReturnsAsync(true);
                
            _mockDatabaseService.Setup(x => x.ExecuteQueryAsync(query, databaseName, It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), cancellationToken))
                .ReturnsAsync(mockDataReader.Object);
            
            // Act
            await _serverDatabaseService.ExecuteQueryInDatabaseAsync(databaseName, query, null, null, cancellationToken);
            
            // Assert
            _mockDatabaseService.Verify(x => x.DoesDatabaseExistAsync(databaseName, It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), cancellationToken), Times.Once);
            _mockDatabaseService.Verify(x => x.ExecuteQueryAsync(query, databaseName, It.IsAny<ToolCallTimeoutContext?>(), null, cancellationToken), Times.Once);
        }
        
        [Fact(DisplayName = "SDSEQ-006: ExecuteQueryInDatabaseAsync with timeout passes timeout to database service")]
        public async Task SDSEQ006()
        {
            // Arrange
            string databaseName = "TestDb";
            string query = "SELECT * FROM Users";
            int timeoutSeconds = 120;
            var mockDataReader = new Mock<IAsyncDataReader>();
            
            _mockDatabaseService.Setup(x => x.DoesDatabaseExistAsync(databaseName, It.IsAny<ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
                
            _mockDatabaseService.Setup(x => x.ExecuteQueryAsync(query, databaseName, It.IsAny<ToolCallTimeoutContext?>(), timeoutSeconds, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockDataReader.Object);
            
            // Act
            var result = await _serverDatabaseService.ExecuteQueryInDatabaseAsync(databaseName, query, null, timeoutSeconds);
            
            // Assert
            result.Should().Be(mockDataReader.Object);
            _mockDatabaseService.Verify(x => x.ExecuteQueryAsync(query, databaseName, It.IsAny<ToolCallTimeoutContext?>(), timeoutSeconds, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}