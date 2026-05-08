# publish-images.ps1
# This script builds the docker images and saves them as .tar files for manual deployment.

$apiImage = "cf-events-api"
$webImage = "cf-events-web"

Write-Host "--- Building Docker Images ---" -ForegroundColor Cyan
docker compose build

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Docker build failed." -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "--- Saving cf-events-api to tar ---" -ForegroundColor Cyan
docker save $apiImage -o cf-events-api.tar

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Failed to save cf-events-api.tar" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "--- Saving cf-events-web to tar ---" -ForegroundColor Cyan
docker save $webImage -o cf-events-web.tar

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Failed to save cf-events-web.tar" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "--- Successfully created .tar files ---" -ForegroundColor Green
Get-ChildItem *.tar | Select-Object Name, @{Name="Size(MB)";Expression={"{0:N2}" -f ($_.Length / 1MB)}} | Format-Table
