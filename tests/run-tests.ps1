$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$modulePath = Join-Path $repoRoot 'src\AITerminalLauncher.psm1'
$configPath = Join-Path $repoRoot 'config.json'

function Get-Cn {
    param([string] $Literal)
    return (ConvertFrom-Json ('"' + $Literal + '"'))
}

function Assert-Equal {
    param(
        $Expected,
        $Actual,
        [Parameter(Mandatory = $true)] [string] $Message
    )

    if ($Expected -ne $Actual) {
        throw "Assertion failed: $Message. Expected '$Expected', got '$Actual'."
    }
}

function Assert-Throws {
    param(
        [Parameter(Mandatory = $true)] [scriptblock] $ScriptBlock,
        [Parameter(Mandatory = $true)] [string] $Message
    )

    $threw = $false
    try {
        & $ScriptBlock
    }
    catch {
        $threw = $true
    }

    if (-not $threw) {
        throw "Assertion failed: $Message. Expected command to throw."
    }
}

function Assert-ThrowsContains {
    param(
        [Parameter(Mandatory = $true)] [scriptblock] $ScriptBlock,
        [Parameter(Mandatory = $true)] [string] $ExpectedText,
        [Parameter(Mandatory = $true)] [string] $Message
    )

    try {
        & $ScriptBlock
    }
    catch {
        if ($_.Exception.Message -notlike "*$ExpectedText*") {
            throw "Assertion failed: $Message. Expected exception containing '$ExpectedText', got '$($_.Exception.Message)'."
        }

        return
    }

    throw "Assertion failed: $Message. Expected command to throw."
}

function Assert-Contains {
    param(
        [Parameter(Mandatory = $true)] $Collection,
        [Parameter(Mandatory = $true)] $Expected,
        [Parameter(Mandatory = $true)] [string] $Message
    )

    if ($Collection -notcontains $Expected) {
        throw "Assertion failed: $Message. Expected collection to contain '$Expected'."
    }
}

function Assert-NotContains {
    param(
        [Parameter(Mandatory = $true)] $Collection,
        [Parameter(Mandatory = $true)] $Unexpected,
        [Parameter(Mandatory = $true)] [string] $Message
    )

    if ($Collection -contains $Unexpected) {
        throw "Assertion failed: $Message. Expected collection not to contain '$Unexpected'."
    }
}

function ConvertTo-Array {
    param($Value)

    if ($null -eq $Value) {
        return @()
    }

    if ($Value -is [System.Array]) {
        return $Value
    }

    return @($Value)
}

$toolIdInvalidMessage = Get-Cn '\u5de5\u5177 ID ''bad_id'' \u5305\u542b\u65e0\u6548\u5b57\u7b26\u3002'
$contextMenuLabelPrefix = Get-Cn '\u4f7f\u7528 '
$contextMenuLabelSuffix = Get-Cn ' \u6253\u5f00'
$contextMenuInstalled = Get-Cn '\u5df2\u5b89\u88c5 AI Terminal Launcher \u53f3\u952e\u83dc\u5355\u9879\u3002'
$contextMenuRemoved = Get-Cn '\u5df2\u79fb\u9664 AI Terminal Launcher \u53f3\u952e\u83dc\u5355\u9879\u3002'
$settingsTitle = Get-Cn 'AI Terminal Launcher \u8bbe\u7f6e'
$settingsAddTool = Get-Cn '\u6dfb\u52a0\u5de5\u5177'
$settingsSave = Get-Cn '\u4fdd\u5b58'
$toolEditorTitle = Get-Cn '\u6dfb\u52a0\u5de5\u5177'
$toolEditorTray = Get-Cn '\u5728\u6258\u76d8\u83dc\u5355\u4e2d\u663e\u793a'
$saveText = Get-Cn '\u4fdd\u5b58'
$traySettings = Get-Cn '\u8bbe\u7f6e'
$trayLaunchAtLogin = Get-Cn '\u5f00\u673a\u542f\u52a8'

Import-Module $modulePath -Force

$config = Get-AITLConfig -ConfigPath $configPath

Assert-Equal 1 $config.version 'config version'
Assert-Equal 'wt' $config.terminal.preferred 'preferred terminal defaults to wt'
Assert-Equal 'powershell' $config.terminal.fallback 'fallback terminal defaults to powershell'
Assert-Equal 3 @($config.tools).Count 'three default tools exist'

$absoluteConfigPath = (Resolve-Path -LiteralPath $configPath).ProviderPath

$missingConfigPath = Join-Path $repoRoot 'missing-config.json'
Assert-Throws { Get-AITLConfig -ConfigPath $missingConfigPath } 'missing config throws'

$badConfigPath = Join-Path $env:TEMP 'aitl-bad-config.json'
Set-Content -LiteralPath $badConfigPath -Value '{ invalid json' -Encoding UTF8
try {
    Assert-Throws { Get-AITLConfig -ConfigPath $badConfigPath } 'malformed config throws'
}
finally {
    Remove-Item -LiteralPath $badConfigPath -Force -ErrorAction SilentlyContinue
}

