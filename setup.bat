@echo off
REM Configuration script for AtmoceSolarApp

echo.
echo ============================================
echo AtmoceSolarApp - Configuration Setup
echo ============================================
echo.

REM Check if dotnet is installed
dotnet --version >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
	echo ERROR: .NET SDK not found. Please install .NET 9.0 or later.
	pause
	exit /b 1
)

echo [OK] .NET SDK found

REM Setup User Secrets
echo.
echo Would you like to setup User Secrets for development? (Y/N)
set /p choice="Choice: "
if /i "%choice%"=="Y" (
	echo.
	echo Initializing User Secrets...
	dotnet user-secrets init --force

	echo.
	echo Enter your Atmoce Cloud API Base URL:
	echo (Example: https://cloud-api.atmoce.com/openapi/v1)
	set /p baseUrl="URL: "
	dotnet user-secrets set "Atmoce:BaseUrl" "%baseUrl%"

	echo.
	echo Enter your Atmoce API Key:
	set /p apiKey="API Key: "
	dotnet user-secrets set "Atmoce:ApiKey" "%apiKey%"

	echo.
	echo Enter your Atmoce API Secret:
	set /p apiSecret="API Secret: "
	dotnet user-secrets set "Atmoce:ApiSecret" "%apiSecret%"

	echo.
	echo Enter your SQL Server connection string (or press Enter for default LocalDB):
	echo (Default: Server=^(localdb^)\mssqllocaldb;Database=AtmoceSolarDb;Integrated Security=True;)
	set /p connStr="Connection String: "
	if not "%connStr%"=="" (
		dotnet user-secrets set "ConnectionStrings:DefaultConnection" "%connStr%"
	)

	echo.
	echo [OK] User Secrets configured successfully
) else (
	echo [INFO] User Secrets setup skipped. Edit appsettings.json manually.
)

REM Build the project
echo.
echo Building the project...
dotnet build
if %ERRORLEVEL% NEQ 0 (
	echo ERROR: Build failed
	pause
	exit /b 1
)
echo [OK] Build successful

REM Apply migrations
echo.
echo Applying database migrations...
dotnet ef database update
if %ERRORLEVEL% NEQ 0 (
	echo ERROR: Database migration failed
	pause
	exit /b 1
)
echo [OK] Database migrations applied

echo.
echo ============================================
echo Setup completed successfully!
echo ============================================
echo.
echo Next steps:
echo 1. Run 'dotnet run' to start the application
echo 2. Open https://localhost:7000 in your browser
echo 3. Go to /sync to synchronize your Atmoce sites
echo.
pause
