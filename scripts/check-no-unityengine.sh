#!/usr/bin/env bash
set -euo pipefail

if grep -R "UnityEngine" src/Sim.Core >/dev/null 2>&1; then
  echo "ERROR: UnityEngine reference found under src/Sim.Core."
  exit 1
fi

echo "PASS: no UnityEngine references found under src/Sim.Core."