$resolvedConfigPath = Resolve-AITLConfigPath -ExplicitPath $configPath
Assert-Equal $absoluteConfigPath $resolvedConfigPath 'explicit config path wins and resolves to full provider path'

$relativeConfigPath = '.\config.json'
$resolvedRelativeConfigPath = Resolve-AITLConfigPath -ExplicitPath $relativeConfigPath
Assert-Equal $absoluteConfigPath $resolvedRelativeConfigPath 'relative explicit config path resolves to full provider path'

$previousConfigPathEnv = $env:AITL_CONFIG_PATH
try {
    $env:AITL_CONFIG_PATH = $configPath
    $resolvedEnvConfigPath = Resolve-AITLConfigPath
    Assert-Equal $absoluteConfigPath $resolvedEnvConfigPath 'AITL_CONFIG_PATH is used when explicit path is absent'

    Remove-Item Env:AITL_CONFIG_PATH -ErrorAction SilentlyContinue
    $resolvedDefaultConfigPath = Resolve-AITLConfigPath
    Assert-Equal $absoluteConfigPath $resolvedDefaultConfigPath 'default config path falls back to repo config when env var is absent'
}
finally {
    if ($null -eq $previousConfigPathEnv) {
        Remove-Item Env:AITL_CONFIG_PATH -ErrorAction SilentlyContinue
    }
    else {
        $env:AITL_CONFIG_PATH = $previousConfigPathEnv
    }
}

$resolvedCurrentPath = Resolve-AITLTargetPath -Path $repoRoot
Assert-Equal (Resolve-Path -LiteralPath $repoRoot).ProviderPath $resolvedCurrentPath 'target path resolves to full provider path'

Assert-Throws { Resolve-AITLTargetPath -Path (Join-Path $repoRoot 'definitely-missing-folder') } 'missing target path throws'

$codexTool = Get-AITLToolConfig -Config $config -ToolId 'codex'
Assert-Equal 'codex' $codexTool.command 'codex tool config resolves'

Assert-Throws { Get-AITLToolConfig -Config $config -ToolId 'missing' } 'unknown tool throws'

$invalidToolConfig = Get-AITLConfig -ConfigPath $configPath
$invalidToolConfig.tools[0].id = 'bad_id'
Assert-ThrowsContains { Get-AITLToolConfig -Config $invalidToolConfig -ToolId 'bad_id' } $toolIdInvalidMessage 'invalid tool id throws cleanly'

$wtCommand = New-AITLLaunchCommand -Terminal $config.terminal.preferred -FallbackTerminal $config.terminal.fallback -TargetPath $repoRoot -ToolConfig $codexTool
Assert-Equal 'wt.exe' $wtCommand.FilePath 'Windows Terminal executable is wt.exe'
Assert-Contains $wtCommand.ArgumentList '-d' 'Windows Terminal args include directory flag'
Assert-Contains $wtCommand.ArgumentList $repoRoot 'target path is preserved'
Assert-Contains $wtCommand.ArgumentList 'powershell.exe' 'Windows Terminal opens PowerShell tab'
Assert-Contains $wtCommand.ArgumentList '-NoExit' 'Windows Terminal keeps PowerShell open'
Assert-Contains $wtCommand.ArgumentList '-Command' 'Windows Terminal passes command'
Assert-Contains $wtCommand.ArgumentList 'codex' 'tool command is present'
Assert-Equal $null $wtCommand.WorkingDirectory 'Windows Terminal does not need Start-Process working directory'

$psCommand = New-AITLLaunchCommand -Terminal $config.terminal.fallback -FallbackTerminal $config.terminal.fallback -TargetPath $repoRoot -ToolConfig $codexTool
Assert-Equal 'powershell.exe' $psCommand.FilePath 'PowerShell fallback executable is powershell.exe'
Assert-Equal $repoRoot $psCommand.WorkingDirectory 'PowerShell fallback uses Start-Process working directory'
Assert-Contains $psCommand.ArgumentList '-NoExit' 'PowerShell fallback keeps window open'
Assert-Contains $psCommand.ArgumentList '-Command' 'PowerShell fallback passes command'
Assert-Contains $psCommand.ArgumentList 'codex' 'PowerShell fallback command includes tool command'
Assert-NotContains $psCommand.ArgumentList '-WorkingDirectory' 'PowerShell fallback does not pass unsupported powershell.exe -WorkingDirectory'

$spacePath = Join-Path $env:TEMP 'aitl path with spaces'
$chinesePath = Join-Path $env:TEMP 'aitlChinesePath'
$spaceCommand = New-AITLLaunchCommand -Terminal $config.terminal.preferred -FallbackTerminal $config.terminal.fallback -TargetPath $spacePath -ToolConfig $codexTool
$chineseCommand = New-AITLLaunchCommand -Terminal $config.terminal.preferred -FallbackTerminal $config.terminal.fallback -TargetPath $chinesePath -ToolConfig $codexTool
Assert-Contains $spaceCommand.ArgumentList $spacePath 'path with spaces is preserved as one argument'
Assert-Contains $chineseCommand.ArgumentList $chinesePath 'Chinese path is preserved as one argument'

