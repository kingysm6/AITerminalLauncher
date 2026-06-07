param(
    [Parameter(Mandatory = $true)]
    [string] $Tool,

    [Parameter(Mandatory = $true)]
    [string] $Path,

    [string] $ConfigPath,

    [switch] $DryRun
)

$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$modulePath = Join-Path $scriptRoot 'src\AITerminalLauncher.psm1'

Import-Module $modulePath -Force

$resolvedConfigPath = Resolve-AITLConfigPath -ExplicitPath $ConfigPath
$config = Get-AITLConfig -ConfigPath $resolvedConfigPath
$targetPath = Resolve-AITLTargetPath -Path $Path
$toolConfig = Get-AITLToolConfig -Config $config -ToolId $Tool
$launchCommand = New-AITLLaunchCommand -Terminal $config.terminal.preferred -FallbackTerminal $config.terminal.fallback -TargetPath $targetPath -ToolConfig $toolConfig

if ($DryRun) {
    $launchCommand | ConvertTo-Json -Depth 5
    exit 0
}

$startProcessParameters = @{
    FilePath = $launchCommand.FilePath
    ArgumentList = $launchCommand.ArgumentList
}

if ($null -ne $launchCommand.WorkingDirectory) {
    $startProcessParameters.WorkingDirectory = $launchCommand.WorkingDirectory
}

Start-Process @startProcessParameters


