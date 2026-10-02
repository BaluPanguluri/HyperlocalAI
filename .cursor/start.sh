#!/usr/bin/env bash
set -euo pipefail

if ! curl -sf http://127.0.0.1:11434/api/tags >/dev/null 2>&1; then
  if ! pgrep -x ollama >/dev/null 2>&1; then
    ollama serve >>/tmp/ollama.log 2>&1 &
  fi
  for _ in $(seq 1 90); do
    curl -sf http://127.0.0.1:11434/api/tags >/dev/null 2>&1 && break
    sleep 1
  done
fi

if ! curl -sf http://127.0.0.1:11434/api/tags >/dev/null 2>&1; then
  echo "Ollama did not become ready on 127.0.0.1:11434" >&2
  exit 1
fi

if ! ollama list 2>/dev/null | grep -q 'gemma3:4b'; then
  ollama pull gemma3:4b
fi

cd /workspace/src/HyperlocalAI.Web
export ASPNETCORE_ENVIRONMENT=Development
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export ASPNETCORE_URLS=http://0.0.0.0:5179
exec dotnet run --no-build -c Release --project /workspace/src/HyperlocalAI.Web/HyperlocalAI.Web.csproj --urls http://0.0.0.0:5179
