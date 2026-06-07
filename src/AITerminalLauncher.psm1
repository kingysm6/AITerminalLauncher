function Convert-AITLUnicodeLiteral {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Literal
    )

    return (ConvertFrom-Json ('"' + $Literal + '"'))
}

function Get-AITLText {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Key
    )

    switch ($Key) {
        'ConfigNotFound' {
            return Convert-AITLUnicodeLiteral '\u672a\u627e\u5230\u914d\u7f6e\u6587\u4ef6\uff1a{0}'
        }
        'ToolIdEmpty' {
            return Convert-AITLUnicodeLiteral '\u5de5\u5177 ID \u4e0d\u80fd\u4e3a\u7a7a\u3002'
        }
        'ToolIdInvalid' {
            return Convert-AITLUnicodeLiteral '\u5de5\u5177 ID ''{0}'' \u5305\u542b\u65e0\u6548\u5b57\u7b26\u3002'
        }
        'ConfigLoadFailed' {
            return Convert-AITLUnicodeLiteral '\u52a0\u8f7d\u914d\u7f6e\u6587\u4ef6 ''{0}'' \u5931\u8d25\uff1a{1}'
        }
        'TargetDirectoryNotFound' {
            return Convert-AITLUnicodeLiteral '\u672a\u627e\u5230\u76ee\u6807\u76ee\u5f55\uff1a{0}'
        }
        'ToolsSectionMissing' {
            return Convert-AITLUnicodeLiteral '\u914d\u7f6e\u7f3a\u5c11 tools \u8282\u3002'
        }
        'UnknownToolId' {
            return Convert-AITLUnicodeLiteral '\u672a\u77e5\u5de5\u5177 ID ''{0}''\u3002\u5f53\u524d\u53ef\u7528\u5e76\u5df2\u542f\u7528\u7684\u5de5\u5177\uff1a{1}'
        }
        'ToolCommandMissing' {
            return Convert-AITLUnicodeLiteral '\u5de5\u5177\u914d\u7f6e\u7f3a\u5c11\u547d\u4ee4\u3002'
        }
        'UnsupportedTerminal' {
            return Convert-AITLUnicodeLiteral '\u4e0d\u652f\u6301\u7684\u7ec8\u7aef\uff1a''{0}''\u3002'
        }
        'ContextMenuLabel' {
            return Convert-AITLUnicodeLiteral '\u4f7f\u7528 {0} \u6253\u5f00'
        }
        'ContextMenuInstalled' {
            return Convert-AITLUnicodeLiteral '\u5df2\u5b89\u88c5 AI Terminal Launcher \u53f3\u952e\u83dc\u5355\u9879\u3002'
        }
        'ContextMenuRemoved' {
            return Convert-AITLUnicodeLiteral '\u5df2\u79fb\u9664 AI Terminal Launcher \u53f3\u952e\u83dc\u5355\u9879\u3002'
        }
        default {
            throw "Unknown AITL text key: $Key"
        }
    }
}

function Format-AITLText {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Key,

        [object[]] $Args = @()
    )

    $text = Get-AITLText -Key $Key
    if ($Args.Count -eq 0) {
        return $text
    }

    return ($text -f $Args)
}

function Resolve-AITLConfigPath {
    param(
        [string] $ExplicitPath
    )

    $candidatePath = $null
    if (-not [string]::IsNullOrWhiteSpace($ExplicitPath)) {
        $candidatePath = $ExplicitPath
    }
    elseif (-not [string]::IsNullOrWhiteSpace($env:AITL_CONFIG_PATH)) {
        $candidatePath = $env:AITL_CONFIG_PATH
    }
    else {
        $candidatePath = Join-Path $PSScriptRoot '..\config.json'
    }

    try {
        return (Resolve-Path -LiteralPath $candidatePath -ErrorAction Stop).ProviderPath
    }
    catch {
        throw (Format-AITLText -Key 'ConfigNotFound' -Args @($candidatePath))
    }
}

function Test-AITLToolId {
    param(
        [AllowEmptyString()]
        [string] $ToolId
    )

    if ([string]::IsNullOrWhiteSpace($ToolId)) {
        return $false
    }

    return $ToolId -cmatch '^[a-z0-9-]+$'
}

