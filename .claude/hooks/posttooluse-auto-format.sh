#!/bin/bash
# PostToolUse: auto-format after AI edits for .NET & React

TOOL_NAME=$1

if [[ "$TOOL_NAME" = "Write" || \
      "$TOOL_NAME" = "Edit" ]]; then
  
  # Format .NET (C#) code
  dotnet format
  
  # Format and lint React (JS/TS) code
  npx prettier --write "**/*.{js,jsx,ts,tsx}"
  npx eslint --fix "**/*.{js,jsx,ts,tsx}"
  
  # Stage the formatted files
  git add -A
fi
