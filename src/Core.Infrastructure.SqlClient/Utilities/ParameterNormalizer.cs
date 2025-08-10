using System;
using System.Collections.Generic;

namespace Core.Infrastructure.SqlClient.Utilities
{
    /// <summary>
    /// Utility class for normalizing parameter names and dictionaries to ensure consistent handling
    /// of SQL Server parameters with or without the @ prefix.
    /// </summary>
    public static class ParameterNormalizer
    {
        /// <summary>
        /// Normalizes a parameter name by removing the @ prefix if present.
        /// </summary>
        /// <param name="name">The parameter name to normalize</param>
        /// <returns>The parameter name without the @ prefix</returns>
        public static string NormalizeParameterName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return name;
                
            return name.StartsWith("@") ? name.Substring(1) : name;
        }
        
        /// <summary>
        /// Normalizes a dictionary of parameters to ensure consistent naming.
        /// This creates a new dictionary with case-insensitive keys and all parameter names without @ prefixes.
        /// </summary>
        /// <param name="parameters">The original parameter dictionary</param>
        /// <returns>A normalized dictionary with case-insensitive keys and no @ prefixes</returns>
        public static Dictionary<string, object?> NormalizeParameterDictionary(
            Dictionary<string, object?> parameters)
        {
            if (parameters == null)
                return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                
            var normalized = new Dictionary<string, object?>(
                parameters.Count, 
                StringComparer.OrdinalIgnoreCase);
            
            foreach (var kvp in parameters)
            {
                var normalizedKey = NormalizeParameterName(kvp.Key);
                normalized[normalizedKey] = kvp.Value;
            }
            
            return normalized;
        }
        
        /// <summary>
        /// Ensures parameter name has @ prefix for SQL Server compatibility.
        /// </summary>
        /// <param name="name">The parameter name</param>
        /// <returns>The parameter name with @ prefix</returns>
        public static string EnsureParameterPrefix(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return name;
                
            return name.StartsWith("@") ? name : $"@{name}";
        }
        
        /// <summary>
        /// Checks if a parameter name represents the same parameter as another, ignoring @ prefix and case.
        /// </summary>
        /// <param name="name1">First parameter name</param>
        /// <param name="name2">Second parameter name</param>
        /// <returns>True if the parameters represent the same parameter</returns>
        public static bool AreParameterNamesEqual(string name1, string name2)
        {
            if (string.IsNullOrWhiteSpace(name1) && string.IsNullOrWhiteSpace(name2))
                return true;
                
            if (string.IsNullOrWhiteSpace(name1) || string.IsNullOrWhiteSpace(name2))
                return false;
            
            var normalized1 = NormalizeParameterName(name1);
            var normalized2 = NormalizeParameterName(name2);
            
            return string.Equals(normalized1, normalized2, StringComparison.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// Finds a parameter value in a dictionary using case-insensitive comparison and ignoring @ prefix.
        /// </summary>
        /// <param name="parameters">The parameter dictionary</param>
        /// <param name="parameterName">The parameter name to search for</param>
        /// <param name="value">The found value, if any</param>
        /// <returns>True if the parameter was found</returns>
        public static bool TryGetParameterValue(
            Dictionary<string, object?> parameters,
            string parameterName,
            out object? value)
        {
            value = null;
            
            if (parameters == null || string.IsNullOrWhiteSpace(parameterName))
                return false;
            
            var normalizedName = NormalizeParameterName(parameterName);
            
            // First try exact match with normalized name
            if (parameters.TryGetValue(normalizedName, out value))
                return true;
            
            // Then try case-insensitive search
            foreach (var kvp in parameters)
            {
                if (AreParameterNamesEqual(kvp.Key, parameterName))
                {
                    value = kvp.Value;
                    return true;
                }
            }
            
            return false;
        }
    }
}