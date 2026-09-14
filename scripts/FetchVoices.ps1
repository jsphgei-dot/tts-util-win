<#
.SYNOPSIS
    Downloads sherpa voice models into the voices directory.

.DESCRIPTION
    Every voice listed here is published by the k2-fsa sherpa-onnx project and
    carries a licence that permits free redistribution. The authoritative
    licence ships inside each model folder as LICENSE or MODEL_CARD; this
    script prints it after extraction.

.EXAMPLE
    .\FetchVoices.ps1 -List

.EXAMPLE
    .\FetchVoices.ps1 -Default

.EXAMPLE
    .\FetchVoices.ps1 -Name kokoro-en-v0_19
#>
[CmdletBinding(DefaultParameterSetName = 'Default')]
param(
    [Parameter(ParameterSetName = 'List')]
    [switch]$List,

    [Parameter(ParameterSetName = 'Default')]
    [switch]$Default,

    [Parameter(ParameterSetName = 'Name')]
    [string[]]$Name,

    [Parameter(ParameterSetName = 'All')]
    [switch]$All,

    [string]$Destination,

    [switch]$Force
)

$ErrorActionPreference = 'Stop'

$baseUrl = 'https://github.com/k2-fsa/sherpa-onnx/releases/download/tts-models'

$catalog = @(
    [pscustomobject]@{
        Id       = 'vits-piper-en_US-ljspeech-high'
        Language = 'English (US)'
        Kind     = 'vits'
        SizeMb   = 110
        Licence  = 'LJ Speech data set, public domain'
        Default  = $true
    }
    [pscustomobject]@{
        Id       = 'vits-piper-en_US-libritts_r-medium'
        Language = 'English (US), 900+ speakers'
        Kind     = 'vits'
        SizeMb   = 78
        Licence  = 'LibriTTS-R, CC BY 4.0'
        Default  = $true
    }
    [pscustomobject]@{
        Id       = 'kokoro-en-v0_19'
        Language = 'English, 11 voices'
        Kind     = 'kokoro'
        SizeMb   = 305
        Licence  = 'Apache 2.0'
        Default  = $true
    }
    [pscustomobject]@{
        Id       = 'vits-piper-en_GB-alan-medium'
        Language = 'English (GB)'
        Kind     = 'vits'
        SizeMb   = 64
        Licence  = 'See MODEL_CARD inside the folder'
        Default  = $false
    }
    [pscustomobject]@{
        Id       = 'vits-piper-en_US-amy-low'
        Language = 'English (US), small and fast'
        Kind     = 'vits'
        SizeMb   = 64
        Licence  = 'See MODEL_CARD inside the folder'
        Default  = $false
    }
    [pscustomobject]@{
        Id       = 'vits-piper-en_US-lessac-medium'
        Language = 'English (US)'
        Kind     = 'vits'
        SizeMb   = 64
        Licence  = 'See MODEL_CARD inside the folder'
        Default  = $false
    }
)

if ($List) {
    $catalog | Format-Table Id, Language, Kind, SizeMb, Licence, Default -AutoSize
    return
}

if (-not $Destination) {
    $Destination = Join-Path (Split-Path -Parent $PSScriptRoot) 'voices'
}

New-Item -ItemType Directory -Force -Path $Destination | Out-Null

$selected = switch ($PSCmdlet.ParameterSetName) {
    'All'  { $catalog }
    'Name' { $catalog | Where-Object { $Name -contains $_.Id } }
    default { $catalog | Where-Object { $_.Default } }
}

if (-not $selected) {
    Write-Error "No voice matched. Run with -List to see the catalogue."
    return
}

$total = ($selected | Measure-Object -Property SizeMb -Sum).Sum
Write-Host "Downloading $($selected.Count) voice(s), about $total MB, into $Destination" -ForegroundColor Cyan

foreach ($voice in $selected) {
    $targetDir = Join-Path $Destination $voice.Id

    if ((Test-Path $targetDir) -and -not $Force) {
        Write-Host "  $($voice.Id): already present, skipping." -ForegroundColor DarkGray
        continue
    }

    if (Test-Path $targetDir) { Remove-Item -Recurse -Force $targetDir }

    $archive = Join-Path ([System.IO.Path]::GetTempPath()) "$($voice.Id).tar.bz2"
    $url = "$baseUrl/$($voice.Id).tar.bz2"

    Write-Host "  $($voice.Id): downloading $($voice.SizeMb) MB" -ForegroundColor Cyan
    $previous = $ProgressPreference
    $ProgressPreference = 'SilentlyContinue'
    try {
        Invoke-WebRequest -Uri $url -OutFile $archive -UseBasicParsing
    }
    finally {
        $ProgressPreference = $previous
    }

    Write-Host "  $($voice.Id): extracting" -ForegroundColor Cyan
    tar -xf $archive -C $Destination
    if ($LASTEXITCODE -ne 0) { throw "tar failed for $($voice.Id)." }
    Remove-Item $archive -Force

    if (-not (Test-Path (Join-Path $targetDir 'tokens.txt'))) {
        Write-Warning "  $($voice.Id): no tokens.txt found; the app may not detect this voice."
    }

    Write-Host "  $($voice.Id): installed. Licence: $($voice.Licence)" -ForegroundColor Green

    $licenceFile = Get-ChildItem -Path $targetDir -Filter 'LICENSE*' -File -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($licenceFile) {
        Write-Host "    Bundled licence: $($licenceFile.FullName)" -ForegroundColor DarkGray
    }
}

Write-Host "Done. Start TtsUtilWin.exe and press Rescan if it is already running." -ForegroundColor Cyan
