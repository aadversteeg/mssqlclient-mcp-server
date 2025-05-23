namespace Ave.Testing.ModelContextProtocol;

/// <summary>
/// Helper utilities for working with Model Context Protocol messages in test scenarios.
/// </summary>
public class McpMessageHelper
{
    /// <summary>
    /// Creates a simple MCP request message for testing purposes.
    /// </summary>
    /// <param name="id">The message ID</param>
    /// <param name="method">The method name</param>
    /// <param name="parameters">Optional parameters</param>
    /// <returns>A JSON-serializable object representing an MCP request</returns>
    public static object CreateRequest(string id, string method, object? parameters = null)
    {
        return new
        {
            jsonrpc = "2.0",
            id,
            method,
            @params = parameters
        };
    }
}
