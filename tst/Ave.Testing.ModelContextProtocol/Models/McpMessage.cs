using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ave.Testing.ModelContextProtocol.Models
{
    /// <summary>
    /// Base class for MCP messages
    /// </summary>
    public abstract class McpMessage
    {
        /// <summary>
        /// JSON-RPC version
        /// </summary>
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        /// <summary>
        /// Message ID
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// Deserialize JSON string to an MCP message object
        /// </summary>
        /// <typeparam name="T">Type of message to deserialize</typeparam>
        /// <param name="json">JSON string</param>
        /// <returns>Deserialized message or null if deserialization failed</returns>
        public static T? Deserialize<T>(string json) where T : McpMessage
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<T>(json, options);
        }

        /// <summary>
        /// Serialize this message to a JSON string
        /// </summary>
        /// <returns>JSON string</returns>
        public string Serialize()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
            return JsonSerializer.Serialize(this, GetType(), options);
        }
    }
}