<#
.SYNOPSIS
    Points git at the hooks kept in .githooks.

.DESCRIPTION
    Run once per clone. Sets core.hooksPath, so the hooks travel with the repository
    instead of living in .git\hooks where they cannot be committed.

.EXAMPLE
    .\InstallHooks.ps1

.EXAMPLE
    .\InstallHooks.ps1 -Uninstall
#>
param([switch]$Uninstall)

$ErrorActionPreference = 'Stop'

$root = (git rev-parse --show-toplevel).Trim()

if ($Uninstall)
{
    git -C $root config --unset core.hooksPath
    Write-Host 'Hooks disabled. git now uses .git\hooks.' -ForegroundColor Yellow
    return
}

git -C $root config core.hooksPath .githooks

Write-Host 'Hooks installed:' -ForegroundColor Green
Write-Host '  pre-commit  whitespace, file sizes, stray build output, dotnet format, warning free build, core tests'
Write-Host '  commit-msg  Conventional Commits subject, 200 word body, no tool attribution'
Write-Host '  pre-push    the full test suite'
Write-Host ''
Write-Host 'Bypass one commit with --no-verify, or a session with $env:SKIP_HOOKS = 1.' -ForegroundColor DarkGray
