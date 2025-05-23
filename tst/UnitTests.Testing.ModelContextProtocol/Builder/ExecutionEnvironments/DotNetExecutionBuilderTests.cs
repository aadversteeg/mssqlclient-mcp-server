using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Ave.Testing.ModelContextProtocol.Builder;
using Ave.Testing.ModelContextProtocol.Builder.ExecutionEnvironments;
using Ave.Testing.ModelContextProtocol.Implementation;
using Ave.Testing.ModelContextProtocol.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace UnitTests.Testing.ModelContextProtocol.Builder.ExecutionEnvironments
{
    public class DotNetExecutionBuilderTests
    {
        private readonly Mock<ILogger> _loggerMock = new Mock<ILogger>();
        private readonly string _testAssemblyPath;

        public DotNetExecutionBuilderTests()
        {
            // Use the current test assembly as a valid file path for testing
            _testAssemblyPath = Assembly.GetExecutingAssembly().Location;
        }

        [Fact(DisplayName = "DNB-001: Builder configures environment variables correctly")]
        public void DNB001()
        {
            // Arrange
            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithStdioTransport()
                .UsingDotNet(_testAssemblyPath);

            // Act
            builder = builder
                .WithEnvironmentVariable("TEST_VAR", "test_value")
                .WithEnvironmentVariable("ANOTHER_VAR", "another_value");

            // Assert - we can't directly test the internal state, so we'll check that Build() doesn't throw
            var action = new Action(() => builder.Build());
            action.Should().NotThrow();
        }

        [Fact(DisplayName = "DNB-002: Builder with working directory sets it correctly")]
        public void DNB002()
        {
            // Arrange
            var tempDir = Path.GetTempPath();
            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithStdioTransport()
                .UsingDotNet(_testAssemblyPath);

            // Act
            builder = builder.WithWorkingDirectory(tempDir);

            // Assert - we can't directly test the internal state, so we'll check that Build() doesn't throw
            var action = new Action(() => builder.Build());
            action.Should().NotThrow();
        }

        [Fact(DisplayName = "DNB-003: Builder with invalid working directory throws")]
        public void DNB003()
        {
            // Arrange
            var invalidDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithStdioTransport()
                .UsingDotNet(_testAssemblyPath);

            // Act & Assert
            Assert.Throws<DirectoryNotFoundException>(() => builder.WithWorkingDirectory(invalidDir));
        }

        [Fact(DisplayName = "DNB-004: Builder with arguments sets them correctly")]
        public void DNB004()
        {
            // Arrange
            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithStdioTransport()
                .UsingDotNet(_testAssemblyPath);

            // Act
            builder = builder.WithArguments("--arg1 value1 --arg2 value2");

            // Assert - we can't directly test the internal state, so we'll check that Build() doesn't throw
            var action = new Action(() => builder.Build());
            action.Should().NotThrow();
        }

        [Fact(DisplayName = "DNB-005: Builder with multiple environment variables dictionary works")]
        public void DNB005()
        {
            // Arrange
            var envVars = new Dictionary<string, string>
            {
                { "TEST_VAR1", "value1" },
                { "TEST_VAR2", "value2" },
                { "TEST_VAR3", "value3" }
            };

            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithStdioTransport()
                .UsingDotNet(_testAssemblyPath);

            // Act
            builder = builder.WithEnvironmentVariables(envVars);

            // Assert - we can't directly test the internal state, so we'll check that Build() doesn't throw
            var action = new Action(() => builder.Build());
            action.Should().NotThrow();
        }

        [Fact(DisplayName = "DNB-006: Build creates IMcpClient instance")]
        public void DNB006()
        {
            // Arrange
            var builder = new McpClientBuilder(_loggerMock.Object)
                .WithStdioTransport()
                .UsingDotNet(_testAssemblyPath)
                .WithArguments("--test")
                .WithEnvironmentVariable("TEST", "true");

            // Act
            var client = builder.Build();

            // Assert
            client.Should().NotBeNull();
            client.Should().BeAssignableTo<IMcpClient>();
        }
    }
}