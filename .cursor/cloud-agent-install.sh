#!/usr/bin/env bash
set -euo pipefail
cd /workspace
dotnet restore HyperlocalAI.sln
dotnet build HyperlocalAI.sln -c Release --no-restore
