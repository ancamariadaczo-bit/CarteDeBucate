#!/usr/bin/env bash

set -euo pipefail

if [[ $# -lt 1 || $# -gt 2 ]]; then
    echo "Usage: $0 <recipe-id> [application-base-url]"
    echo "Example: $0 190 http://localhost:5080"
    exit 1
fi

RECIPE_ID="$1"
APPLICATION_BASE_URL="${2:-http://localhost:5080}"
APPLICATION_BASE_URL="${APPLICATION_BASE_URL%/}"

if [[ ! "$RECIPE_ID" =~ ^[1-9][0-9]*$ ]]; then
    echo "Recipe id must be a positive integer."
    exit 1
fi

if [[ ! "$APPLICATION_BASE_URL" =~ ^https?://[^[:space:]]+$ ]]; then
    echo "Application base URL must start with http:// or https://."
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

if ! command -v curl >/dev/null 2>&1; then
    echo "curl is not available."
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

PHOTO_RECORD="$(
    sqlite3 -separator '|' "$DATABASE_PATH" \
        "SELECT Id, RecipeId
         FROM RecipePhotos
         WHERE StorageFileName = '$RESTORED_STORAGE_NAME';"
)"

PHOTO_ID=""

if [[ -n "$PHOTO_RECORD" ]]; then
    if [[ "$PHOTO_RECORD" == *$'\n'* ]]; then
        echo "More than one fixture metadata row was found. Aborting."
        exit 1
    fi

    IFS='|' read -r PHOTO_ID METADATA_RECIPE_ID <<< "$PHOTO_RECORD"

    if [[ "$METADATA_RECIPE_ID" != "$RECIPE_ID" ]]; then
        echo "Fixture metadata belongs to recipe $METADATA_RECIPE_ID, not $RECIPE_ID."
        echo "Aborting without changing data."
        exit 1
    fi
fi

echo
echo "WARNING: this script will remove only the reconciliation fixtures"
echo "from the active application data."
echo
echo "Database:"
echo "  $DATABASE_PATH"
echo
echo "Photo directory:"
echo "  $PHOTO_ROOT"
echo

if [[ -n "$PHOTO_ID" ]]; then
    echo "The restored fixture photo will be deleted through the application:"
    echo "  DELETE $APPLICATION_BASE_URL/api/photos/$PHOTO_ID"
else
    echo "No restored fixture metadata was found in RecipePhotos."
fi

echo
read -r -p "Type CLEANUP-LIVE-FIXTURES to continue: " CONFIRMATION

if [[ "$CONFIRMATION" != "CLEANUP-LIVE-FIXTURES" ]]; then
    echo "Cancelled."
    exit 1
fi

if [[ -n "$PHOTO_ID" ]]; then
    echo
    echo "Deleting restored fixture photo through RecipePhotoService..."

    if ! DELETE_RESPONSE="$(
        curl \
            --silent \
            --show-error \
            --request DELETE \
            --write-out $'\n%{http_code}' \
            "$APPLICATION_BASE_URL/api/photos/$PHOTO_ID"
    )"; then
        echo "The application could not be reached."
        echo "Aborting filesystem cleanup."
        exit 1
    fi

    DELETE_HTTP_STATUS="${DELETE_RESPONSE##*$'\n'}"
    DELETE_BODY="${DELETE_RESPONSE%$'\n'*}"

    if [[ -n "$DELETE_BODY" ]]; then
        printf '%s\n' "$DELETE_BODY"
    fi

    if [[ ! "$DELETE_HTTP_STATUS" =~ ^2[0-9][0-9]$ ]]; then
        echo "DELETE returned HTTP $DELETE_HTTP_STATUS instead of a 2xx response."

        if [[ "$DELETE_HTTP_STATUS" =~ ^3[0-9][0-9]$ ]]; then
            echo "The request was redirected."
            echo "Run the script with the HTTPS application URL, for example:"
            echo "  $0 $RECIPE_ID https://localhost:7080"
        fi

        echo "Aborting filesystem cleanup."
        exit 1
    fi

    echo

    REMAINING_METADATA="$(
        sqlite3 "$DATABASE_PATH" \
            "SELECT COUNT(*)
             FROM RecipePhotos
             WHERE Id = $PHOTO_ID
               AND StorageFileName = '$RESTORED_STORAGE_NAME';"
    )"

    if [[ "$REMAINING_METADATA" != "0" ]]; then
        echo "The application returned successfully, but fixture metadata still exists."
        echo "Aborting filesystem cleanup."
        exit 1
    fi
fi

echo "Removing remaining fixture files..."

rm -f -- \
    "$PHOTO_ROOT/staging/$OLD_STAGING_TOKEN.upload" \
    "$PHOTO_ROOT/staging/$NEW_STAGING_TOKEN.upload" \
    "$PHOTO_ROOT/staging/$INVALID_STAGING_NAME" \
    "$PHOTO_ROOT/$ORPHAN_STORAGE_NAME" \
    "$PHOTO_ROOT/$RESTORED_STORAGE_NAME"

rm -rf -- "$PHOTO_ROOT/quarantine/$QUARANTINE_TOKEN"

REMAINING_FIXTURE_METADATA="$(
    sqlite3 "$DATABASE_PATH" \
        "SELECT COUNT(*)
         FROM RecipePhotos
         WHERE StorageFileName = '$RESTORED_STORAGE_NAME';"
)"

if [[ "$REMAINING_FIXTURE_METADATA" != "0" ]]; then
    echo "Fixture files were removed, but fixture metadata still exists."
    exit 1
fi

echo
echo "Reconciliation fixtures were cleaned successfully."
echo "Existing application photos and reconciliation backups were not removed."
