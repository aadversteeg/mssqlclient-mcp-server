using System.Text.Json.Serialization;

namespace IntegrationTests.Models
{
    public class McpRequest : McpMessage
    {
        [JsonPropertyName("method")]
        public string Method { get; set; } = string.Empty;

        [JsonPropertyName("params")]
        public object? Params { get; set; }

        public McpRequest()
        {
            Id = Guid.NewGuid().ToString();
        }

        public McpRequest(string method, object? parameters = null) : this()
        {
            Method = method;
            Params = parameters;
        }
    }
}