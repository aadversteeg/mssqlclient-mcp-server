using System;
using System.IO;
using Ave.Testing.ModelContextProtocol.Builder;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace UnitTests.Testing.ModelContextProtocol.Builder
{
    public class McpClientBuilderTests
    {
        private readonly Mock<ILogger> _loggerMock = new Mock<ILogger>();
        private readonly string _testExecutablePath = Path.Combine(AppContext.BaseDirectory, "testExecutable.exe");

        [Fact(DisplayName = "MCB-001: Builder creates instance with logger")]
        public void MCB001()
        {
            // Arrange & Act
            var builder = new McpClientBuilder()
                .WithLogger(_loggerMock.Object);

            // Assert
            builder.Should().NotBeNull();
        }

        [Fact(DisplayName = "MCB-002: Builder creates instance without logger")]
        public void MCB002()
        {
            // Arrange & Act
            var builder = new McpClientBuilder();

            // Assert
            builder.Should().NotBeNull();
        }

        [Fact(DisplayName = "MCB-003: StdioTransport returns appropriate builder")]
        public void MCB003()
        {
            // Arrange
            var builder = new McpClientBuilder(_loggerMock.Object);

            // Act
            var stdioBuilder = builder.WithStdioTransport();

            // Assert
            stdioBuilder.Should().NotBeNull();
            stdioBuilder.Should().BeOfType<StdioTransportBuilder>();
        }

        [Fact(DisplayName = "MCB-004: SseTransport returns appropriate builder")]
        public void MCB004()
        {
            // Arrange
            var builder = new McpClientBuilder(_loggerMock.Object);

            // Act
            var sseBuilder = builder.WithSseTransport();

            // Assert
            sseBuilder.Should().NotBeNull();
            sseBuilder.Should().BeOfType<SseTransportBuilder>();
        }

        [Fact(DisplayName = "MCB-005: SseTransport can be used without logger")]
        public void MCB005()
        {
            // Arrange
            var builder = new McpClientBuilder();

            // Act
            var sseBuilder = builder.WithSseTransport();

            // Assert
            sseBuilder.Should().NotBeNull();
            sseBuilder.Should().BeOfType<SseTransportBuilder>();
        }

        [Fact(DisplayName = "MCB-006: StdioTransport can be used without logger")]
        public void MCB006()
        {
            // Arrange
            var builder = new McpClientBuilder();

            // Act
            var stdioBuilder = builder.WithStdioTransport();

            // Assert
            stdioBuilder.Should().NotBeNull();
            stdioBuilder.Should().BeOfType<StdioTransportBuilder>();
        }

        [Fact(DisplayName = "MCB-007: DotNetExecutionBuilder validates assembly path")]
        public void MCB007()
        {
            // Arrange
            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithStdioTransport();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.UsingDotNet(string.Empty));
        }

        [Fact(DisplayName = "MCB-008: DockerExecutionBuilder validates image name")]
        public void MCB008()
        {
            // Arrange
            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithStdioTransport();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.UsingDocker(string.Empty));
        }

        [Fact(DisplayName = "MCB-009: CommandExecutionBuilder validates executable path")]
        public void MCB009()
        {
            // Arrange
            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithStdioTransport();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.UsingCommand(string.Empty));
        }

        [Fact(DisplayName = "MCB-010: HttpClientExecutionBuilder validates server URL")]
        public void MCB010()
        {
            // Arrange
            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithSseTransport();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.UseHttpClient(string.Empty));
        }

        [Fact(DisplayName = "MCB-011: Builder supports method chaining")]
        public void MCB011()
        {
            // Arrange & Act
            var action = new Action(() =>
            {
                var builder = new McpClientBuilder()
                    .WithLogger(_loggerMock.Object)
                    .WithStdioTransport()
                    .UsingCommand("dotnet")
                    .WithEnvironmentVariable("TEST", "value")
                    .WithArguments("--arg1 value1");
            });

            // Assert
            action.Should().NotThrow();
        }
    }
}