# Shared helpers for the git hooks in this folder.

$ErrorActionPreference = 'Stop'

$script:RepoRoot = (git rev-parse --show-toplevel).Trim()
$script:Solution = Join-Path $RepoRoot 'TtsUtilWin.sln'
$script:Failures = @()

function Write-Step([string]$Message)
{
    Write-Host "  $Message" -ForegroundColor Cyan
}

function Add-Failure([string]$Message)
{
    $script:Failures += $Message
    Write-Host "  FAIL  $Message" -ForegroundColor Red
}

function Write-Pass([string]$Message)
{
    Write-Host "  ok    $Message" -ForegroundColor DarkGray
}

function Exit-WithFailures([string]$HookName, [string]$Bypass)
{
    if ($script:Failures.Count -eq 0)
    {
        Write-Host "$HookName passed." -ForegroundColor Green
        exit 0
    }

    Write-Host ''
    Write-Host "$HookName found $($script:Failures.Count) problem(s):" -ForegroundColor Red

    foreach ($failure in $script:Failures)
    {
        Write-Host "  * $failure" -ForegroundColor Red
    }

    Write-Host ''
    Write-Host "Fix them, or bypass with $Bypass." -ForegroundColor Yellow
    exit 1
}

function Get-StagedFiles
{
    $files = git diff --cached --name-only --diff-filter=ACM
    if (-not $files) { return @() }
    return @($files)
}

function Test-HooksSkipped([string]$HookName)
{
    if ($env:SKIP_HOOKS -eq '1')
    {
        Write-Host "$HookName skipped (SKIP_HOOKS=1)." -ForegroundColor Yellow
        return $true
    }

    return $false
}
