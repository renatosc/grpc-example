# Start Multiple gRPC Servers for Load Balancing Demo
# This script starts 3 C# gRPC server instances on different ports

Write-Host "Starting gRPC Load Balancing Demo Servers" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green

$currentDir = Get-Location
$serverPath = Join-Path $currentDir "csharp-server\GrpcServer"
$serverExe = Join-Path $serverPath "bin\Debug\net9.0\GrpcServer.exe"
$ports = @(7001, 7002, 7003)

# Check if server project exists
if (-not (Test-Path $serverPath)) {
    Write-Host "ERROR: Server project not found at: $serverPath" -ForegroundColor Red
    Write-Host "Please run this script from the gRPC-Example root directory" -ForegroundColor Yellow
    exit 1
}

# Build the server first
Write-Host "Building server project..." -ForegroundColor Cyan
Push-Location $serverPath
try {
    $buildResult = dotnet build --configuration Debug
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Failed to build server project" -ForegroundColor Red
        exit 1
    }
    Write-Host "SUCCESS: Server built successfully" -ForegroundColor Green
} finally {
    Pop-Location
}

# Check if executable exists
if (-not (Test-Path $serverExe)) {
    Write-Host "ERROR: Server executable not found at: $serverExe" -ForegroundColor Red
    exit 1
}

# Clean up any existing processes
Write-Host "Cleaning up existing server processes..." -ForegroundColor Yellow
Get-Process -Name "GrpcServer" -ErrorAction SilentlyContinue | Stop-Process -Force

# Clean up any existing jobs
$existingJobs = Get-Job | Where-Object { $_.Name -like "GrpcServer-*" }
if ($existingJobs) {
    Write-Host "Cleaning up existing server jobs..." -ForegroundColor Yellow
    $existingJobs | Stop-Job -PassThru | Remove-Job
}

# Start each server instance using the executable directly
foreach ($port in $ports) {
    $serverUrl = "https://localhost:$port"
    Write-Host "Starting server on $serverUrl..." -ForegroundColor Yellow
    
    # Start server using the executable directly with environment variable
    $job = Start-Job -ScriptBlock {
        param($serverExe, $serverUrl, $serverPath)
        Set-Location $serverPath
        $env:ASPNETCORE_URLS = $serverUrl
        & $serverExe
    } -ArgumentList $serverExe, $serverUrl, $serverPath -Name "GrpcServer-$port"
    
    Write-Host "SUCCESS: Server started on port $port (Job ID: $($job.Id))" -ForegroundColor Green
    
    # Small delay between server starts
    Start-Sleep -Seconds 2
}

Write-Host ""
Write-Host "All servers starting up..." -ForegroundColor Cyan

Write-Host ""
Write-Host "Server Status:" -ForegroundColor Magenta
$jobs = Get-Job | Where-Object { $_.Name -like "GrpcServer-*" }
$runningCount = 0
foreach ($job in $jobs) {
    $port = $job.Name.Split('-')[1]
    $status = $job.State
    $color = if ($status -eq "Running") { "Green" } else { "Red" }
    Write-Host "   Port $port : $status" -ForegroundColor $color
    
    if ($status -eq "Running") {
        $runningCount++
    } else {
        # Show any error output if job failed
        $output = Receive-Job -Job $job -ErrorAction SilentlyContinue
        if ($output) {
            Write-Host "     Error: $($output -join ' ' | Out-String)" -ForegroundColor Gray
        }
    }
}

Write-Host ""
if ($runningCount -gt 0) {
    Write-Host "SUCCESS: $runningCount server(s) are running!" -ForegroundColor Green
    Write-Host ""
    Write-Host "To test the servers:" -ForegroundColor Cyan
    Write-Host "   .\test-servers.ps1" -ForegroundColor White
    Write-Host ""
    Write-Host "To run the client demo:" -ForegroundColor Cyan
    Write-Host "   dotnet run --project .\csharp-client\GrpcClient\GrpcClient.csproj" -ForegroundColor White
} else {
    Write-Host "ERROR: No servers are running!" -ForegroundColor Red
}

Write-Host ""
Write-Host "To stop all servers:" -ForegroundColor Yellow
Write-Host "   .\stop-servers.ps1" -ForegroundColor White
Write-Host ""
