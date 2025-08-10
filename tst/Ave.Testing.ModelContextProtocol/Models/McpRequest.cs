using System.Text.Json.Serialization;

namespace Ave.Testing.ModelContextProtocol.Models
{
    /// <summary>
    /// Represents an MCP request message
    /// </summary>
    public class McpRequest : McpMessage
    {
        /// <summary>
        /// The method to be invoked
        /// </summary>
        [JsonPropertyName("method")]
        public string Method { get; set; } = string.Empty;

        /// <summary>
        /// The parameters for the method
        /// </summary>
        [JsonPropertyName("params")]
        public object? Params { get; set; }

        /// <summary>
        /// Creates a new MCP request with a generated ID
        /// </summary>
        public McpRequest()
        {
            Id = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Creates a new MCP request with the specified method and parameters
        /// </summary>
        /// <param name="method">The method name</param>
        /// <param name="parameters">Optional parameters</param>
        public McpRequest(string method, object? parameters = null) : this()
        {
            Method = method;
            Params = parameters;
        }
    }
}