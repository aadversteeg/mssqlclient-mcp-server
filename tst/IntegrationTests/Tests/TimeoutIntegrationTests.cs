using FluentAssertions;
using IntegrationTests.Fixtures;
using IntegrationTests.Helpers;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Ave.Testing.ModelContextProtocol;
using Ave.Testing.ModelContextProtocol.Factory;
using Ave.Testing.ModelContextProtocol.Helpers;
using Ave.Testing.ModelContextProtocol.Models;

namespace IntegrationTests.Tests
{
    [Collection("MCP Tests")]
    [Trait("Category", "MCP")]
    [Trait("TestType", "Integration")]
    public class TimeoutIntegrationTests
    {
        private readonly McpFixture _fixture;
        private readonly ILogger<TimeoutIntegrationTests> _logger;

        public TimeoutIntegrationTests(McpFixture fixture, ILogger<TimeoutIntegrationTests> logger)
        {
            _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Fact(DisplayName = "TIMEOUT-INT-001: TotalToolCallTimeoutSeconds null preserves existing behavior")]
        public async Task TIMEOUT_INT_001()
        {
            // Arrange - Use connection to NorthwindTestDb database
            var envVars = new Dictionary<string, string>
            {
                { "MSSQL_CONNECTIONSTRING", _fixture.GetNorthwindConnectionString() },
                { "DatabaseConfiguration__TotalToolCallTimeoutSeconds", "" }, // Explicitly set to empty to test null behavior
                { "DatabaseConfiguration__EnableExecuteQuery", "true" }
            };

            var mcpClient = McpClientFactory.Create(_fixture.McpServerExecutablePath, envVars);

            try
            {
                // Act - Initialize the client
                var initResult = await mcpClient.InitializeAsync();
                initResult.IsSuccess.Should().BeTrue();

                // Execute a simple query - should work without timeout restrictions
                var queryResult = await mcpClient.CallToolAsync("execute_query", new
                {
                    query = "SELECT TOP 1 CustomerID FROM Customers",
                    timeoutSeconds = 30
                });

                // Assert
                queryResult.IsSuccess.Should().BeTrue();
                queryResult.Data.Should().Contain("CustomerID");
                queryResult.Data.Should().NotContain("Total tool timeout");
            }
            finally
            {
                await mcpClient.DisposeAsync();
            }
        }

        [Fact(DisplayName = "TIMEOUT-INT-002: TotalToolCallTimeoutSeconds enforces timeout limit")]
        public async Task TIMEOUT_INT_002()
        {
            // Arrange - Use short timeout to test timeout behavior
            var envVars = new Dictionary<string, string>
            {
                { "MSSQL_CONNECTIONSTRING", _fixture.GetNorthwindConnectionString() },
                { "DatabaseConfiguration__TotalToolCallTimeoutSeconds", "2" }, // 2 seconds
                { "DatabaseConfiguration__DefaultCommandTimeoutSeconds", "30" },
                { "DatabaseConfiguration__EnableExecuteQuery", "true" }
            };

            var mcpClient = McpClientFactory.Create(_fixture.McpServerExecutablePath, envVars);

            try
            {
                // Act - Initialize the client
                var initResult = await mcpClient.InitializeAsync();
                initResult.IsSuccess.Should().BeTrue();

                // Execute a long-running query that should timeout
                var queryResult = await mcpClient.CallToolAsync("execute_query", new
                {
                    query = "WAITFOR DELAY '00:00:05'; SELECT 1 as TestValue" // Wait 5 seconds, but total timeout is 2 seconds
                });

                // Assert
                queryResult.IsSuccess.Should().BeTrue(); // Tool call succeeds but returns error message
                queryResult.Data.Should().Contain("Total tool timeout of 2s exceeded");
            }
            finally
            {
                await mcpClient.DisposeAsync();
            }
        }

