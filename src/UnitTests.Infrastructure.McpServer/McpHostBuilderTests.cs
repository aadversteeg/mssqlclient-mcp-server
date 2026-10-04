using Core.Infrastructure.McpServer;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace UnitTests.Infrastructure.McpServer
{
    /// <summary>
    /// A reload-enabled file source rooted at the working directory makes the server watch that whole
    /// tree; launched from a home directory that is every directory under $HOME.
    /// </summary>
    public class McpHostBuilderTests
    {
        [Fact(DisplayName = "MHB-001: Create pins the content root to the install directory")]
        public void MHB001()
        {
            var builder = McpHostBuilder.Create([]);

            Path.TrimEndingDirectorySeparator(builder.Environment.ContentRootPath)
                .Should().Be(Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory));
        }

        [Fact(DisplayName = "MHB-002: Create disables reload-on-change for every file source")]
        public void MHB002()
        {
            var builder = McpHostBuilder.Create([]);

            var sources = ((IConfigurationBuilder)builder.Configuration).Sources
                .OfType<FileConfigurationSource>()
                .ToList();

            sources.Should().NotBeEmpty();
            sources.Should().OnlyContain(source => !source.ReloadOnChange);
        }
    }
}
