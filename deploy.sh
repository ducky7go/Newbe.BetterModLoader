#!/bin/bash
# Deployment script for Newbe.BetterModLoader

# PUBLISHING IS DISABLED. BML will not be released on the Steam Workshop again
# (see "Publishing: NOT ALLOWED" in AGENTS.md). This guard runs before anything
# else, so nothing is bumped, built or pushed. Do not remove it to get around the
# freeze; only the owner may lift it.
echo "Error: Newbe.BetterModLoader is frozen and is no longer published to Steam." >&2
echo "       See 'Publishing: NOT ALLOWED' in AGENTS.md. Nothing was changed." >&2
exit 1

set -e

# Load environment variables from .env file
if [ -f .env ]; then
    set -a
    source .env
    set +a
else
    echo "Warning: .env file not found"
fi

echo "==================================="
echo "BML Deployment Script"
echo "==================================="
echo ""

# Define info.ini path
INFO_INI="NewMods/Newbe.BetterModLoader/assets/info.ini"

# Step 1: Bump version
echo "Step 1: Bumping version..."
CURRENT_VERSION=$(grep "^version =" "$INFO_INI" | cut -d'=' -f2 | tr -d ' ')
echo "Current version: $CURRENT_VERSION"

# Split version into components
IFS='.' read -r MAJOR MINOR PATCH <<< "$CURRENT_VERSION"

# Bump patch version
NEW_PATCH=$((PATCH + 1))
NEW_VERSION="$MAJOR.$MINOR.$NEW_PATCH"

echo "New version: $NEW_VERSION"

# Update version in info.ini
sed -i "s/^version = $CURRENT_VERSION/version = $NEW_VERSION/" "$INFO_INI"
echo "Version bumped!"
echo ""

# Step 2: Build the project
echo "Step 2: Building project..."
dotnet build -c Release
echo "Build completed!"
echo ""

# Step 3: Publish to Steam Workshop using ducky
echo "Step 3: Publishing to Steam Workshop..."

# Check if MOD_PATH is set
if [ -z "$MOD_PATH" ]; then
    echo "Error: MOD_PATH not set in .env file"
    exit 1
fi

# Check if mod directory exists
if [ ! -d "$MOD_PATH" ]; then
    echo "Error: Mod directory not found: $MOD_PATH"
    exit 1
fi

echo "Using mod path: $MOD_PATH"

# Push to Steam Workshop
ducky steam push "$MOD_PATH" --update-description

echo ""
echo "==================================="
echo "Deployment completed successfully!"
echo "Version: $CURRENT_VERSION -> $NEW_VERSION"
echo "==================================="
