#!/bin/bash

# XScriptableDB Packaging Script
# This script packages the Unity package into a .tgz file for distribution.

PACKAGE_PATH="Packages/jp.xeon.x-scriptable-db"
DIST_PATH="Dist"

# Ensure Dist directory exists
mkdir -p "$DIST_PATH"

# Clear previous builds
rm -f "$DIST_PATH"/*

echo -e "\033[0;36mPackaging XScriptableDB...\033[0m"

# Navigate to package directory
cd "$PACKAGE_PATH" || exit

# Use npm pack to create the tarball
PACKAGE_FILE=$(npm pack 2>/dev/null)

if [ $? -ne 0 ]; then
    echo -e "\033[0;31mnpm pack failed.\033[0m"
    exit 1
fi

# Move the package to Dist folder
mv "$PACKAGE_FILE" "../../$DIST_PATH/"

# Return to root
cd ../../

# Copy documentation to Dist folder
echo "Copying LICENSE and README.md..."
cp -f "DOCS_BOOTH_INSTALL.md" "$DIST_PATH/"
cp -f "LICENSE" "$DIST_PATH/"
cp -f "README.md" "$DIST_PATH/"

# Create ZIP file
# We zip the contents of the Dist folder
echo "Creating ZIP archive..."
ZIP_NAME="XScriptableDB_v1.1.3.zip"
rm -f "$ZIP_NAME"

# Change directory to Dist to zip contents without the 'Dist' prefix
cd "$DIST_PATH" || exit
zip -r "../$ZIP_NAME" ./*
cd ..

echo -e "\033[0;32mSuccessfully packaged: $ZIP_NAME\033[0m"
echo -e "\033[0;33mThe ZIP file is ready for distribution at the project root.\033[0m"
