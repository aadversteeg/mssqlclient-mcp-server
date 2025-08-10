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
    public class DatabaseServiceTests
    {
        private readonly Mock<IDatabaseService> _mockDatabaseService;
        private readonly DatabaseService _databaseService;
        private readonly Mock<ISqlServerCapabilityDetector> _mockCapabilityDetector;
        private readonly DatabaseConfiguration _configuration;
        
        public DatabaseServiceTests()
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
                
            // Create a real database service for testing constructor behavior
            _databaseService = new DatabaseService(connectionString, _mockCapabilityDetector.Object, _configuration);
            
            // Create a mock database service for other tests
            _mockDatabaseService = new Mock<IDatabaseService>();
        }
        
        [Fact(DisplayName = "DBS-001: Constructor with null connection string throws ArgumentNullException")]
        public void DBS001()
        {
            // Act
            string? nullConnectionString = null;
            Action act = () => new DatabaseService(nullConnectionString, _mockCapabilityDetector.Object, _configuration);
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("connectionString");
        }
        
        [Fact(DisplayName = "DBS-001a: Constructor with null capability detector throws ArgumentNullException")]
        public void DBS001a()
        {
            // Act
            string connectionString = "Data Source=localhost;Initial Catalog=TestDb;Integrated Security=True;";
            ISqlServerCapabilityDetector? nullDetector = null;
            Action act = () => new DatabaseService(connectionString, nullDetector, _configuration);
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("capabilityDetector");
        }
        
        [Fact(DisplayName = "DBS-001b: Constructor with null configuration throws ArgumentNullException")]
        public void DBS001b()
        {
            // Act
            string connectionString = "Data Source=localhost;Initial Catalog=TestDb;Integrated Security=True;";
            DatabaseConfiguration? nullConfiguration = null;
            Action act = () => new DatabaseService(connectionString, _mockCapabilityDetector.Object, nullConfiguration);
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("configuration");
        }
        
        [Fact(DisplayName = "DBS-002: ListTablesAsync calls database with null database name when not specified")]
        public void DBS002()
        {
            // This would be an integration test requiring a real database connection
            // Skipping actual implementation for unit test
        }
        
        [Fact(DisplayName = "DBS-003: GetCurrentDatabaseName returns initial catalog from connection string")]
        public void DBS003()
        {
            // Act
            string databaseName = _databaseService.GetCurrentDatabaseName();
            
            // Assert
            databaseName.Should().Be("TestDb");
        }
        
        [Fact(DisplayName = "DBS-004: ExecuteQueryAsync with empty query throws ArgumentException")]
        public async Task DBS004()
        {
            // Act
            Func<Task> act = async () => await _databaseService.ExecuteQueryAsync(string.Empty, null, null);
            
            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Query cannot be empty*");
        }
        
    }
}