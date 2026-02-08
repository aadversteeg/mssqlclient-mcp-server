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
    public class McpIntegrationTests
    {
        private readonly McpFixture _fixture;
        private readonly ILogger<McpIntegrationTests> _logger;

        public McpIntegrationTests(McpFixture fixture)
        {
            _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            _logger = loggerFactory.CreateLogger<McpIntegrationTests>();
        }

        private async Task<McpClient> CreateDatabaseModeClientAsync(CancellationToken cancellationToken)
        {
            var envVars = new Dictionary<string, string>
            {
                { "MSSQL_CONNECTIONSTRING", EnvironmentVariableHelper.GetDefaultConnectionString() },
                { "DatabaseConfiguration__EnableExecuteQuery", "true" }
            };

            var transport = new StdioClientTransport(new StdioClientTransportOptions
            {
                Command = _fixture.McpServerExecutablePath,
                EnvironmentVariables = envVars
            });

            return await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
        }

        private async Task<McpClient> CreateServerModeClientAsync(CancellationToken cancellationToken)
        {
            var envVars = new Dictionary<string, string>
            {
                { "MSSQL_CONNECTIONSTRING", EnvironmentVariableHelper.GetServerModeConnectionString() },
                { "DatabaseConfiguration__EnableExecuteQuery", "true" }
            };

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

        [Fact(DisplayName = "MCP-INT-001: Database mode registers expected tools")]
        public async Task MCP_INT_001()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await using var client = await CreateDatabaseModeClientAsync(cts.Token);

            var tools = await client.ListToolsAsync(cancellationToken: cts.Token);
            var toolNames = tools.Select(t => t.Name).ToList();

            _logger.LogInformation("Database mode tools: {Tools}", string.Join(", ", toolNames));

            // Database mode should contain these tools
            toolNames.Should().Contain("list_tables");
            toolNames.Should().Contain("execute_query");
            toolNames.Should().Contain("get_table_schema");
            toolNames.Should().Contain("server_capabilities");

            // Database mode should NOT contain server-mode tools
            toolNames.Should().NotContain("list_databases");
            toolNames.Should().NotContain("list_tables_in_database");
            toolNames.Should().NotContain("execute_query_in_database");
            toolNames.Should().NotContain("get_table_schema_in_database");
        }

        [Fact(DisplayName = "MCP-INT-002: Server mode registers expected tools")]
        public async Task MCP_INT_002()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await using var client = await CreateServerModeClientAsync(cts.Token);

            var tools = await client.ListToolsAsync(cancellationToken: cts.Token);
            var toolNames = tools.Select(t => t.Name).ToList();

            _logger.LogInformation("Server mode tools: {Tools}", string.Join(", ", toolNames));

            // Server mode should contain these tools
            toolNames.Should().Contain("list_databases");
            toolNames.Should().Contain("list_tables_in_database");
            toolNames.Should().Contain("execute_query_in_database");
            toolNames.Should().Contain("server_capabilities");

            // Server mode should NOT contain database-mode tools
            toolNames.Should().NotContain("list_tables");
            toolNames.Should().NotContain("execute_query");
            toolNames.Should().NotContain("get_table_schema");
        }

        [Fact(DisplayName = "MCP-INT-003: Database mode list_tables returns tables")]
        public async Task MCP_INT_003()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await using var client = await CreateDatabaseModeClientAsync(cts.Token);

            var result = await client.CallToolAsync(
                "list_tables",
                new Dictionary<string, object?>(),
                cancellationToken: cts.Token);

            var text = GetTextContent(result);
            _logger.LogInformation("list_tables result: {Text}", text);

            result.IsError.Should().NotBe(true);
            text.Should().NotBeNullOrEmpty();
        }

        [Fact(DisplayName = "MCP-INT-004: Database mode execute_query runs SELECT @@VERSION")]
        public async Task MCP_INT_004()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await using var client = await CreateDatabaseModeClientAsync(cts.Token);

            var result = await client.CallToolAsync(
                "execute_query",
                new Dictionary<string, object?>
                {
                    ["query"] = "SELECT @@VERSION AS Version"
                },
                cancellationToken: cts.Token);

            var text = GetTextContent(result);
            _logger.LogInformation("execute_query result: {Text}", text);

            result.IsError.Should().NotBe(true);
            text.Should().Contain("Microsoft SQL Server");
        }

        [Fact(DisplayName = "MCP-INT-005: Database mode execute_query with invalid SQL returns error text")]
        public async Task MCP_INT_005()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await using var client = await CreateDatabaseModeClientAsync(cts.Token);

            var result = await client.CallToolAsync(
                "execute_query",
                new Dictionary<string, object?>
                {
                    ["query"] = "SELECT * FROM non_existent_table_xyz_12345"
                },
                cancellationToken: cts.Token);

            var text = GetTextContent(result);
            _logger.LogInformation("invalid SQL result: {Text}", text);

            // The server returns the SQL error as text content
            text.Should().NotBeNullOrEmpty();
            text.Should().ContainAny("Invalid object name", "error", "Error");
        }

        [Fact(DisplayName = "MCP-INT-006: Database mode get_table_schema returns schema for spt_monitor")]
        public async Task MCP_INT_006()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await using var client = await CreateDatabaseModeClientAsync(cts.Token);

            var result = await client.CallToolAsync(
                "get_table_schema",
                new Dictionary<string, object?>
                {
                    ["tableName"] = "spt_monitor"
                },
                cancellationToken: cts.Token);

            var text = GetTextContent(result);
            _logger.LogInformation("get_table_schema result: {Text}", text);

            result.IsError.Should().NotBe(true);
            text.Should().NotBeNullOrEmpty();
        }

        [Fact(DisplayName = "MCP-INT-007: Server mode list_databases contains master")]
        public async Task MCP_INT_007()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await using var client = await CreateServerModeClientAsync(cts.Token);

            var result = await client.CallToolAsync(
                "list_databases",
                new Dictionary<string, object?>(),
                cancellationToken: cts.Token);

            var text = GetTextContent(result);
            _logger.LogInformation("list_databases result: {Text}", text);

            result.IsError.Should().NotBe(true);
            text.Should().Contain("master");
        }

        [Fact(DisplayName = "MCP-INT-008: Server mode list_tables_in_database returns tables for master")]
        public async Task MCP_INT_008()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await using var client = await CreateServerModeClientAsync(cts.Token);

            var result = await client.CallToolAsync(
                "list_tables_in_database",
                new Dictionary<string, object?>
                {
                    ["databaseName"] = "master"
                },
                cancellationToken: cts.Token);

            var text = GetTextContent(result);
            _logger.LogInformation("list_tables_in_database result: {Text}", text);

            result.IsError.Should().NotBe(true);
            text.Should().NotBeNullOrEmpty();
        }

        [Fact(DisplayName = "MCP-INT-009: Server mode execute_query_in_database runs query in master")]
        public async Task MCP_INT_009()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await using var client = await CreateServerModeClientAsync(cts.Token);

            var result = await client.CallToolAsync(
                "execute_query_in_database",
                new Dictionary<string, object?>
                {
                    ["databaseName"] = "master",
                    ["query"] = "SELECT @@VERSION AS Version"
                },
                cancellationToken: cts.Token);

            var text = GetTextContent(result);
            _logger.LogInformation("execute_query_in_database result: {Text}", text);

            result.IsError.Should().NotBe(true);
            text.Should().Contain("Microsoft SQL Server");
        }
    }
}
