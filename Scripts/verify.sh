#!/usr/bin/env bash
set -euo pipefail
project_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
dotnet_command="${GATEFALL_DOTNET:-dotnet}"
"$dotnet_command" run --project "$project_root/Tests/Core/Gatefall.Specs.csproj" --configfile "$project_root/Tests/Core/NuGet.Config"

# Set UNITY_EDITOR to the actual Unity executable to include import/compilation.
if [[ -n "${UNITY_EDITOR:-}" ]]; then
  "$UNITY_EDITOR" -batchmode -nographics -quit -projectPath "$project_root" \
    -executeMethod Gatefall.Editor.ProjectSetup.Prepare -logFile "$project_root/unity-verification.log"
fi
