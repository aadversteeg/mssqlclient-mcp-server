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
    public class ServerDatabaseServiceTests
    {
        private readonly Mock<IDatabaseService> _mockDatabaseService;
        private readonly ServerDatabaseService _serverDatabaseService;
        
        public ServerDatabaseServiceTests()
        {
            _mockDatabaseService = new Mock<IDatabaseService>();
            _serverDatabaseService = new ServerDatabaseService(_mockDatabaseService.Object);
        }
        
        [Fact(DisplayName = "SDS-001: Constructor with null database service throws ArgumentNullException")]
        public void SDS001()
        {
            // Act
            IDatabaseService? nullService = null;
            Action act = () => new ServerDatabaseService(nullService);
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("databaseService");
        }
        
        [Fact(DisplayName = "SDS-002: Constructor works with any database, not just master")]
        public void SDS002()
        {
            // Arrange
            var mockNonMasterService = new Mock<IDatabaseService>();
            mockNonMasterService.Setup(x => x.GetCurrentDatabaseName())
                .Returns("NotMaster");
            
            // Act - Should not throw an exception
            var serverDbService = new ServerDatabaseService(mockNonMasterService.Object);
            
            // Assert
            serverDbService.Should().NotBeNull();
        }
        
        [Fact(DisplayName = "SDS-003: ListTablesAsync delegates to database service with provided database name")]
        public async Task SDS003()
        {
            // Arrange
            var databaseName = "TestDb";
            var expectedTables = new List<TableInfo>
            {
                new TableInfo("dbo", "Table1", 10, 1.5, DateTime.Now, DateTime.Now, 2, 1, "Normal"),
                new TableInfo("dbo", "Table2", 5, 0.5, DateTime.Now, DateTime.Now, 1, 0, "Normal")
            };
            
            _mockDatabaseService.Setup(x => x.ListTablesAsync(databaseName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedTables);
            
            _mockDatabaseService.Setup(x => x.DoesDatabaseExistAsync(databaseName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            
            // Act
            var result = await _serverDatabaseService.ListTablesAsync(databaseName);
            
            // Assert
            result.Should().BeEquivalentTo(expectedTables);
            _mockDatabaseService.Verify(x => x.ListTablesAsync(databaseName, It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "SDS-004: ListTablesAsync with empty database name throws ArgumentException")]
        public async Task SDS004()
        {
            // Act
            Func<Task> act = async () => await _serverDatabaseService.ListTablesAsync(string.Empty);
            
            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Database name cannot be empty*");
        }
        
        [Fact(DisplayName = "SDS-005: ListTablesAsync with non-existent database throws InvalidOperationException")]
        public async Task SDS005()
        {
            // Arrange
            var databaseName = "NonExistentDb";
            
            _mockDatabaseService.Setup(x => x.DoesDatabaseExistAsync(databaseName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            
            // Act
            Func<Task> act = async () => await _serverDatabaseService.ListTablesAsync(databaseName);
            
            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage($"*Database '{databaseName}' does not exist*");
        }
        
        [Fact(DisplayName = "SDS-006: ListTablesAsync passes cancellation token to database service")]
        public async Task SDS006()
        {
            // Arrange
            var databaseName = "TestDb";
            var cancellationToken = new CancellationToken();
            
            _mockDatabaseService.Setup(x => x.DoesDatabaseExistAsync(databaseName, cancellationToken))
                .ReturnsAsync(true);
            
            // Act
            await _serverDatabaseService.ListTablesAsync(databaseName, cancellationToken);
            
            // Assert
            _mockDatabaseService.Verify(x => x.DoesDatabaseExistAsync(databaseName, cancellationToken), Times.Once);
            _mockDatabaseService.Verify(x => x.ListTablesAsync(databaseName, cancellationToken), Times.Once);
        }
        
        [Fact(DisplayName = "SDS-007: ListDatabasesAsync delegates to database service")]
        public async Task SDS007()
        {
            // Arrange
            var expectedDatabases = new List<DatabaseInfo>
            {
                new DatabaseInfo("master", "ONLINE", 100.5, "sa", "150", "SQL_Latin1_General_CP1_CI_AS", DateTime.Now, "SIMPLE", false),
                new DatabaseInfo("TestDb", "ONLINE", 50.2, "sa", "150", "SQL_Latin1_General_CP1_CI_AS", DateTime.Now, "SIMPLE", false)
            };
            
            _mockDatabaseService.Setup(x => x.ListDatabasesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedDatabases);
            
            // Act
            var result = await _serverDatabaseService.ListDatabasesAsync();
            
            // Assert
            result.Should().BeEquivalentTo(expectedDatabases);
            _mockDatabaseService.Verify(x => x.ListDatabasesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "SDS-008: ListDatabasesAsync passes cancellation token to database service")]
        public async Task SDS008()
        {
            // Arrange
            var cancellationToken = new CancellationToken();
            
            // Act
            await _serverDatabaseService.ListDatabasesAsync(cancellationToken);
            
            // Assert
            _mockDatabaseService.Verify(x => x.ListDatabasesAsync(cancellationToken), Times.Once);
        }
    }
}