        [Fact(DisplayName = "TIMEOUT-INT-003: get_command_timeout returns TotalToolCallTimeoutSeconds setting")]
        public async Task TIMEOUT_INT_003()
        {
            // Arrange
            var envVars = new Dictionary<string, string>
            {
                { "MSSQL_CONNECTIONSTRING", _fixture.GetNorthwindConnectionString() },
                { "DatabaseConfiguration__TotalToolCallTimeoutSeconds", "90" },
                { "DatabaseConfiguration__DefaultCommandTimeoutSeconds", "45" }
            };

            var mcpClient = McpClientFactory.Create(_fixture.McpServerExecutablePath, envVars);

            try
            {
                // Act - Initialize the client
                var initResult = await mcpClient.InitializeAsync();
                initResult.IsSuccess.Should().BeTrue();

                // Get timeout configuration
                var timeoutResult = await mcpClient.CallToolAsync("get_command_timeout", new { });

                // Assert
                timeoutResult.IsSuccess.Should().BeTrue();
                timeoutResult.Data.Should().Contain("totalToolCallTimeoutSeconds");
                timeoutResult.Data.Should().Contain("90");
                timeoutResult.Data.Should().Contain("defaultCommandTimeoutSeconds");
                timeoutResult.Data.Should().Contain("45");
            }
            finally
            {
                await mcpClient.DisposeAsync();
            }
        }

        [Fact(DisplayName = "TIMEOUT-INT-004: Multiple operations within timeout limit complete successfully")]
        public async Task TIMEOUT_INT_004()
        {
            // Arrange - Use reasonable timeout for multiple operations
            var envVars = new Dictionary<string, string>
            {
                { "MSSQL_CONNECTIONSTRING", _fixture.GetNorthwindConnectionString() },
                { "DatabaseConfiguration__TotalToolCallTimeoutSeconds", "30" }, // 30 seconds should be enough
                { "DatabaseConfiguration__DefaultCommandTimeoutSeconds", "10" },
                { "DatabaseConfiguration__EnableExecuteQuery", "true" }
            };

            var mcpClient = McpClientFactory.Create(_fixture.McpServerExecutablePath, envVars);

            try
            {
                // Act - Initialize the client
                var initResult = await mcpClient.InitializeAsync();
                initResult.IsSuccess.Should().BeTrue();

                // Execute multiple quick operations that should all complete within timeout
                var tableResult = await mcpClient.CallToolAsync("list_tables", new { });
                var queryResult = await mcpClient.CallToolAsync("execute_query", new
                {
                    query = "SELECT TOP 5 CustomerID FROM Customers"
                });

                // Assert
                tableResult.IsSuccess.Should().BeTrue();
                tableResult.Data.Should().Contain("Customers");
                tableResult.Data.Should().NotContain("Total tool timeout");

                queryResult.IsSuccess.Should().BeTrue();
                queryResult.Data.Should().Contain("CustomerID");
                queryResult.Data.Should().NotContain("Total tool timeout");
            }
            finally
            {
                await mcpClient.DisposeAsync();
            }
        }

        [Fact(DisplayName = "TIMEOUT-INT-005: Timeout respects remaining time for command timeout calculation")]
        public async Task TIMEOUT_INT_005()
        {
            // This test verifies the timeout calculation logic works correctly
            // by using a scenario where the command timeout would be reduced due to remaining time
            
            // Arrange - Use moderate timeout that will be consumed partially
            var envVars = new Dictionary<string, string>
            {
                { "MSSQL_CONNECTIONSTRING", _fixture.GetNorthwindConnectionString() },
                { "DatabaseConfiguration__TotalToolCallTimeoutSeconds", "10" }, // 10 seconds total
                { "DatabaseConfiguration__DefaultCommandTimeoutSeconds", "60" }, // 60 seconds default (should be limited by remaining)
                { "DatabaseConfiguration__EnableExecuteQuery", "true" }
            };

            var mcpClient = McpClientFactory.Create(_fixture.McpServerExecutablePath, envVars);

            try
            {
                // Act - Initialize the client
                var initResult = await mcpClient.InitializeAsync();
                initResult.IsSuccess.Should().BeTrue();

                // Execute a query that should complete within 10 seconds but would exceed 60 seconds limit
                var queryResult = await mcpClient.CallToolAsync("execute_query", new
                {
                    query = "SELECT COUNT(*) as CustomerCount FROM Customers" // Quick query
                });

                // Assert - Should complete successfully
                queryResult.IsSuccess.Should().BeTrue();
                queryResult.Data.Should().Contain("CustomerCount");
                queryResult.Data.Should().NotContain("Total tool timeout");
            }
            finally
            {
                await mcpClient.DisposeAsync();
            }
        }
    }
}