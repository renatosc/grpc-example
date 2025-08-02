# Test gRPC Servers
# This script tests connectivity to all gRPC server instances using gRPC calls

Write-Host ">> Testing gRPC Server Connectivity" -ForegroundColor Green
Write-Host "====================================" -ForegroundColor Green

$ports = @(7001, 7002, 7003)
$allServersRunning = $true

foreach ($port in $ports) {
    Write-Host "Testing gRPC server on port $port..." -ForegroundColor Yellow
    
    try {
        # Test gRPC connectivity by checking if the port is listening
        $connection = Test-NetConnection -ComputerName localhost -Port $port -WarningAction SilentlyContinue
        
        if ($connection.TcpTestSucceeded) {
            Write-Host "SUCCESS: Server on port $port is accepting connections" -ForegroundColor Green
        } else {
            Write-Host "ERROR: Server on port $port is NOT accepting connections" -ForegroundColor Red
            $allServersRunning = $false
        }
    }
    catch {
        Write-Host "ERROR: Could not test port $port - $($_.Exception.Message)" -ForegroundColor Red
        $allServersRunning = $false
    }
}

Write-Host ""
if ($allServersRunning) {
    Write-Host "SUCCESS: All servers are accepting connections!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Ready to run the client demo:" -ForegroundColor Cyan
    Write-Host "   dotnet run --project .\csharp-client\GrpcClient\GrpcClient.csproj" -ForegroundColor White
} else {
    Write-Host "WARNING: Some servers are not accepting connections." -ForegroundColor Yellow
    Write-Host "Try starting the servers first:" -ForegroundColor Cyan
    Write-Host "   .\start-servers.ps1" -ForegroundColor White
}

Write-Host ""
