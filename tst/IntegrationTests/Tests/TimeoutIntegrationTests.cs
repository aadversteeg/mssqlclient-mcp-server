using FluentAssertions;
using IntegrationTests.Fixtures;
using IntegrationTests.Helpers;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace IntegrationTests.Tests
{
    [Collection("MCP Tests")]
    [Trait("Category", "MCP")]
    [Trait("TestType", "Integration")]
    public class TimeoutIntegrationTests
    {
        private readonly McpFixture _fixture;
        private readonly ILogger<TimeoutIntegrationTests> _logger;

        public TimeoutIntegrationTests(McpFixture fixture)
        {
            _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            _logger = loggerFactory.CreateLogger<TimeoutIntegrationTests>();
        }

        private async Task<McpClient> CreateMcpClientAsync(
            Dictionary<string, string> envVars,
            CancellationToken cancellationToken)
        {
            var transport = new StdioClientTransport(new StdioClientTransportOptions
            {
                Command = _fixture.McpServerExecutablePath,
                EnvironmentVariables = envVars
            });

            return await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
        }

        private static string GetTextContent(CallToolResult result)
        {
            return string.Join("", result.Content.OfType<TextContentBlock>().Select(c => c.Text));
        }

        [Fact(DisplayName = "TIMEOUT-INT-001: TotalToolCallTimeoutSeconds null preserves existing behavior")]
        public async Task TIMEOUT_INT_001()
        {
            // Arrange - Connection string has Database=master so server runs in database mode.
            // With no total timeout set, queries should work without restrictions.
            var envVars = new Dictionary<string, string>
            {
                { "MSSQL_CONNECTIONSTRING", EnvironmentVariableHelper.GetDefaultConnectionString() },
                { "DatabaseConfiguration__TotalToolCallTimeoutSeconds", "" },
                { "DatabaseConfiguration__EnableExecuteQuery", "true" },
                { "DatabaseConfiguration__EnableStartQuery", "true" }
            };

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await using var client = await CreateMcpClientAsync(envVars, cts.Token);

            // Act - Execute a simple query; should work without timeout restrictions
            var result = await client.CallToolAsync(
                "execute_query",
                new Dictionary<string, object?>
                {
                    ["query"] = "SELECT 1 AS TestValue"
                },
                cancellationToken: cts.Token);

            // Assert
            var text = GetTextContent(result);
            _logger.LogInformation("Result: {Text}", text);
            result.IsError.Should().NotBe(true);
            text.Should().Contain("TestValue");
            text.Should().NotContain("Total tool timeout");
        }

        [Fact(DisplayName = "TIMEOUT-INT-002: TotalToolCallTimeoutSeconds enforces timeout limit")]
        public async Task TIMEOUT_INT_002()
        {
            // Arrange - Use a short total timeout that the query will exceed
            var envVars = new Dictionary<string, string>
            {
                { "MSSQL_CONNECTIONSTRING", EnvironmentVariableHelper.GetDefaultConnectionString() },
                { "DatabaseConfiguration__TotalToolCallTimeoutSeconds", "2" },
                { "DatabaseConfiguration__DefaultCommandTimeoutSeconds", "30" },
                { "DatabaseConfiguration__EnableExecuteQuery", "true" },
                { "DatabaseConfiguration__EnableStartQuery", "true" }
            };

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await using var client = await CreateMcpClientAsync(envVars, cts.Token);

            // Act - Execute a long-running query that should exceed the 2-second total timeout
            var result = await client.CallToolAsync(
                "execute_query",
                new Dictionary<string, object?>
                {
                    ["query"] = "WAITFOR DELAY '00:00:05'; SELECT 1 AS TestValue"
                },
                cancellationToken: cts.Token);

            // Assert - Tool call succeeds at the protocol level but returns timeout error message
            var text = GetTextContent(result);
            _logger.LogInformation("Result: {Text}", text);
            text.Should().Contain("Total tool timeout of 2s exceeded");
        }

        [Fact(DisplayName = "TIMEOUT-INT-003: get_command_timeout returns TotalToolCallTimeoutSeconds setting")]
        public async Task TIMEOUT_INT_003()
        {
            // Arrange - Configure specific timeout values
            var envVars = new Dictionary<string, string>
            {
                { "MSSQL_CONNECTIONSTRING", EnvironmentVariableHelper.GetDefaultConnectionString() },
                { "DatabaseConfiguration__TotalToolCallTimeoutSeconds", "90" },
                { "DatabaseConfiguration__DefaultCommandTimeoutSeconds", "45" },
                { "DatabaseConfiguration__EnableStartQuery", "true" }
            };

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await using var client = await CreateMcpClientAsync(envVars, cts.Token);

            // Act - Get timeout configuration
            var result = await client.CallToolAsync(
                "get_command_timeout",
                new Dictionary<string, object?>(),
                cancellationToken: cts.Token);

            // Assert - Verify the configured values are returned
            var text = GetTextContent(result);
            _logger.LogInformation("Result: {Text}", text);
            result.IsError.Should().NotBe(true);
            text.Should().Contain("totalToolCallTimeoutSeconds");
            text.Should().Contain("90");
            text.Should().Contain("defaultCommandTimeoutSeconds");
            text.Should().Contain("45");
        }

        [Fact(DisplayName = "TIMEOUT-INT-004: Multiple operations within timeout limit complete successfully")]
        public async Task TIMEOUT_INT_004()
        {
            // Arrange - Use reasonable timeout for multiple operations
            var envVars = new Dictionary<string, string>
            {
                { "MSSQL_CONNECTIONSTRING", EnvironmentVariableHelper.GetDefaultConnectionString() },
                { "DatabaseConfiguration__TotalToolCallTimeoutSeconds", "30" },
                { "DatabaseConfiguration__DefaultCommandTimeoutSeconds", "10" },
                { "DatabaseConfiguration__EnableExecuteQuery", "true" },
                { "DatabaseConfiguration__EnableStartQuery", "true" }
            };

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            await using var client = await CreateMcpClientAsync(envVars, cts.Token);

            // Act - Execute multiple quick operations that should all complete within timeout
            var tableResult = await client.CallToolAsync(
                "list_tables",
                new Dictionary<string, object?>(),
                cancellationToken: cts.Token);

            var queryResult = await client.CallToolAsync(
                "execute_query",
                new Dictionary<string, object?>
                {
                    ["query"] = "SELECT TOP 5 name FROM sys.tables"
                },
                cancellationToken: cts.Token);

            // Assert - Both operations should succeed without timeout
            var tableText = GetTextContent(tableResult);
            var queryText = GetTextContent(queryResult);
            _logger.LogInformation("Table result: {Text}", tableText);
            _logger.LogInformation("Query result: {Text}", queryText);

            tableResult.IsError.Should().NotBe(true);
            tableText.Should().NotContain("Total tool timeout");

            queryResult.IsError.Should().NotBe(true);
            queryText.Should().NotContain("Total tool timeout");
        }

        [Fact(DisplayName = "TIMEOUT-INT-005: Timeout respects remaining time for command timeout calculation")]
        public async Task TIMEOUT_INT_005()
        {
            // Arrange - DefaultCommandTimeoutSeconds (60) is larger than TotalToolCallTimeoutSeconds (10),
            // so the effective command timeout should be capped by the remaining total time
            var envVars = new Dictionary<string, string>
            {
                { "MSSQL_CONNECTIONSTRING", EnvironmentVariableHelper.GetDefaultConnectionString() },
                { "DatabaseConfiguration__TotalToolCallTimeoutSeconds", "10" },
                { "DatabaseConfiguration__DefaultCommandTimeoutSeconds", "60" },
                { "DatabaseConfiguration__EnableExecuteQuery", "true" },
                { "DatabaseConfiguration__EnableStartQuery", "true" }
            };

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await using var client = await CreateMcpClientAsync(envVars, cts.Token);

            // Act - Execute a quick query that should complete within the 10-second total timeout
            var result = await client.CallToolAsync(
                "execute_query",
                new Dictionary<string, object?>
                {
                    ["query"] = "SELECT COUNT(*) AS TableCount FROM sys.tables"
                },
                cancellationToken: cts.Token);

            // Assert - Should complete successfully despite DefaultCommandTimeoutSeconds > TotalToolCallTimeoutSeconds
            var text = GetTextContent(result);
            _logger.LogInformation("Result: {Text}", text);
            result.IsError.Should().NotBe(true);
            text.Should().Contain("TableCount");
            text.Should().NotContain("Total tool timeout");
        }
    }
}
