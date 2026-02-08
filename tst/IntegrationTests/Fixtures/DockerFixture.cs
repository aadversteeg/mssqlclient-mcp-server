using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IntegrationTests.Fixtures
{
    /// <summary>
    /// Fixture for managing Docker containers during integration tests
    /// </summary>
    public class DockerFixture : IAsyncLifetime
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DockerFixture> _logger;
        private readonly int _portRangeStart;
        private readonly int _portRangeEnd;
        private string? _containerName;
        private bool _ownsContainer;

        public int SqlServerPort { get; private set; }
        public string SqlServerConnectionString { get; private set; } = string.Empty;

        public DockerFixture()
        {
            // Build configuration
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.test.json", optional: false)
                .AddEnvironmentVariables()
                .Build();

            // Create logger factory and logger
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            _logger = loggerFactory.CreateLogger<DockerFixture>();

            // Parse port range
            var testConfig = _configuration.GetSection("IntegrationTests");
            var sqlPortRange = ParsePortRange(testConfig["SqlServer:PortRange"], 14330, 14339);
            _portRangeStart = sqlPortRange.start;
            _portRangeEnd = sqlPortRange.end;
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing Docker test environment");

            // Check if we should use existing containers
            if (_configuration.GetValue<bool>("IntegrationTests:UseExistingContainers"))
            {
                _logger.LogInformation("Using existing containers as specified in configuration");

                string password = _configuration["IntegrationTests:SqlServer:Password"] ?? "IntegrationTest!123";
                string database = _configuration["IntegrationTests:SqlServer:DatabaseName"] ?? "master";

                if (_configuration.GetValue<bool>("IntegrationTests:UseLocalSqlServer"))
                {
                    SqlServerConnectionString = _configuration["IntegrationTests:LocalSqlServerConnectionString"] ??
                        $"Server=localhost;Database={database};User Id=sa;Password={password};TrustServerCertificate=True;";
                }
                else
                {
                    SqlServerConnectionString = $"Server=localhost,14330;Database={database};User Id=sa;Password={password};TrustServerCertificate=True;";
                }

                return;
            }

            // Find a free port by scanning the range for one where no SQL Server responds
            SqlServerPort = await FindFreePortAsync();
            _logger.LogInformation("Allocated free SQL Server port: {Port}", SqlServerPort);

            // Start a new container with a unique name
            _containerName = $"mssql-test-{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            string image = _configuration["IntegrationTests:SqlServer:ImageName"] ?? "mcr.microsoft.com/mssql/server:2022-latest";
            string password2 = _configuration["IntegrationTests:SqlServer:Password"] ?? "IntegrationTest!123";

            _logger.LogInformation("Starting SQL Server container {ContainerName} on port {Port}", _containerName, SqlServerPort);

            var startInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"run --name {_containerName} -e \"ACCEPT_EULA=Y\" -e \"SA_PASSWORD={password2}\" -p {SqlServerPort}:1433 -d {image}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logger.LogError("Failed to start SQL Server container: {Error}", error);
                throw new InvalidOperationException($"Failed to start SQL Server container: {error}");
            }

            _logger.LogInformation("SQL Server container started: {ContainerId}", output.Trim());
            _ownsContainer = true;

            // Wait for SQL Server to be ready using a connection retry loop
            SqlServerConnectionString = BuildConnectionString(SqlServerPort);
            await WaitForSqlServerAsync(SqlServerPort);

            _logger.LogInformation("Docker test environment initialized with connection string: {ConnectionString}",
                SqlServerConnectionString.Replace("Password=", "Password=***"));
        }

        public async Task DisposeAsync()
        {
            if (!_ownsContainer || string.IsNullOrEmpty(_containerName))
            {
                _logger.LogInformation("No container to clean up (not owned or no container name)");
                return;
            }

            _logger.LogInformation("Removing container {ContainerName}", _containerName);

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"rm -f {_containerName}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = startInfo };
                process.Start();

                await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    _logger.LogWarning("Failed to remove container {ContainerName}: {Error}", _containerName, error);
                }
                else
                {
                    _logger.LogInformation("Container {ContainerName} removed successfully", _containerName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error removing container {ContainerName}", _containerName);
            }
        }

        private async Task<int> FindFreePortAsync()
        {
            for (int port = _portRangeStart; port <= _portRangeEnd; port++)
            {
                _logger.LogInformation("Checking port {Port}...", port);
                if (!await IsPortInUseAsync(port))
                {
                    _logger.LogInformation("Port {Port} is free", port);
                    return port;
                }
                _logger.LogInformation("Port {Port} is occupied, skipping", port);
            }

            throw new InvalidOperationException(
                $"No free ports available in range {_portRangeStart}-{_portRangeEnd}. " +
                "All ports are already in use.");
        }

        private async Task<bool> IsPortInUseAsync(int port)
        {
            try
            {
                using var client = new System.Net.Sockets.TcpClient();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                await client.ConnectAsync("localhost", port, cts.Token);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> TryConnectAsync(int port)
        {
            try
            {
                using var connection = new System.Data.SqlClient.SqlConnection(
                    $"Server=localhost,{port};Database=master;User Id=sa;Password=IntegrationTest!123;TrustServerCertificate=True;Connect Timeout=5;");
                await connection.OpenAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task WaitForSqlServerAsync(int port)
        {
            const int maxAttempts = 30;
            const int delayMs = 2000;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                _logger.LogInformation("Waiting for SQL Server to be ready (attempt {Attempt}/{Max})...", attempt, maxAttempts);

                if (await TryConnectAsync(port))
                {
                    _logger.LogInformation("SQL Server is ready on port {Port}", port);
                    return;
                }

                await Task.Delay(delayMs);
            }

            throw new TimeoutException($"SQL Server did not become ready on port {port} after {maxAttempts} attempts");
        }

        private static string BuildConnectionString(int port) =>
            $"Server=localhost,{port};Database=master;User Id=sa;Password=IntegrationTest!123;TrustServerCertificate=True;";

        private (int start, int end) ParsePortRange(string? portRange, int defaultStart, int defaultEnd)
        {
            if (string.IsNullOrEmpty(portRange))
            {
                return (defaultStart, defaultEnd);
            }

            var parts = portRange.Split('-');
            if (parts.Length != 2)
            {
                return (defaultStart, defaultEnd);
            }

            if (!int.TryParse(parts[0], out int start) || !int.TryParse(parts[1], out int end))
            {
                return (defaultStart, defaultEnd);
            }

            return (start, end);
        }
    }
}
