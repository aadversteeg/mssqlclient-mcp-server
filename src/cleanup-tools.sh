#!/bin/bash

TOOLS_DIR="/mnt/d/ai/mcp/src/mssqlclient-mcp-server/src/Core.Infrastructure.McpServer/Tools"

# Define files to keep
KEEP_FILES=(
  "ExecuteQueryTool.cs"
  "GetTableSchemaTool.cs"
  "ListDatabasesTool.cs"
  "ListTablesTool.cs"
  "MasterExecuteQueryTool.cs"
  "MasterGetTableSchemaTool.cs"
  "MasterListTablesTool.cs"
)

# Change to the tools directory
cd "$TOOLS_DIR" || exit 1

# List all files in the directory
all_files=(*.cs)

# Remove files that are not in the keep list
for file in "${all_files[@]}"; do
  keep=false
  
  for keep_file in "${KEEP_FILES[@]}"; do
    if [ "$file" = "$keep_file" ]; then
      keep=true
      break
    fi
  done
  
  if [ "$keep" = false ]; then
    echo "Removing $file"
    rm -f "$file"
  else
    echo "Keeping $file"
  fi
done

echo "Cleanup complete"