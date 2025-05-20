namespace Core.Infrastructure.McpServer.Configuration
{
    public class DatabaseConfiguration
    {
        public string ConnectionString { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets whether the execute query tools should be enabled.
        /// When false, the execute_query and execute_query_in_database tools will not be registered.
        /// Default is false.
        /// </summary>
        public bool EnableExecuteQuery { get; set; } = false;
    }
}
