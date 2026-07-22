param(
  [Parameter(Mandatory = $true)]
  [string]$NpmScript
)

$ErrorActionPreference = "Continue"

try {
  chcp.com 65001 | Out-Null
  [Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
  [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
  $OutputEncoding = [System.Text.UTF8Encoding]::new($false)
}
catch {
  # Keep running even if the host does not allow code page changes.
}

$utf8NoBom = [System.Text.UTF8Encoding]::new($false)

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$outputDir = Join-Path $repoRoot "cypress\CypressTestOutput"
$outputFile = Join-Path $outputDir "CypressOutput.txt"

Set-Location $repoRoot

New-Item -ItemType Directory -Force $outputDir | Out-Null

$header = @"
==================== $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') npm.cmd run $NpmScript ====================

"@

[System.IO.File]::WriteAllText($outputFile, $header, $utf8NoBom)

$cmd = "chcp 65001 >NUL && npm.cmd run $NpmScript 2>&1"

# Intentionally append each line as it arrives so the log file always contains
# the latest streamed Cypress output, even if the process is interrupted.
& cmd.exe /d /s /c $cmd | ForEach-Object {
  $line = [string]$_

  Write-Host $line

  [System.IO.File]::AppendAllText(
    $outputFile,
    $line + [Environment]::NewLine,
    $utf8NoBom
  )
}

$exitCode = $LASTEXITCODE

$footer = @"

==================== Exit code: $exitCode ====================

"@

Write-Host $footer

[System.IO.File]::AppendAllText($outputFile, $footer, $utf8NoBom)

exit $exitCode