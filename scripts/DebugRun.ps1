<#
.SYNOPSIS
    Runs a throwaway copy of the app from a temporary folder.

.DESCRIPTION
    Builds the app into a temporary directory marked portable, so its settings,
    draft and scripts live there and nothing touches the copy you use every day.
    Voices are borrowed from the repository, or from -VoicesDirectory.

    The window closes and the folder is deleted when you press Ctrl+C here, or
    when you close the window yourself.

.EXAMPLE
    .\DebugRun.ps1

.EXAMPLE
    .\DebugRun.ps1 -Configuration Release -Keep
#>
[CmdletBinding()]
param(
    [string]$Configuration = 'Debug',
    [string]$VoicesDirectory,
    [switch]$Keep
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'src\TtsUtil.App\TtsUtil.App.csproj'

if (-not $VoicesDirectory) {
    $inRepo = Join-Path $root 'voices'
    if (Test-Path $inRepo) { $VoicesDirectory = $inRepo }
}

$temp = Join-Path ([System.IO.Path]::GetTempPath()) "TtsUtilWinDebug-$([guid]::NewGuid().ToString('N').Substring(0, 8))"
New-Item -ItemType Directory -Path $temp | Out-Null

Write-Host "Building $Configuration into $temp" -ForegroundColor Cyan
dotnet build $project -c $Configuration -o $temp --nologo -v q
if ($LASTEXITCODE -ne 0) { throw 'The build failed.' }

# The marker keeps settings, draft.txt and scripts inside the throwaway folder.
Set-Content -Path (Join-Path $temp 'portable.txt') -Value 'Temporary debug copy.'

$exe = Join-Path $temp 'TtsUtilWin.exe'
$arguments = @()
if ($VoicesDirectory) { $arguments = @('--voices', $VoicesDirectory) }

$app = $null

try {
    $app = Start-Process -FilePath $exe -ArgumentList $arguments -PassThru
    Write-Host "Running as process $($app.Id). Press Ctrl+C to close it." -ForegroundColor Green
    Wait-Process -Id $app.Id
}
finally {
    if ($app -and -not $app.HasExited) {
        Stop-Process -Id $app.Id -Force -ErrorAction SilentlyContinue
        $app.WaitForExit(5000) | Out-Null
    }

    if ($Keep) {
        Write-Host "Left behind at $temp" -ForegroundColor Yellow
    }
    else {
        Remove-Item $temp -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host 'Closed, and the temporary folder is gone.' -ForegroundColor Cyan
    }
}
