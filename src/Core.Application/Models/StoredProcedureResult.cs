using System.Collections.Generic;
using Core.Application.Interfaces;

namespace Core.Application.Models
{
    /// <summary>
    /// Represents the result of executing a stored procedure, including the data reader and any output parameters.
    /// </summary>
    public class StoredProcedureResult
    {
        /// <summary>
        /// The data reader containing the result sets from the stored procedure.
        /// </summary>
        public IAsyncDataReader DataReader { get; set; }
        
        /// <summary>
        /// Dictionary containing output parameter values after stored procedure execution.
        /// Keys are parameter names (without @ prefix), values are the output values.
        /// </summary>
        public Dictionary<string, object?> OutputParameters { get; set; }
        
        /// <summary>
        /// Dictionary containing return values from the stored procedure execution.
        /// This typically includes the RETURN_VALUE parameter.
        /// </summary>
        public Dictionary<string, object?> ReturnValues { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the StoredProcedureResult class.
        /// </summary>
        /// <param name="dataReader">The data reader containing the result sets</param>
        public StoredProcedureResult(IAsyncDataReader dataReader)
        {
            DataReader = dataReader;
            OutputParameters = new Dictionary<string, object?>();
            ReturnValues = new Dictionary<string, object?>();
        }
        
        /// <summary>
        /// Initializes a new instance of the StoredProcedureResult class with all components.
        /// </summary>
        /// <param name="dataReader">The data reader containing the result sets</param>
        /// <param name="outputParameters">The output parameters dictionary</param>
        /// <param name="returnValues">The return values dictionary</param>
        public StoredProcedureResult(
            IAsyncDataReader dataReader,
            Dictionary<string, object?> outputParameters,
            Dictionary<string, object?> returnValues)
        {
            DataReader = dataReader;
            OutputParameters = outputParameters ?? new Dictionary<string, object?>();
            ReturnValues = returnValues ?? new Dictionary<string, object?>();
        }
    }
}