$quotedToolConfig = [pscustomobject]@{
    id = 'quoted-tool'
    displayName = 'Quoted Tool'
    command = 'C:\Program Files\Test Tool\tool.exe'
    args = @('--label', 'hello world', '--flag')
    enabled = $true
}
$quotedPsCommand = New-AITLLaunchCommand -Terminal 'powershell' -FallbackTerminal 'powershell' -TargetPath $repoRoot -ToolConfig $quotedToolConfig
$quotedCommandText = $quotedPsCommand.ArgumentList[2]
if ($quotedCommandText -notmatch [regex]::Escape('"C:\Program Files\Test Tool\tool.exe"')) {
    throw "Assertion failed: tool command with spaces is quoted inside PowerShell command text. Actual: $quotedCommandText"
}
if ($quotedCommandText -notmatch [regex]::Escape('"hello world"')) {
    throw "Assertion failed: tool argument with spaces is quoted inside PowerShell command text. Actual: $quotedCommandText"
}

$launcherPath = Join-Path $repoRoot 'launcher.ps1'
$launcherDryRunPath = Join-Path $env:TEMP 'aitl-launcher-dry-run'
New-Item -ItemType Directory -Path $launcherDryRunPath -Force | Out-Null
$launcherOverrideConfigPath = Join-Path $env:TEMP 'aitl-launcher-config.json'
$launcherOverrideConfig = [ordered]@{
    version = 1
    terminal = @{
        preferred = 'powershell'
        fallback = 'wt'
    }
    startup = @{
        launchAtLogin = $false
        startMinimizedToTray = $true
    }
    fallbackBehavior = @{
        mode = 'folderPicker'
    }
    tools = @(
        [ordered]@{
            id = 'codex'
            displayName = 'Codex'
            command = 'codex'
            args = @()
            enabled = $true
            showInContextMenu = $true
            showInTrayMenu = $true
            hotkey = @{
                enabled = $true
                modifiers = @('Control', 'Alt')
                key = 'C'
            }
        }
    )
}
Set-Content -LiteralPath $launcherOverrideConfigPath -Value ($launcherOverrideConfig | ConvertTo-Json -Depth 8) -Encoding UTF8
try {
    $launcherOutput = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $launcherPath -Tool codex -Path $launcherDryRunPath -ConfigPath $launcherOverrideConfigPath -DryRun 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Assertion failed: launcher dry-run exits 0. Output: $launcherOutput"
    }

    $launcherText = $launcherOutput -join [Environment]::NewLine
    $launcherData = $launcherText | ConvertFrom-Json
    Assert-Equal 'powershell.exe' $launcherData.FilePath 'launcher dry-run includes structured launch executable'
    Assert-Equal $launcherDryRunPath $launcherData.WorkingDirectory 'launcher dry-run includes structured working directory'
    Assert-Contains $launcherData.ArgumentList '-NoExit' 'launcher dry-run includes PowerShell no-exit flag'
    Assert-Contains $launcherData.ArgumentList '-Command' 'launcher dry-run includes command flag'
    Assert-Contains $launcherData.ArgumentList 'codex' 'launcher dry-run includes codex'
    Assert-NotContains $launcherData.ArgumentList '-WorkingDirectory' 'launcher dry-run does not inject unsupported powershell working-directory argument'
}
finally {
    Remove-Item -LiteralPath $launcherOverrideConfigPath -Force -ErrorAction SilentlyContinue
}

$contextMenuToolIds = @(
    @($config.tools) |
        Where-Object { $_.enabled -and $_.showInContextMenu } |
        ForEach-Object { $_.id }
)

