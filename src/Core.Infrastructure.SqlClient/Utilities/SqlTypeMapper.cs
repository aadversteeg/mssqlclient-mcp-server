using System;
using System.Data;
using System.Globalization;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace Core.Infrastructure.SqlClient.Utilities
{
    /// <summary>
    /// Utility class for mapping JSON values to SQL Server parameter types based on the stored procedure's parameter metadata.
    /// </summary>
    public static class SqlTypeMapper
    {
        /// <summary>
        /// Creates a SqlParameter with the appropriate type and value based on SQL Server parameter metadata.
        /// </summary>
        /// <param name="parameterName">The parameter name (with or without @ prefix)</param>
        /// <param name="value">The value to convert</param>
        /// <param name="sqlDataType">The SQL data type name from sys.types</param>
        /// <param name="maxLength">The maximum length from sys.parameters</param>
        /// <param name="precision">The precision from sys.parameters</param>
        /// <param name="scale">The scale from sys.parameters</param>
        /// <param name="isOutput">Whether this is an output parameter</param>
        /// <returns>A configured SqlParameter</returns>
        public static SqlParameter CreateSqlParameter(
            string parameterName,
            object? value,
            string sqlDataType,
            int maxLength,
            byte precision,
            byte scale,
            bool isOutput)
        {
            var param = new SqlParameter(parameterName, DBNull.Value);
            
            if (isOutput)
            {
                param.Direction = value != null ? ParameterDirection.InputOutput : ParameterDirection.Output;
            }
            
            switch (sqlDataType.ToLower())
            {
                case "int":
                    param.SqlDbType = SqlDbType.Int;
                    param.Value = ConvertToInt32(value) ?? (object)DBNull.Value;
                    break;
                    
                case "bigint":
                    param.SqlDbType = SqlDbType.BigInt;
                    param.Value = ConvertToInt64(value) ?? (object)DBNull.Value;
                    break;
                    
                case "smallint":
                    param.SqlDbType = SqlDbType.SmallInt;
                    param.Value = ConvertToInt16(value) ?? (object)DBNull.Value;
                    break;
                    
                case "tinyint":
                    param.SqlDbType = SqlDbType.TinyInt;
                    param.Value = ConvertToByte(value) ?? (object)DBNull.Value;
                    break;
                    
                case "decimal":
                case "numeric":
                    param.SqlDbType = SqlDbType.Decimal;
                    param.Precision = precision;
                    param.Scale = scale;
                    param.Value = ConvertToDecimal(value) ?? (object)DBNull.Value;
                    break;
                    
                case "float":
                    param.SqlDbType = SqlDbType.Float;
                    param.Value = ConvertToDouble(value) ?? (object)DBNull.Value;
                    break;
                    
                case "real":
                    param.SqlDbType = SqlDbType.Real;
                    param.Value = ConvertToSingle(value) ?? (object)DBNull.Value;
                    break;
                    
                case "money":
                    param.SqlDbType = SqlDbType.Money;
                    param.Value = ConvertToDecimal(value) ?? (object)DBNull.Value;
                    break;
                    
                case "smallmoney":
                    param.SqlDbType = SqlDbType.SmallMoney;
                    param.Value = ConvertToDecimal(value) ?? (object)DBNull.Value;
                    break;
                    
                case "bit":
                    param.SqlDbType = SqlDbType.Bit;
                    param.Value = ConvertToBoolean(value) ?? (object)DBNull.Value;
                    break;
                    
                case "datetime":
                    param.SqlDbType = SqlDbType.DateTime;
                    param.Value = ConvertToDateTime(value) ?? (object)DBNull.Value;
                    break;
                    
                case "datetime2":
                    param.SqlDbType = SqlDbType.DateTime2;
                    param.Value = ConvertToDateTime(value) ?? (object)DBNull.Value;
                    break;
                    
                case "date":
                    param.SqlDbType = SqlDbType.Date;
                    param.Value = ConvertToDateTime(value)?.Date ?? (object)DBNull.Value;
                    break;
                    
                case "time":
                    param.SqlDbType = SqlDbType.Time;
                    param.Value = ConvertToTimeSpan(value) ?? (object)DBNull.Value;
                    break;
                    
                case "datetimeoffset":
                    param.SqlDbType = SqlDbType.DateTimeOffset;
                    param.Value = ConvertToDateTimeOffset(value) ?? (object)DBNull.Value;
                    break;
                    
                case "char":
                    param.SqlDbType = SqlDbType.Char;
                    param.Size = maxLength;
                    param.Value = (object?)ConvertToString(value) ?? DBNull.Value;
                    break;
                    
                case "varchar":
                    param.SqlDbType = SqlDbType.VarChar;
                    param.Size = maxLength;
                    param.Value = (object?)ConvertToString(value) ?? DBNull.Value;
                    break;
                    
                case "nchar":
                    param.SqlDbType = SqlDbType.NChar;
                    param.Size = maxLength / 2; // nchar uses 2 bytes per character
                    param.Value = (object?)ConvertToString(value) ?? DBNull.Value;
                    break;
                    
                case "nvarchar":
                    param.SqlDbType = SqlDbType.NVarChar;
                    param.Size = maxLength == -1 ? -1 : maxLength / 2;
                    param.Value = (object?)ConvertToString(value) ?? DBNull.Value;
                    break;
                    
                case "text":
                    param.SqlDbType = SqlDbType.Text;
                    param.Value = (object?)ConvertToString(value) ?? DBNull.Value;
                    break;
                    
                case "ntext":
                    param.SqlDbType = SqlDbType.NText;
                    param.Value = (object?)ConvertToString(value) ?? DBNull.Value;
                    break;
                    
                case "binary":
                    param.SqlDbType = SqlDbType.Binary;
                    param.Size = maxLength;
                    param.Value = (object?)ConvertToBinary(value) ?? DBNull.Value;
                    break;
                    
                case "varbinary":
                    param.SqlDbType = SqlDbType.VarBinary;
                    param.Size = maxLength;
                    param.Value = (object?)ConvertToBinary(value) ?? DBNull.Value;
                    break;
                    
                case "image":
                    param.SqlDbType = SqlDbType.Image;
                    param.Value = (object?)ConvertToBinary(value) ?? DBNull.Value;
                    break;
                    
                case "uniqueidentifier":
                    param.SqlDbType = SqlDbType.UniqueIdentifier;
                    param.Value = ConvertToGuid(value) ?? (object)DBNull.Value;
                    break;
                    
                case "xml":
                    param.SqlDbType = SqlDbType.Xml;
                    param.Value = (object?)ConvertToXml(value) ?? DBNull.Value;
                    break;
                    
                default:
                    // For unknown types, let SQL Server handle the conversion
                    param.Value = value ?? DBNull.Value;
                    break;
            }
            
            return param;
        }
        
        /// <summary>
        /// Converts a value to a 32-bit integer.
        /// </summary>
        private static int? ConvertToInt32(object? value)
        {
            if (value == null) return null;
            
            if (value is int i) return i;
            if (value is long l && l >= int.MinValue && l <= int.MaxValue) return (int)l;
            if (value is double d && d >= int.MinValue && d <= int.MaxValue && d == Math.Floor(d)) return (int)d;
            if (value is decimal dec && dec >= int.MinValue && dec <= int.MaxValue && dec == Math.Floor(dec)) return (int)dec;
            if (value is JsonElement element && element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out int jsonInt)) return jsonInt;
            
            if (value is string s && int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
                return parsed;
            
            throw new InvalidCastException($"Cannot convert value '{value}' to Int32");
        }
        
        /// <summary>
        /// Converts a value to a 64-bit integer.
        /// </summary>
        private static long? ConvertToInt64(object? value)
        {
            if (value == null) return null;
            
            if (value is long l) return l;
            if (value is int i) return i;
            if (value is double d && d >= long.MinValue && d <= long.MaxValue && d == Math.Floor(d)) return (long)d;
            if (value is decimal dec && dec >= long.MinValue && dec <= long.MaxValue && dec == Math.Floor(dec)) return (long)dec;
            if (value is JsonElement element && element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out long jsonLong)) return jsonLong;
            
            if (value is string s && long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsed))
                return parsed;
            
            throw new InvalidCastException($"Cannot convert value '{value}' to Int64");
        }
        
        /// <summary>
        /// Converts a value to a 16-bit integer.
        /// </summary>
        private static short? ConvertToInt16(object? value)
        {
            if (value == null) return null;
            
            if (value is short s) return s;
            if (value is int i && i >= short.MinValue && i <= short.MaxValue) return (short)i;
            if (value is long l && l >= short.MinValue && l <= short.MaxValue) return (short)l;
            if (value is double d && d >= short.MinValue && d <= short.MaxValue && d == Math.Floor(d)) return (short)d;
            if (value is JsonElement element && element.ValueKind == JsonValueKind.Number && element.TryGetInt16(out short jsonShort)) return jsonShort;
            
            if (value is string str && short.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out short parsed))
                return parsed;
            
            throw new InvalidCastException($"Cannot convert value '{value}' to Int16");
        }
        
        /// <summary>
        /// Converts a value to a byte.
        /// </summary>
        private static byte? ConvertToByte(object? value)
        {
            if (value == null) return null;
            
            if (value is byte b) return b;
            if (value is int i && i >= byte.MinValue && i <= byte.MaxValue) return (byte)i;
            if (value is long l && l >= byte.MinValue && l <= byte.MaxValue) return (byte)l;
            if (value is double d && d >= byte.MinValue && d <= byte.MaxValue && d == Math.Floor(d)) return (byte)d;
            if (value is JsonElement element && element.ValueKind == JsonValueKind.Number && element.TryGetByte(out byte jsonByte)) return jsonByte;
            
            if (value is string s && byte.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte parsed))
                return parsed;
            
            throw new InvalidCastException($"Cannot convert value '{value}' to Byte");
        }
        
        /// <summary>
        /// Converts a value to a decimal.
        /// </summary>
        private static decimal? ConvertToDecimal(object? value)
        {
            if (value == null) return null;
            
            if (value is decimal d) return d;
            if (value is int i) return i;
            if (value is long l) return l;
            if (value is double dbl) return (decimal)dbl;
            if (value is float f) return (decimal)f;
            if (value is JsonElement element && element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out decimal jsonDecimal)) return jsonDecimal;
            
            if (value is string s && decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed))
                return parsed;
            
            throw new InvalidCastException($"Cannot convert value '{value}' to Decimal");
        }
        
        /// <summary>
        /// Converts a value to a double.
        /// </summary>
        private static double? ConvertToDouble(object? value)
        {
            if (value == null) return null;
            
            if (value is double d) return d;
            if (value is float f) return f;
            if (value is int i) return i;
            if (value is long l) return l;
            if (value is decimal dec) return (double)dec;
            if (value is JsonElement element && element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out double jsonDouble)) return jsonDouble;
            
            if (value is string s && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
                return parsed;
            
            throw new InvalidCastException($"Cannot convert value '{value}' to Double");
        }
        
        /// <summary>
        /// Converts a value to a single-precision float.
        /// </summary>
        private static float? ConvertToSingle(object? value)
        {
            if (value == null) return null;
            
            if (value is float f) return f;
            if (value is double d && d >= float.MinValue && d <= float.MaxValue) return (float)d;
            if (value is int i) return i;
            if (value is long l) return l;
            if (value is decimal dec) return (float)dec;
            if (value is JsonElement element && element.ValueKind == JsonValueKind.Number && element.TryGetSingle(out float jsonFloat)) return jsonFloat;
            
            if (value is string s && float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
                return parsed;
            
            throw new InvalidCastException($"Cannot convert value '{value}' to Single");
        }
        
        /// <summary>
        /// Converts a value to a boolean.
        /// </summary>
        private static bool? ConvertToBoolean(object? value)
        {
            if (value == null) return null;
            
            if (value is bool b) return b;
            if (value is int i) return i != 0;
            if (value is long l) return l != 0;
            if (value is double d) return d != 0.0;
            if (value is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.True) return true;
                if (element.ValueKind == JsonValueKind.False) return false;
                if (element.ValueKind == JsonValueKind.Number) return element.GetDouble() != 0.0;
            }
            
            if (value is string s)
            {
                if (bool.TryParse(s, out bool parsed)) return parsed;
                if (s == "1" || s.Equals("yes", StringComparison.OrdinalIgnoreCase) || s.Equals("y", StringComparison.OrdinalIgnoreCase)) return true;
                if (s == "0" || s.Equals("no", StringComparison.OrdinalIgnoreCase) || s.Equals("n", StringComparison.OrdinalIgnoreCase)) return false;
            }
            
            throw new InvalidCastException($"Cannot convert value '{value}' to Boolean");
        }
        
        /// <summary>
        /// Converts a value to a DateTime.
        /// </summary>
        private static DateTime? ConvertToDateTime(object? value)
        {
            if (value == null) return null;
            
            if (value is DateTime dt) return dt;
            if (value is DateTimeOffset dto) return dto.DateTime;
            if (value is JsonElement element && element.ValueKind == JsonValueKind.String && DateTime.TryParse(element.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime jsonDateTime)) return jsonDateTime;
            
            if (value is string s && DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime parsed))
                return parsed;
            
            throw new InvalidCastException($"Cannot convert value '{value}' to DateTime");
        }
        
        /// <summary>
        /// Converts a value to a TimeSpan.
        /// </summary>
        private static TimeSpan? ConvertToTimeSpan(object? value)
        {
            if (value == null) return null;
            
            if (value is TimeSpan ts) return ts;
            if (value is DateTime dt) return dt.TimeOfDay;
            if (value is JsonElement element && element.ValueKind == JsonValueKind.String && TimeSpan.TryParse(element.GetString(), CultureInfo.InvariantCulture, out TimeSpan jsonTimeSpan)) return jsonTimeSpan;
            
            if (value is string s && TimeSpan.TryParse(s, CultureInfo.InvariantCulture, out TimeSpan parsed))
                return parsed;
            
            throw new InvalidCastException($"Cannot convert value '{value}' to TimeSpan");
        }
        
        /// <summary>
        /// Converts a value to a DateTimeOffset.
        /// </summary>
        private static DateTimeOffset? ConvertToDateTimeOffset(object? value)
        {
            if (value == null) return null;
            
            if (value is DateTimeOffset dto) return dto;
            if (value is DateTime dt) return new DateTimeOffset(dt);
            if (value is JsonElement element && element.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(element.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset jsonDateTimeOffset)) return jsonDateTimeOffset;
            
            if (value is string s && DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset parsed))
                return parsed;
            
            throw new InvalidCastException($"Cannot convert value '{value}' to DateTimeOffset");
        }
        
        /// <summary>
        /// Converts a value to a string.
        /// </summary>
        private static string? ConvertToString(object? value)
        {
            if (value == null) return null;
            
            if (value is string s) return s;
            if (value is JsonElement element && element.ValueKind == JsonValueKind.String) return element.GetString();
            
            return value.ToString();
        }
        
        /// <summary>
        /// Converts a value to a byte array.
        /// </summary>
        private static byte[]? ConvertToBinary(object? value)
        {
            if (value == null) return null;
            
            if (value is byte[] bytes) return bytes;
            if (value is JsonElement element && element.ValueKind == JsonValueKind.String)
            {
                var base64String = element.GetString();
                if (!string.IsNullOrEmpty(base64String))
                {
                    try
                    {
                        return Convert.FromBase64String(base64String);
                    }
                    catch (FormatException)
                    {
                        // If it's not valid base64, treat it as a regular string and convert to bytes
                        return System.Text.Encoding.UTF8.GetBytes(base64String);
                    }
                }
            }
            
            if (value is string str)
            {
                try
                {
                    return Convert.FromBase64String(str);
                }
                catch (FormatException)
                {
                    // If it's not valid base64, treat it as a regular string and convert to bytes
                    return System.Text.Encoding.UTF8.GetBytes(str);
                }
            }
            
            throw new InvalidCastException($"Cannot convert value '{value}' to byte array");
        }
        
        /// <summary>
        /// Converts a value to a GUID.
        /// </summary>
        private static Guid? ConvertToGuid(object? value)
        {
            if (value == null) return null;
            
            if (value is Guid g) return g;
            if (value is JsonElement element && element.ValueKind == JsonValueKind.String && Guid.TryParse(element.GetString(), out Guid jsonGuid)) return jsonGuid;
            
            if (value is string s && Guid.TryParse(s, out Guid parsed))
                return parsed;
            
            throw new InvalidCastException($"Cannot convert value '{value}' to Guid");
        }
        
        /// <summary>
        /// Converts a value to XML string format.
        /// </summary>
        private static string? ConvertToXml(object? value)
        {
            if (value == null) return null;
            
            if (value is string s) return s;
            if (value is JsonElement element && element.ValueKind == JsonValueKind.String) return element.GetString();
            
            // For objects, convert to JSON string which can then be processed as XML if needed
            return value.ToString();
        }
    }
}