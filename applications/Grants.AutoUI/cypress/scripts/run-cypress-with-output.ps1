param(
  [Parameter(Mandatory = $true)]
  [string]$NpmScript
)

$ErrorActionPreference = "Continue"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$outputDir = Join-Path $repoRoot "cypress\CypressTestOutput"
$outputFile = Join-Path $outputDir "CypressOutput.txt"

Set-Location $repoRoot

New-Item -ItemType Directory -Force $outputDir | Out-Null

$header = @"

==================== $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') npm.cmd run $NpmScript ====================

"@

Set-Content -Path $outputFile -Value $header -Encoding UTF8

& npm.cmd run $NpmScript 2>&1 |
  Tee-Object -FilePath $outputFile -Append

$exitCode = $LASTEXITCODE

@"

==================== Exit code: $exitCode ====================

"@ | Tee-Object -FilePath $outputFile -Append

exit $exitCode