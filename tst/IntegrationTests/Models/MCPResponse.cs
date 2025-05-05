using System.Text.Json.Serialization;

namespace IntegrationTests.Models
{
    public class MCPResponse : MCPMessage
    {
        [JsonPropertyName("result")]
        public object? Result { get; set; }

        [JsonPropertyName("error")]
        public MCPError? Error { get; set; }

        public bool IsSuccess => Error == null;
    }

    public class MCPError
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public object? Data { get; set; }
    }
}