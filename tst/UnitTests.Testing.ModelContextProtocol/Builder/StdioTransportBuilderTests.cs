using System;
using Ave.Testing.ModelContextProtocol.Builder;
using Ave.Testing.ModelContextProtocol.Builder.ExecutionEnvironments;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace UnitTests.Testing.ModelContextProtocol.Builder
{
    public class StdioTransportBuilderTests
    {
        private readonly Mock<ILogger> _loggerMock = new Mock<ILogger>();
        private readonly string _testAssemblyPath = "path/to/test.dll";
        private readonly string _testImageName = "test-image:latest";
        private readonly string _testExePath = "dotnet";

        [Fact(DisplayName = "STB-001: UsingDotNet returns DotNetExecutionBuilder")]
        public void STB001()
        {
            // Arrange
            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithStdioTransport();

            // Act
            var dotNetBuilder = builder.UsingDotNet(_testAssemblyPath);

            // Assert
            dotNetBuilder.Should().NotBeNull();
            dotNetBuilder.Should().BeOfType<DotNetExecutionBuilder>();
        }

        [Fact(DisplayName = "STB-002: UsingDocker returns DockerExecutionBuilder")]
        public void STB002()
        {
            // Arrange
            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithStdioTransport();

            // Act
            var dockerBuilder = builder.UsingDocker(_testImageName);

            // Assert
            dockerBuilder.Should().NotBeNull();
            dockerBuilder.Should().BeOfType<DockerExecutionBuilder>();
        }

        [Fact(DisplayName = "STB-003: UsingCommand returns CommandExecutionBuilder")]
        public void STB003()
        {
            // Arrange
            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithStdioTransport();

            // Act
            var commandBuilder = builder.UsingCommand(_testExePath);

            // Assert
            commandBuilder.Should().NotBeNull();
            commandBuilder.Should().BeOfType<CommandExecutionBuilder>();
        }

        [Fact(DisplayName = "STB-004: UsingAutoExe returns CommandExecutionBuilder")]
        public void STB004()
        {
            // Arrange
            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithStdioTransport();

            // Act
            var autoExeBuilder = builder.UsingAutoExe();

            // Assert
            autoExeBuilder.Should().NotBeNull();
            autoExeBuilder.Should().BeOfType<CommandExecutionBuilder>();
        }

        [Fact(DisplayName = "STB-005: UsingBuild returns CommandExecutionBuilder")]
        public void STB005()
        {
            // Arrange
            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithStdioTransport();

            // Act
            var buildBuilder = builder.UsingBuild(forceBuild: true);

            // Assert
            buildBuilder.Should().NotBeNull();
            buildBuilder.Should().BeOfType<CommandExecutionBuilder>();
        }

        [Fact(DisplayName = "STB-006: UsingDotNet with empty path throws")]
        public void STB006()
        {
            // Arrange
            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithStdioTransport();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.UsingDotNet(string.Empty));
        }

        [Fact(DisplayName = "STB-007: UsingDocker with empty image throws")]
        public void STB007()
        {
            // Arrange
            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithStdioTransport();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.UsingDocker(string.Empty));
        }

        [Fact(DisplayName = "STB-008: UsingCommand with empty path throws")]
        public void STB008()
        {
            // Arrange
            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithStdioTransport();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.UsingCommand(string.Empty));
        }

        [Fact(DisplayName = "STB-009: Transport can work without logger")]
        public void STB009()
        {
            // Arrange
            var builder = new McpClientBuilder()
                .WithStdioTransport();

            // Act
            var commandBuilder = builder.UsingCommand(_testExePath);

            // Assert
            commandBuilder.Should().NotBeNull();
            commandBuilder.Should().BeOfType<CommandExecutionBuilder>();
        }
    }
}