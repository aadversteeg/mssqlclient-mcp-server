using Ave.Testing.ModelContextProtocol;
using System.Text.Json;

namespace UnitTests.Testing.ModelContextProtocol;

[Trait("Category", "Unit")]
[Trait("TestType", "Unit")]
public class McpMessageHelperTests
{
    [Fact(DisplayName = "MMH-001: CreateRequest should create valid MCP request")]
    public void MMH001()
    {
        // Arrange
        var id = "test-id-123";
        var method = "test.method";
        var parameters = new { foo = "bar", count = 42 };
        
        // Act
        var request = McpMessageHelper.CreateRequest(id, method, parameters);
        var serialized = JsonSerializer.Serialize(request);
        
        // Assert
        serialized.Should().Contain("\"jsonrpc\":\"2.0\"");
        serialized.Should().Contain($"\"id\":\"{id}\"");
        serialized.Should().Contain($"\"method\":\"{method}\"");
        serialized.Should().Contain("\"params\":{");
        serialized.Should().Contain("\"foo\":\"bar\"");
        serialized.Should().Contain("\"count\":42");
    }
    
    [Fact(DisplayName = "MMH-002: CreateRequest should handle null parameters")]
    public void MMH002()
    {
        // Arrange
        var id = "test-id-456";
        var method = "test.empty.method";
        
        // Act
        var request = McpMessageHelper.CreateRequest(id, method);
        var serialized = JsonSerializer.Serialize(request);
        
        // Assert
        serialized.Should().Contain("\"jsonrpc\":\"2.0\"");
        serialized.Should().Contain($"\"id\":\"{id}\"");
        serialized.Should().Contain($"\"method\":\"{method}\"");
        serialized.Should().Contain("\"params\":null");
    }
}
