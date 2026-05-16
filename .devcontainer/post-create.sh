#!/bin/bash
set -e

echo "==> Restoring .NET packages..."
dotnet restore /workspaces/5coy-archive/5coy-archive.sln

echo "==> Installing Claude Code CLI..."
npm install -g @anthropic-ai/claude-code

echo "==> Dev environment ready."
dotnet --version && node --version && gh --version && claude --version
