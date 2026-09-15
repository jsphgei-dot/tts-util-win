<#
.SYNOPSIS
    Runs the installer's version comparison tests.

.DESCRIPTION
    Compiles installer\VersionTests.iss, which includes the same Version.iss the real
    installer uses, runs it silently and reports what it found. Exits non zero on a
    failure, so it can gate a build.

.EXAMPLE
    .\TestInstaller.ps1
#>
param([string]$InnoSetupPath)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$installerDir = Join-Path $root 'installer'

if (-not $InnoSetupPath)
{
    $candidates = @(
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'),
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
        (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe')
    )

    $InnoSetupPath = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if (-not $InnoSetupPath)
{
    Write-Error 'ISCC.exe not found. Install Inno Setup 6 or pass -InnoSetupPath.'
}

$work = Join-Path ([System.IO.Path]::GetTempPath()) ("ttsutil-instest-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work | Out-Null

try
{
    $compile = & $InnoSetupPath "/O$work" (Join-Path $installerDir 'VersionTests.iss') 2>&1

    if ($LASTEXITCODE -ne 0)
    {
        $compile | Select-Object -Last 20 | ForEach-Object { Write-Host $_ }
        Write-Error 'The test script did not compile.'
    }

    $report = Join-Path $work 'report.txt'
    $exe = Join-Path $work 'VersionTests.exe'

    & $exe /VERYSILENT "/report=$report" | Out-Null

    if (-not (Test-Path $report))
    {
        Write-Error 'The test run produced no report.'
    }

    $lines = Get-Content $report

    foreach ($line in $lines)
    {
        $colour = if ($line -like 'FAIL*') { 'Red' } elseif ($line -like 'pass*') { 'DarkGray' } else { 'White' }
        Write-Host "  $line" -ForegroundColor $colour
    }

    if (($lines | Where-Object { $_ -like 'FAIL*' }).Count -gt 0)
    {
        Write-Host 'Installer version tests failed.' -ForegroundColor Red
        $script:ExitCode = 1
    }
    else
    {
        Write-Host "Installer version tests passed ($(($lines | Where-Object { $_ -like 'pass*' }).Count) cases)." -ForegroundColor Green
        $script:ExitCode = 0
    }
}
finally
{
    Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue
}

exit $script:ExitCode
