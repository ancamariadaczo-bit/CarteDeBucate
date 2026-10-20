#!/usr/bin/env bash

set -euo pipefail

if [[ $# -ne 1 ]]; then
    echo "Usage: $0 <recipe-id>"
    exit 1
fi

RECIPE_ID="$1"

if [[ ! "$RECIPE_ID" =~ ^[1-9][0-9]*$ ]]; then
    echo "Recipe id must be a positive integer."
    exit 1
fi

WEB_DIRECTORY="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

DATABASE_PATH="$WEB_DIRECTORY/recipes.db"
PHOTO_ROOT="$WEB_DIRECTORY/recipe-photos"

OLD_STAGING_TOKEN="11111111111111111111111111111111"
ORPHAN_STORAGE_NAME="22222222222222222222222222222222.jpg"
NEW_STAGING_TOKEN="33333333333333333333333333333333"
QUARANTINE_TOKEN="44444444444444444444444444444444"
RESTORED_STORAGE_NAME="55555555555555555555555555555555.jpg"
INVALID_STAGING_NAME="not-a-token.upload"

if ! command -v sqlite3 >/dev/null 2>&1; then
    echo "sqlite3 is not available."
    exit 1
fi

if [[ ! -f "$DATABASE_PATH" ]]; then
    echo "Database not found: $DATABASE_PATH"
    exit 1
fi

if [[ ! -d "$PHOTO_ROOT" ]]; then
    echo "Photo directory not found: $PHOTO_ROOT"
    exit 1
fi

mkdir -p "$PHOTO_ROOT/staging"
mkdir -p "$PHOTO_ROOT/quarantine"

RECIPE_EXISTS="$(
    sqlite3 "$DATABASE_PATH" \
        "SELECT COUNT(*) FROM Recipes WHERE Id = $RECIPE_ID;"
)"

if [[ "$RECIPE_EXISTS" != "1" ]]; then
    echo "Recipe $RECIPE_ID does not exist."
    echo
    echo "Available recipes:"

    sqlite3 -header -column "$DATABASE_PATH" \
        "SELECT Id, Name FROM Recipes ORDER BY Id DESC LIMIT 10;"

    exit 1
fi

EXISTING_METADATA="$(
    sqlite3 "$DATABASE_PATH" \
        "SELECT COUNT(*)
         FROM RecipePhotos
         WHERE StorageFileName = '$RESTORED_STORAGE_NAME';"
)"

if [[ "$EXISTING_METADATA" != "0" ]]; then
    echo "Fixture metadata already exists in RecipePhotos."
    exit 1
fi

FIXTURE_PATHS=(
    "$PHOTO_ROOT/staging/$OLD_STAGING_TOKEN.upload"
    "$PHOTO_ROOT/staging/$NEW_STAGING_TOKEN.upload"
    "$PHOTO_ROOT/staging/$INVALID_STAGING_NAME"
    "$PHOTO_ROOT/$ORPHAN_STORAGE_NAME"
    "$PHOTO_ROOT/$RESTORED_STORAGE_NAME"
    "$PHOTO_ROOT/quarantine/$QUARANTINE_TOKEN"
)

for fixture_path in "${FIXTURE_PATHS[@]}"; do
    if [[ -e "$fixture_path" ]]; then
        echo "Fixture path already exists:"
        echo "$fixture_path"
        echo "Aborting without changing data."
        exit 1
    fi
done

echo
echo "WARNING: this script will modify the active application data."
echo
echo "Database:"
echo "  $DATABASE_PATH"
echo
echo "Photo directory:"
echo "  $PHOTO_ROOT"
echo
echo "Recipe id:"
echo "  $RECIPE_ID"
echo

read -r -p "Type APPLY-LIVE-FIXTURES to continue: " CONFIRMATION

if [[ "$CONFIRMATION" != "APPLY-LIVE-FIXTURES" ]]; then
    echo "Cancelled."
    exit 1
fi

BACKUP_DIRECTORY="$WEB_DIRECTORY/reconciliation-backup-$(date +%Y%m%d-%H%M%S)"

