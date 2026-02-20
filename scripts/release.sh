#!/bin/bash

# OverSync Organized Release Script
# Usage: ./scripts/release.sh [major|minor|patch]

set -e

if [ -z "$1" ]; then
    echo "Usage: ./scripts/release.sh [major|minor|patch]"
    exit 1
fi

LEVEL=$1

# 1. Bump versions using npm (this updates package.json)
echo "Bumping version level: $LEVEL..."
NEW_VERSION=$(npm version $LEVEL --no-git-tag-version | sed 's/v//')

echo "New version: $NEW_VERSION"

# 2. Update tauri.conf.json to match
echo "Syncing tauri.conf.json..."
sed -i "s/\"version\": \".*\"/\"version\": \"$NEW_VERSION\"/" src-tauri/tauri.conf.json

# 3. Add changes to git
git add package.json package-lock.json src-tauri/tauri.conf.json

# 4. Commit with standardized message
COMMIT_MSG="chore(release): v$NEW_VERSION"
git commit -m "$COMMIT_MSG"

# 5. Create the tag
git tag -a "v$NEW_VERSION" -m "$COMMIT_MSG"

echo "------------------------------------------------"
echo "Release v$NEW_VERSION prepared locally!"
echo "Run 'git push origin main --follow-tags' to trigger the CI/CD build."
echo "------------------------------------------------"
