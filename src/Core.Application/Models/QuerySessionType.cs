namespace Core.Application.Models
{
    /// <summary>
    /// Enumeration of query session types.
    /// </summary>
    public enum QuerySessionType
    {
        /// <summary>
        /// SQL query execution.
        /// </summary>
        Query,
        
        /// <summary>
        /// Stored procedure execution.
        /// </summary>
        StoredProcedure
    }
}