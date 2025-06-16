using Core.Application.Interfaces;
using Core.Application.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Core.Infrastructure.SqlClient
{
    /// <summary>
    /// Background service that periodically cleans up completed query sessions.
    /// </summary>
    public class SessionCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<SessionCleanupService> _logger;
        private readonly DatabaseConfiguration _configuration;
        
        public SessionCleanupService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<SessionCleanupService> logger,
            IOptions<DatabaseConfiguration> configuration)
        {
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Session cleanup service started with cleanup interval of {IntervalMinutes} minutes", 
                _configuration.SessionCleanupIntervalMinutes);
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var sessionManager = scope.ServiceProvider.GetRequiredService<IQuerySessionManager>();
                    
                    await sessionManager.CleanupCompletedSessions();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during session cleanup");
                }
                
                await Task.Delay(TimeSpan.FromMinutes(_configuration.SessionCleanupIntervalMinutes), stoppingToken);
            }
            
            _logger.LogInformation("Session cleanup service stopped");
        }
    }
}