namespace Core.Application.Models
{
    /// <summary>
    /// Factory for creating tool call timeout contexts and cancellation tokens.
    /// </summary>
    public static class ToolCallTimeoutFactory
    {
        /// <summary>
        /// Creates a timeout context with cancellation token for a tool call.
        /// </summary>
        /// <param name="configuration">Database configuration containing timeout settings</param>
        /// <returns>A tuple containing the timeout context and cancellation token source, or null if no timeout is configured</returns>
        public static (ToolCallTimeoutContext? Context, CancellationTokenSource? TokenSource) CreateTimeout(DatabaseConfiguration configuration)
        {
            if (configuration.TotalToolCallTimeoutSeconds == null)
            {
                return (null, null);
            }

            var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(configuration.TotalToolCallTimeoutSeconds.Value));
            var context = new ToolCallTimeoutContext(configuration.TotalToolCallTimeoutSeconds.Value, tokenSource.Token);
            
            return (context, tokenSource);
        }

        /// <summary>
        /// Combines a timeout context cancellation token with an existing cancellation token.
        /// </summary>
        /// <param name="timeoutContext">The timeout context containing the timeout cancellation token</param>
        /// <param name="existingToken">Existing cancellation token to combine</param>
        /// <returns>Combined cancellation token, or the existing token if no timeout context</returns>
        public static CancellationToken CombineTokens(ToolCallTimeoutContext? timeoutContext, CancellationToken existingToken = default)
        {
            if (timeoutContext == null)
            {
                return existingToken;
            }

            if (existingToken == default)
            {
                return timeoutContext.CancellationToken;
            }

            // Create a combined cancellation token
            var combined = CancellationTokenSource.CreateLinkedTokenSource(timeoutContext.CancellationToken, existingToken);
            return combined.Token;
        }
    }
}