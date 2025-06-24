@echo off
echo Setting up Boola POS environment...
echo.

REM Check if dotnet is available
where dotnet >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: .NET SDK not found. Please install .NET 6.0 SDK or later.
    pause
    exit /b 1
)

REM Restore dependencies
echo Restoring dependencies...
dotnet restore

REM Build the project
echo Building the project...
dotnet build -c Release

if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Build failed. See error messages above.
    pause
    exit /b 1
)

echo.
echo ================================================
echo Build completed successfully!
echo.
echo To create the installation package, run:
echo   CreateInstaller.bat
echo ================================================
echo.
pause
