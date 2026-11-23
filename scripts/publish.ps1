
$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot

$project = Join-Path $root "src\SearchableTaskSwitcher\Searchable Task Switcher.csproj"
$output = Join-Path $root "artifacts\publish"

if (-not (Test-Path -LiteralPath $project -PathType Leaf)) {
    throw "Project file not found at $project"
}

Write-Host "Publishing Searchable Task Switcher..."

dotnet publish $project `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=true `
    -o $output

if ($LASTEXITCODE -ne 0) {
    throw "Publishing failed with exit code $LASTEXITCODE"
}

$exe = Join-Path $output "SearchableTaskSwitcher.exe"

if (-not (Test-Path -LiteralPath $exe -PathType Leaf)) {
    throw "Publishing failed: executable not found at $exe"
}

Write-Host "Publish completed successfully."
Write-Host "Output: $output"
