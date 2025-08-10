# Ave.Testing.ModelContextProtocol

A library for testing MCP (Model Context Protocol) servers and clients, providing utilities for establishing connections, sending requests, and verifying responses.

## Features

- Create MCP clients with fluent builder pattern
- Connect to MCP servers via different transport methods
- Support for various execution environments (.NET, Docker, custom commands)
- Easy configuration of environment variables and runtime settings
- Testing utilities for MCP message serialization/deserialization

## Getting Started

### Builder Pattern Usage

This library provides a fluent builder pattern for creating MCP clients, making it easy to configure complex settings in a type-safe manner.

#### Basic Usage Examples

```csharp
// Create a client for a .NET assembly using stdio transport
var dotnetClient = new McpClientBuilder()
    .WithLogger(logger)
    .WithStdioTransport()
        .UsingDotNet("path/to/mcp-server.dll")
        .WithEnvironmentVariable("CONNECTION_STRING", connectionString)
        .WithWorkingDirectory(workingDir)
        .WithArguments("--no-build")
    .Build();

// Create a client for a Docker container using stdio transport
var dockerClient = new McpClientBuilder()
    .WithLogger(logger)
    .WithStdioTransport()
        .UsingDocker("mcp-server:latest")
        .WithEnvironmentVariable("CONNECTION_STRING", connectionString)
        .WithContainerName("mcp-server-instance")
        .WithPortMapping("8080", "8080")
        .WithVolumeMapping("/host/config", "/app/config", true)
        .WithCommand("dotnet", "run")
    .Build();

// Create a client with automatic exe resolution
var autoExeClient = new McpClientBuilder()
    .WithLogger(logger)
    .WithStdioTransport()
        .UsingAutoExe()
        .WithEnvironmentVariable("KEY", "value")
    .Build();

// Create a client with build-on-demand
var buildClient = new McpClientBuilder()
    .WithLogger(logger)
    .WithStdioTransport()
        .UsingBuild(forceBuild: true)
        .WithEnvironmentVariable("KEY", "value")
    .Build();
```

#### Sending Requests

Once you've built a client, you can use it to send requests to the MCP server:

```csharp
// Start the client
client.Start();

// Create a request
var request = new McpRequest
{
    Method = "tool_name",
    Params = new 
    {
        parameter1 = "value1",
        parameter2 = "value2"
    }
};

// Send the request
var response = await client.SendRequestAsync(request);

// Check the response
if (response.IsSuccess)
{
    // Handle successful response
    var result = response.Result;
}
else
{
    // Handle error
    var errorCode = response.Error.Code;
    var errorMessage = response.Error.Message;
}
```

### Helper Utilities

The library also provides helper utilities for working with MCP messages:

```csharp
// Create a simple request object
var requestObj = McpMessageHelper.CreateRequest(
    id: "req-123",
    method: "tool_name",
    parameters: new { param1 = "value1" }
);
```

## Builder Configuration Options

### Transport Types

- **StdioTransport**: Communicates with the MCP server via standard input/output
  - Compatible with .NET, Docker, and custom command execution
  
- **SseTransport**: Communicates with the MCP server via Server-Sent Events over HTTP *(placeholder for future implementation)*
  - Compatible with HTTP client execution

### Execution Environments

#### DotNet Execution

For running a .NET assembly:

```csharp
.WithStdioTransport()
    .UsingDotNet("path/to/assembly.dll")
    .WithEnvironmentVariable(name, value)
    .WithWorkingDirectory(directory)
    .WithArguments(args)
```

#### Docker Execution

For running a Docker container:

```csharp
.WithStdioTransport()
    .UsingDocker("image:tag")
    .WithEnvironmentVariable(name, value)
    .WithContainerName(name)
    .WithPortMapping(hostPort, containerPort)
    .WithVolumeMapping(hostPath, containerPath, readOnly)
    .WithNetwork(networkName)
    .WithCommand(command, args)
    .RemoveWhenExited(true)
```

#### Command Execution

For running a custom command:

```csharp
.WithStdioTransport()
    .UsingCommand("path/to/executable")
    .WithEnvironmentVariable(name, value)
    .WithWorkingDirectory(directory)
    .WithArguments(args)
```

#### Auto-resolution and Build-on-demand

For automatically finding or building the MCP server:

```csharp
.WithStdioTransport()
    .UsingAutoExe()  // Auto-find the executable
    // Or
    .UsingBuild(forceBuild: true)  // Build if needed
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.