namespace Core.Application.Models
{
    /// <summary>
    /// Represents metadata about a stored procedure parameter retrieved from SQL Server system tables.
    /// </summary>
    public class StoredProcedureParameterMetadata
    {
        /// <summary>
        /// The parameter name including @ prefix (e.g., @CustomerID).
        /// </summary>
        public string ParameterName { get; set; } = string.Empty;
        
        /// <summary>
        /// The parameter ID from sys.parameters. Return value typically has ID 0.
        /// </summary>
        public int ParameterId { get; set; }
        
        /// <summary>
        /// The SQL data type name from sys.types (e.g., int, varchar, datetime).
        /// </summary>
        public string DataType { get; set; } = string.Empty;
        
        /// <summary>
        /// The maximum length of the parameter from sys.parameters.
        /// For string types, this is the maximum character/byte length.
        /// For numeric types, this may be the storage size.
        /// </summary>
        public int MaxLength { get; set; }
        
        /// <summary>
        /// The precision of the parameter for numeric types.
        /// </summary>
        public byte Precision { get; set; }
        
        /// <summary>
        /// The scale of the parameter for numeric types (number of decimal places).
        /// </summary>
        public byte Scale { get; set; }
        
        /// <summary>
        /// Indicates whether this is an output parameter.
        /// </summary>
        public bool IsOutput { get; set; }
        
        /// <summary>
        /// Indicates whether the parameter has a default value.
        /// </summary>
        public bool HasDefaultValue { get; set; }
        
        /// <summary>
        /// The default value of the parameter, if any.
        /// </summary>
        public object? DefaultValue { get; set; }
        
        /// <summary>
        /// Gets a display-friendly representation of the parameter type including length/precision info.
        /// </summary>
        public string GetDisplayType()
        {
            var baseType = DataType.ToLower();
            
            return baseType switch
            {
                "varchar" or "char" or "varbinary" or "binary" => 
                    MaxLength == -1 ? $"{DataType}(MAX)" : $"{DataType}({MaxLength})",
                "nvarchar" or "nchar" => 
                    MaxLength == -1 ? $"{DataType}(MAX)" : $"{DataType}({MaxLength / 2})",
                "decimal" or "numeric" => 
                    Scale > 0 ? $"{DataType}({Precision},{Scale})" : $"{DataType}({Precision})",
                "float" => 
                    Precision > 0 ? $"{DataType}({Precision})" : DataType,
                _ => DataType
            };
        }
        
        /// <summary>
        /// Gets a description of whether the parameter is required or optional.
        /// </summary>
        public string GetRequirementDescription()
        {
            if (IsOutput) return "OUTPUT";
            if (HasDefaultValue) return "Optional";
            return "Required";
        }
        
        /// <summary>
        /// Returns a string representation of the parameter metadata.
        /// </summary>
        public override string ToString()
        {
            var direction = IsOutput ? " OUTPUT" : "";
            var defaultInfo = HasDefaultValue ? $" = {DefaultValue}" : "";
            return $"{ParameterName} {GetDisplayType()}{direction}{defaultInfo}";
        }
    }
}