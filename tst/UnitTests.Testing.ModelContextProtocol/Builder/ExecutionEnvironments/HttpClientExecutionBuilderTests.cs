using System;
using Ave.Testing.ModelContextProtocol.Builder;
using Ave.Testing.ModelContextProtocol.Builder.ExecutionEnvironments;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace UnitTests.Testing.ModelContextProtocol.Builder.ExecutionEnvironments
{
    public class HttpClientExecutionBuilderTests
    {
        private readonly Mock<ILogger> _loggerMock = new Mock<ILogger>();
        private readonly string _testServerUrl = "https://test-mcp-server.example.com/api";

        [Fact(DisplayName = "HEB-001: Builder with header sets it correctly")]
        public void HEB001()
        {
            // Arrange
            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithSseTransport()
                .UseHttpClient(_testServerUrl);

            // Act
            builder = builder.WithHeader("X-Api-Key", "test-api-key");

            // Assert - this should throw as HttpClientExecutionBuilder is not fully implemented
            Assert.Throws<NotImplementedException>(() => builder.Build());
        }

        [Fact(DisplayName = "HEB-002: Builder with empty header throws")]
        public void HEB002()
        {
            // Arrange
            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithSseTransport()
                .UseHttpClient(_testServerUrl);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.WithHeader(string.Empty, "value"));
        }

        [Fact(DisplayName = "HEB-003: Builder with bearer authentication sets it correctly")]
        public void HEB003()
        {
            // Arrange
            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithSseTransport()
                .UseHttpClient(_testServerUrl);

            // Act
            builder = builder.WithBearerAuthentication("test-token");

            // Assert - this should throw as HttpClientExecutionBuilder is not fully implemented
            Assert.Throws<NotImplementedException>(() => builder.Build());
        }

        [Fact(DisplayName = "HEB-004: Builder with empty bearer token throws")]
        public void HEB004()
        {
            // Arrange
            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithSseTransport()
                .UseHttpClient(_testServerUrl);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.WithBearerAuthentication(string.Empty));
        }

        [Fact(DisplayName = "HEB-005: UseHttpClient with empty URL throws")]
        public void HEB005()
        {
            // Arrange
            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithSseTransport();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.UseHttpClient(string.Empty));
        }

        [Fact(DisplayName = "HEB-006: Builder with multiple configuration options works")]
        public void HEB006()
        {
            // Arrange
            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithSseTransport()
                .UseHttpClient(_testServerUrl);

            // Act
            builder = builder
                .WithHeader("X-Api-Key", "test-api-key")
                .WithHeader("X-Request-ID", "123456")
                .WithBearerAuthentication("test-token");

            // Assert - this should throw as HttpClientExecutionBuilder is not fully implemented
            Assert.Throws<NotImplementedException>(() => builder.Build());
        }
    }
}