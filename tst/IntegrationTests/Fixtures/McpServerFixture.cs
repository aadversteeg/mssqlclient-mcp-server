using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Xunit;

namespace IntegrationTests.Fixtures
{
    /// <summary>
    /// Fixture for connecting to and managing the server during tests
    /// </summary>
    public class McpServerFixture : IClassFixture<DockerFixture>, IAsyncLifetime
    {
        private readonly DockerFixture _dockerFixture;
        private readonly ILogger<McpServerFixture> _logger;
        private readonly IConfiguration _configuration;
        private HttpClient _httpClient;

        public string McpServerEndpoint => _dockerFixture.McpServerEndpoint;
        public string SqlServerConnectionString => _dockerFixture.SqlServerConnectionString;

        public McpServerFixture(DockerFixture dockerFixture)
        {
            _dockerFixture = dockerFixture;
            
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
            
            _logger = loggerFactory.CreateLogger<McpServerFixture>();
            _httpClient = new HttpClient();
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing server fixture");
            
            // Check if we should skip Docker tests
            if (Environment.GetEnvironmentVariable("SKIP_DOCKER_TESTS") == "true")
            {
                _logger.LogInformation("Skipping server initialization as SKIP_DOCKER_TESTS is set");
                // Create a mock HttpClient
                _httpClient = new HttpClient();
                return;
            }
            
            // Initialize the HTTP client
            if (string.IsNullOrEmpty(_dockerFixture.McpServerEndpoint))
            {
                _logger.LogWarning("McpServerEndpoint is null or empty. Using fallback endpoint.");
                _httpClient = new HttpClient
                {
                    BaseAddress = new Uri("http://localhost:5100"),
                    Timeout = TimeSpan.FromSeconds(30)
                };
            }
            else
            {
                _httpClient = new HttpClient
                {
                    BaseAddress = new Uri(_dockerFixture.McpServerEndpoint),
                    Timeout = TimeSpan.FromSeconds(30)
                };
            }
            
            _logger.LogInformation("Server fixture initialized successfully");
        }

        public async Task DisposeAsync()
        {
            _logger.LogInformation("Cleaning up server fixture");
            _httpClient?.Dispose();
        }
    }
}