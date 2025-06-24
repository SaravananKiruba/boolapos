# Creating an Installer for Boola POS

This README provides instructions for building an installer for the Boola POS application.

## Prerequisites

1. Install the WiX Toolset v3.11 or later from [https://wixtoolset.org/releases/](https://wixtoolset.org/releases/)
2. Install the WiX Toolset Visual Studio Extension (optional, but recommended)
3. Make sure you've built the Boola POS project in Release mode

## Steps to Build the Installer

### Step 1: Build the Boola POS Project

1. Open the "Page Navigation App.sln" solution in Visual Studio
2. Set the configuration to Release
3. Build the solution (Build > Build Solution)

### Step 2: Generate File Components

1. Navigate to the BoolaPOSInstaller directory
2. Run Heat.bat to generate FileComponents.wxs
   - This will scan your build output and create a component list for the installer
   - You may need to edit the Heat.bat file to update the path to WiX Toolset if it's different on your system

### Step 3: Add FileComponents.wxs to the Project

1. In Visual Studio, right-click on the BoolaPOSInstaller project
2. Select "Add > Existing Item"
3. Browse to and select FileComponents.wxs
4. Make sure to include this file in the build by setting its Build Action to "Compile"

### Step 4: Build the Installer

1. Right-click on the BoolaPOSInstaller project
2. Select "Build"
3. The installer (.msi file) will be created in the "bin\Release" folder of the BoolaPOSInstaller project

### Step 5: Test the Installer

1. Run the installer on a test machine
2. Verify that all components are installed correctly
3. Test the application functionality

## User Accounts for Testing

The application comes with pre-configured test user accounts for different roles:

1. Admin User:
   - Username: admin
   - Password: Admin@123
   - Role: Administrator (full access)

2. Manager User:
   - Username: manager
   - Password: Manager@123
   - Role: Manager (management access to most features)

3. Cashier User:
   - Username: cashier
   - Password: Cashier@123
   - Role: Cashier (access to sales and customer operations)

4. Inventory User:
   - Username: inventory
   - Password: Inventory@123
   - Role: Inventory (access to product and inventory management)

## Distributing the Installer

The installer (.msi file) can be shared with customers for testing. They simply need to:

1. Download the .msi file
2. Double-click to run the installer
3. Follow the on-screen instructions
4. Launch the application from the desktop shortcut or Start menu

## Troubleshooting

- If you encounter an error about missing dependencies, check if all required DLLs are included in the installer
- If the application fails to start, check that the database file is properly installed
- If there are permission issues, ensure the installer is running with administrative privileges
