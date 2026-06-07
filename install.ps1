param(
    [string] $ConfigPath,

    [switch] $DryRun
)

$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$modulePath = Join-Path $scriptRoot 'src\AITerminalLauncher.psm1'
$launcherPath = Join-Path $scriptRoot 'launcher.ps1'

Import-Module $modulePath -Force

$resolvedConfigPath = Resolve-AITLConfigPath -ExplicitPath $ConfigPath
$config = Get-AITLConfig -ConfigPath $resolvedConfigPath
$tools = @($config.tools) | Where-Object { $_.enabled -and $_.showInContextMenu }

$operations = @()
foreach ($tool in $tools) {
    $label = Format-AITLText -Key 'ContextMenuLabel' -Args @($tool.displayName)
    $operations += New-AITLInstallRegistryOperation -LauncherPath $launcherPath -ToolId $tool.id -Label $label -ConfigPath $resolvedConfigPath
}

if ($DryRun) {
    $operations | ConvertTo-Json -Depth 5
    exit 0
}

foreach ($operation in $operations) {
    New-Item -Path $operation.KeyPath -Force | Out-Null
    Set-Item -LiteralPath $operation.KeyPath -Value $operation.Label

    $commandKeyPath = Join-Path $operation.KeyPath 'command'
    New-Item -Path $commandKeyPath -Force | Out-Null
    Set-Item -LiteralPath $commandKeyPath -Value $operation.Command
}

Write-Output (Get-AITLText -Key 'ContextMenuInstalled')
