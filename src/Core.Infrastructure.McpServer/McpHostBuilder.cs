using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace Core.Infrastructure.McpServer
{
    /// <summary>
    /// Creates the host builder for the server, including all of its configuration sources.
    /// </summary>
    /// <remarks>
    /// The server is launched from wherever its client sits, often the user's home directory. With
    /// the framework defaults that directory becomes the content root, and the default appsettings
    /// sources watch it recursively for changes: on Linux one inotify watch per directory under it.
    /// Configuration is only read at startup, so the content root is pinned to the install directory
    /// and reload-on-change is switched off for every source.
    /// </remarks>
    internal static class McpHostBuilder
    {
        /// <summary>
        /// Read by the host defaults while the builder is being constructed, so it has to be seeded
        /// before construction rather than set on the builder afterwards.
        /// </summary>
        internal const string ReloadConfigOnChangeKey = "hostBuilder:reloadConfigOnChange";

        public static HostApplicationBuilder Create(string[] args)
        {
            var configuration = new ConfigurationManager();
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ReloadConfigOnChangeKey] = "false"
            });

            var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
            {
                Args = args,
                ContentRootPath = AppContext.BaseDirectory,
                Configuration = configuration
            });

            // Add appsettings.json configuration, use full path in case working folder is different
            string? basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            basePath ??= Directory.GetCurrentDirectory();

            builder.Configuration.AddJsonFile(
                Path.Combine(basePath, "appsettings.json"),
                optional: true,
                reloadOnChange: false);

            builder.Configuration.AddJsonFile(
                Path.Combine(basePath, $"appsettings.{builder.Environment.EnvironmentName}.json"),
                optional: true,
                reloadOnChange: false);

            builder.Configuration.AddUserSecrets(
                Assembly.GetExecutingAssembly(),
                optional: true,
                reloadOnChange: false);

            builder.Configuration.AddEnvironmentVariables();

            return builder;
        }
    }
}
