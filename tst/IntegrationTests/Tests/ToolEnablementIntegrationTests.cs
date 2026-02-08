using FluentAssertions;
using IntegrationTests.Fixtures;
using IntegrationTests.Helpers;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;

namespace IntegrationTests.Tests
{
    [Collection("MCP Tests")]
    [Trait("Category", "MCP")]
    [Trait("TestType", "Integration")]
    public class ToolEnablementIntegrationTests
    {
        private readonly McpFixture _fixture;
        private readonly DockerFixture _dockerFixture;
        private readonly ILogger<ToolEnablementIntegrationTests> _logger;

        public ToolEnablementIntegrationTests(McpFixture fixture, DockerFixture dockerFixture)
        {
            _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
            _dockerFixture = dockerFixture ?? throw new ArgumentNullException(nameof(dockerFixture));

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            _logger = loggerFactory.CreateLogger<ToolEnablementIntegrationTests>();
        }

        private async Task<McpClient> CreateClientAsync(
            string connectionString,
            Dictionary<string, string>? enableFlags,
            CancellationToken cancellationToken)
        {
            var envVars = new Dictionary<string, string>
            {
                { "MSSQL_CONNECTIONSTRING", connectionString }
            };

            if (enableFlags != null)
            {
                foreach (var kvp in enableFlags)
                {
                    envVars[kvp.Key] = kvp.Value;
                }
            }

            var transport = new StdioClientTransport(new StdioClientTransportOptions
            {
                Command = _fixture.McpServerExecutablePath,
                EnvironmentVariables = envVars
            });

            return await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
        }

        private async Task<List<string>> GetToolNamesAsync(
            string connectionString,
            Dictionary<string, string>? enableFlags,
            CancellationToken cancellationToken)
        {
            await using var client = await CreateClientAsync(connectionString, enableFlags, cancellationToken);
            var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);
            var toolNames = tools.Select(t => t.Name).ToList();

            _logger.LogInformation("Tools: {Tools}", string.Join(", ", toolNames));

            return toolNames;
        }

        // =====================================================================
        // Database mode tests (TE-INT-001 through TE-INT-006)
        // =====================================================================

        [Fact(DisplayName = "TE-INT-001: Database mode with no flags enables only read-only tools")]
        public async Task TE_INT_001()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            var toolNames = await GetToolNamesAsync(
                EnvironmentVariableHelper.GetDefaultConnectionString(_dockerFixture.SqlServerPort),
                null,
                cts.Token);

            toolNames.Should().Contain("list_tables");
            toolNames.Should().Contain("get_table_schema");
            toolNames.Should().Contain("list_stored_procedures");
            toolNames.Should().Contain("get_stored_procedure_definition");
            toolNames.Should().Contain("get_stored_procedure_parameters");
            toolNames.Should().Contain("server_capabilities");

