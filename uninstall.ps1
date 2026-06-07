param(
    [string] $ConfigPath,

    [switch] $DryRun
)

$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$modulePath = Join-Path $scriptRoot 'src\AITerminalLauncher.psm1'

Import-Module $modulePath -Force

$toolIds = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)

if (-not [string]::IsNullOrWhiteSpace($ConfigPath) -or -not [string]::IsNullOrWhiteSpace($env:AITL_CONFIG_PATH)) {
    try {
        $resolvedConfigPath = Resolve-AITLConfigPath -ExplicitPath $ConfigPath
        $config = Get-AITLConfig -ConfigPath $resolvedConfigPath
        foreach ($tool in @($config.tools) | Where-Object { $_.showInContextMenu }) {
            $null = $toolIds.Add([string] $tool.id)
        }
    }
    catch {
    }
}
else {
    try {
        $resolvedConfigPath = Resolve-AITLConfigPath -ExplicitPath $ConfigPath
        $config = Get-AITLConfig -ConfigPath $resolvedConfigPath
        foreach ($tool in @($config.tools) | Where-Object { $_.showInContextMenu }) {
            $null = $toolIds.Add([string] $tool.id)
        }
    }
    catch {
    }
}

foreach ($toolId in Get-AITLOwnedRegistryToolIds) {
    $null = $toolIds.Add([string] $toolId)
}

$operations = @()
foreach ($toolId in @($toolIds | Sort-Object)) {
    $operations += New-AITLUninstallRegistryOperation -ToolId $toolId
}

if ($DryRun) {
    $operations | ConvertTo-Json -Depth 5
    exit 0
}

foreach ($operation in $operations) {
    if (Test-Path -LiteralPath $operation.KeyPath) {
        Remove-Item -LiteralPath $operation.KeyPath -Recurse -Force
    }
}

Write-Output (Get-AITLText -Key 'ContextMenuRemoved')
