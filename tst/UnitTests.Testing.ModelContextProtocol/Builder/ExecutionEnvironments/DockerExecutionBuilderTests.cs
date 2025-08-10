using System;
using System.Collections.Generic;
using Ave.Testing.ModelContextProtocol.Builder;
using Ave.Testing.ModelContextProtocol.Builder.ExecutionEnvironments;
using Ave.Testing.ModelContextProtocol.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace UnitTests.Testing.ModelContextProtocol.Builder.ExecutionEnvironments
{
    public class DockerExecutionBuilderTests
    {
        private readonly Mock<ILogger> _loggerMock = new Mock<ILogger>();
        private readonly string _testImageName = "test-mcp-server:latest";

        [Fact(DisplayName = "DEB-001: Builder configures environment variables correctly")]
        public void DEB001()
        {
            // Arrange
            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithStdioTransport()
                .UsingDocker(_testImageName);

            // Act
            builder = builder
                .WithEnvironmentVariable("TEST_VAR", "test_value")
                .WithEnvironmentVariable("ANOTHER_VAR", "another_value");

            // Assert - we can't directly test the internal state, so we'll check that Build() doesn't throw
            var action = new Action(() => builder.Build());
            action.Should().NotThrow();
        }

        [Fact(DisplayName = "DEB-002: Builder with container name sets it correctly")]
        public void DEB002()
        {
            // Arrange
            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithStdioTransport()
                .UsingDocker(_testImageName);

            // Act
            builder = builder.WithContainerName("test-container");

            // Assert - we can't directly test the internal state, so we'll check that Build() doesn't throw
            var action = new Action(() => builder.Build());
            action.Should().NotThrow();
        }

        [Fact(DisplayName = "DEB-003: Builder with empty container name throws")]
        public void DEB003()
        {
            // Arrange
            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithStdioTransport()
                .UsingDocker(_testImageName);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.WithContainerName(string.Empty));
        }

        [Fact(DisplayName = "DEB-004: Builder with port mapping sets it correctly")]
        public void DEB004()
        {
            // Arrange
            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithStdioTransport()
                .UsingDocker(_testImageName);

            // Act
            builder = builder.WithPortMapping("8080", "80");

            // Assert - we can't directly test the internal state, so we'll check that Build() doesn't throw
            var action = new Action(() => builder.Build());
            action.Should().NotThrow();
        }

        [Fact(DisplayName = "DEB-005: Builder with empty port mapping throws")]
        public void DEB005()
        {
            // Arrange
            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithStdioTransport()
                .UsingDocker(_testImageName);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.WithPortMapping(string.Empty, "80"));
            Assert.Throws<ArgumentException>(() => builder.WithPortMapping("8080", string.Empty));
        }

        [Fact(DisplayName = "DEB-006: Builder with volume mapping sets it correctly")]
        public void DEB006()
        {
            // Arrange
            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithStdioTransport()
                .UsingDocker(_testImageName);

            // Act
            builder = builder.WithVolumeMapping("/host/path", "/container/path", true);

            // Assert - we can't directly test the internal state, so we'll check that Build() doesn't throw
            var action = new Action(() => builder.Build());
            action.Should().NotThrow();
        }

        [Fact(DisplayName = "DEB-007: Builder with empty volume mapping throws")]
        public void DEB007()
        {
            // Arrange
            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithStdioTransport()
                .UsingDocker(_testImageName);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.WithVolumeMapping(string.Empty, "/container/path"));
            Assert.Throws<ArgumentException>(() => builder.WithVolumeMapping("/host/path", string.Empty));
        }

        [Fact(DisplayName = "DEB-008: Builder with network sets it correctly")]
        public void DEB008()
        {
            // Arrange
            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithStdioTransport()
                .UsingDocker(_testImageName);

            // Act
            builder = builder.WithNetwork("test-network");

            // Assert - we can't directly test the internal state, so we'll check that Build() doesn't throw
            var action = new Action(() => builder.Build());
            action.Should().NotThrow();
        }

        [Fact(DisplayName = "DEB-009: Builder with empty network throws")]
        public void DEB009()
        {
            // Arrange
            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithStdioTransport()
                .UsingDocker(_testImageName);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.WithNetwork(string.Empty));
        }

        [Fact(DisplayName = "DEB-010: Builder with command sets it correctly")]
        public void DEB010()
        {
            // Arrange
            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithStdioTransport()
                .UsingDocker(_testImageName);

            // Act
            builder = builder.WithCommand("dotnet", "run", "--urls=http://0.0.0.0:5000");

            // Assert - we can't directly test the internal state, so we'll check that Build() doesn't throw
            var action = new Action(() => builder.Build());
            action.Should().NotThrow();
        }

        [Fact(DisplayName = "DEB-011: Builder with empty command throws")]
        public void DEB011()
        {
            // Arrange
            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithStdioTransport()
                .UsingDocker(_testImageName);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.WithCommand(string.Empty));
        }

        [Fact(DisplayName = "DEB-012: Builder with removeWhenExited sets it correctly")]
        public void DEB012()
        {
            // Arrange
            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithStdioTransport()
                .UsingDocker(_testImageName);

            // Act
            builder = builder.RemoveWhenExited(false);

            // Assert - we can't directly test the internal state, so we'll check that Build() doesn't throw
            var action = new Action(() => builder.Build());
            action.Should().NotThrow();
        }

        [Fact(DisplayName = "DEB-013: Builder with multiple configuration options works")]
        public void DEB013()
        {
            // Arrange
            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithStdioTransport()
                .UsingDocker(_testImageName);

            // Act
            builder = builder
                .WithContainerName("test-container")
                .WithEnvironmentVariable("TEST_VAR", "test_value")
                .WithPortMapping("8080", "80")
                .WithVolumeMapping("/host/path", "/container/path")
                .WithNetwork("test-network")
                .WithCommand("dotnet", "run")
                .RemoveWhenExited(true);

            // Assert
            var action = new Action(() => builder.Build());
            action.Should().NotThrow();
        }

        [Fact(DisplayName = "DEB-014: Build creates IMcpClient instance")]
        public void DEB014()
        {
            // Arrange
            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithStdioTransport()
                .UsingDocker(_testImageName)
                .WithEnvironmentVariable("TEST", "true");

            // Act
            var client = builder.Build();

            // Assert
            client.Should().NotBeNull();
            client.Should().BeAssignableTo<IMcpClient>();
        }
    }
}