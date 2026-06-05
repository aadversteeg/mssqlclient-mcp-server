using System.ComponentModel;
using System.Reflection;
using Core.Application.Models;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace Core.Infrastructure.McpServer.Extensions
{
    internal static class McpServerBuilderExtensions
    {
        /// <summary>
        /// Registers all McpServerTool methods on <typeparamref name="T"/>, resolving {PropertyName}
        /// placeholders in their Description attributes from <paramref name="config"/>.
        /// </summary>
        public static IMcpServerBuilder WithTools<T>(this IMcpServerBuilder builder, DatabaseConfiguration config) where T : class
        {
            builder.Services.AddSingleton<T>();

            var tools = typeof(T).GetMethods()
                .Where(m => m.GetCustomAttribute<McpServerToolAttribute>() != null)
                .Select(method =>
                {
                    var description = method.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty;
                    description = DescriptionPlaceholderResolver.Resolve(description, config);
                    return McpServerTool.Create(
                        method,
                        createTargetFunc: ctx => ctx.Server.Services!.GetRequiredService<T>(),
                        options: new McpServerToolCreateOptions
                        {
                            Description = description
                        });
                });

            return builder.WithTools(tools);
        }
    }
}