            toolNames.Should().NotContain("execute_query");
            toolNames.Should().NotContain("execute_stored_procedure");
            toolNames.Should().NotContain("start_query");
            toolNames.Should().NotContain("start_stored_procedure");
        }

        [Fact(DisplayName = "TE-INT-002: Database mode with EnableExecuteQuery only enables execute_query")]
        public async Task TE_INT_002()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            var toolNames = await GetToolNamesAsync(
                EnvironmentVariableHelper.GetDefaultConnectionString(_dockerFixture.SqlServerPort),
                new Dictionary<string, string>
                {
                    { "DatabaseConfiguration__EnableExecuteQuery", "true" }
                },
                cts.Token);

            toolNames.Should().Contain("execute_query");

            toolNames.Should().NotContain("execute_stored_procedure");
            toolNames.Should().NotContain("start_query");
            toolNames.Should().NotContain("start_stored_procedure");
        }

        [Fact(DisplayName = "TE-INT-003: Database mode with EnableExecuteStoredProcedure only enables execute_stored_procedure")]
        public async Task TE_INT_003()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            var toolNames = await GetToolNamesAsync(
                EnvironmentVariableHelper.GetDefaultConnectionString(_dockerFixture.SqlServerPort),
                new Dictionary<string, string>
                {
                    { "DatabaseConfiguration__EnableExecuteStoredProcedure", "true" }
                },
                cts.Token);

            toolNames.Should().Contain("execute_stored_procedure");

            toolNames.Should().NotContain("execute_query");
            toolNames.Should().NotContain("start_query");
            toolNames.Should().NotContain("start_stored_procedure");
        }

        [Fact(DisplayName = "TE-INT-004: Database mode with EnableStartQuery only enables start_query and session tools")]
        public async Task TE_INT_004()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            var toolNames = await GetToolNamesAsync(
                EnvironmentVariableHelper.GetDefaultConnectionString(_dockerFixture.SqlServerPort),
                new Dictionary<string, string>
                {
                    { "DatabaseConfiguration__EnableStartQuery", "true" }
                },
                cts.Token);

            toolNames.Should().Contain("start_query");
            toolNames.Should().Contain("get_session_status");
            toolNames.Should().Contain("get_session_results");
            toolNames.Should().Contain("stop_session");
            toolNames.Should().Contain("list_sessions");

            toolNames.Should().NotContain("execute_query");
            toolNames.Should().NotContain("execute_stored_procedure");
            toolNames.Should().NotContain("start_stored_procedure");
        }

        [Fact(DisplayName = "TE-INT-005: Database mode with EnableStartStoredProcedure only enables start_stored_procedure and session tools")]
        public async Task TE_INT_005()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            var toolNames = await GetToolNamesAsync(
                EnvironmentVariableHelper.GetDefaultConnectionString(_dockerFixture.SqlServerPort),
                new Dictionary<string, string>
                {
                    { "DatabaseConfiguration__EnableStartStoredProcedure", "true" }
                },
                cts.Token);

            toolNames.Should().Contain("start_stored_procedure");
            toolNames.Should().Contain("get_session_status");
            toolNames.Should().Contain("get_session_results");
            toolNames.Should().Contain("stop_session");
            toolNames.Should().Contain("list_sessions");

            toolNames.Should().NotContain("execute_query");
            toolNames.Should().NotContain("execute_stored_procedure");
            toolNames.Should().NotContain("start_query");
        }

        [Fact(DisplayName = "TE-INT-006: Database mode with all flags enabled registers all execution tools")]
        public async Task TE_INT_006()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            var toolNames = await GetToolNamesAsync(
                EnvironmentVariableHelper.GetDefaultConnectionString(_dockerFixture.SqlServerPort),
                new Dictionary<string, string>
                {
                    { "DatabaseConfiguration__EnableExecuteQuery", "true" },
                    { "DatabaseConfiguration__EnableExecuteStoredProcedure", "true" },
                    { "DatabaseConfiguration__EnableStartQuery", "true" },
                    { "DatabaseConfiguration__EnableStartStoredProcedure", "true" }
                },
                cts.Token);

            toolNames.Should().Contain("execute_query");
            toolNames.Should().Contain("execute_stored_procedure");
            toolNames.Should().Contain("start_query");
            toolNames.Should().Contain("start_stored_procedure");
            toolNames.Should().Contain("get_session_status");
        }

        // =====================================================================
        // Server mode tests (TE-INT-007 through TE-INT-012)
        // =====================================================================

        [Fact(DisplayName = "TE-INT-007: Server mode with no flags enables only read-only tools")]
        public async Task TE_INT_007()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            var toolNames = await GetToolNamesAsync(
                EnvironmentVariableHelper.GetServerModeConnectionString(_dockerFixture.SqlServerPort),
                null,
                cts.Token);

            toolNames.Should().Contain("list_databases");
            toolNames.Should().Contain("list_tables_in_database");
            toolNames.Should().Contain("get_table_schema_in_database");
            toolNames.Should().Contain("list_stored_procedures_in_database");
            toolNames.Should().Contain("get_stored_procedure_definition_in_database");
            toolNames.Should().Contain("get_stored_procedure_parameters");
            toolNames.Should().Contain("server_capabilities");

            toolNames.Should().NotContain("execute_query_in_database");
            toolNames.Should().NotContain("execute_stored_procedure_in_database");
            toolNames.Should().NotContain("start_query_in_database");
            toolNames.Should().NotContain("start_stored_procedure_in_database");
        }

        [Fact(DisplayName = "TE-INT-008: Server mode with EnableExecuteQuery only enables execute_query_in_database")]
        public async Task TE_INT_008()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            var toolNames = await GetToolNamesAsync(
                EnvironmentVariableHelper.GetServerModeConnectionString(_dockerFixture.SqlServerPort),
                new Dictionary<string, string>
                {
                    { "DatabaseConfiguration__EnableExecuteQuery", "true" }
                },
                cts.Token);

            toolNames.Should().Contain("execute_query_in_database");

            toolNames.Should().NotContain("execute_stored_procedure_in_database");
            toolNames.Should().NotContain("start_query_in_database");
            toolNames.Should().NotContain("start_stored_procedure_in_database");
        }

        [Fact(DisplayName = "TE-INT-009: Server mode with EnableExecuteStoredProcedure only enables execute_stored_procedure_in_database")]
        public async Task TE_INT_009()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            var toolNames = await GetToolNamesAsync(
                EnvironmentVariableHelper.GetServerModeConnectionString(_dockerFixture.SqlServerPort),
                new Dictionary<string, string>
                {
                    { "DatabaseConfiguration__EnableExecuteStoredProcedure", "true" }
                },
                cts.Token);

            toolNames.Should().Contain("execute_stored_procedure_in_database");

            toolNames.Should().NotContain("execute_query_in_database");
            toolNames.Should().NotContain("start_query_in_database");
            toolNames.Should().NotContain("start_stored_procedure_in_database");
        }

        [Fact(DisplayName = "TE-INT-010: Server mode with EnableStartQuery only enables start_query_in_database and session tools")]
        public async Task TE_INT_010()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            var toolNames = await GetToolNamesAsync(
                EnvironmentVariableHelper.GetServerModeConnectionString(_dockerFixture.SqlServerPort),
                new Dictionary<string, string>
                {
                    { "DatabaseConfiguration__EnableStartQuery", "true" }
                },
                cts.Token);

            toolNames.Should().Contain("start_query_in_database");
            toolNames.Should().Contain("get_session_status");
            toolNames.Should().Contain("get_session_results");
            toolNames.Should().Contain("stop_session");
            toolNames.Should().Contain("list_sessions");

            toolNames.Should().NotContain("execute_query_in_database");
            toolNames.Should().NotContain("execute_stored_procedure_in_database");
            toolNames.Should().NotContain("start_stored_procedure_in_database");
        }

        [Fact(DisplayName = "TE-INT-011: Server mode with EnableStartStoredProcedure only enables start_stored_procedure_in_database and session tools")]
        public async Task TE_INT_011()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            var toolNames = await GetToolNamesAsync(
                EnvironmentVariableHelper.GetServerModeConnectionString(_dockerFixture.SqlServerPort),
                new Dictionary<string, string>
                {
                    { "DatabaseConfiguration__EnableStartStoredProcedure", "true" }
                },
                cts.Token);

            toolNames.Should().Contain("start_stored_procedure_in_database");
            toolNames.Should().Contain("get_session_status");
            toolNames.Should().Contain("get_session_results");
            toolNames.Should().Contain("stop_session");
            toolNames.Should().Contain("list_sessions");

            toolNames.Should().NotContain("execute_query_in_database");
            toolNames.Should().NotContain("execute_stored_procedure_in_database");
            toolNames.Should().NotContain("start_query_in_database");
        }

        [Fact(DisplayName = "TE-INT-012: Server mode with all flags enabled registers all execution tools")]
        public async Task TE_INT_012()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            var toolNames = await GetToolNamesAsync(
                EnvironmentVariableHelper.GetServerModeConnectionString(_dockerFixture.SqlServerPort),
                new Dictionary<string, string>
                {
                    { "DatabaseConfiguration__EnableExecuteQuery", "true" },
                    { "DatabaseConfiguration__EnableExecuteStoredProcedure", "true" },
                    { "DatabaseConfiguration__EnableStartQuery", "true" },
                    { "DatabaseConfiguration__EnableStartStoredProcedure", "true" }
                },
                cts.Token);

            toolNames.Should().Contain("execute_query_in_database");
            toolNames.Should().Contain("execute_stored_procedure_in_database");
            toolNames.Should().Contain("start_query_in_database");
            toolNames.Should().Contain("start_stored_procedure_in_database");
            toolNames.Should().Contain("get_session_status");
        }
    }
}