$codexContextLabel = $contextMenuLabelPrefix + 'Codex' + $contextMenuLabelSuffix
$installOperations = New-AITLInstallRegistryOperation -LauncherPath $launcherPath -ToolId 'codex' -Label $codexContextLabel -ConfigPath $absoluteConfigPath
Assert-Equal 2 $installOperations.Count 'codex install creates folder and background operations'
Assert-Contains @($installOperations.KeyPath) 'HKCU:\Software\Classes\Directory\shell\AITerminalLauncher_codex' 'codex folder key is deterministic'
Assert-Contains @($installOperations.KeyPath) 'HKCU:\Software\Classes\Directory\Background\shell\AITerminalLauncher_codex' 'codex background key is deterministic'
foreach ($operation in $installOperations) {
    Assert-Equal 'SetContextMenuEntry' $operation.Action 'install operation action is explicit'
    Assert-Equal $codexContextLabel $operation.Label 'install operation label is configured'
    if ($operation.Command -notmatch 'powershell\.exe') { throw "Assertion failed: registry command invokes powershell.exe. Command: $($operation.Command)" }
    if ($operation.Command -notmatch [regex]::Escape($launcherPath)) { throw "Assertion failed: registry command includes launcher path. Command: $($operation.Command)" }
    if ($operation.Command -notmatch '-Tool "codex"') { throw "Assertion failed: registry command includes codex tool. Command: $($operation.Command)" }
    if ($operation.Command -notmatch [regex]::Escape("-ConfigPath `"$absoluteConfigPath`"")) { throw "Assertion failed: registry command includes absolute config path. Command: $($operation.Command)" }
    if ($operation.Command -notmatch '-Path "%V"') { throw "Assertion failed: registry command includes Explorer path placeholder. Command: $($operation.Command)" }
}

$uninstallOperations = New-AITLUninstallRegistryOperation -ToolId 'codex'
Assert-Equal 2 $uninstallOperations.Count 'codex uninstall creates folder and background removal operations'
Assert-Contains @($uninstallOperations.KeyPath) 'HKCU:\Software\Classes\Directory\shell\AITerminalLauncher_codex' 'codex uninstall includes folder key'
Assert-Contains @($uninstallOperations.KeyPath) 'HKCU:\Software\Classes\Directory\Background\shell\AITerminalLauncher_codex' 'codex uninstall includes background key'
foreach ($operation in $uninstallOperations) {
    Assert-Equal 'RemoveContextMenuEntry' $operation.Action 'uninstall operation action is explicit'
}

$installOutput = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'install.ps1') -ConfigPath '.\config.json' -DryRun 2>&1
if ($LASTEXITCODE -ne 0) { throw "Assertion failed: install dry-run exits 0. Output: $installOutput" }
$installText = $installOutput -join [Environment]::NewLine
$installData = ConvertTo-Array ($installText | ConvertFrom-Json)
Assert-Equal ($contextMenuToolIds.Count * 2) $installData.Count 'install dry-run creates directory and background operations per tool'
foreach ($toolId in $contextMenuToolIds) {
    Assert-Contains @($installData.KeyPath) "HKCU:\Software\Classes\Directory\shell\AITerminalLauncher_$toolId" "install dry-run includes folder key for $toolId"
    Assert-Contains @($installData.KeyPath) "HKCU:\Software\Classes\Directory\Background\shell\AITerminalLauncher_$toolId" "install dry-run includes background key for $toolId"
}
foreach ($operation in $installData) {
    if ($operation.Command -notmatch [regex]::Escape("-ConfigPath `"$absoluteConfigPath`"")) { throw "Assertion failed: install dry-run includes absolute config path. Command: $($operation.Command)" }
    if (-not $operation.Label.StartsWith($contextMenuLabelPrefix, [System.StringComparison]::Ordinal) -or -not $operation.Label.EndsWith($contextMenuLabelSuffix, [System.StringComparison]::Ordinal)) {
        throw "Assertion failed: install dry-run uses Chinese context-menu labels. Label: $($operation.Label)"
    }
}

$invalidToolConfigPath = Join-Path $env:TEMP 'aitl-invalid-tool-config.json'
Set-Content -LiteralPath $invalidToolConfigPath -Value ($invalidToolConfig | ConvertTo-Json -Depth 20) -Encoding UTF8
try {
    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $invalidInstallOutput = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'install.ps1') -ConfigPath $invalidToolConfigPath -DryRun 2>&1
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }

    if ($LASTEXITCODE -eq 0) { throw "Assertion failed: install dry-run rejects invalid tool id config. Output: $invalidInstallOutput" }
    $invalidInstallText = $invalidInstallOutput -join [Environment]::NewLine
    if ($invalidInstallText -match 'contains invalid characters') {
        throw "Assertion failed: install dry-run still reports invalid tool id in English. Output: $invalidInstallText"
    }
    if ($invalidInstallText -notlike "*$toolIdInvalidMessage*") {
        throw "Assertion failed: install dry-run does not surface a Chinese invalid tool id message. Output: $invalidInstallText"
    }
}
finally {
    Remove-Item -LiteralPath $invalidToolConfigPath -Force -ErrorAction SilentlyContinue
}

$staleDirectoryKeyPath = 'HKCU:\Software\Classes\Directory\shell\AITerminalLauncher_stale'
$staleBackgroundKeyPath = 'HKCU:\Software\Classes\Directory\Background\shell\AITerminalLauncher_stale'
$nonOwnedDirectoryKeyPath = 'HKCU:\Software\Classes\Directory\shell\NotAITerminalLauncher_sibling'
$nonOwnedBackgroundKeyPath = 'HKCU:\Software\Classes\Directory\Background\shell\NotAITerminalLauncher_sibling'

$expectedOwnedToolIds = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
foreach ($toolId in Get-AITLOwnedRegistryToolIds) {
    $null = $expectedOwnedToolIds.Add([string] $toolId)
}
$null = $expectedOwnedToolIds.Add('stale')

$expectedConfiguredAndOwnedToolIds = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
foreach ($toolId in $contextMenuToolIds) {
    $null = $expectedConfiguredAndOwnedToolIds.Add([string] $toolId)
}
foreach ($toolId in $expectedOwnedToolIds) {
    $null = $expectedConfiguredAndOwnedToolIds.Add([string] $toolId)
}

