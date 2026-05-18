#!/usr/bin/env bash
#
# backup-media-bucket.sh
#
# Pre-flight backup for migrate-media-paths.sh. MUST be run before the
# migration to ensure a recoverable pre-state in case the migration goes
# wrong.
#
# Layers three independent recovery paths:
#   1. Enables S3 versioning (in-place, fastest restore)
#   2. Captures an inventory manifest (auditable text artefact)
#   3. Syncs the bucket to a local directory + tars it (definitive copy)
#
# Idempotent: re-running checks state and skips already-done steps.
#
# Requires: aws cli on PATH, AWS creds with s3 + s3api permissions.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"

usage() {
  cat >&2 <<EOF
USAGE:
  $(basename "$0") <bucket> [--backup-dir <path>] [--skip-local-sync] [--skip-versioning]

ARGUMENTS:
  <bucket>             S3 bucket name (e.g. 5coy-media-prod).

OPTIONS:
  --backup-dir <path>  Local directory for the sync backup.
                       Default: \${REPO_ROOT}/backups/<bucket>-<UTC-date>
  --skip-local-sync    Skip step 3 (local sync + tar). Versioning + inventory
                       only.
  --skip-versioning    Skip step 1 (bucket versioning check / enable).
                       Use if your org policy controls versioning elsewhere.
  --no-tar             After local sync, do not create the tar.gz archive.
  -h | --help          Show this message.

EXIT CODES:
  0   backup complete
  1   precondition failure (missing cli, missing bucket, permission denied)
  2   verification mismatch (sync object count != bucket object count)

After successful completion, migrate-media-paths.sh --execute is safe to run.
EOF
}

# --- args ---
BUCKET=""
BACKUP_DIR=""
SKIP_LOCAL_SYNC=0
SKIP_VERSIONING=0
NO_TAR=0
UTC_DATE="$(date -u +%Y-%m-%d)"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --backup-dir) BACKUP_DIR="$2"; shift 2 ;;
    --skip-local-sync) SKIP_LOCAL_SYNC=1; shift ;;
    --skip-versioning) SKIP_VERSIONING=1; shift ;;
    --no-tar) NO_TAR=1; shift ;;
    -h|--help) usage; exit 0 ;;
    -*) echo "Unknown option: $1" >&2; usage; exit 1 ;;
    *)
      if [[ -z "$BUCKET" ]]; then
        BUCKET="$1"
      else
        echo "Unexpected positional arg: $1" >&2
        usage; exit 1
      fi
      shift
      ;;
  esac
done

if [[ -z "$BUCKET" ]]; then
  usage; exit 1
fi
if [[ -z "$BACKUP_DIR" ]]; then
  BACKUP_DIR="${REPO_ROOT}/backups/${BUCKET}-${UTC_DATE}"
fi

# --- preflight ---
command -v aws >/dev/null 2>&1 || { echo "aws cli not on PATH" >&2; exit 1; }

# Confirm we can list the bucket.
if ! aws s3api head-bucket --bucket "$BUCKET" >/dev/null 2>&1; then
  echo "Cannot access bucket s3://${BUCKET}/. Check creds and bucket name." >&2
  exit 1
fi

echo "=== backup-media-bucket.sh ==="
echo "  Bucket:        s3://${BUCKET}"
echo "  Backup dir:    ${BACKUP_DIR}"
echo "  UTC date:      ${UTC_DATE}"
echo

# --- step 1: versioning ---
if [[ $SKIP_VERSIONING -eq 0 ]]; then
  echo "[1/3] Bucket versioning..."
  STATUS="$(aws s3api get-bucket-versioning --bucket "$BUCKET" \
              --query 'Status' --output text 2>/dev/null || echo "None")"
  if [[ "$STATUS" == "Enabled" ]]; then
    echo "  ✓ versioning already Enabled"
  else
    echo "  versioning currently: ${STATUS}"
    echo "  enabling versioning..."
    aws s3api put-bucket-versioning --bucket "$BUCKET" \
      --versioning-configuration Status=Enabled
    echo "  ✓ versioning now Enabled"
  fi
  echo
else
  echo "[1/3] Bucket versioning... SKIPPED (--skip-versioning)"
  echo
fi

# --- step 2: inventory manifest ---
echo "[2/3] Inventory manifest..."
mkdir -p "$BACKUP_DIR"
MANIFEST="${BACKUP_DIR}/inventory-${UTC_DATE}.txt"
aws s3 ls "s3://${BUCKET}/" --recursive --summarize > "$MANIFEST"
OBJ_COUNT="$(grep -c '^20' "$MANIFEST" || echo 0)"
TOTAL_SIZE="$(grep 'Total Size' "$MANIFEST" | awk '{print $3, $4}')"
echo "  ✓ wrote ${MANIFEST}"
echo "    objects: ${OBJ_COUNT}, total size: ${TOTAL_SIZE}"
echo

# --- step 3: local sync ---
if [[ $SKIP_LOCAL_SYNC -eq 0 ]]; then
  echo "[3/3] Local sync..."
  SYNC_DIR="${BACKUP_DIR}/objects"
  mkdir -p "$SYNC_DIR"
  echo "  syncing s3://${BUCKET}/ → ${SYNC_DIR}/ ..."
  aws s3 sync "s3://${BUCKET}/" "$SYNC_DIR/" --no-progress
  LOCAL_COUNT="$(find "$SYNC_DIR" -type f | wc -l | tr -d ' ')"
  echo "  ✓ synced ${LOCAL_COUNT} files"

  if [[ "$LOCAL_COUNT" != "$OBJ_COUNT" ]]; then
    echo "  WARN: local file count (${LOCAL_COUNT}) does not match bucket object count (${OBJ_COUNT})" >&2
    echo "  This can happen if objects were added/deleted during the sync." >&2
    echo "  Inspect ${MANIFEST} and ${SYNC_DIR} to reconcile." >&2
    exit 2
  fi

  if [[ $NO_TAR -eq 0 ]]; then
    TAR_PATH="${BACKUP_DIR}.tar.gz"
    echo "  archiving → ${TAR_PATH} ..."
    tar -czf "$TAR_PATH" -C "$(dirname "$BACKUP_DIR")" "$(basename "$BACKUP_DIR")"
    TAR_SIZE="$(du -h "$TAR_PATH" | awk '{print $1}')"
    echo "  ✓ archive ${TAR_SIZE}"
  fi
  echo
else
  echo "[3/3] Local sync... SKIPPED (--skip-local-sync)"
  echo
fi

cat <<EOF
=== Backup complete ===

Recovery paths now available:
  1. In-place version restore (versioning enabled on bucket).
     Restore an individual object: see AWS console → bucket → object →
     Versions tab; or aws s3api list-object-versions / get-object.
  2. Inventory manifest: ${MANIFEST}
     Diff against post-migration state to detect drift.
EOF

if [[ $SKIP_LOCAL_SYNC -eq 0 ]]; then
  echo "  3. Local sync: ${BACKUP_DIR}/objects/"
  [[ $NO_TAR -eq 0 ]] && echo "     Tarball:    ${BACKUP_DIR}.tar.gz"
  echo "     Restore via: aws s3 sync ${BACKUP_DIR}/objects/ s3://${BUCKET}/"
fi

echo
echo "Safe to proceed with: ./scripts/migrate-media-paths.sh ${BUCKET} [--execute]"
