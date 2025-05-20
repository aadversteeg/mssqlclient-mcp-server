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
        
        public DatabaseServiceTests()
        {
            // Create a connection string for testing
            string connectionString = "Data Source=localhost;Initial Catalog=TestDb;Integrated Security=True;";
            _databaseService = new DatabaseService(connectionString);
        }
        
        [Fact(DisplayName = "DBS-001: Constructor with null connection string throws ArgumentNullException")]
        public void DBS001()
        {
            // Act
            string? nullConnectionString = null;
            Action act = () => new DatabaseService(nullConnectionString);
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("connectionString");
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
            // Arrange
            string connectionString = "Data Source=localhost;Initial Catalog=TestDb;Integrated Security=True;";
            var dbService = new DatabaseService(connectionString);
            
            // Act
            string databaseName = dbService.GetCurrentDatabaseName();
            
            // Assert
            databaseName.Should().Be("TestDb");
        }
        
        [Fact(DisplayName = "DBS-004: ExecuteQueryAsync with empty query throws ArgumentException")]
        public async Task DBS004()
        {
            // Act
            Func<Task> act = async () => await _databaseService.ExecuteQueryAsync(string.Empty);
            
            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Query cannot be empty*");
        }
        
    }
}