<#
.SYNOPSIS
    Runs a throwaway copy of the app from a temporary folder.

.DESCRIPTION
    Builds the app into a temporary directory marked portable, so its settings,
    draft and scripts live there and nothing touches the copy you use every day.
    Voices are borrowed from the repository, or from -VoicesDirectory.

    The window closes and the folder is deleted when you press Ctrl+C here, or
    when you close the window yourself.

    -Runtime builds one of the three shipped architectures, self contained, the
    way a release is built. A build this machine cannot execute is left on disk
    rather than launched.

.EXAMPLE
    .\DebugRun.ps1

.EXAMPLE
    .\DebugRun.ps1 -Configuration Release -Keep

.EXAMPLE
    .\DebugRun.ps1 -Runtime win-x86
#>
[CmdletBinding()]
param(
    [string]$Configuration = 'Debug',
    [ValidateSet('win-x64', 'win-arm64', 'win-x86')]
    [string]$Runtime,
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

$arguments = @('-c', $Configuration, '-o', $temp, '--nologo', '-v', 'q')

if ($Runtime) {
    # A self contained build of the app cannot carry the test project along, so the csproj
    # test target stands aside for this one.
    $arguments += @('-r', $Runtime, '--self-contained', 'true', '-p:SkipTests=true')
}

Write-Host "Building $Configuration$(if ($Runtime) { "/$Runtime" }) into $temp" -ForegroundColor Cyan
dotnet build $project @arguments
if ($LASTEXITCODE -ne 0) { throw 'The build failed.' }

# The marker keeps settings, draft.txt and scripts inside the throwaway folder.
Set-Content -Path (Join-Path $temp 'portable.txt') -Value 'Temporary debug copy.'

$exe = Join-Path $temp 'TtsUtilWin.exe'
$runArguments = @()
if ($VoicesDirectory) { $runArguments = @('--voices', $VoicesDirectory) }

# An ARM64 build does not start on an Intel machine, and a 64 bit one does not start on a
# 32 bit one, so say where it is instead of launching something that cannot run.
$host64 = [Environment]::Is64BitOperatingSystem
$hostArm = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture -eq 'Arm64'
$runnable = switch ($Runtime) {
    'win-arm64' { $hostArm }
    'win-x64' { $host64 }
    default { $true }
}

if (-not $runnable) {
    Write-Host "A $Runtime build cannot run on this machine. It is at $temp" -ForegroundColor Yellow
    return
}

$app = $null

try {
    $app = Start-Process -FilePath $exe -ArgumentList $runArguments -PassThru
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
