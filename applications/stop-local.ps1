param(
    [ValidateRange(1, 65535)]
    [int[]]$Ports = @(4200, 7000)
)

$ErrorActionPreference = "Stop"
$applicationDirectory = Join-Path $PSScriptRoot "Grants.ApplicantPortal"
$frontendDirectory = Join-Path $applicationDirectory "src\Grants.ApplicantPortal.Frontend"
$backendDirectory = Join-Path $applicationDirectory "src\Grants.ApplicantPortal.Backend"
$processes = @(Get-CimInstance Win32_Process)
$processesById = @{}
foreach ($process in $processes) {
    $processesById[[int]$process.ProcessId] = $process
}

$protectedIds = [System.Collections.Generic.HashSet[int]]::new()
$ancestorId = $PID
while ($ancestorId -gt 0 -and $protectedIds.Add($ancestorId)) {
    $ancestor = $processesById[$ancestorId]
    if (-not $ancestor) { break }
    $ancestorId = [int]$ancestor.ParentProcessId
}

$targetIds = [System.Collections.Generic.HashSet[int]]::new()
$stopOrder = [System.Collections.Generic.List[int]]::new()
$serviceProcessNames = @(
    "node.exe", "dotnet.exe", "Grants.ApplicantPortal.API.Web.exe",
    "esbuild.exe", "powershell.exe", "pwsh.exe", "cmd.exe"
)
foreach ($process in $processes) {
    $processId = [int]$process.ProcessId
    if ($protectedIds.Contains($processId) -or $process.Name -notin $serviceProcessNames) {
        continue
    }

    $processDetails = "$($process.CommandLine) $($process.ExecutablePath)"
    $isPortalProcess = $processDetails.IndexOf($frontendDirectory, [StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $processDetails.IndexOf($backendDirectory, [StringComparison]::OrdinalIgnoreCase) -ge 0
    if ($isPortalProcess -and $targetIds.Add($processId)) {
        $stopOrder.Add($processId)
    }
}

$listeners = @(Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue)
foreach ($listener in $listeners) {
    $processId = [int]$listener.OwningProcess
    if ($listener.LocalPort -in $Ports -and -not $protectedIds.Contains($processId) -and $processId -gt 4) {
        if ($targetIds.Add($processId)) { $stopOrder.Add($processId) }
    }
}

do {
    $addedChild = $false
    foreach ($process in $processes) {
        $processId = [int]$process.ProcessId
        if ($targetIds.Contains([int]$process.ParentProcessId) -and
            -not $protectedIds.Contains($processId) -and $targetIds.Add($processId)) {
            $stopOrder.Add($processId)
            $addedChild = $true
        }
    }
} while ($addedChild)

$checkedPorts = @($Ports) + @(
    $listeners | Where-Object { $targetIds.Contains([int]$_.OwningProcess) } |
        Select-Object -ExpandProperty LocalPort
)
$checkedPorts = @($checkedPorts | Sort-Object -Unique)
Write-Host "Checking portal ports: $($checkedPorts -join ', ')" -ForegroundColor Cyan

$failedIds = [System.Collections.Generic.List[int]]::new()
for ($index = $stopOrder.Count - 1; $index -ge 0; $index--) {
    $processId = $stopOrder[$index]
    $process = Get-Process -Id $processId -ErrorAction SilentlyContinue
    if (-not $process) { continue }

    Write-Host "Stopping $($process.ProcessName) (PID $processId)..." -ForegroundColor Yellow
    try {
        try {
            $killOutput = & taskkill.exe /PID $processId /T /F 2>&1
            Write-Verbose ($killOutput -join [Environment]::NewLine)
        }
        catch {
            Write-Verbose "Process-tree kill failed for PID ${processId}: $($_.Exception.Message)"
        }

        if (Get-Process -Id $processId -ErrorAction SilentlyContinue) {
            Stop-Process -Id $processId -Force -ErrorAction Stop
        }
    }
    catch {
        if (Get-Process -Id $processId -ErrorAction SilentlyContinue) {
            $failedIds.Add($processId)
            Write-Warning "Could not stop PID ${processId}: $($_.Exception.Message)"
        }
    }
}

$remainingListeners = @(Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue |
    Where-Object { $_.LocalPort -in $checkedPorts })
$remainingIds = @($targetIds | Where-Object { Get-Process -Id $_ -ErrorAction SilentlyContinue })
if ($remainingListeners.Count -gt 0 -or $remainingIds.Count -gt 0 -or $failedIds.Count -gt 0) {
    throw "Some services could not be stopped. Listening ports: $($remainingListeners.LocalPort -join ', '); remaining PIDs: $($remainingIds -join ', '); failed PIDs: $($failedIds -join ', ')."
}

$outputDirectory = Join-Path $backendDirectory "src\Grants.ApplicantPortal.API.Web\bin"
$dlls = @()
if (Test-Path -LiteralPath $outputDirectory) {
    $dlls = @(Get-ChildItem -LiteralPath $outputDirectory -Filter "Grants.ApplicantPortal*.dll" -File -Recurse)
}

$lockedDlls = [System.Collections.Generic.List[string]]::new()
foreach ($dll in $dlls) {
    $stream = $null
    try {
        $stream = [System.IO.File]::Open(
            $dll.FullName, [System.IO.FileMode]::Open,
            [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
        Write-Host "Unlocked: $($dll.FullName)" -ForegroundColor Cyan
    }
    catch {
        $lockedDlls.Add($dll.FullName)
        Write-Warning "DLL is still locked or inaccessible: $($dll.FullName). $($_.Exception.Message)"
    }
    finally {
        if ($stream) { $stream.Dispose() }
    }
}

if ($lockedDlls.Count -gt 0) {
    throw "Backend DLLs are still locked or inaccessible. Do not rebuild yet: $($lockedDlls -join ', ')."
}
if ($dlls.Count -eq 0) {
    Write-Host "No backend output DLLs found; nothing to check for file locks." -ForegroundColor Cyan
}

Write-Host "Local frontend and backend services are stopped; backend DLL checks passed." -ForegroundColor Green