function Assert-AITLToolId {
    param(
        [AllowEmptyString()]
        [string] $ToolId
    )

    if ([string]::IsNullOrWhiteSpace($ToolId)) {
        throw (Get-AITLText -Key 'ToolIdEmpty')
    }

    if (-not (Test-AITLToolId -ToolId $ToolId)) {
        throw (Format-AITLText -Key 'ToolIdInvalid' -Args @($ToolId))
    }

    return $ToolId
}

function Assert-AITLConfigToolIds {
    param(
        $Tools
    )

    foreach ($tool in @($Tools)) {
        if ($null -eq $tool) {
            continue
        }

        $toolId = [string] $tool.id
        $null = Assert-AITLToolId -ToolId $toolId
    }
}

function Get-AITLConfig {
    param(
        [Parameter(Mandatory = $true)]
        [string] $ConfigPath
    )

    if (-not (Test-Path -LiteralPath $ConfigPath -PathType Leaf)) {
        throw (Format-AITLText -Key 'ConfigNotFound' -Args @($ConfigPath))
    }

    try {
        $json = Get-Content -LiteralPath $ConfigPath -Raw -ErrorAction Stop
        $config = $json | ConvertFrom-Json -ErrorAction Stop
        if ($null -ne $config.tools) {
            Assert-AITLConfigToolIds -Tools $config.tools
        }

        return $config
    }
    catch {
        throw (Format-AITLText -Key 'ConfigLoadFailed' -Args @($ConfigPath, $_.Exception.Message))
    }
}

function Resolve-AITLTargetPath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        throw (Format-AITLText -Key 'TargetDirectoryNotFound' -Args @($Path))
    }

    return (Resolve-Path -LiteralPath $Path -ErrorAction Stop).ProviderPath
}

function Get-AITLToolConfig {
    param(
        [Parameter(Mandatory = $true)]
        $Config,

        [Parameter(Mandatory = $true)]
        [string] $ToolId
    )

    if ($null -eq $Config.tools) {
        throw (Get-AITLText -Key 'ToolsSectionMissing')
    }

    $validatedToolId = Assert-AITLToolId -ToolId $ToolId
    Assert-AITLConfigToolIds -Tools $Config.tools

    $tools = @($Config.tools)
    $toolConfig = $tools | Where-Object { $_.id -eq $validatedToolId -and $_.enabled } | Select-Object -First 1
    if ($null -eq $toolConfig) {
        $validTools = @(
            $tools |
                Where-Object { $_.enabled } |
                ForEach-Object { $_.id } |
                Sort-Object
        ) -join ', '
        throw (Format-AITLText -Key 'UnknownToolId' -Args @($ToolId, $validTools))
    }

    return $toolConfig
}

function Join-AITLToolCommand {
    param(
        [Parameter(Mandatory = $true)]
        $ToolConfig
    )

    if ([string]::IsNullOrWhiteSpace($ToolConfig.command)) {
        throw (Get-AITLText -Key 'ToolCommandMissing')
    }

    $parts = @((Format-AITLPowerShellArgument -Value ([string] $ToolConfig.command)))
    if ($null -ne $ToolConfig.args) {
        foreach ($arg in $ToolConfig.args) {
            $parts += Format-AITLPowerShellArgument -Value ([string] $arg)
        }
    }

    return ($parts -join ' ')
}

