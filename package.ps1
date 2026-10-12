$stagingDir = "$PSScriptRoot\staging"
$distDir = "$PSScriptRoot\dist"
$manifest = Get-Content "$PSScriptRoot\manifest.json" -Raw | ConvertFrom-Json
$version = $manifest.version_number
$outputZip = "$distDir\pmadd-Unlimited_Upgrades-$version.zip"
$dllSource = "$PSScriptRoot\src\bin\AllUpgradesMod.dll"

Write-Host "Building project in Release mode..."
dotnet build "$PSScriptRoot\src\AllUpgradesMod.csproj" -c Release

if (-not (Test-Path $dllSource)) {
    Write-Error "Build failed or DLL not found at $dllSource"
    exit 1
}

Write-Host "Creating staging directory..."
if (Test-Path $stagingDir) { Remove-Item -Path $stagingDir -Recurse -Force }
New-Item -ItemType Directory -Path $stagingDir -Force | Out-Null

if (-not (Test-Path $distDir)) {
    New-Item -ItemType Directory -Path $distDir -Force | Out-Null
}

Write-Host "Copying files to staging..."
Copy-Item -Path $dllSource -Destination $stagingDir -Force
Copy-Item -Path "$PSScriptRoot\manifest.json" -Destination $stagingDir -Force
Copy-Item -Path "$PSScriptRoot\README.md" -Destination $stagingDir -Force
Copy-Item -Path "$PSScriptRoot\icon.png" -Destination $stagingDir -Force

Write-Host "Creating Thunderstore zip package: $outputZip..."
if (Test-Path $outputZip) { Remove-Item -Path $outputZip -Force }
Compress-Archive -Path "$stagingDir\*" -DestinationPath $outputZip -Force

Write-Host "Cleaning up staging directory..."
Remove-Item -Path $stagingDir -Recurse -Force

Write-Host "Package created successfully in dist folder: $outputZip"