try {
    New-Item -Path $staleDirectoryKeyPath -Force | Out-Null
    New-Item -Path (Join-Path $staleDirectoryKeyPath 'command') -Force | Out-Null
    New-Item -Path $staleBackgroundKeyPath -Force | Out-Null
    New-Item -Path (Join-Path $staleBackgroundKeyPath 'command') -Force | Out-Null
    New-Item -Path $nonOwnedDirectoryKeyPath -Force | Out-Null
    New-Item -Path (Join-Path $nonOwnedDirectoryKeyPath 'command') -Force | Out-Null
    New-Item -Path $nonOwnedBackgroundKeyPath -Force | Out-Null
    New-Item -Path (Join-Path $nonOwnedBackgroundKeyPath 'command') -Force | Out-Null

    $uninstallOutput = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'uninstall.ps1') -ConfigPath $configPath -DryRun 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Assertion failed: uninstall dry-run exits 0. Output: $uninstallOutput" }
    $uninstallText = $uninstallOutput -join [Environment]::NewLine
    $uninstallData = ConvertTo-Array ($uninstallText | ConvertFrom-Json)
    Assert-Equal ($expectedConfiguredAndOwnedToolIds.Count * 2) $uninstallData.Count 'uninstall dry-run includes configured and stale owned entries'
    foreach ($toolId in @($expectedConfiguredAndOwnedToolIds | Sort-Object)) {
        Assert-Contains @($uninstallData.KeyPath) "HKCU:\Software\Classes\Directory\shell\AITerminalLauncher_$toolId" "uninstall dry-run includes folder key for $toolId"
        Assert-Contains @($uninstallData.KeyPath) "HKCU:\Software\Classes\Directory\Background\shell\AITerminalLauncher_$toolId" "uninstall dry-run includes background key for $toolId"
    }
    Assert-Contains @($uninstallData.KeyPath) $staleDirectoryKeyPath 'uninstall dry-run includes stale folder key'
    Assert-Contains @($uninstallData.KeyPath) $staleBackgroundKeyPath 'uninstall dry-run includes stale background key'
    Assert-NotContains @($uninstallData.KeyPath) $nonOwnedDirectoryKeyPath 'uninstall dry-run excludes non-owned folder sibling key'
    Assert-NotContains @($uninstallData.KeyPath) $nonOwnedBackgroundKeyPath 'uninstall dry-run excludes non-owned background sibling key'
    foreach ($operation in $uninstallData) {
        Assert-Equal 'RemoveContextMenuEntry' $operation.Action 'uninstall dry-run includes removal operations'
    }

    $missingUninstallConfigPath = Join-Path $repoRoot 'definitely-missing-uninstall-config.json'
    $missingUninstallOutput = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'uninstall.ps1') -ConfigPath $missingUninstallConfigPath -DryRun 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Assertion failed: uninstall dry-run falls back when config file is missing. Output: $missingUninstallOutput" }
    $missingUninstallText = $missingUninstallOutput -join [Environment]::NewLine
    $missingUninstallData = ConvertTo-Array ($missingUninstallText | ConvertFrom-Json)
    Assert-Equal ($expectedOwnedToolIds.Count * 2) $missingUninstallData.Count 'uninstall dry-run with missing config includes only owned entries'
    foreach ($toolId in @($expectedOwnedToolIds | Sort-Object)) {
        Assert-Contains @($missingUninstallData.KeyPath) "HKCU:\Software\Classes\Directory\shell\AITerminalLauncher_$toolId" "uninstall dry-run with missing config includes folder key for owned tool $toolId"
        Assert-Contains @($missingUninstallData.KeyPath) "HKCU:\Software\Classes\Directory\Background\shell\AITerminalLauncher_$toolId" "uninstall dry-run with missing config includes background key for owned tool $toolId"
    }
    Assert-NotContains @($missingUninstallData.KeyPath) $nonOwnedDirectoryKeyPath 'uninstall dry-run with missing config excludes non-owned folder sibling key'
    Assert-NotContains @($missingUninstallData.KeyPath) $nonOwnedBackgroundKeyPath 'uninstall dry-run with missing config excludes non-owned background sibling key'

    $badUninstallConfigPath = Join-Path $env:TEMP 'aitl-bad-uninstall-config.json'
    Set-Content -LiteralPath $badUninstallConfigPath -Value '{ invalid json' -Encoding UTF8
    try {
        $badUninstallOutput = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'uninstall.ps1') -ConfigPath $badUninstallConfigPath -DryRun 2>&1
        if ($LASTEXITCODE -ne 0) { throw "Assertion failed: uninstall dry-run falls back when config file cannot be loaded. Output: $badUninstallOutput" }
        $badUninstallText = $badUninstallOutput -join [Environment]::NewLine
        $badUninstallData = ConvertTo-Array ($badUninstallText | ConvertFrom-Json)
        Assert-Equal ($expectedOwnedToolIds.Count * 2) $badUninstallData.Count 'uninstall dry-run with invalid config includes only owned entries'
        foreach ($toolId in @($expectedOwnedToolIds | Sort-Object)) {
            Assert-Contains @($badUninstallData.KeyPath) "HKCU:\Software\Classes\Directory\shell\AITerminalLauncher_$toolId" "uninstall dry-run with invalid config includes folder key for owned tool $toolId"
            Assert-Contains @($badUninstallData.KeyPath) "HKCU:\Software\Classes\Directory\Background\shell\AITerminalLauncher_$toolId" "uninstall dry-run with invalid config includes background key for owned tool $toolId"
        }
        Assert-NotContains @($badUninstallData.KeyPath) $nonOwnedDirectoryKeyPath 'uninstall dry-run with invalid config excludes non-owned folder sibling key'
        Assert-NotContains @($badUninstallData.KeyPath) $nonOwnedBackgroundKeyPath 'uninstall dry-run with invalid config excludes non-owned background sibling key'
    }
    finally {
        Remove-Item -LiteralPath $badUninstallConfigPath -Force -ErrorAction SilentlyContinue
    }
}
finally {
    if (Test-Path -LiteralPath $staleDirectoryKeyPath) {
        Remove-Item -LiteralPath $staleDirectoryKeyPath -Recurse -Force
    }

    if (Test-Path -LiteralPath $staleBackgroundKeyPath) {
        Remove-Item -LiteralPath $staleBackgroundKeyPath -Recurse -Force
    }

    if (Test-Path -LiteralPath $nonOwnedDirectoryKeyPath) {
        Remove-Item -LiteralPath $nonOwnedDirectoryKeyPath -Recurse -Force
    }

    if (Test-Path -LiteralPath $nonOwnedBackgroundKeyPath) {
        Remove-Item -LiteralPath $nonOwnedBackgroundKeyPath -Recurse -Force
    }
}

