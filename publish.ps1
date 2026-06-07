<#
.SYNOPSIS
    Publishes AI Terminal Launcher as a self-contained single-file executable.

.DESCRIPTION
    Produces a distributable build under dist\AITerminalLauncher that runs on a
    target machine WITHOUT a preinstalled .NET runtime. The managed assemblies
    and runtime are bundled into a single AITerminalLauncher.App.exe; the
    PowerShell launch backend (launcher.ps1 / install.ps1 / uninstall.ps1 and
    src\AITerminalLauncher.psm1) is copied alongside the exe because those
    scripts are executed by powershell.exe and must exist as real files.

.PARAMETER Runtime
    Target runtime identifier. Defaults to win-x64.

.PARAMETER Configuration
    Build configuration. Defaults to Release.

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File publish.ps1
#>

param(
    [string] $Runtime = 'win-x64',
    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

$repoRoot = $PSScriptRoot
$projectPath = Join-Path $repoRoot 'src\AITerminalLauncher.App\AITerminalLauncher.App.csproj'
$publishDir = Join-Path $repoRoot 'dist\AITerminalLauncher'

Write-Output "AI Terminal Launcher publish"
Write-Output "  runtime       : $Runtime"
Write-Output "  configuration : $Configuration"
Write-Output "  output        : $publishDir"
Write-Output ''

# Start from a clean output folder so stale framework-dependent artifacts from a
# previous publish never leak into the self-contained package.
if (Test-Path -LiteralPath $publishDir) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}

& dotnet publish $projectPath `
    -c $Configuration `
    -r $Runtime `
    --self-contained `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $publishDir

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

# Distribution package does not need debug symbols.
Get-ChildItem -LiteralPath $publishDir -Filter *.pdb -Recurse -ErrorAction SilentlyContinue |
    Remove-Item -Force -ErrorAction SilentlyContinue

# Verify the package contains everything the runtime needs: the single exe plus
# the PowerShell backend in the layout ScriptPathResolver and launcher.ps1 expect.
$required = @(
    'AITerminalLauncher.App.exe',
    'launcher.ps1',
    'install.ps1',
    'uninstall.ps1',
    'src\AITerminalLauncher.psm1'
)

foreach ($relativePath in $required) {
    if (-not (Test-Path -LiteralPath (Join-Path $publishDir $relativePath))) {
        throw "发布产物缺少必需文件: $relativePath"
    }
}

$exePath = Join-Path $publishDir 'AITerminalLauncher.App.exe'
$exeSizeMb = [Math]::Round((Get-Item -LiteralPath $exePath).Length / 1MB, 1)

Write-Output ''
Write-Output "发布完成。单文件可执行: AITerminalLauncher.App.exe ($exeSizeMb MB)"
Write-Output '产物布局:'

Push-Location $publishDir
try {
    Get-ChildItem -Recurse -File | ForEach-Object {
        $relativePath = (Resolve-Path -LiteralPath $_.FullName -Relative).TrimStart('.', '\')
        $sizeKb = [Math]::Round($_.Length / 1KB, 0)
        Write-Output ("  {0,-36} {1,8} KB" -f $relativePath, $sizeKb)
    }
}
finally {
    Pop-Location
}
