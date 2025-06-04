using System.Text.Json;

namespace Core.Infrastructure.SqlClient.Utilities
{
    /// <summary>
    /// Utility class to convert JSON parameters to appropriate .NET types for SQL parameters.
    /// </summary>
    public static class JsonParameterConverter
    {
        /// <summary>
        /// Parses a JSON string into a parameter dictionary.
        /// </summary>
        /// <param name="parametersJson">The JSON string to parse</param>
        /// <returns>A dictionary with converted parameter values</returns>
        public static Dictionary<string, object?> ParseParametersFromJson(string parametersJson)
        {
            if (string.IsNullOrWhiteSpace(parametersJson) || parametersJson.Trim() == "{}")
                return new Dictionary<string, object?>();

            var jsonParameters = JsonSerializer.Deserialize<Dictionary<string, object?>>(parametersJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return ConvertParameters(jsonParameters ?? new Dictionary<string, object?>());
        }

        /// <summary>
        /// Converts a dictionary that may contain JsonElement values to a dictionary with
        /// corresponding .NET types that SQL Server can handle.
        /// </summary>
        /// <param name="parameters">The input parameter dictionary</param>
        /// <returns>A new dictionary with converted parameter values</returns>
        public static Dictionary<string, object?> ConvertParameters(Dictionary<string, object?> parameters)
        {
            if (parameters == null)
                return new Dictionary<string, object?>();

            var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            foreach (var param in parameters)
            {
                // If parameter is null, keep it as null
                if (param.Value == null)
                {
                    result[param.Key] = null;
                    continue;
                }

                // If parameter is a JsonElement, convert it to an appropriate .NET type
                if (param.Value is JsonElement jsonElement)
                {
                    result[param.Key] = ConvertJsonElement(jsonElement);
                }
                else
                {
                    // If it's already a .NET type, just use it directly
                    result[param.Key] = param.Value;
                }
            }

            return result;
        }

        /// <summary>
        /// Converts a JsonElement to an appropriate .NET type based on its ValueKind.
        /// </summary>
        /// <param name="element">The JsonElement to convert</param>
        /// <returns>A .NET object of the appropriate type</returns>
        private static object? ConvertJsonElement(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Null => null,
                JsonValueKind.String => element.GetString(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number when element.TryGetInt32(out int intValue) => intValue,
                JsonValueKind.Number when element.TryGetInt64(out long longValue) => longValue,
                JsonValueKind.Number when element.TryGetDouble(out double doubleValue) => doubleValue,
                JsonValueKind.Number => element.GetDecimal(), // Fallback for other numeric types
                JsonValueKind.Array => ConvertJsonArray(element),
                JsonValueKind.Object => element.GetRawText(), // Convert objects to JSON string
                _ => element.GetRawText() // Default fallback to raw JSON
            };
        }

        /// <summary>
        /// Converts a JSON array to a .NET array.
        /// </summary>
        /// <param name="arrayElement">The JsonElement representing an array</param>
        /// <returns>An array of converted values</returns>
        private static object[] ConvertJsonArray(JsonElement arrayElement)
        {
            var result = new List<object>();
            foreach (var item in arrayElement.EnumerateArray())
            {
                var value = ConvertJsonElement(item);
                if (value != null)
                {
                    result.Add(value);
                }
                else
                {
                    result.Add(DBNull.Value);
                }
            }
            return result.ToArray();
        }
    }
}