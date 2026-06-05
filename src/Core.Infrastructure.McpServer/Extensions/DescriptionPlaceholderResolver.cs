using System.Text.RegularExpressions;

namespace Core.Infrastructure.McpServer.Extensions
{
    /// <summary>
    /// Resolves {PropertyName} placeholders in description strings using values from a source object.
    /// Unknown placeholders are left unchanged.
    /// </summary>
    internal static class DescriptionPlaceholderResolver
    {
        public static string Resolve(string template, object source)
        {
            return Regex.Replace(template, @"\{(\w+)\}", match =>
            {
                var propertyName = match.Groups[1].Value;
                var property = source.GetType().GetProperty(propertyName);
                return property?.GetValue(source)?.ToString() ?? match.Value;
            });
        }
    }
}
