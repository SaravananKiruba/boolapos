@echo off
echo Creating Boola POS Installer Package...
echo.

REM Set environment variables
set PUBLISH_DIR=bin\Release\net6.0-windows\publish
set INSTALLER_DIR=Installer
set EXE_NAME=BoolaPOS.exe
set OUTPUT_FOLDER=BoolaPOS_Setup
set OUTPUT_ZIP=BoolaPOS_Setup.zip

REM Check if dotnet is available
where dotnet >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: .NET SDK not found. Please install .NET 6.0 SDK or later.
    pause
    exit /b 1
)

REM Build and publish the application
echo Step 1: Building and publishing the application...
dotnet publish -c Release -p:PublishProfile=SelfContainedSingleFile

if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Build failed. See error messages above.
    pause
    exit /b 1
)

REM Create installer directory structure
echo Step 2: Creating installer package...
if exist "%INSTALLER_DIR%" rmdir /s /q "%INSTALLER_DIR%"
mkdir "%INSTALLER_DIR%"
mkdir "%INSTALLER_DIR%\%OUTPUT_FOLDER%"

REM Copy published files to installer directory
xcopy /E /I "%PUBLISH_DIR%" "%INSTALLER_DIR%\%OUTPUT_FOLDER%"

REM Create batch file to create shortcuts
echo @echo off > "%INSTALLER_DIR%\%OUTPUT_FOLDER%\CreateShortcuts.bat"
echo echo Creating shortcuts for Boola POS... >> "%INSTALLER_DIR%\%OUTPUT_FOLDER%\CreateShortcuts.bat"
echo. >> "%INSTALLER_DIR%\%OUTPUT_FOLDER%\CreateShortcuts.bat"
echo set SCRIPT_DIR=%%~dp0 >> "%INSTALLER_DIR%\%OUTPUT_FOLDER%\CreateShortcuts.bat"
echo set EXE_PATH=%%SCRIPT_DIR%%%EXE_NAME% >> "%INSTALLER_DIR%\%OUTPUT_FOLDER%\CreateShortcuts.bat"
echo. >> "%INSTALLER_DIR%\%OUTPUT_FOLDER%\CreateShortcuts.bat"
echo REM Create desktop shortcut >> "%INSTALLER_DIR%\%OUTPUT_FOLDER%\CreateShortcuts.bat"
echo echo Creating desktop shortcut... >> "%INSTALLER_DIR%\%OUTPUT_FOLDER%\CreateShortcuts.bat"
echo powershell "$WshShell = New-Object -ComObject WScript.Shell; $Shortcut = $WshShell.CreateShortcut([System.Environment]::GetFolderPath('Desktop') + '\Boola POS.lnk'); $Shortcut.TargetPath = '%%EXE_PATH%%'; $Shortcut.Save()" >> "%INSTALLER_DIR%\%OUTPUT_FOLDER%\CreateShortcuts.bat"
echo. >> "%INSTALLER_DIR%\%OUTPUT_FOLDER%\CreateShortcuts.bat"
echo REM Create start menu shortcut >> "%INSTALLER_DIR%\%OUTPUT_FOLDER%\CreateShortcuts.bat"
echo echo Creating start menu shortcut... >> "%INSTALLER_DIR%\%OUTPUT_FOLDER%\CreateShortcuts.bat"
echo powershell "$StartMenu = [System.Environment]::GetFolderPath('StartMenu'); $ProgramsPath = [System.IO.Path]::Combine($StartMenu, 'Programs', 'Boola POS'); if (!(Test-Path $ProgramsPath)) { New-Item -ItemType Directory -Path $ProgramsPath -Force | Out-Null }; $WshShell = New-Object -ComObject WScript.Shell; $Shortcut = $WshShell.CreateShortcut([System.IO.Path]::Combine($ProgramsPath, 'Boola POS.lnk')); $Shortcut.TargetPath = '%%EXE_PATH%%'; $Shortcut.Save()" >> "%INSTALLER_DIR%\%OUTPUT_FOLDER%\CreateShortcuts.bat"
echo. >> "%INSTALLER_DIR%\%OUTPUT_FOLDER%\CreateShortcuts.bat"
echo echo Shortcuts created successfully! >> "%INSTALLER_DIR%\%OUTPUT_FOLDER%\CreateShortcuts.bat"
echo pause >> "%INSTALLER_DIR%\%OUTPUT_FOLDER%\CreateShortcuts.bat"

