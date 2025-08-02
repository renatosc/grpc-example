# Start Multiple gRPC Servers for Load Balancing Demo
# This script starts gRPC server instances on different ports (both C# and Java)

param(
    [string]$ServerType = "mixed",  # "csharp", "java", or "mixed"
    [int[]]$Ports = @(7001, 7002, 7003, 7011)
)

Write-Host "Starting gRPC Load Balancing Demo Servers" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green
Write-Host "Server Type: $ServerType" -ForegroundColor Cyan
Write-Host "Ports: $($Ports -join ', ')" -ForegroundColor Cyan
Write-Host ""

$currentDir = Get-Location

# Server paths
$csharpServerPath = Join-Path $currentDir "csharp-server\GrpcServer"
$csharpServerExe = Join-Path $csharpServerPath "bin\Debug\net9.0\GrpcServer.exe"
$javaServerPath = Join-Path $currentDir "java-server"
$javaServerJar = Join-Path $javaServerPath "target\java-server-1.0.0.jar"

# Check if server projects exist
if ($ServerType -eq "csharp" -or $ServerType -eq "mixed") {
    if (-not (Test-Path $csharpServerPath)) {
        Write-Host "ERROR: C# server project not found at: $csharpServerPath" -ForegroundColor Red
        Write-Host "Please run this script from the gRPC-Example root directory" -ForegroundColor Yellow
        exit 1
    }
}

if ($ServerType -eq "java" -or $ServerType -eq "mixed") {
    if (-not (Test-Path $javaServerJar)) {
        Write-Host "ERROR: Java server JAR not found at: $javaServerJar" -ForegroundColor Red
        Write-Host "Please build the Java server first with 'mvn clean package'" -ForegroundColor Yellow
        exit 1
    }
}

# Build C# server if needed
if ($ServerType -eq "csharp" -or $ServerType -eq "mixed") {
    Write-Host "Building C# server project..." -ForegroundColor Cyan
    Push-Location $csharpServerPath
    try {
        $buildResult = dotnet build --configuration Debug
        if ($LASTEXITCODE -ne 0) {
            Write-Host "ERROR: Failed to build C# server project" -ForegroundColor Red
            exit 1
        }
        Write-Host "SUCCESS: C# server built successfully" -ForegroundColor Green
    } finally {
        Pop-Location
    }

    # Check if executable exists
    if (-not (Test-Path $csharpServerExe)) {
        Write-Host "ERROR: C# server executable not found at: $csharpServerExe" -ForegroundColor Red
        exit 1
    }
}

# Clean up any existing processes
Write-Host "Cleaning up existing server processes..." -ForegroundColor Yellow
Get-Process -Name "GrpcServer" -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process -Name "java" -ErrorAction SilentlyContinue | Where-Object { $_.CommandLine -like "*java-server*" } | Stop-Process -Force

# Clean up any existing jobs
$existingJobs = Get-Job | Where-Object { $_.Name -like "GrpcServer-*" -or $_.Name -like "JavaServer-*" }
if ($existingJobs) {
    Write-Host "Cleaning up existing server jobs..." -ForegroundColor Yellow
    $existingJobs | Stop-Job -PassThru | Remove-Job
}

# Start servers based on configuration
foreach ($port in $Ports) {
    if ($ServerType -eq "mixed") {
        # For mixed mode, alternate between C# and Java
        # C# on 7001, 7002, 7003 and Java on 7011
        if ($port -eq 7011) {
            $useJava = $true
        } else {
            $useJava = $false
        }
    } elseif ($ServerType -eq "java") {
        $useJava = $true
    } else {
        $useJava = $false
    }

    if ($useJava) {
        # Start Java server
        Write-Host "Starting Java server on port $port..." -ForegroundColor Yellow
        
        $job = Start-Job -ScriptBlock {
            param($javaServerJar, $port, $javaServerPath)
            Set-Location $javaServerPath
            java -jar $javaServerJar $port
        } -ArgumentList $javaServerJar, $port, $javaServerPath -Name "JavaServer-$port"
        
        Write-Host "SUCCESS: Java server started on port $port (Job ID: $($job.Id))" -ForegroundColor Green    } else {
        # Start C# server
        Write-Host "Starting C# server on port $port (HTTP/2 plaintext for cross-language compatibility)..." -ForegroundColor Yellow
        
        $job = Start-Job -ScriptBlock {
            param($csharpServerExe, $port, $csharpServerPath)
            Set-Location $csharpServerPath
            $env:GRPC_PORT = $port
            & $csharpServerExe $port
        } -ArgumentList $csharpServerExe, $port, $csharpServerPath -Name "GrpcServer-$port"
        
        Write-Host "SUCCESS: C# server started on port $port (Job ID: $($job.Id))" -ForegroundColor Green
    }
    
    # Small delay between server starts
    Start-Sleep -Seconds 2
}

Write-Host ""
Write-Host "All servers starting up..." -ForegroundColor Cyan

Write-Host ""
Write-Host "Server Status:" -ForegroundColor Magenta
$jobs = Get-Job | Where-Object { $_.Name -like "GrpcServer-*" -or $_.Name -like "JavaServer-*" }
$runningCount = 0
foreach ($job in $jobs) {
    $parts = $job.Name.Split('-')
    $serverType = if ($parts[0] -eq "GrpcServer") { "C#" } else { "Java" }
    $port = $parts[1]
    $status = $job.State
    $color = if ($status -eq "Running") { "Green" } else { "Red" }
    Write-Host "   Port $port ($serverType) : $status" -ForegroundColor $color
    
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
    if ($ServerType -eq "mixed") {
        Write-Host "   # Test with mixed servers (C# + Java):" -ForegroundColor Gray
        Write-Host "   dotnet run --project .\csharp-client\GrpcClient\GrpcClient.csproj 7001 7002 7003 http://localhost:7011" -ForegroundColor White
    } elseif ($ServerType -eq "java") {
        Write-Host "   # Test with Java servers:" -ForegroundColor Gray
        $javaUrls = $Ports | ForEach-Object { "http://localhost:$_" }
        Write-Host "   dotnet run --project .\csharp-client\GrpcClient\GrpcClient.csproj $($javaUrls -join ' ')" -ForegroundColor White
    } else {
        Write-Host "   # Test with C# servers:" -ForegroundColor Gray
        Write-Host "   dotnet run --project .\csharp-client\GrpcClient\GrpcClient.csproj $($Ports -join ' ')" -ForegroundColor White
    }
} else {
    Write-Host "ERROR: No servers are running!" -ForegroundColor Red
}

Write-Host ""
Write-Host "To stop all servers:" -ForegroundColor Yellow
Write-Host "   .\stop-servers.ps1" -ForegroundColor White
Write-Host ""
