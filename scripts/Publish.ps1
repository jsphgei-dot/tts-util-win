<#
.SYNOPSIS
    Packages a release and writes the manifest the program checks for updates.

.DESCRIPTION
    Builds the portable folder and the setup program, zips both, hashes them, and writes
    latest.json. With -Publish it also creates the GitHub release in the public distribution
    repository and pushes the manifest there, which is what makes the update notice appear.

.EXAMPLE
    .\Publish.ps1

.EXAMPLE
    .\Publish.ps1 -Publish -NotesFile dist\RELEASE_NOTES_v0.3.0-beta.md
#>
[CmdletBinding()]
param(
    [string]$DistributionRepo = 'jsphgei-dot/tts-util-win-releases',

    [string]$NotesFile,

    [switch]$Publish,

    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$dist = Join-Path $root 'dist'

# The version lives in one place, so read it rather than asking for it again here.
$props = Get-Content (Join-Path $root 'Directory.Build.props') -Raw
$prefix = [regex]::Match($props, '<VersionPrefix>([^<]+)</VersionPrefix>').Groups[1].Value
$suffix = [regex]::Match($props, '<VersionSuffix>([^<]+)</VersionSuffix>').Groups[1].Value
$code = [int][regex]::Match($props, '<VersionCode>([^<]+)</VersionCode>').Groups[1].Value
$version = if ($suffix) { "$prefix-$suffix" } else { $prefix }
$tag = "v$version"

Write-Host "Packaging $version (version code $code)" -ForegroundColor Cyan

& (Join-Path $PSScriptRoot 'BuildPortable.ps1') -Installer -SkipTests:$SkipTests
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

$portableZip = Join-Path $dist "TtsUtilWin-$version-portable.zip"
$setupExe = Join-Path $dist "TtsUtilWin-$version-setup.exe"
$setupZip = Join-Path $dist "TtsUtilWin-$version-setup.zip"

if (-not (Test-Path $setupExe)) { throw "No setup program at $setupExe." }

Remove-Item $portableZip, $setupZip -ErrorAction SilentlyContinue
Compress-Archive -Path (Join-Path $dist 'TtsUtilWin') -DestinationPath $portableZip
Compress-Archive -Path $setupExe -DestinationPath $setupZip

function Get-Sha256([string]$path) { (Get-FileHash -Algorithm SHA256 -Path $path).Hash.ToLowerInvariant() }

$releaseUrl = "https://github.com/$DistributionRepo/releases/tag/$tag"
$downloadBase = "https://github.com/$DistributionRepo/releases/download/$tag"

# The program reads this file, and refuses any download whose hash does not match what is here.
$manifest = [ordered]@{
    versionName = $version
    versionCode = $code
    releaseUrl  = $releaseUrl
    setup       = [ordered]@{
        url    = "$downloadBase/$(Split-Path -Leaf $setupZip)"
        bytes  = (Get-Item $setupZip).Length
        sha256 = Get-Sha256 $setupZip
    }
    portable    = [ordered]@{
        url    = "$downloadBase/$(Split-Path -Leaf $portableZip)"
        bytes  = (Get-Item $portableZip).Length
        sha256 = Get-Sha256 $portableZip
    }
}

$manifestPath = Join-Path $dist 'latest.json'
$manifest | ConvertTo-Json -Depth 4 | Set-Content -Path $manifestPath -Encoding utf8

Write-Host "Wrote $manifestPath" -ForegroundColor Green
Write-Host "  setup    $($manifest.setup.sha256)"
Write-Host "  portable $($manifest.portable.sha256)"

if (-not $Publish) {
    Write-Host 'Nothing published. Re-run with -Publish when the notes are ready.' -ForegroundColor Yellow
    return
}

if (-not $NotesFile) { throw 'Publishing needs -NotesFile pointing at the release notes.' }
if (-not (Test-Path $NotesFile)) { throw "No notes at $NotesFile." }

Write-Host "Creating $tag in $DistributionRepo" -ForegroundColor Cyan
gh release create $tag $setupZip $portableZip --repo $DistributionRepo --title $version `
    --notes-file $NotesFile --prerelease
if ($LASTEXITCODE -ne 0) { throw 'Creating the release failed.' }

# The manifest has to land after the assets, or a reader could be sent to a download that is
# not there yet.
$clone = Join-Path $env:TEMP "ttsutilwin-releases-$([guid]::NewGuid().ToString('N'))"
gh repo clone $DistributionRepo $clone -- --depth 1
Copy-Item $manifestPath (Join-Path $clone 'latest.json') -Force
git -C $clone add latest.json
git -C $clone commit -m "chore(release): point the manifest at $version"
git -C $clone push
Remove-Item $clone -Recurse -Force

Write-Host "Published $version." -ForegroundColor Green