function Format-AITLPowerShellArgument {
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string] $Value
    )

    if ($Value -notmatch '[\s"`]') {
        return $Value
    }

    $escapedValue = $Value.Replace('`', '``').Replace('"', '`"')
    return "`"$escapedValue`""
}

function New-AITLLaunchCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Terminal,

        [Parameter(Mandatory = $true)]
        [string] $FallbackTerminal,

        [Parameter(Mandatory = $true)]
        [string] $TargetPath,

        [Parameter(Mandatory = $true)]
        $ToolConfig
    )

    $toolCommand = Join-AITLToolCommand -ToolConfig $ToolConfig
    $normalizedTerminal = $Terminal.ToLowerInvariant()
    $normalizedFallbackTerminal = $FallbackTerminal.ToLowerInvariant()

    if ($normalizedTerminal -eq 'wt') {
        return [pscustomobject]@{
            FilePath = 'wt.exe'
            ArgumentList = @('-d', $TargetPath, 'powershell.exe', '-NoExit', '-Command', $toolCommand)
            WorkingDirectory = $null
        }
    }

    if ($normalizedTerminal -eq 'powershell' -or $normalizedFallbackTerminal -eq 'powershell') {
        return [pscustomobject]@{
            FilePath = 'powershell.exe'
            ArgumentList = @('-NoExit', '-Command', $toolCommand)
            WorkingDirectory = $TargetPath
        }
    }

    throw (Format-AITLText -Key 'UnsupportedTerminal' -Args @($Terminal))
}

function New-AITLRegistryKeyPath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $ToolId,

        [Parameter(Mandatory = $true)]
        [ValidateSet('Directory', 'Background')]
        [string] $Location
    )

    $validatedToolId = Assert-AITLToolId -ToolId $ToolId
    $keyName = "AITerminalLauncher_$validatedToolId"
    if ($Location -eq 'Directory') {
        return "HKCU:\Software\Classes\Directory\shell\$keyName"
    }

    return "HKCU:\Software\Classes\Directory\Background\shell\$keyName"
}

function New-AITLInstallRegistryOperation {
    param(
        [Parameter(Mandatory = $true)]
        [string] $LauncherPath,

        [Parameter(Mandatory = $true)]
        [string] $ToolId,

        [Parameter(Mandatory = $true)]
        [string] $Label,

        [Parameter(Mandatory = $true)]
        [string] $ConfigPath
    )

    $validatedToolId = Assert-AITLToolId -ToolId $ToolId
    $command = "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$LauncherPath`" -Tool `"$validatedToolId`" -ConfigPath `"$ConfigPath`" -Path `"%V`""
    $operations = @()
    foreach ($location in @('Directory', 'Background')) {
        $operations += [pscustomobject]@{
            Action = 'SetContextMenuEntry'
            Tool = $validatedToolId
            Location = $location
            KeyPath = New-AITLRegistryKeyPath -ToolId $validatedToolId -Location $location
            Label = $Label
            Command = $command
        }
    }

    return $operations
}

function New-AITLUninstallRegistryOperation {
    param(
        [Parameter(Mandatory = $true)]
        [string] $ToolId
    )

    $validatedToolId = Assert-AITLToolId -ToolId $ToolId
    $operations = @()
    foreach ($location in @('Directory', 'Background')) {
        $operations += [pscustomobject]@{
            Action = 'RemoveContextMenuEntry'
            Tool = $validatedToolId
            Location = $location
            KeyPath = New-AITLRegistryKeyPath -ToolId $validatedToolId -Location $location
        }
    }

    return $operations
}

function Get-AITLOwnedRegistryToolIds {
    $toolIds = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)

    foreach ($rootPath in @(
        'HKCU:\Software\Classes\Directory\shell',
        'HKCU:\Software\Classes\Directory\Background\shell'
    )) {
        if (-not (Test-Path -LiteralPath $rootPath)) {
            continue
        }

        foreach ($item in Get-ChildItem -LiteralPath $rootPath -ErrorAction SilentlyContinue) {
            if ($item.PSChildName -like 'AITerminalLauncher_*') {
                $toolId = $item.PSChildName.Substring('AITerminalLauncher_'.Length)
                if (Test-AITLToolId -ToolId $toolId) {
                    $null = $toolIds.Add($toolId)
                }
            }
        }
    }

    return @($toolIds | Sort-Object)
}

Export-ModuleMember -Function Convert-AITLUnicodeLiteral, Get-AITLText, Format-AITLText, Resolve-AITLConfigPath, Get-AITLConfig, Resolve-AITLTargetPath, Get-AITLToolConfig, New-AITLLaunchCommand, New-AITLInstallRegistryOperation, New-AITLUninstallRegistryOperation, Get-AITLOwnedRegistryToolIds
