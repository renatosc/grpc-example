# Stop All gRPC Servers
# This script stops all running gRPC server instances

Write-Host ">> Stopping all gRPC servers..." -ForegroundColor Yellow

# Get all jobs with GrpcServer or JavaServer in the name
$grpcJobs = Get-Job | Where-Object { $_.Name -like "GrpcServer-*" -or $_.Name -like "JavaServer-*" }

if ($grpcJobs.Count -eq 0) {
    Write-Host "INFO: No gRPC server jobs found." -ForegroundColor Cyan
} else {
    Write-Host "Found $($grpcJobs.Count) gRPC server job(s):" -ForegroundColor Cyan
      foreach ($job in $grpcJobs) {
        $parts = $job.Name.Split('-')
        $serverType = if ($parts[0] -eq "GrpcServer") { "C#" } else { "Java" }
        $port = $parts[1]
        Write-Host "   Port $port ($serverType) (Job ID: $($job.Id)) - Status: $($job.State)" -ForegroundColor White
    }
    
    Write-Host ""
    Write-Host "Stopping jobs..." -ForegroundColor Yellow
    $grpcJobs | Stop-Job
    
    Write-Host "Removing jobs..." -ForegroundColor Yellow
    $grpcJobs | Remove-Job
    
    Write-Host "SUCCESS: All gRPC servers stopped and cleaned up." -ForegroundColor Green
}

# Also try to kill any remaining dotnet/java processes on our ports (fallback)
Write-Host ""
Write-Host "Checking for any remaining processes on ports 7001-7003, 7011..." -ForegroundColor Cyan

$ports = @(7001, 7002, 7003, 7011)
foreach ($port in $ports) {
    try {
        $connections = netstat -ano | Select-String ":$port "
        if ($connections) {
            Write-Host "WARNING: Found process(es) still using port $port" -ForegroundColor Yellow
            $connections | ForEach-Object {
                $line = $_.Line.Trim()
                if ($line -match '\s+(\d+)$') {
                    $pid = $matches[1]
                    Write-Host "   Attempting to stop process PID: $pid" -ForegroundColor Gray
                    try {
                        Stop-Process -Id $pid -Force -ErrorAction SilentlyContinue
                        Write-Host "   SUCCESS: Stopped process $pid" -ForegroundColor Green
                    } catch {
                        Write-Host "   ERROR: Could not stop process $pid" -ForegroundColor Red
                    }
                }
            }
        }
    } catch {
        # Ignore errors when checking ports
    }
}

Write-Host ""
Write-Host "All cleanup completed!" -ForegroundColor Green
