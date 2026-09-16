#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
: "${MANAGED_DIR:?Set MANAGED_DIR to the Valheim Managed directory.}"
: "${BEPINEX_DIR:?Set BEPINEX_DIR to the profile BepInEx directory.}"
dotnet test tests/PlayerTradingReforged.Tests/PlayerTradingReforged.Tests.csproj -c Release \
  -p:ManagedDir="$MANAGED_DIR" -p:BepInExDir="$BEPINEX_DIR"
dotnet run --project tests/CompatibilityCheck/CompatibilityCheck.csproj -c Release \
  -p:BepInExDir="$BEPINEX_DIR" -- "$MANAGED_DIR" "$BEPINEX_DIR" \
  src/PlayerTradingReforged/bin/Release/netstandard2.1/PlayerTradingReforged.dll
bun run scripts/validate-package.ts
