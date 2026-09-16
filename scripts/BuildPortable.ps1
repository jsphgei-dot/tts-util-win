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
    [switch]$IncludeVoices,
    [switch]$SkipTests,
    [switch]$Installer
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'src\TtsUtil.App\TtsUtil.App.csproj'

if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $root 'dist\TtsUtilWin'
}

if (-not $SkipTests) {
    Write-Host 'Running core unit tests' -ForegroundColor Cyan
    dotnet test (Join-Path $root 'tests\TtsUtil.Core.Tests\TtsUtil.Core.Tests.csproj') -c $Configuration --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw 'Core tests failed; nothing was published.' }

    # SkipTests stops the app csproj target repeating the core tests here.
    Write-Host 'Running user interface tests' -ForegroundColor Cyan
    dotnet test (Join-Path $root 'tests\TtsUtil.App.Tests\TtsUtil.App.Tests.csproj') -c $Configuration --nologo -v q -p:SkipTests=true
    if ($LASTEXITCODE -ne 0) { throw 'UI tests failed; nothing was published.' }
}

Write-Host "Publishing $Configuration/$Runtime to $OutputDirectory" -ForegroundColor Cyan

if (Test-Path $OutputDirectory) { Remove-Item -Recurse -Force $OutputDirectory }

# The tests already ran above; SkipTests stops the csproj target repeating them.
dotnet publish $project `
    -c $Configuration `
    -r $Runtime `
    -p:SkipTests=true `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=none `
    -o $OutputDirectory

if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }

$voicesTarget = Join-Path $OutputDirectory 'voices'
New-Item -ItemType Directory -Force -Path $voicesTarget | Out-Null

# The marker keeps this copy portable: settings and voices stay in this folder.
Set-Content -Path (Join-Path $OutputDirectory 'portable.txt') -Value 'Remove this file to make this copy use %APPDATA% and %LOCALAPPDATA% instead.'

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
# FetchVoices.ps1 works out its default destination from its parent folder, so it has to sit
# in scripts\ for models to land in this folder's voices directory rather than beside it.
$scriptsTarget = Join-Path $OutputDirectory 'scripts'
New-Item -ItemType Directory -Force -Path $scriptsTarget | Out-Null
Copy-Item -Path (Join-Path $root 'scripts\FetchVoices.ps1') -Destination $scriptsTarget -Force

$exe = Join-Path $OutputDirectory 'TtsUtilWin.exe'
$sizeMb = [math]::Round((Get-Item $exe).Length / 1MB, 1)
Write-Host "Built $exe ($sizeMb MB)." -ForegroundColor Green
Write-Host 'Copy the whole folder to run it anywhere; settings.json is written beside the exe.' -ForegroundColor Cyan

if ($Installer) {
    $iscc = (Get-Command iscc.exe -ErrorAction SilentlyContinue).Source
    if (-not $iscc) {
        $candidates = @(
            "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
            "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
            "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
        )
        foreach ($candidate in $candidates) {
            if (Test-Path $candidate) { $iscc = $candidate; break }
        }
    }
    if (-not $iscc) { throw 'Inno Setup was not found. Install it with: winget install JRSoftware.InnoSetup' }

    if (-not $SkipTests) {
        Write-Host 'Running installer version tests' -ForegroundColor Cyan
        & (Join-Path $PSScriptRoot 'TestInstaller.ps1') -InnoSetupPath $iscc
        if ($LASTEXITCODE -ne 0) { throw 'Installer version tests failed; no installer was built.' }
    }

    $props = [xml](Get-Content (Join-Path $root 'Directory.Build.props'))
    $prefix = ($props.Project.PropertyGroup.VersionPrefix | Where-Object { $_ }) -join ''
    $suffix = ($props.Project.PropertyGroup.VersionSuffix | Where-Object { $_ }) -join ''
    $code = ($props.Project.PropertyGroup.VersionCode | Where-Object { $_ }) -join ''
    $appVersion = if ($suffix) { "$prefix-$suffix" } else { $prefix }
    $fileVersion = "$prefix.$code"

    Write-Host "Compiling installer for $appVersion" -ForegroundColor Cyan
    & $iscc "/DAppVersion=$appVersion" "/DFileVersion=$fileVersion" "/DSourceDir=$OutputDirectory" `
        (Join-Path $root 'installer\TtsUtilWin.iss')
    if ($LASTEXITCODE -ne 0) { throw 'Inno Setup compilation failed.' }

    $setup = Join-Path $root "dist\TtsUtilWin-$appVersion-setup.exe"
    $setupMb = [math]::Round((Get-Item $setup).Length / 1MB, 1)
    Write-Host "Built $setup ($setupMb MB)." -ForegroundColor Green
}
