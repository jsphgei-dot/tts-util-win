<#
.SYNOPSIS
    Packages a release and writes the manifest the program checks for updates.

.DESCRIPTION
    Builds the portable folder and the setup program, zips both, hashes them, and writes
    latest.json. With -Publish it also creates the GitHub release and pushes the manifest, which
    is what makes the update notice appear.

    The release goes to two repositories while copies built before 0.12.0 are still worth
    serving. Those ask the retired distribution repository for their manifest, so it gets the
    same tag, the same assets, and a manifest of its own naming its own copies. Pass an empty
    -MirrorRepo once that repository is archived, and the mirror step is skipped.

.EXAMPLE
    .\Publish.ps1

.EXAMPLE
    .\Publish.ps1 -Publish -SkipBuild -NotesFile dist\RELEASE_NOTES_v0.3.0-beta.md

.EXAMPLE
    .\Publish.ps1 -Publish -NotesFile dist\notes.md -CertThumbprint ABC123DEF456
#>
[CmdletBinding()]
param(
    [string]$Repo = 'jsphgei-dot/tts-util-win',

    [string]$MirrorRepo = 'jsphgei-dot/tts-util-win-releases',

    [ValidateSet('win-x64', 'win-arm64', 'win-x86')]
    [string]$Runtime = 'win-x64',

    [string]$NotesFile,

    [switch]$Publish,

    [switch]$SkipTests,

    [switch]$SkipBuild,

    [string]$CertThumbprint,

    [string]$TimestampUrl = 'http://timestamp.digicert.com'
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$dist = Join-Path $root 'dist'
$arch = $Runtime -replace '^win-', ''

# The version lives in one place, so read it rather than asking for it again here.
$props = Get-Content (Join-Path $root 'Directory.Build.props') -Raw
$prefix = [regex]::Match($props, '<VersionPrefix>([^<]+)</VersionPrefix>').Groups[1].Value
$suffix = [regex]::Match($props, '<VersionSuffix>([^<]+)</VersionSuffix>').Groups[1].Value
$code = [int][regex]::Match($props, '<VersionCode>([^<]+)</VersionCode>').Groups[1].Value
$version = if ($suffix) { "$prefix-$suffix" } else { $prefix }
$tag = "v$version"

# Checked before the build rather than after it, so a missing file costs a second, not a build.
if ($Publish) {
    if (-not $NotesFile) { throw 'Publishing needs -NotesFile pointing at the release notes.' }
    if (-not (Test-Path $NotesFile)) { throw "No notes at $NotesFile." }
}

Write-Host "Packaging $version $arch (version code $code)" -ForegroundColor Cyan
if (-not $CertThumbprint) {
    Write-Host 'No certificate given, so this release is unsigned and SmartScreen will warn.' -ForegroundColor Yellow
}

$portableZip = Join-Path $dist "TtsUtilWin-$version-$arch-portable.zip"
$setupExe = Join-Path $dist "TtsUtilWin-$version-$arch-setup.exe"
$setupZip = Join-Path $dist "TtsUtilWin-$version-$arch-setup.zip"

# The exe and the setup are both signed inside the build, before either is zipped, so the
# hashes written below belong to the signed files.
if ($SkipBuild) {
    # The setup program carries a build time, so a rebuild changes both hashes and the notes
    # written from the previous run stop matching. This publishes what is already in dist.
    foreach ($zip in @($portableZip, $setupZip)) {
        if (-not (Test-Path $zip)) { throw "-SkipBuild needs $zip, which is not there." }
    }
}
else {
    & (Join-Path $PSScriptRoot 'BuildPortable.ps1') -Runtime $Runtime -Installer -SkipTests:$SkipTests `
        -CertThumbprint $CertThumbprint -TimestampUrl $TimestampUrl
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

    if (-not (Test-Path $setupExe)) { throw "No setup program at $setupExe." }

    Remove-Item $portableZip, $setupZip -ErrorAction SilentlyContinue
    Compress-Archive -Path (Join-Path $dist "TtsUtilWin-$arch") -DestinationPath $portableZip
    Compress-Archive -Path $setupExe -DestinationPath $setupZip
}

function Get-Sha256([string]$path) { (Get-FileHash -Algorithm SHA256 -Path $path).Hash.ToLowerInvariant() }

# The program reads this file, and refuses any download whose hash does not match what is here.
# A repository gets a manifest naming its own copies of the assets, never the other one's.
function New-Manifest([string]$repo)
{
    $downloadBase = "https://github.com/$repo/releases/download/$tag"

    return [ordered]@{
        versionName = $version
        versionCode = $code
        releaseUrl  = "https://github.com/$repo/releases/tag/$tag"
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
}

$manifest = New-Manifest $Repo
$manifestPath = Join-Path $dist 'latest.json'
$manifest | ConvertTo-Json -Depth 4 | Set-Content -Path $manifestPath -Encoding utf8

Write-Host "Wrote $manifestPath" -ForegroundColor Green
Write-Host "  setup    $($manifest.setup.sha256)"
Write-Host "  portable $($manifest.portable.sha256)"

if (-not $Publish) {
    Write-Host 'Nothing published. Re-run with -Publish when the notes are ready.' -ForegroundColor Yellow
    return
}

# A build the notes were not written from is caught here rather than on the release page.
$notes = Get-Content $NotesFile -Raw
foreach ($hash in @($manifest.setup.sha256, $manifest.portable.sha256)) {
    if ($notes -notlike "*$hash*") {
        throw "The notes do not carry $hash. Copy the hashes above into $NotesFile, then re-run with -SkipBuild."
    }
}

function Send-Assets([string]$repo)
{
    Write-Host "Creating $tag in $repo" -ForegroundColor Cyan
    # A second architecture lands on the release the first one made rather than failing on it.
    gh release view $tag --repo $repo *> $null
    if ($LASTEXITCODE -eq 0) {
        gh release upload $tag $setupZip $portableZip --repo $repo --clobber
        if ($LASTEXITCODE -ne 0) { throw "Uploading to the existing release in $repo failed." }
    }
    else {
        gh release create $tag $setupZip $portableZip --repo $repo --title "TTS Util Win $version" `
            --notes-file $NotesFile --prerelease
        if ($LASTEXITCODE -ne 0) { throw "Creating the release in $repo failed." }
    }
}

