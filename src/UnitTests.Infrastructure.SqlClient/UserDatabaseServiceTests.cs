using System;
using System.Collections.Generic;
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
    public class UserDatabaseServiceTests
    {
        private readonly Mock<IDatabaseService> _mockDatabaseService;
        private readonly UserDatabaseService _userDatabaseService;
        
        public UserDatabaseServiceTests()
        {
            _mockDatabaseService = new Mock<IDatabaseService>();
            _userDatabaseService = new UserDatabaseService(_mockDatabaseService.Object);
        }
        
        [Fact(DisplayName = "UDS-001: Constructor with null database service throws ArgumentNullException")]
        public void UDS001()
        {
            // Act
            IDatabaseService? nullService = null;
            Action act = () => new UserDatabaseService(nullService);
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("databaseService");
        }
        
        [Fact(DisplayName = "UDS-002: ListTablesAsync delegates to database service with null database name")]
        public async Task UDS002()
        {
            // Arrange
            var expectedTables = new List<TableInfo>
            {
                new TableInfo("dbo", "Table1", 10, 1.5, DateTime.Now, DateTime.Now, 2, 1, "Normal"),
                new TableInfo("dbo", "Table2", 5, 0.5, DateTime.Now, DateTime.Now, 1, 0, "Normal")
            };
            
            _mockDatabaseService.Setup(x => x.ListTablesAsync(null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedTables);
            
            // Act
            var result = await _userDatabaseService.ListTablesAsync();
            
            // Assert
            result.Should().BeEquivalentTo(expectedTables);
            _mockDatabaseService.Verify(x => x.ListTablesAsync(null, It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "UDS-003: ListTablesAsync passes cancellation token to database service")]
        public async Task UDS003()
        {
            // Arrange
            var cancellationToken = new CancellationToken();
            
            // Act
            await _userDatabaseService.ListTablesAsync(cancellationToken);
            
            // Assert
            _mockDatabaseService.Verify(x => x.ListTablesAsync(null, cancellationToken), Times.Once);
        }
        
        [Fact(DisplayName = "UDS-004: ExecuteQueryAsync delegates to database service with null database name")]
        public async Task UDS004()
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
        
        [Fact(DisplayName = "UDS-005: ExecuteQueryAsync with empty query throws ArgumentException")]
        public async Task UDS005()
        {
            // Act
            Func<Task> act = async () => await _userDatabaseService.ExecuteQueryAsync(string.Empty);
            
            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Query cannot be empty*");
        }
        
        [Fact(DisplayName = "UDS-006: ExecuteQueryAsync passes cancellation token to database service")]
        public async Task UDS006()
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