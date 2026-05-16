#!/bin/bash
set -e

echo "==> Restoring .NET packages..."
dotnet restore /workspaces/5coy-archive/5coy-archive.sln

echo "==> Installing Azure Functions Core Tools v4..."
npm install -g azure-functions-core-tools@4 --unsafe-perm true

echo "==> Installing Claude Code CLI..."
npm install -g @anthropic-ai/claude-code

echo "==> Dev environment ready."
dotnet --version && node --version && gh --version && func --version && claude --version
