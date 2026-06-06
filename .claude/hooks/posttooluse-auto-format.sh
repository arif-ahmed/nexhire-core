Please create a Claude Code PostToolUse hook to automatically format my .NET and React project. 

Perform the following steps in order:
1. Create a directory named `.claude/hooks` in the root of this project (if it doesn't already exist).
2. Create a file named `posttooluse-auto-format.sh` inside that directory.
3. Write the following bash script into that file exactly as provided:

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

4. Make the script executable by running the command: `chmod +x .claude/hooks/posttooluse-auto-format.sh`.

Let me know once you have completed these steps.