$previousErrorActionPreference = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
try {
    $dotnetOutput = & dotnet run --project (Join-Path $repoRoot 'tests\AITerminalLauncher.Core.Tests\AITerminalLauncher.Core.Tests.csproj') 2>&1
}
finally {
    $ErrorActionPreference = $previousErrorActionPreference
}
if ($LASTEXITCODE -ne 0) {
    throw "Assertion failed: .NET test runner exits 0. Output: $dotnetOutput"
}

$settingsFormSource = Get-Content (Join-Path $repoRoot 'src\AITerminalLauncher.App\Forms\SettingsForm.cs') -Raw -Encoding UTF8
$uiThemeSource = Get-Content (Join-Path $repoRoot 'src\AITerminalLauncher.App\Forms\UiTheme.cs') -Raw -Encoding UTF8
$roundedControlsSource = Get-Content (Join-Path $repoRoot 'src\AITerminalLauncher.App\Forms\RoundedControls.cs') -Raw -Encoding UTF8
if ($roundedControlsSource -match [regex]::Escape('g.Clear(Parent?.BackColor')) {
    throw 'Assertion failed: rounded controls should not clear with transparent parent backgrounds.'
}
if ($roundedControlsSource -notmatch [regex]::Escape('ResolveBackgroundColor(')) {
    throw 'Assertion failed: rounded controls should resolve a non-transparent background color.'
}
if (($roundedControlsSource -split [regex]::Escape('SupportsTransparentBackColor')).Count -lt 4) {
    throw 'Assertion failed: every transparent rounded control should opt in to transparent backgrounds.'
}
if ($roundedControlsSource -match [regex]::Escape('Closed += (_, _) => menu.Dispose()')) {
    throw 'Assertion failed: rounded select menus should not be disposed synchronously while WinForms is closing them.'
}
if ($uiThemeSource -notmatch [regex]::Escape('Color.FromArgb(0xF5, 0xF8, 0xFB)')) {
    throw 'Assertion failed: UI theme should use a soft rounded light background.'
}
if ($uiThemeSource -notmatch [regex]::Escape('Color.FromArgb(0x14, 0x9E, 0xCA)')) {
    throw 'Assertion failed: UI theme should use a calm cyan-blue accent color.'
}
if ($uiThemeSource -notmatch [regex]::Escape('Radius = 18')) {
    throw 'Assertion failed: UI theme should use rounded controls.'
}
if ($settingsFormSource -notmatch [regex]::Escape('Radius = 24')) {
    throw 'Assertion failed: settings form should use rounded cards.'
}
if ($settingsFormSource -notmatch [regex]::Escape('RoundedLabel CreateStatusPill')) {
    throw 'Assertion failed: status pills should be painted directly as rounded labels.'
}
if ($settingsFormSource -notmatch [regex]::Escape('RoundedSelect _preferredTerminalSelect') -or $settingsFormSource -match [regex]::Escape('new ComboBox')) {
    throw 'Assertion failed: settings terminal selectors should use the custom rounded select control.'
}
if ($settingsFormSource -match [regex]::Escape('GridLines = true')) {
    throw 'Assertion failed: settings tool list should avoid hard grid lines in the rounded design.'
}
if ($settingsFormSource -notmatch [regex]::Escape("Text = `"$settingsTitle`"")) {
    throw 'Assertion failed: settings window title is localized to Chinese.'
}
if ($settingsFormSource -notmatch 'CreateToolbarButton\(' -or $settingsFormSource -notmatch 'primary: true') {
    throw 'Assertion failed: settings form add-tool button is localized to Chinese.'
}
if ($settingsFormSource -notmatch [regex]::Escape("CreateActionButton(`"$settingsSave`"")) {
    throw 'Assertion failed: settings form save button is localized to Chinese.'
}
if ($settingsFormSource -notmatch [regex]::Escape("BuildToolCommandBar()")) {
    throw 'Assertion failed: settings form should expose tool actions in a compact command bar.'
}
if ($settingsFormSource -notmatch [regex]::Escape('UpdateToolCommandBarState()') -or $settingsFormSource -notmatch [regex]::Escape('_enabledToggleButton.Text') -or $settingsFormSource -notmatch [regex]::Escape('Enabled == true')) {
    throw 'Assertion failed: settings enable-toggle toolbar button should show enable or disable based on selected tool state.'
}
if ($settingsFormSource -notmatch [regex]::Escape("BuildToolCard(")) {
    throw 'Assertion failed: settings form should render tools as rounded cards.'
}
if ($settingsFormSource -notmatch [regex]::Escape("Height = 68") -or $settingsFormSource -notmatch [regex]::Escape("Height = 22")) {
    throw 'Assertion failed: settings tool cards should use compact row and pill heights.'
}
if ($settingsFormSource -match [regex]::Escape('ListView')) {
    throw 'Assertion failed: settings form should not use ListView for the main tool surface.'
}
if ($settingsFormSource -notmatch [regex]::Escape("ToggleLaunchAtLoginSetting()")) {
    throw 'Assertion failed: settings form should expose a large clickable launch-at-login toggle.'
}
if ($settingsFormSource -match [regex]::Escape('private readonly CheckBox _launchAtLoginCheckBox;')) {
    throw 'Assertion failed: settings form launch-at-login control should not be a tiny checkbox.'
}
if ($settingsFormSource -notmatch [regex]::Escape("BuildToolListCard()")) {
    throw 'Assertion failed: settings form should isolate the tool list card layout.'
}
if ($settingsFormSource -notmatch [regex]::Escape("outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));")) {
    throw 'Assertion failed: settings form should use a compact lower settings area.'
}
if ($settingsFormSource -match [regex]::Escape("body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 176));")) {
    throw 'Assertion failed: settings form should not reserve a right-side vertical action column.'
}

$toolEditorFormSource = Get-Content (Join-Path $repoRoot 'src\AITerminalLauncher.App\Forms\ToolEditorForm.cs') -Raw -Encoding UTF8
if ($toolEditorFormSource -notmatch [regex]::Escape('HotkeyKeyInput _hotkeyKeyInput') -or $toolEditorFormSource -match [regex]::Escape('new ComboBox')) {
    throw 'Assertion failed: tool editor hotkey key should use keyboard input capture instead of a dropdown.'
}
if ($roundedControlsSource -notmatch [regex]::Escape('internal sealed class HotkeyKeyInput') -or $roundedControlsSource -notmatch [regex]::Escape('OnKeyDown')) {
    throw 'Assertion failed: hotkey key input should capture pressed keys directly.'
}
if ($toolEditorFormSource -notmatch [regex]::Escape("Text = tool is null ? `"$toolEditorTitle`"")) {
    throw 'Assertion failed: tool editor add title is localized to Chinese.'
}
if ($toolEditorFormSource -notmatch [regex]::Escape("_showInTrayMenuCheckBox = new CheckBox { Text = `"$toolEditorTray`"")) {
    throw 'Assertion failed: tool editor tray-visibility label is localized to Chinese.'
}
if ($toolEditorFormSource -notmatch [regex]::Escape("Text = `"$saveText`"")) {
    throw 'Assertion failed: tool editor save button is localized to Chinese.'
}

$trayContextSource = Get-Content (Join-Path $repoRoot 'src\AITerminalLauncher.App\Tray\LauncherApplicationContext.cs') -Raw -Encoding UTF8
if ($trayContextSource -notmatch [regex]::Escape("new ToolStripMenuItem(`"$traySettings`")")) {
    throw 'Assertion failed: tray settings menu is localized to Chinese.'
}
if ($trayContextSource -notmatch [regex]::Escape("new ToolStripMenuItem(`"$trayLaunchAtLogin`")")) {
    throw 'Assertion failed: tray launch-at-login menu is localized to Chinese.'
}
if ($trayContextSource -match [regex]::Escape("_notifyIcon.DoubleClick += static (_, _) => { };")) {
    throw 'Assertion failed: tray context should not subscribe an empty double-click handler.'
}
if ($trayContextSource -notmatch [regex]::Escape("RecentLaunchTargetReuseWindow") -or $trayContextSource -notmatch [regex]::Escape("GetRecentLaunchTargetPath()")) {
    throw 'Assertion failed: remembered launch target should only be reused briefly, not forever.'
}
if ($trayContextSource -notmatch [regex]::Escape("_foregroundPollTimer") -or $trayContextSource -notmatch [regex]::Escape("RefreshHotkeyRegistrationForForeground(")) {
    throw 'Assertion failed: global hotkeys should be registered only while Explorer is the foreground window.'
}
if ($trayContextSource -match [regex]::Escape("TryRegisterHotkeys(showWarning: true);`r`n`r`n        if (openSettingsOnStartup)")) {
    throw 'Assertion failed: tray startup should not register global hotkeys unconditionally.'
}
$openSettingsIndex = $trayContextSource.IndexOf('private void OpenSettings()', [System.StringComparison]::Ordinal)
$settingsDialogIndex = $trayContextSource.IndexOf('form.ShowDialog()', $openSettingsIndex, [System.StringComparison]::Ordinal)
$pauseHotkeysIndex = $trayContextSource.IndexOf('_globalHotkeyService.UnregisterAll();', $openSettingsIndex, [System.StringComparison]::Ordinal)
$resumeHotkeysIndex = $trayContextSource.IndexOf('RefreshHotkeyRegistrationForForeground(showWarning: true);', $settingsDialogIndex, [System.StringComparison]::Ordinal)
if ($openSettingsIndex -lt 0 -or $settingsDialogIndex -lt 0 -or $pauseHotkeysIndex -lt 0 -or $pauseHotkeysIndex -gt $settingsDialogIndex) {
    throw 'Assertion failed: settings dialog should unregister global hotkeys before showing hotkey capture controls.'
}
if ($resumeHotkeysIndex -lt 0) {
    throw 'Assertion failed: settings dialog should restore global hotkeys after closing.'
}

$globalHotkeySource = Get-Content (Join-Path $repoRoot 'src\AITerminalLauncher.App\Hotkeys\GlobalHotkeyService.cs') -Raw -Encoding UTF8
$duplicateCheckIndex = $globalHotkeySource.IndexOf('var duplicates = HotkeyConflictDetector.FindDuplicates(toolList);', [System.StringComparison]::Ordinal)
$unregisterIndex = $globalHotkeySource.IndexOf('UnregisterAll();', [System.StringComparison]::Ordinal)
if ($duplicateCheckIndex -lt 0 -or $unregisterIndex -lt 0 -or $duplicateCheckIndex -gt $unregisterIndex) {
    throw 'Assertion failed: duplicate hotkeys should be detected before clearing existing registrations.'
}

$installScriptSource = Get-Content (Join-Path $repoRoot 'install.ps1') -Raw -Encoding UTF8
if ($installScriptSource -notmatch [regex]::Escape("Format-AITLText -Key 'ContextMenuLabel'")) {
    throw 'Assertion failed: install script uses Chinese context-menu labels.'
}
if ($installScriptSource -notmatch [regex]::Escape("Get-AITLText -Key 'ContextMenuInstalled'")) {
    throw 'Assertion failed: install script success output is localized to Chinese.'
}

$uninstallScriptSource = Get-Content (Join-Path $repoRoot 'uninstall.ps1') -Raw -Encoding UTF8
if ($uninstallScriptSource -notmatch [regex]::Escape("Get-AITLText -Key 'ContextMenuRemoved'")) {
    throw 'Assertion failed: uninstall script success output is localized to Chinese.'
}

$appProjectSource = Get-Content (Join-Path $repoRoot 'src\AITerminalLauncher.App\AITerminalLauncher.App.csproj') -Raw -Encoding UTF8
$programSource = Get-Content (Join-Path $repoRoot 'src\AITerminalLauncher.App\Program.cs') -Raw -Encoding UTF8
if ($programSource -notmatch [regex]::Escape('Global\AITerminalLauncher.App.SingleInstance') -or $programSource -notmatch [regex]::Escape('createdNew')) {
    throw 'Assertion failed: app startup should enforce a single tray instance with a named mutex.'
}
if ($programSource -notmatch [regex]::Escape('SingleInstanceMessageWindow.RequestShowSettings()')) {
    throw 'Assertion failed: double-clicking a second app instance should ask the running tray app to show settings.'
}
if ($appProjectSource -notmatch [regex]::Escape('Include="..\..\launcher.ps1"')) {
    throw 'Assertion failed: app project publishes launcher.ps1.'
}
if ($appProjectSource -notmatch [regex]::Escape('Include="..\..\install.ps1"')) {
    throw 'Assertion failed: app project publishes install.ps1.'
}
if ($appProjectSource -notmatch [regex]::Escape('Include="..\..\uninstall.ps1"')) {
    throw 'Assertion failed: app project publishes uninstall.ps1.'
}
if ($appProjectSource -notmatch [regex]::Escape('TargetPath>src\AITerminalLauncher.psm1<')) {
    throw 'Assertion failed: app project publishes the PowerShell module under src.'
}
if ($appProjectSource -notmatch [regex]::Escape('<CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>')) {
    throw 'Assertion failed: app project marks publish assets to copy during publish.'
}

'All tests passed.'
