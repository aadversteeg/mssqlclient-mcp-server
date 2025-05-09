# SQL Server MCP Client

A Microsoft SQL Server client implementing the Model Context Protocol (MCP). This server provides SQL query capabilities through a simple MCP interface.

## Overview

The SQL Server MCP client is built with .NET Core using the Model Context Protocol C# SDK ([github.com/modelcontextprotocol/csharp-sdk](https://github.com/modelcontextprotocol/csharp-sdk)). It provides tools for executing SQL queries, listing tables, and retrieving schema information from SQL Server databases. The server is designed to be lightweight and demonstrates how to create a custom MCP server with practical database functionality. It can be deployed either directly on a machine or as a Docker container.

## Features

- Execute SQL queries on a connected SQL Server database
- List all tables in the connected database with schema and row count information
- Retrieve detailed schema information for specific tables
- Configure database connection through environment variables

## Getting Started

### Prerequisites

- .NET 9.0 (for local development/deployment)
- Docker (for container deployment)


### Build Instructions (for development)

If you want to build the project from source:

1. Clone this repository:
   ```bash
   git clone https://github.com/aadversteeg/mssqlclient.git
   ```

2. Navigate to the project root directory:
   ```bash
   cd mssqlclient
   ```

3. Build the project using:
   ```bash
   dotnet build src/mssqlclient.sln
   ```

4. Run the tests:
   ```bash
   dotnet test src/mssqlclient.sln
   ```

## Docker Support

### Local Registry

The SQL Server MCP Client is available in your local registry at port 5000.

```bash
# Pull the latest version
docker pull localhost:5000/mssqlclient-mcp-server:latest
```

### Manual Docker Build

If you need to build the Docker image yourself:

```bash
# Navigate to the repository root
cd mssqlclient

# Build the Docker image
docker build -f src/Core.Infrastructure.McpServer/Dockerfile -t mssqlclient-mcp-server:latest src/

# Run the locally built image
docker run -d --name mssql-mcp -e "MSSQL_CONNECTIONSTRING=Server=your_server;Database=your_db;User Id=your_user;Password=your_password;TrustServerCertificate=True;" mssqlclient-mcp-server:latest
```

### Local Registry Push

To push to your local registry:

```bash
# Build the Docker image
docker build -f src/Core.Infrastructure.McpServer/Dockerfile -t localhost:5000/mssqlclient-mcp-server:latest src/

# Push to local registry
docker push localhost:5000/mssqlclient-mcp-server:latest
```

## MCP Protocol Usage

### Client Integration

To connect to the SQL Server MCP Client from your applications:

1. Use the Model Context Protocol C# SDK or any MCP-compatible client
2. Configure your client to connect to the server's endpoint
3. Call the available tools described below

### Available Tools

#### execute_query

Executes a SQL query on the connected SQL Server database.

Parameters:
- `query` (required): The SQL query to execute.

Example request:
```json
{
  "name": "execute_query",
  "parameters": {
    "query": "SELECT TOP 5 * FROM Customers"
  }
}
```

Example response:
```
| CustomerID | CompanyName                      | ContactName        |
| ---------- | -------------------------------- | ------------------ |
| ALFKI      | Alfreds Futterkiste              | Maria Anders       |
| ANATR      | Ana Trujillo Emparedados y h...  | Ana Trujillo       |
| ANTON      | Antonio Moreno Taquería          | Antonio Moreno     |
| AROUT      | Around the Horn                  | Thomas Hardy       |
| BERGS      | Berglunds snabbköp               | Christina Berglund |

Total rows: 5
```

#### list_tables

Lists all tables in the connected SQL Server database with schema and row count information.

Example request:
```json
{
  "name": "list_tables",
  "parameters": {}
}
```

Example response:
```
Available Tables:

Schema | Table Name | Row Count
------ | ---------- | ---------
dbo    | Customers  | 91
dbo    | Products   | 77
dbo    | Orders     | 830
dbo    | Employees  | 9
```

#### get_table_schema

Gets the schema of a table from the connected SQL Server database.

Parameters:
- `tableName` (required): The name of the table to get schema information for.

Example request:
```json
{
  "name": "get_table_schema",
  "parameters": {
    "tableName": "Customers"
  }
}
```

Example response:
```
Schema for table: Customers

Column Name | Data Type | Max Length | Is Nullable
----------- | --------- | ---------- | -----------
CustomerID  | nchar     | 5          | NO
CompanyName | nvarchar  | 40         | NO
ContactName | nvarchar  | 30         | YES
ContactTitle| nvarchar  | 30         | YES
Address     | nvarchar  | 60         | YES
City        | nvarchar  | 15         | YES
Region      | nvarchar  | 15         | YES
PostalCode  | nvarchar  | 10         | YES
Country     | nvarchar  | 15         | YES
Phone       | nvarchar  | 24         | YES
Fax         | nvarchar  | 24         | YES
```

## Configuration

### Database Connection String

The SQL Server connection string is required to connect to your database. This connection string should include server information, database name, authentication details, and any required connection options.

You can set the connection string using the `MSSQL_CONNECTIONSTRING` environment variable:

```bash
# When running the Docker container
docker run -e "MSSQL_CONNECTIONSTRING=Server=your_server;Database=your_db;User Id=your_user;Password=your_password;TrustServerCertificate=True;" localhost:5000/mssqlclient-mcp-server:latest
```

A typical connection string includes:
- Server name or IP address
- Database name
- Authentication method (SQL Server or Windows Authentication)
- Optional parameters like encryption settings

Example connection strings:
```
# SQL Server Authentication
Server=database.example.com;Database=Northwind;User Id=sa;Password=YourPassword;TrustServerCertificate=True;

# Windows Authentication
Server=database.example.com;Database=Northwind;Integrated Security=SSPI;TrustServerCertificate=True;

# SQL Server with specific port
Server=database.example.com,1433;Database=Northwind;User Id=sa;Password=YourPassword;TrustServerCertificate=True;
```

If no connection string is provided, the server will return an error message when attempting to use the tools.

> Integrated security will not work from a docker container!

## Configuring Claude Desktop

### Using Local Installation

To configure Claude Desktop to use a locally installed SQL Server MCP client:

1. Add the server configuration to the `mcpServers` section in your Claude Desktop configuration:
```json
"mssql": {
  "command": "dotnet",
  "args": [
    "YOUR_PATH_TO_DLL\\Core.Infrastructure.McpServer.dll"
  ],
  "env": {
    "MSSQL_CONNECTIONSTRING": "Server=your_server;Database=your_db;User Id=your_user;Password=your_password;TrustServerCertificate=True;"
  }
}
```

2. Save the file and restart Claude Desktop

### Using Docker Container

To use the SQL Server MCP client from a Docker container with Claude Desktop:

1. Add the server configuration to the `mcpServers` section in your Claude Desktop configuration:
```json
"mssql": {
  "command": "docker",
  "args": [
    "run",
    "--rm",
    "-i",
    "-e", "MSSQL_CONNECTIONSTRING=Server=your_server;Database=your_db;User Id=your_user;Password=your_password;TrustServerCertificate=True;",
    "localhost:5000/mssqlclient-mcp-server:latest"
  ]
}
```

2. Save the file and restart Claude Desktop

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.