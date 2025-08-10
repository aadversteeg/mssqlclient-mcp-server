# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/runtime:9.0-noble-chiseled-extra AS base
USER $APP_UID
WORKDIR /app


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0-noble AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Core.Infrastructure.McpServer/Core.Infrastructure.McpServer.csproj", "Core.Infrastructure.McpServer/"]
COPY ["Core.Application/Core.Application.csproj", "Core.Application/"]
COPY ["Core.Infrastructure.SqlClient/Core.Infrastructure.SqlClient.csproj", "Core.Infrastructure.SqlClient/"]
RUN dotnet restore "./Core.Infrastructure.McpServer/Core.Infrastructure.McpServer.csproj"
COPY . .
WORKDIR "/src/Core.Infrastructure.McpServer"
RUN dotnet build "./Core.Infrastructure.McpServer.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Core.Infrastructure.McpServer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Core.Infrastructure.McpServer.dll"]