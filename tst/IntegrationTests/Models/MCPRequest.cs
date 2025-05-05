using System.Text.Json.Serialization;

namespace IntegrationTests.Models
{
    public class MCPRequest : MCPMessage
    {
        [JsonPropertyName("method")]
        public string Method { get; set; } = string.Empty;

        [JsonPropertyName("params")]
        public object? Params { get; set; }

        public MCPRequest()
        {
            Id = Guid.NewGuid().ToString();
        }

        public MCPRequest(string method, object? parameters = null) : this()
        {
            Method = method;
            Params = parameters;
        }
    }
}