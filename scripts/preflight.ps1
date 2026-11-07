# Read-only tooling audit. Run from PowerShell: ./scripts/preflight.ps1
$ErrorActionPreference = 'Continue'
$missing = [System.Collections.Generic.List[string]]::new()

function Show-Version {
    param([string]$Name, [string[]]$Arguments)
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        Write-Host "MISSING: $Name"
        $missing.Add($Name)
        return
    }
    Write-Host "CHECK: $Name"
    & $Name @Arguments
    if ($LASTEXITCODE -ne 0) { Write-Host "Could not query $Name successfully." }
}

Show-Version -Name 'git' -Arguments @('--version')
Show-Version -Name 'dotnet' -Arguments @('--list-sdks')
Show-Version -Name 'node' -Arguments @('--version')
Show-Version -Name 'npm' -Arguments @('--version')
Show-Version -Name 'docker' -Arguments @('--version')

if (Get-Command dotnet -ErrorAction SilentlyContinue) {
    $sdkLines = @(& dotnet --list-sdks)
    if (-not ($sdkLines | Where-Object { $_ -match '^10\.' })) {
        Write-Host 'ACTION: Install a .NET 10 SDK (a runtime alone is insufficient).'
    }
}

if (Get-Command node -ErrorAction SilentlyContinue) {
    $nodeText = (& node --version).Trim()
    if ($nodeText -match '^v(\d+)\.(\d+)\.(\d+)') {
        if ([int]$Matches[1] -ne 24 -or [int]$Matches[2] -lt 15) {
            Write-Host 'ACTION: This kit targets Node 24 LTS >=24.15.0 for Angular 22.'
        }
    }
}

if (Get-Command docker -ErrorAction SilentlyContinue) {
    Write-Host 'CHECK: Docker Compose'
    & docker compose version
    Write-Host 'CHECK: Docker engine OS (expected linux)'
    & docker info --format '{{.OSType}}'
    if ($LASTEXITCODE -eq 0) {
        Write-Host 'CHECK: Running SQL Server Compose services (status only)'
        & docker ps --filter 'label=com.docker.compose.service=sqlserver' --format '{{.Names}}: {{.Status}}'
    } else {
        Write-Host 'ACTION: Start Docker Desktop with its Linux/WSL2 backend.'
    }
}

Write-Host 'No tools were installed and no repository files or secrets were read.'
Write-Host 'Next: compare output with START-HERE.txt; complete missing prerequisites.'
if ($missing.Count -gt 0) { exit 1 }