REM Create README.txt with instructions
echo Boola POS Installation Instructions > "%INSTALLER_DIR%\%OUTPUT_FOLDER%\README.txt"
echo =============================== >> "%INSTALLER_DIR%\%OUTPUT_FOLDER%\README.txt"
echo. >> "%INSTALLER_DIR%\%OUTPUT_FOLDER%\README.txt"
echo Installation Steps: >> "%INSTALLER_DIR%\%OUTPUT_FOLDER%\README.txt"
echo 1. Extract all files to a permanent location on your computer (e.g., C:\BoolaPOS) >> "%INSTALLER_DIR%\%OUTPUT_FOLDER%\README.txt"
echo 2. Run CreateShortcuts.bat to create desktop and start menu shortcuts >> "%INSTALLER_DIR%\%OUTPUT_FOLDER%\README.txt"
echo 3. Launch the application using the shortcuts or directly by running %EXE_NAME% >> "%INSTALLER_DIR%\%OUTPUT_FOLDER%\README.txt"
echo. >> "%INSTALLER_DIR%\%OUTPUT_FOLDER%\README.txt"
echo Test User Accounts: >> "%INSTALLER_DIR%\%OUTPUT_FOLDER%\README.txt"
echo ------------------------- >> "%INSTALLER_DIR%\%OUTPUT_FOLDER%\README.txt"
echo Admin:     Username: admin     Password: Admin@123 >> "%INSTALLER_DIR%\%OUTPUT_FOLDER%\README.txt"
echo Manager:   Username: manager   Password: Manager@123 >> "%INSTALLER_DIR%\%OUTPUT_FOLDER%\README.txt"
echo Cashier:   Username: cashier   Password: Cashier@123 >> "%INSTALLER_DIR%\%OUTPUT_FOLDER%\README.txt"
echo Inventory: Username: inventory Password: Inventory@123 >> "%INSTALLER_DIR%\%OUTPUT_FOLDER%\README.txt"
echo. >> "%INSTALLER_DIR%\%OUTPUT_FOLDER%\README.txt"
echo For support, please contact your system administrator. >> "%INSTALLER_DIR%\%OUTPUT_FOLDER%\README.txt"

REM Create a simple installer batch file
echo @echo off > "%INSTALLER_DIR%\Install_BoolaPOS.bat"
echo echo Boola POS Installer >> "%INSTALLER_DIR%\Install_BoolaPOS.bat"
echo echo ----------------- >> "%INSTALLER_DIR%\Install_BoolaPOS.bat"
echo echo. >> "%INSTALLER_DIR%\Install_BoolaPOS.bat"
echo set /p INSTALL_DIR="Enter installation path [C:\BoolaPOS]: " >> "%INSTALLER_DIR%\Install_BoolaPOS.bat"
echo if "%%INSTALL_DIR%%"=="" set INSTALL_DIR=C:\BoolaPOS >> "%INSTALLER_DIR%\Install_BoolaPOS.bat"
echo. >> "%INSTALLER_DIR%\Install_BoolaPOS.bat"
echo echo Installing to %%INSTALL_DIR%%... >> "%INSTALLER_DIR%\Install_BoolaPOS.bat"
echo if not exist "%%INSTALL_DIR%%" mkdir "%%INSTALL_DIR%%" >> "%INSTALLER_DIR%\Install_BoolaPOS.bat"
echo xcopy /E /I /Y "%OUTPUT_FOLDER%" "%%INSTALL_DIR%%" >> "%INSTALLER_DIR%\Install_BoolaPOS.bat"
echo echo. >> "%INSTALLER_DIR%\Install_BoolaPOS.bat"
echo echo Installation completed! >> "%INSTALLER_DIR%\Install_BoolaPOS.bat"
echo echo. >> "%INSTALLER_DIR%\Install_BoolaPOS.bat"
echo echo Creating shortcuts... >> "%INSTALLER_DIR%\Install_BoolaPOS.bat"
echo cd /d "%%INSTALL_DIR%%" >> "%INSTALLER_DIR%\Install_BoolaPOS.bat"
echo call CreateShortcuts.bat >> "%INSTALLER_DIR%\Install_BoolaPOS.bat"
echo echo. >> "%INSTALLER_DIR%\Install_BoolaPOS.bat"
echo echo Boola POS has been installed successfully! >> "%INSTALLER_DIR%\Install_BoolaPOS.bat"
echo pause >> "%INSTALLER_DIR%\Install_BoolaPOS.bat"

REM Create zip archive
echo Step 3: Creating zip archive...
powershell Compress-Archive -Path "%INSTALLER_DIR%\*" -DestinationPath "%INSTALLER_DIR%\%OUTPUT_ZIP%" -Force

echo.
echo ================================================
echo Installation package created successfully!
echo.
echo Location: %INSTALLER_DIR%\%OUTPUT_ZIP%
echo.
echo To distribute: Share the %OUTPUT_ZIP% file with your customers.
echo They simply need to extract the zip file and run Install_BoolaPOS.bat
echo ================================================
echo.
pause