# The manifest lands after the assets, or a reader could be sent to a download that is not
# there yet.
function Send-Manifest([string]$repo, $body)
{
    $clone = Join-Path $env:TEMP "ttsutilwin-manifest-$([guid]::NewGuid().ToString('N'))"
    gh repo clone $repo $clone -- --depth 1
    if ($LASTEXITCODE -ne 0) { throw "Cloning $repo failed." }

    # A machine with no global identity still has one here, and the clone is told about it.
    $name = git -C $root config user.name
    $mail = git -C $root config user.email
    if ($name) { git -C $clone config user.name $name }
    if ($mail) { git -C $clone config user.email $mail }

    $body | ConvertTo-Json -Depth 4 | Set-Content -Path (Join-Path $clone 'latest.json') -Encoding utf8
    git -C $clone add latest.json
    git -C $clone commit -m "chore(release): point the manifest at $version"
    if ($LASTEXITCODE -ne 0) { throw "Committing the manifest in $repo failed." }
    git -C $clone push
    if ($LASTEXITCODE -ne 0) { throw "Pushing the manifest to $repo failed, so the release is not offered yet." }
    Remove-Item $clone -Recurse -Force
}

Send-Assets $Repo
if ($MirrorRepo) { Send-Assets $MirrorRepo }

# The manifest names one download per release, so only the x64 build writes it. Publishing
# another architecture would otherwise point every running copy at a build it cannot run.
if ($arch -ne 'x64') {
    Write-Host "Assets uploaded. The manifest still points at x64, which is what $arch cannot change yet." -ForegroundColor Yellow
    return
}

Send-Manifest $Repo $manifest
if ($MirrorRepo) { Send-Manifest $MirrorRepo (New-Manifest $MirrorRepo) }

Write-Host "Published $version." -ForegroundColor Green
