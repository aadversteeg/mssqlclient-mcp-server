using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Models;
using Core.Infrastructure.SqlClient;
using Core.Infrastructure.SqlClient.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace UnitTests.Infrastructure.SqlClient
{
    public class MasterDatabaseServiceTests
    {
        private readonly Mock<IDatabaseService> _mockDatabaseService;
        private readonly MasterDatabaseService _masterDatabaseService;
        
        public MasterDatabaseServiceTests()
        {
            _mockDatabaseService = new Mock<IDatabaseService>();
            
            // Setup GetCurrentDatabaseName to return "master"
            _mockDatabaseService.Setup(x => x.GetCurrentDatabaseName())
                .Returns("master");
            
            _masterDatabaseService = new MasterDatabaseService(_mockDatabaseService.Object);
        }
        
        [Fact(DisplayName = "MDS-001: Constructor with null database service throws ArgumentNullException")]
        public void MDS001()
        {
            // Act
            IDatabaseService? nullService = null;
            Action act = () => new MasterDatabaseService(nullService);
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("databaseService");
        }
        
        [Fact(DisplayName = "MDS-002: Constructor with non-master database service throws ArgumentException")]
        public void MDS002()
        {
            // Arrange
            var mockNonMasterService = new Mock<IDatabaseService>();
            mockNonMasterService.Setup(x => x.GetCurrentDatabaseName())
                .Returns("NotMaster");
            
            // Act
            Action act = () => new MasterDatabaseService(mockNonMasterService.Object);
            
            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*master database*");
        }
        
        [Fact(DisplayName = "MDS-003: ListTablesAsync delegates to database service with provided database name")]
        public async Task MDS003()
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
            var result = await _masterDatabaseService.ListTablesAsync(databaseName);
            
            // Assert
            result.Should().BeEquivalentTo(expectedTables);
            _mockDatabaseService.Verify(x => x.ListTablesAsync(databaseName, It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "MDS-004: ListTablesAsync with empty database name throws ArgumentException")]
        public async Task MDS004()
        {
            // Act
            Func<Task> act = async () => await _masterDatabaseService.ListTablesAsync(string.Empty);
            
            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Database name cannot be empty*");
        }
        
        [Fact(DisplayName = "MDS-005: ListTablesAsync with non-existent database throws InvalidOperationException")]
        public async Task MDS005()
        {
            // Arrange
            var databaseName = "NonExistentDb";
            
            _mockDatabaseService.Setup(x => x.DoesDatabaseExistAsync(databaseName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            
            // Act
            Func<Task> act = async () => await _masterDatabaseService.ListTablesAsync(databaseName);
            
            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage($"*Database '{databaseName}' does not exist*");
        }
        
        [Fact(DisplayName = "MDS-006: ListTablesAsync passes cancellation token to database service")]
        public async Task MDS006()
        {
            // Arrange
            var databaseName = "TestDb";
            var cancellationToken = new CancellationToken();
            
            _mockDatabaseService.Setup(x => x.DoesDatabaseExistAsync(databaseName, cancellationToken))
                .ReturnsAsync(true);
            
            // Act
            await _masterDatabaseService.ListTablesAsync(databaseName, cancellationToken);
            
            // Assert
            _mockDatabaseService.Verify(x => x.DoesDatabaseExistAsync(databaseName, cancellationToken), Times.Once);
            _mockDatabaseService.Verify(x => x.ListTablesAsync(databaseName, cancellationToken), Times.Once);
        }
        
        [Fact(DisplayName = "MDS-007: ListDatabasesAsync delegates to database service")]
        public async Task MDS007()
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
            var result = await _masterDatabaseService.ListDatabasesAsync();
            
            // Assert
            result.Should().BeEquivalentTo(expectedDatabases);
            _mockDatabaseService.Verify(x => x.ListDatabasesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "MDS-008: ListDatabasesAsync passes cancellation token to database service")]
        public async Task MDS008()
        {
            // Arrange
            var cancellationToken = new CancellationToken();
            
            // Act
            await _masterDatabaseService.ListDatabasesAsync(cancellationToken);
            
            // Assert
            _mockDatabaseService.Verify(x => x.ListDatabasesAsync(cancellationToken), Times.Once);
        }
    }
}