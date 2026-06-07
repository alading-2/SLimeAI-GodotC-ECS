#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
db_path="$repo_root/Data/DataOS/Authoring/slimeainew.authoring.db"
snapshot_path="$repo_root/Data/DataOS/Snapshots/runtime_snapshot.json"
handle_path="$repo_root/Data/DataKey/Generated/DataKey_Generated.cs"

echo "==> [1/4] Rebuild authoring DB from seed SQL"
bash "$repo_root/Data/DataOS/Tools/build-authoring-db.sh"

echo ""
echo "==> [2/4] Generate runtime_snapshot.json"
bash "$repo_root/Data/DataOS/Tools/generate-runtime-snapshot.sh" "$db_path" "$snapshot_path"

echo ""
echo "==> [3/4] Generate DataKey_Generated.cs"
python3 "$repo_root/Data/DataOS/Tools/generate-data-key-handles.py" "$snapshot_path" "$handle_path"

echo ""
echo "==> [4/4] Validate DataOS (snapshot + handle consistency)"
bash "$repo_root/Data/DataOS/Tools/validate-dataos.sh" "$db_path"

echo ""
echo "=== Done ==="
echo "  DB:       $db_path"
echo "  Snapshot: $snapshot_path"
echo "  Handles:  $handle_path"
