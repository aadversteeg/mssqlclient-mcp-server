using IntegrationTests.Implementation;
using IntegrationTests.Interfaces;
using Microsoft.Extensions.Logging;

namespace IntegrationTests.Factory
{
    public static class MCPClientFactory
    {
        public static IMCPClient Create(
            string executablePath, 
            Dictionary<string, string>? environmentVariables = null,
            ILogger? logger = null)
        {
            var process = new ProcessWrapper(executablePath, environmentVariables, logger);
            return new MCPClient(process, logger);
        }
    }
}