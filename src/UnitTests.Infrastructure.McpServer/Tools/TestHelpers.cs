using System;
using Microsoft.Extensions.Options;
using Core.Application.Models;

namespace UnitTests.Infrastructure.McpServer.Tools
{
    public static class TestHelpers
    {
        public static IOptions<DatabaseConfiguration> CreateConfiguration(int? totalTimeoutSeconds = 300)
        {
            var config = new DatabaseConfiguration
            {
                TotalToolCallTimeoutSeconds = totalTimeoutSeconds
            };
            return Options.Create(config);
        }
    }
}
