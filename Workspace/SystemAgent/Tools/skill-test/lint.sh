#!/usr/bin/env bash
# SystemAgent skill-test lint runner
# 用法: lint.sh static <all|changed|path> [--no-fail] [--summary-only]
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/../../../.." && pwd)"

MODE="${1:-static}"
SCOPE="${2:-all}"
NO_FAIL=0
SUMMARY_ONLY=0

shift 2 2>/dev/null || true
for arg in "$@"; do
    case "$arg" in
        --no-fail)      NO_FAIL=1 ;;
        --summary-only) SUMMARY_ONLY=1 ;;
    esac
done

if [ "$MODE" != "static" ]; then
    echo "ERROR: 仅支持 'static' mode" >&2
    exit 1
fi

set +e
python3 "$SCRIPT_DIR/lint.py" \
    --root "$ROOT" \
    --scope "$SCOPE" \
    $([ "$SUMMARY_ONLY" = "1" ] && echo "--summary-only")
EC=$?
set -e

if [ "$NO_FAIL" = "1" ]; then
    exit 0
fi
exit "$EC"
