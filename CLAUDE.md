# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands
- Build: `dotnet build`
- Run: `dotnet run --project src/Core.Infrastructure.McpServer/Core.Infrastructure.McpServer.csproj`

## Code Style Guidelines
- Use C# 9+ features including nullable reference types (`Nullable>enable</Nullable>`)
- Follow standard C# naming conventions (PascalCase for types/methods, camelCase for variables)
- Use async/await pattern for asynchronous operations
- Implement proper exception handling with JSON response formatting
- Use JsonSerializerOptions with camelCase property naming for all JSON serialization
- Organize namespaces to match folder structure
- Use XML documentation comments for public APIs and tools
- Ensure all MCP tools have proper [Description] attributes
- Use dependency injection through the IServiceCollection builder
- Format code with 4-space indentation