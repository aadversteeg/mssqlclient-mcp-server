using System;
using Ave.Testing.ModelContextProtocol.Builder;
using Ave.Testing.ModelContextProtocol.Builder.ExecutionEnvironments;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace UnitTests.Testing.ModelContextProtocol.Builder
{
    public class SseTransportBuilderTests
    {
        private readonly Mock<ILogger> _loggerMock = new Mock<ILogger>();

        [Fact(DisplayName = "STB-001: UseHttpClient returns HttpClientExecutionBuilder")]
        public void STB001()
        {
            // Arrange
            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithSseTransport();

            // Act
            var httpBuilder = builder.UseHttpClient("https://example.com/api");

            // Assert
            httpBuilder.Should().NotBeNull();
            httpBuilder.Should().BeOfType<HttpClientExecutionBuilder>();
        }

        [Fact(DisplayName = "STB-002: UseHttpClient with empty URL throws")]
        public void STB002()
        {
            // Arrange
            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithSseTransport();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.UseHttpClient(string.Empty));
        }

        [Fact(DisplayName = "STB-003: UseHttpClient with null-like URL throws")]
        public void STB003()
        {
            // Arrange
            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithSseTransport();
            string? nullLikeUrl = null;

            // Act & Assert
            // This would throw a compiler error if passed directly,
            // so we're testing the principle with a null-like variable
            // which would have the same effect at runtime
#pragma warning disable CS8604 // Possible null reference argument.
            Assert.Throws<ArgumentNullException>(() => builder.UseHttpClient(nullLikeUrl));
#pragma warning restore CS8604 // Possible null reference argument.
        }

        [Fact(DisplayName = "STB-004: Transport can work without logger")]
        public void STB004()
        {
            // Arrange
            var builder = new McpClientBuilder()
                .WithSseTransport();

            // Act
            var httpBuilder = builder.UseHttpClient("https://example.com/api");

            // Assert
            httpBuilder.Should().NotBeNull();
            httpBuilder.Should().BeOfType<HttpClientExecutionBuilder>();
        }
    }
}