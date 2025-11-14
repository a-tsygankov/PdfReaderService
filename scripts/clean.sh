#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="${SCRIPT_DIR}/.."

cd "${ROOT_DIR}"

echo "Stopping and removing volumes..."
docker compose down -v

echo "Pruning unused Docker resources..."
docker system prune -af
