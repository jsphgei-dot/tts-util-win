<#
.SYNOPSIS
    Publishes a portable, self contained Windows build.

.DESCRIPTION
    Produces dist\TtsUtilWin\TtsUtilWin.exe with the .NET runtime and the
    sherpa native libraries bundled into the executable. Voice models stay in
    a voices folder beside the exe, so the whole directory is the portable app.

.EXAMPLE
    .\BuildPortable.ps1

.EXAMPLE
    .\BuildPortable.ps1 -IncludeVoices
#>
[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$Runtime = 'win-x64',
    [string]$OutputDirectory,
    [switch]$IncludeVoices
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'src\TtsUtil.App\TtsUtil.App.csproj'

if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $root 'dist\TtsUtilWin'
}

Write-Host "Publishing $Configuration/$Runtime to $OutputDirectory" -ForegroundColor Cyan

if (Test-Path $OutputDirectory) { Remove-Item -Recurse -Force $OutputDirectory }

dotnet publish $project `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=none `
    -o $OutputDirectory

if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }

$voicesTarget = Join-Path $OutputDirectory 'voices'
New-Item -ItemType Directory -Force -Path $voicesTarget | Out-Null

if ($IncludeVoices) {
    $voicesSource = Join-Path $root 'voices'
    if (Test-Path $voicesSource) {
        Write-Host 'Copying voice models' -ForegroundColor Cyan
        Copy-Item -Path (Join-Path $voicesSource '*') -Destination $voicesTarget -Recurse -Force
    }
    else {
        Write-Warning 'No voices directory to copy. Run FetchVoices.ps1 first.'
    }
}

Copy-Item -Path (Join-Path $root 'README.md') -Destination $OutputDirectory -Force
Copy-Item -Path (Join-Path $root 'LICENSE') -Destination $OutputDirectory -Force
Copy-Item -Path (Join-Path $root 'scripts\FetchVoices.ps1') -Destination $OutputDirectory -Force

$exe = Join-Path $OutputDirectory 'TtsUtilWin.exe'
$sizeMb = [math]::Round((Get-Item $exe).Length / 1MB, 1)
Write-Host "Built $exe ($sizeMb MB)." -ForegroundColor Green
Write-Host 'Copy the whole folder to run it anywhere; settings.json is written beside the exe.' -ForegroundColor Cyan