echo
echo "Creating backup:"
echo "  $BACKUP_DIRECTORY"

mkdir "$BACKUP_DIRECTORY"

sqlite3 "$DATABASE_PATH" \
    ".backup '$BACKUP_DIRECTORY/recipes.db'"

cp -R "$PHOTO_ROOT" \
    "$BACKUP_DIRECTORY/recipe-photos"

echo "Creating reconciliation fixtures..."

# DeletedCount: upload valid, mai vechi de 24 de ore.
printf 'old staging' \
    > "$PHOTO_ROOT/staging/$OLD_STAGING_TOKEN.upload"

touch -t 202001010000 \
    "$PHOTO_ROOT/staging/$OLD_STAGING_TOKEN.upload"

# SkippedTooNewCount: upload valid, cu timestamp curent.
printf 'new staging' \
    > "$PHOTO_ROOT/staging/$NEW_STAGING_TOKEN.upload"

# FailedCount: nume invalid în staging.
printf 'invalid inventory entry' \
    > "$PHOTO_ROOT/staging/$INVALID_STAGING_NAME"

# OrphanedCount: fișier final fără metadata.
printf 'orphan' \
    > "$PHOTO_ROOT/$ORPHAN_STORAGE_NAME"

touch -t 202001010000 \
    "$PHOTO_ROOT/$ORPHAN_STORAGE_NAME"

# RestoredCount: fișier în quarantine care încă are metadata.
mkdir "$PHOTO_ROOT/quarantine/$QUARANTINE_TOKEN"

printf 'test' \
    > "$PHOTO_ROOT/quarantine/$QUARANTINE_TOKEN/$RESTORED_STORAGE_NAME"

touch -t 202001010000 \
    "$PHOTO_ROOT/quarantine/$QUARANTINE_TOKEN/$RESTORED_STORAGE_NAME"

printf '%s\n' \
    "{\"token\":\"$QUARANTINE_TOKEN\",\"storageFileName\":\"$RESTORED_STORAGE_NAME\",\"quarantinedAtUtc\":\"2020-01-01T00:00:00Z\"}" \
    > "$PHOTO_ROOT/quarantine/$QUARANTINE_TOKEN/descriptor.json"

echo "Inserting fixture metadata into RecipePhotos..."

sqlite3 "$DATABASE_PATH" <<SQL
.bail on

PRAGMA foreign_keys = ON;

BEGIN IMMEDIATE;

INSERT INTO RecipePhotos (
    RecipeId,
    StorageFileName,
    OriginalFileName,
    ContentType,
    FileSize,
    CreatedAtUtc,
    DisplayOrder,
    Origin
)
VALUES (
    $RECIPE_ID,
    '$RESTORED_STORAGE_NAME',
    'restore-test.jpg',
    'image/jpeg',
    4,
    '2020-01-01T00:00:00Z',
    9999,
    2
);

COMMIT;
SQL

PHOTO_ID="$(
    sqlite3 "$DATABASE_PATH" \
        "SELECT Id
         FROM RecipePhotos
         WHERE StorageFileName = '$RESTORED_STORAGE_NAME';"
)"

{
    echo "RecipeId=$RECIPE_ID"
    echo "PhotoId=$PHOTO_ID"
    echo "RestoredStorageName=$RESTORED_STORAGE_NAME"
    echo "OrphanStorageName=$ORPHAN_STORAGE_NAME"
    echo "OldStagingToken=$OLD_STAGING_TOKEN"
    echo "NewStagingToken=$NEW_STAGING_TOKEN"
    echo "InvalidStagingName=$INVALID_STAGING_NAME"
    echo "QuarantineToken=$QUARANTINE_TOKEN"
} > "$BACKUP_DIRECTORY/fixture-info.txt"

echo
echo "Live fixtures created successfully."
echo
echo "Backup:"
echo "  $BACKUP_DIRECTORY"
echo
echo "Inserted RecipePhotos id:"
echo "  $PHOTO_ID"
echo
echo "Start the application normally and call:"
echo "  POST http://localhost:5080/api/photos/reconcile"
