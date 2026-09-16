<#
.SYNOPSIS
    Authenticode signing, shared by the build and publish scripts.

.DESCRIPTION
    Dot source this file to get Invoke-CodeSign. Nothing is signed unless a certificate
    thumbprint is passed in, so a machine with no certificate builds exactly as before.

.EXAMPLE
    . .\CodeSigning.ps1
    Invoke-CodeSign -Path dist\TtsUtilWin\TtsUtilWin.exe -Thumbprint ABC123...
#>

function Get-SignTool {
    $found = (Get-Command signtool.exe -ErrorAction SilentlyContinue).Source
    if ($found) { return $found }

    # signtool ships with the Windows SDK, under a folder named after the SDK version, so take
    # the newest x64 copy rather than guessing a version number.
    $kits = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\bin'
    if (Test-Path $kits) {
        $candidate = Get-ChildItem -Path $kits -Filter 'signtool.exe' -Recurse -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -match 'x64' } |
            Sort-Object FullName -Descending |
            Select-Object -First 1
        if ($candidate) { return $candidate.FullName }
    }

    return $null
}

function Invoke-CodeSign {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string[]]$Path,

        [string]$Thumbprint,

        [string]$TimestampUrl = 'http://timestamp.digicert.com'
    )

    if (-not $Thumbprint) { return }

    $signtool = Get-SignTool
    if (-not $signtool) {
        throw 'signtool.exe was not found. Install the Windows SDK, or drop -CertThumbprint to build unsigned.'
    }

    foreach ($file in $Path) {
        if (-not (Test-Path $file)) { throw "Nothing to sign at $file." }

        Write-Host "Signing $file" -ForegroundColor Cyan

        # The timestamp is what keeps the signature valid after the certificate expires.
        & $signtool sign /fd SHA256 /td SHA256 /tr $TimestampUrl /sha1 $Thumbprint $file
        if ($LASTEXITCODE -ne 0) { throw "Signing $file failed." }

        & $signtool verify /pa /q $file
        if ($LASTEXITCODE -ne 0) { throw "$file did not verify after signing." }
    }
}
