# Checks the commit message: Conventional Commits subject, 200 word body, no tool attribution.

param([Parameter(Mandatory = $true)][string]$MessagePath)

. (Join-Path $PSScriptRoot 'common.ps1')

if (Test-HooksSkipped 'commit-msg') { exit 0 }

$lines = @(Get-Content -LiteralPath $MessagePath -Encoding UTF8)
$lines = @($lines | Where-Object { $_ -notmatch '^\s*#' })

while ($lines.Count -gt 0 -and [string]::IsNullOrWhiteSpace($lines[0]))
{
    $lines = $lines[1..($lines.Count - 1)]
}

if ($lines.Count -eq 0)
{
    Write-Host 'Empty commit message.' -ForegroundColor Red
    exit 1
}

$subject = $lines[0]

if ($subject -match '^(Merge |Revert "|fixup!|squash!)')
{
    exit 0
}

Write-Host 'commit-msg' -ForegroundColor White

$types = 'feat|fix|docs|test|refactor|perf|build|ci|chore|style|revert'

if ($subject -cnotmatch "^($types)(\([a-z0-9._\-]+\))?!?: .+")
{
    Add-Failure "Subject must be 'type(scope): summary', all lowercase. Types: $($types -replace '\|', ', ')."
}
elseif ($subject -cmatch "^($types)(\([a-z0-9._\-]+\))?!?: [A-Z]")
{
    Add-Failure 'Summary starts with a capital. Use lowercase imperative mood.'
}
elseif ($subject.TrimEnd().EndsWith('.'))
{
    Add-Failure 'Summary ends with a period. Drop it.'
}
else
{
    Write-Pass 'subject follows Conventional Commits'
}

if ($subject.Length -gt 72)
{
    Add-Failure "Subject is $($subject.Length) characters. Keep it to 72."
}

if ($lines.Count -gt 1 -and -not [string]::IsNullOrWhiteSpace($lines[1]))
{
    Add-Failure 'Put a blank line between the subject and the body.'
}

$body = if ($lines.Count -gt 2) { $lines[2..($lines.Count - 1)] -join ' ' } else { '' }
$words = @($body -split '\s+' | Where-Object { $_ -ne '' })

if ($words.Count -gt 200)
{
    Add-Failure "Body is $($words.Count) words. Keep it to 200; detail belongs in the code or the docs."
}
elseif ($words.Count -gt 0)
{
    Write-Pass "body is $($words.Count) words"
}

$attribution = @($lines | Where-Object { $_ -match '(?i)(co-authored-by:.*claude|generated with \[?claude)' })

if ($attribution.Count -gt 0)
{
    Add-Failure 'Remove the tool attribution line. Commits are authored by you alone.'
}

Exit-WithFailures 'commit-msg' 'git commit --no-verify'
