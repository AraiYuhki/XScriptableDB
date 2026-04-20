# XScriptableDB Packaging Script
# This script packages the Unity package into a .tgz file for distribution.

$PackagePath = "Packages/jp.xeon.x-scriptable-db"
$DistPath = "Dist"

# Ensure Dist directory exists
if (-not (Test-Path $DistPath)) {
    New-Item -ItemType Directory -Path $DistPath | Out-Null
}

# Clear previous builds
Remove-Item -Path "$DistPath/*.tgz" -ErrorAction SilentlyContinue

Write-Host "Packaging XScriptableDB..." -ForegroundColor Cyan

# Navigate to package directory
Push-Location $PackagePath
try {
    $PackageFile = npm pack
    if ($LASTEXITCODE -ne 0) {
        Write-Error "npm pack failed."
        exit $LASTEXITCODE
    }
} finally {
    Pop-Location
}

# Move the package to Dist folder
$SourceFile = Join-Path $PackagePath $PackageFile
Move-Item -Path $SourceFile -Destination $DistPath

# Copy documentation to Dist folder
Write-Host "Copying LICENSE and README.md..." -ForegroundColor Cyan
Copy-Item -Path "DOCS_BOOTH_INSTALL.md" -Destination $DistPath -Force
Copy-Item -Path "LICENSE" -Destination $DistPath -Force
Copy-Item -Path "README.md" -Destination $DistPath -Force

# Create ZIP file
Write-Host "Creating ZIP archive..." -ForegroundColor Cyan
$ZipName = "XScriptableDB_v1.1.3.zip"
if (Test-Path $ZipName) { Remove-Item $ZipName }

# Compress the contents of the Dist folder
Compress-Archive -Path "$DistPath\*" -DestinationPath $ZipName -Force

Write-Host "Successfully packaged: $ZipName" -ForegroundColor Green
Write-Host "The ZIP file is ready for distribution at the project root." -ForegroundColor Yellow

# Open the root folder in explorer
explorer.exe .
