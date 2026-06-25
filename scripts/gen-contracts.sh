#!/usr/bin/env bash
set -euo pipefail

python3 scripts/generate_contracts.py
echo "Contracts generated successfully."
