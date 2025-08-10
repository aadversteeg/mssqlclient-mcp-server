using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IntegrationTests.Tests
{
    /// <summary>
    /// Integration tests for SQL Server connections
    /// </summary>
    [Collection("Docker Resources")]
    [Trait("Category", "SQL")]
    [Trait("TestType", "Integration")]
    public class SqlConnectionTests : IClassFixture<McpServerFixture>
    {
        private readonly McpServerFixture _fixture;
        private readonly ILogger<SqlConnectionTests> _logger;

        public SqlConnectionTests(McpServerFixture fixture)
        {
            _fixture = fixture;
            
            // Create logger factory and logger
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });
            
            _logger = loggerFactory.CreateLogger<SqlConnectionTests>();
        }

        [Fact(DisplayName = "SQL-001: SQL Server connection string should be valid")]
        public void SQL001()
        {
            // Act & Assert
            _fixture.SqlServerConnectionString.Should().NotBeNullOrEmpty();
        }

        [Fact(DisplayName = "SQL-002: Can connect to SQL Server")]
        public async Task SQL002()
        {
            // Act & Assert - With retry logic
            const int maxRetries = 5;
            int retryCount = 0;
            bool connected = false;
            Exception? lastException = null;
            
            _logger.LogInformation("Testing SQL connection with connection string: {ConnectionString}", 
                _fixture.SqlServerConnectionString.Replace("Password=", "Password=***"));
            
            while (retryCount < maxRetries && !connected)
            {
                try
                {
                    await using var connection = new SqlConnection(_fixture.SqlServerConnectionString);
                    
                    // Simply attempt to open the connection
                    _logger.LogInformation("Attempt {Retry} of {MaxRetries} to connect to SQL Server", 
                        retryCount + 1, maxRetries);
                    
                    await connection.OpenAsync();
                    connection.State.Should().Be(System.Data.ConnectionState.Open);
                    
                    _logger.LogInformation("Successfully connected to SQL Server");
                    connected = true;
                    
                    // Always ensure we close the connection
                    if (connection.State != System.Data.ConnectionState.Closed)
                    {
                        await connection.CloseAsync();
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    retryCount++;
                    _logger.LogWarning(ex, "Failed to connect to SQL Server (attempt {Retry} of {MaxRetries}). Retrying in 3 seconds...", 
                        retryCount, maxRetries);
                    
                    // Wait 3 seconds before retrying
                    await Task.Delay(3000);
                }
            }
            
            // If we couldn't connect after all retries, fail the test
            if (!connected && lastException != null)
            {
                _logger.LogError(lastException, "Failed to connect to SQL Server after {MaxRetries} attempts", maxRetries);
                throw lastException;
            }
        }
    }
    
    // Define a collection for Docker resources to ensure they're not run in parallel
    [CollectionDefinition("Docker Resources")]
    public class DockerCollection : ICollectionFixture<DockerFixture>
    {
        // This class just defines the collection, no implementation needed
    }
}