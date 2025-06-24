@echo off
REM This batch file generates the file list for the WiX installer

REM Set the WiX Toolset directory - you may need to update this path
set WIX_DIR="C:\Program Files (x86)\WiX Toolset v3.11\bin"

REM Set the source and output directories
set BUILD_DIR=..\bin\Debug\net6.0-windows
set OUTPUT_FILE=FileComponents.wxs

REM Run the heat command to harvest files
%WIX_DIR%\heat.exe dir %BUILD_DIR% -ag -scom -sfrag -srd -dr INSTALLFOLDER -cg ProductComponents -var var.BoolaPOS.TargetDir -out %OUTPUT_FILE%

echo File list generated successfully.
pause
