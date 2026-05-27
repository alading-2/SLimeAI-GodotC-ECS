#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
db_path="$repo_root/DataOS/Authoring/slimeainew.authoring.db"

rm -f "$db_path"
sqlite3 "$db_path" <<SQL
.read $repo_root/DataOS/Schema/core.sql
.read $repo_root/DataOS/Authoring/SlimeAINew.seed.sql
PRAGMA foreign_key_check;
SQL

echo "built $db_path"
