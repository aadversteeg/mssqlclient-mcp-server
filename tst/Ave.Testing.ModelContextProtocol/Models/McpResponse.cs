using System.Text.Json.Serialization;

namespace Ave.Testing.ModelContextProtocol.Models
{
    /// <summary>
    /// Represents an MCP response message
    /// </summary>
    public class McpResponse : McpMessage
    {
        /// <summary>
        /// The result of the method invocation
        /// </summary>
        [JsonPropertyName("result")]
        public object? Result { get; set; }

        /// <summary>
        /// Error information if the method invocation failed
        /// </summary>
        [JsonPropertyName("error")]
        public McpError? Error { get; set; }

        /// <summary>
        /// Indicates whether the response was successful
        /// </summary>
        public bool IsSuccess => Error == null;
    }

    /// <summary>
    /// Represents an error in an MCP response
    /// </summary>
    public class McpError
    {
        /// <summary>
        /// The error code
        /// </summary>
        [JsonPropertyName("code")]
        public int Code { get; set; }

        /// <summary>
        /// The error message
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Additional error data
        /// </summary>
        [JsonPropertyName("data")]
        public object? Data { get; set; }
    }
}