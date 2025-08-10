using System;
using System.Text.Json;
using Core.Application.Models;
using Core.Infrastructure.McpServer.Tools;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace UnitTests.Infrastructure.McpServer.Tools
{
    public class TimeoutManagementToolsTests
    {
        [Fact(DisplayName = "TMT-001: TimeoutManagementTools constructor with null logger throws ArgumentNullException")]
        public void TMT001()
        {
            // Arrange
            var configuration = new DatabaseConfiguration();
            
            // Act
            ILogger<TimeoutManagementTools>? nullLogger = null;
            Action act = () => new TimeoutManagementTools(nullLogger, configuration);
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }
        
        [Fact(DisplayName = "TMT-002: TimeoutManagementTools constructor with null configuration throws ArgumentNullException")]
        public void TMT002()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<TimeoutManagementTools>>();
            
            // Act
            DatabaseConfiguration? nullConfiguration = null;
            Action act = () => new TimeoutManagementTools(mockLogger.Object, nullConfiguration);
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("configuration");
        }
        
        [Fact(DisplayName = "TMT-003: GetCommandTimeout returns current configuration")]
        public void TMT003()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<TimeoutManagementTools>>();
            var configuration = new DatabaseConfiguration
            {
                DefaultCommandTimeoutSeconds = 30,
                ConnectionTimeoutSeconds = 15,
                MaxConcurrentSessions = 10,
                SessionCleanupIntervalMinutes = 5
            };
            
            var tool = new TimeoutManagementTools(mockLogger.Object, configuration);
            
            // Act
            var result = tool.GetCommandTimeout();
            
            // Assert
            result.Should().NotBeNull();
            
            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("defaultCommandTimeoutSeconds").GetInt32().Should().Be(30);
            jsonDoc.RootElement.GetProperty("connectionTimeoutSeconds").GetInt32().Should().Be(15);
            jsonDoc.RootElement.GetProperty("maxConcurrentSessions").GetInt32().Should().Be(10);
            jsonDoc.RootElement.GetProperty("sessionCleanupIntervalMinutes").GetInt32().Should().Be(5);
        }
        
        [Fact(DisplayName = "TMT-004: SetCommandTimeout updates timeout successfully")]
        public void TMT004()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<TimeoutManagementTools>>();
            var configuration = new DatabaseConfiguration
            {
                DefaultCommandTimeoutSeconds = 30
            };
            
            var tool = new TimeoutManagementTools(mockLogger.Object, configuration);
            var newTimeout = 60;
            
            // Act
            var result = tool.SetCommandTimeout(newTimeout);
            
            // Assert
            result.Should().NotBeNull();
            configuration.DefaultCommandTimeoutSeconds.Should().Be(newTimeout);
            
            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("oldTimeoutSeconds").GetInt32().Should().Be(30);
            jsonDoc.RootElement.GetProperty("newTimeoutSeconds").GetInt32().Should().Be(newTimeout);
            jsonDoc.RootElement.GetProperty("message").GetString().Should().Contain("updated successfully");
            
            // Verify logging occurred
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Default command timeout changed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()), 
                Times.Once);
        }
        
        [Fact(DisplayName = "TMT-005: SetCommandTimeout rejects timeout less than 1")]
        public void TMT005()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<TimeoutManagementTools>>();
            var configuration = new DatabaseConfiguration
            {
                DefaultCommandTimeoutSeconds = 30
            };
            
            var tool = new TimeoutManagementTools(mockLogger.Object, configuration);
            var invalidTimeout = 0;
            
            // Act
            var result = tool.SetCommandTimeout(invalidTimeout);
            
            // Assert
            result.Should().NotBeNull();
            configuration.DefaultCommandTimeoutSeconds.Should().Be(30); // Should remain unchanged
            
            // Verify it's valid JSON with error
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("between 1 and 3600");
        }
        
        [Fact(DisplayName = "TMT-006: SetCommandTimeout rejects timeout greater than 3600")]
        public void TMT006()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<TimeoutManagementTools>>();
            var configuration = new DatabaseConfiguration
            {
                DefaultCommandTimeoutSeconds = 30
            };
            
            var tool = new TimeoutManagementTools(mockLogger.Object, configuration);
            var invalidTimeout = 3601;
            
            // Act
            var result = tool.SetCommandTimeout(invalidTimeout);
            
            // Assert
            result.Should().NotBeNull();
            configuration.DefaultCommandTimeoutSeconds.Should().Be(30); // Should remain unchanged
            
            // Verify it's valid JSON with error
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("between 1 and 3600");
        }
        
        [Fact(DisplayName = "TMT-007: SetCommandTimeout accepts valid boundary values")]
        public void TMT007()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<TimeoutManagementTools>>();
            var configuration = new DatabaseConfiguration
            {
                DefaultCommandTimeoutSeconds = 30
            };
            
            var tool = new TimeoutManagementTools(mockLogger.Object, configuration);
            
            // Test lower boundary
            var result1 = tool.SetCommandTimeout(1);
            result1.Should().NotBeNull();
            configuration.DefaultCommandTimeoutSeconds.Should().Be(1);
            
            var jsonDoc1 = JsonDocument.Parse(result1);
            jsonDoc1.RootElement.GetProperty("newTimeoutSeconds").GetInt32().Should().Be(1);
            
            // Test upper boundary
            var result2 = tool.SetCommandTimeout(3600);
            result2.Should().NotBeNull();
            configuration.DefaultCommandTimeoutSeconds.Should().Be(3600);
            
            var jsonDoc2 = JsonDocument.Parse(result2);
            jsonDoc2.RootElement.GetProperty("newTimeoutSeconds").GetInt32().Should().Be(3600);
        }
    }
}