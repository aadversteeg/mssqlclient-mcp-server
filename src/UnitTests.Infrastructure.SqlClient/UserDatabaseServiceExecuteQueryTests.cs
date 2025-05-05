using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Infrastructure.SqlClient;
using Core.Infrastructure.SqlClient.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace UnitTests.Infrastructure.SqlClient
{
    public class UserDatabaseServiceExecuteQueryTests
    {
        private readonly Mock<IDatabaseService> _mockDatabaseService;
        private readonly UserDatabaseService _userDatabaseService;
        
        public UserDatabaseServiceExecuteQueryTests()
        {
            _mockDatabaseService = new Mock<IDatabaseService>();
            _userDatabaseService = new UserDatabaseService(_mockDatabaseService.Object);
        }
        
        [Fact(DisplayName = "UDSEQ-001: ExecuteQueryAsync delegates to database service with null database name")]
        public async Task UDSEQ001()
        {
            // Arrange
            string query = "SELECT * FROM Users";
            var mockDataReader = new Mock<IAsyncDataReader>();
            
            _mockDatabaseService.Setup(x => x.ExecuteQueryAsync(query, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockDataReader.Object);
            
            // Act
            var result = await _userDatabaseService.ExecuteQueryAsync(query);
            
            // Assert
            result.Should().Be(mockDataReader.Object);
            _mockDatabaseService.Verify(x => x.ExecuteQueryAsync(query, null, It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "UDSEQ-002: ExecuteQueryAsync with empty query throws ArgumentException")]
        public async Task UDSEQ002()
        {
            // Act
            Func<Task> act = async () => await _userDatabaseService.ExecuteQueryAsync(string.Empty);
            
            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Query cannot be empty*");
        }
        
        [Fact(DisplayName = "UDSEQ-003: ExecuteQueryAsync passes cancellation token to database service")]
        public async Task UDSEQ003()
        {
            // Arrange
            string query = "SELECT * FROM Users";
            var cancellationToken = new CancellationToken();
            var mockDataReader = new Mock<IAsyncDataReader>();
            
            _mockDatabaseService.Setup(x => x.ExecuteQueryAsync(query, null, cancellationToken))
                .ReturnsAsync(mockDataReader.Object);
            
            // Act
            await _userDatabaseService.ExecuteQueryAsync(query, cancellationToken);
            
            // Assert
            _mockDatabaseService.Verify(x => x.ExecuteQueryAsync(query, null, cancellationToken), Times.Once);
        }
    }
}