#!/usr/bin/env pwsh

<#
.SYNOPSIS
	Setup script for AtmoceSolarApp
.DESCRIPTION
	Initializes the project with User Secrets and database migrations
.EXAMPLE
	.\setup.ps1
#>

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "AtmoceSolarApp - Configuration Setup" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Check if dotnet is installed
try {
	$dotnetVersion = dotnet --version
	Write-Host "[OK] .NET SDK found: $dotnetVersion" -ForegroundColor Green
}
catch {
	Write-Host "[ERROR] .NET SDK not found. Please install .NET 9.0 or later." -ForegroundColor Red
	exit 1
}

# Setup User Secrets
Write-Host ""
$setupSecrets = Read-Host "Would you like to setup User Secrets for development? (Y/N)"
if ($setupSecrets -eq "Y" -or $setupSecrets -eq "y") {
	Write-Host ""
	Write-Host "Initializing User Secrets..." -ForegroundColor Yellow
	dotnet user-secrets init --force

	Write-Host ""
	Write-Host "Enter your Atmoce Cloud API Base URL:" -ForegroundColor Yellow
	Write-Host "(Example: https://cloud-api.atmoce.com/openapi/v1)"
	$baseUrl = Read-Host "URL"
	dotnet user-secrets set "Atmoce:BaseUrl" $baseUrl

	Write-Host ""
	Write-Host "Enter your Atmoce API Key:" -ForegroundColor Yellow
	$apiKey = Read-Host "API Key"
	dotnet user-secrets set "Atmoce:ApiKey" $apiKey

	Write-Host ""
	Write-Host "Enter your Atmoce API Secret:" -ForegroundColor Yellow
	$apiSecret = Read-Host "API Secret" -AsSecureString
	$plainSecret = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto([System.Runtime.InteropServices.Marshal]::SecureStringToCoTaskMemAlloc($apiSecret))
	dotnet user-secrets set "Atmoce:ApiSecret" $plainSecret

	Write-Host ""
	Write-Host "Enter your SQL Server connection string (or press Enter for default LocalDB):" -ForegroundColor Yellow
	Write-Host "(Default: Server=(localdb)\mssqllocaldb;Database=AtmoceSolarDb;Integrated Security=True;)"
	$connStr = Read-Host "Connection String"
	if ($connStr -ne "") {
		dotnet user-secrets set "ConnectionStrings:DefaultConnection" $connStr
	}

	Write-Host ""
	Write-Host "[OK] User Secrets configured successfully" -ForegroundColor Green
}
else {
	Write-Host "[INFO] User Secrets setup skipped. Edit appsettings.json manually." -ForegroundColor Yellow
}

# Build the project
Write-Host ""
Write-Host "Building the project..." -ForegroundColor Yellow
dotnet build
if ($LASTEXITCODE -ne 0) {
	Write-Host "[ERROR] Build failed" -ForegroundColor Red
	exit 1
}
Write-Host "[OK] Build successful" -ForegroundColor Green

# Apply migrations
Write-Host ""
Write-Host "Applying database migrations..." -ForegroundColor Yellow
dotnet ef database update
if ($LASTEXITCODE -ne 0) {
	Write-Host "[ERROR] Database migration failed" -ForegroundColor Red
	exit 1
}
Write-Host "[OK] Database migrations applied" -ForegroundColor Green

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Setup completed successfully!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Run 'dotnet run' to start the application"
Write-Host "2. Open https://localhost:7000 in your browser"
Write-Host "3. Go to /sync to synchronize your Atmoce sites"
Write-Host ""
