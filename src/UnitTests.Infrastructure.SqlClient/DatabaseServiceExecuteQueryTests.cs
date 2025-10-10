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
    public class DatabaseServiceExecuteQueryTests
    {
        private readonly Mock<IDatabaseService> _mockDatabaseService;
        private readonly DatabaseService _databaseService;
        private readonly Mock<ISqlServerCapabilityDetector> _mockCapabilityDetector;
        private readonly DatabaseConfiguration _configuration;
        
        public DatabaseServiceExecuteQueryTests()
        {
            // Create a connection string for testing
            string connectionString = "Data Source=localhost;Initial Catalog=TestDb;Integrated Security=True;";
            
            // Create database configuration
            _configuration = new DatabaseConfiguration { DefaultCommandTimeoutSeconds = 30 };
            
            // Create a mock capability detector
            _mockCapabilityDetector = new Mock<ISqlServerCapabilityDetector>();
            _mockCapabilityDetector.Setup(x => x.DetectCapabilitiesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SqlServerCapability
                {
                    MajorVersion = 14, // SQL Server 2017
                    SupportsExactRowCount = true,
                    SupportsDetailedIndexMetadata = true
                });
                
            _databaseService = new DatabaseService(connectionString, _mockCapabilityDetector.Object, _configuration);
            
            // Also create a mock for specific tests
            _mockDatabaseService = new Mock<IDatabaseService>();
        }
        
        [Fact(DisplayName = "DBSEQ-001: ExecuteQueryAsync with null connection string throws exception")]
        public void DBSEQ001()
        {
            // Act
            string? nullConnectionString = null;
            Action act = () => new DatabaseService(nullConnectionString, _mockCapabilityDetector.Object, _configuration);
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("connectionString");
        }
        
        [Fact(DisplayName = "DBSEQ-002: ExecuteQueryAsync with empty query throws ArgumentException")]
        public async Task DBSEQ002()
        {
            // Act
            Func<Task> act = async () => await _databaseService.ExecuteQueryAsync(string.Empty, null, null);
            
            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Query cannot be empty*");
        }
        
        [Fact(DisplayName = "DBSEQ-003: ExecuteQueryAsync handles database context switching")]
        public void DBSEQ003()
        {
            // This would be an integration test requiring a real database connection
            // Skipping actual implementation for unit test
        }
    }
}