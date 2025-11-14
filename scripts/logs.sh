#!/bin/bash
set -e

SERVICE="${1:-api}"

echo "Tailing logs for service: ${SERVICE}"
docker compose logs -f "${SERVICE}"
