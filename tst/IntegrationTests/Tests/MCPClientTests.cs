using System.Text.Json;
using FluentAssertions;
using IntegrationTests.Factory;
using IntegrationTests.Helpers;
using IntegrationTests.Interfaces;
using IntegrationTests.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace IntegrationTests.Tests
{
    public class MCPClientTests
    {
        private readonly ILogger<MCPClientTests> _logger;
        
        public MCPClientTests()
        {
            // Create logger
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });
            
            _logger = loggerFactory.CreateLogger<MCPClientTests>();
        }
        
        [Fact(DisplayName = "MCP-001: SendRequest should return valid response")]
        public async Task MCP001()
        {
            // Arrange
            var mockProcess = new Mock<IProcessWrapper>();
            mockProcess.Setup(p => p.HasExited).Returns(false);
            mockProcess.Setup(p => p.ReadLineAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("{\"jsonrpc\":\"2.0\",\"id\":\"test-id\",\"result\":{\"name\":\"test-model\"}}");
            
            var client = new Implementation.MCPClient(mockProcess.Object, _logger);
            var request = new MCPRequest("model.info") { Id = "test-id" };
            
            // Act
            var response = await client.SendRequestAsync(request);
            
            // Assert
            response.Should().NotBeNull();
            response!.IsSuccess.Should().BeTrue();
            response.Result.Should().NotBeNull();
            response.Id.Should().Be("test-id");
            
            // Verify process interactions
            mockProcess.Verify(p => p.WriteLineAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            mockProcess.Verify(p => p.FlushAsync(It.IsAny<CancellationToken>()), Times.Once);
            mockProcess.Verify(p => p.ReadLineAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "MCP-002: SendRequest when process not running should throw exception")]
        public async Task MCP002()
        {
            // Arrange
            var mockProcess = new Mock<IProcessWrapper>();
            mockProcess.Setup(p => p.HasExited).Returns(true);
            
            var client = new Implementation.MCPClient(mockProcess.Object, _logger);
            var request = new MCPRequest("model.info");
            
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => client.SendRequestAsync(request));
        }
        
        [Fact(DisplayName = "MCP-003: MCPRequest should generate unique IDs")]
        public void MCP003()
        {
            // Arrange & Act
            var request1 = new MCPRequest("method1");
            var request2 = new MCPRequest("method1");
            
            // Assert
            request1.Id.Should().NotBeNullOrEmpty();
            request2.Id.Should().NotBeNullOrEmpty();
            request1.Id.Should().NotBe(request2.Id);
        }
        
        [Fact(DisplayName = "MCP-004: MCPResponse IsSuccess should be true when Error is null")]
        public void MCP004()
        {
            // Arrange
            var response = new MCPResponse
            {
                Id = "test-id",
                Result = new { name = "test-model" }
            };
            
            // Act & Assert
            response.IsSuccess.Should().BeTrue();
        }
        
        [Fact(DisplayName = "MCP-005: MCPResponse IsSuccess should be false when Error is not null")]
        public void MCP005()
        {
            // Arrange
            var response = new MCPResponse
            {
                Id = "test-id",
                Error = new MCPError { Code = -32600, Message = "Invalid Request" }
            };
            
            // Act & Assert
            response.IsSuccess.Should().BeFalse();
        }
        
        [Fact(DisplayName = "MCP-006: MCPMessage Serialize should produce valid JSON")]
        public void MCP006()
        {
            // Arrange
            var request = new MCPRequest("model.info")
            {
                Id = "test-id"
            };
            
            // Act
            string json = request.Serialize();
            
            // Assert
            json.Should().Contain("\"jsonrpc\":\"2.0\"");
            json.Should().Contain("\"id\":\"test-id\"");
            json.Should().Contain("\"method\":\"model.info\"");
        }
        
        [Fact(DisplayName = "MCP-007: MCPMessage Deserialize should produce valid object")]
        public void MCP007()
        {
            // Arrange
            string json = "{\"jsonrpc\":\"2.0\",\"id\":\"test-id\",\"result\":{\"name\":\"test-model\"}}";
            
            // Act
            var response = MCPMessage.Deserialize<MCPResponse>(json);
            
            // Assert
            response.Should().NotBeNull();
            response!.Id.Should().Be("test-id");
            response.JsonRpc.Should().Be("2.0");
            response.IsSuccess.Should().BeTrue();
        }
    }
}