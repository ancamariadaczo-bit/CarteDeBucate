#!/bin/sh

set -eu

REPO_ROOT=$(git rev-parse --show-toplevel)

git -C "$REPO_ROOT" config --local core.hooksPath .githooks
chmod +x "$REPO_ROOT/.githooks/pre-push"

echo "Git hooks are configured from .githooks."
