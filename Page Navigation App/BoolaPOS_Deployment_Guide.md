# Boola POS - Test User Accounts & Installer Creation Guide

## 1. Test User Accounts

The application has been updated to include multiple test user accounts with different roles. When sharing the application with customers for testing, they can now use these accounts instead of creating new ones from scratch:

| Role | Username | Password | Access Level |
|------|----------|----------|-------------|
| Admin | admin | Admin@123 | Full system access |
| Manager | manager | Manager@123 | Management access to most features |
| Cashier | cashier | Cashier@123 | Access to sales and customer operations |
| Inventory | inventory | Inventory@123 | Access to product and inventory management |

These test accounts are automatically created when the application starts up if they don't already exist. This allows testers to log in with different roles and test the appropriate functionality for each role.

## 2. Creating an Installer for Boola POS

### Prerequisites

1. **Install WiX Toolset v3.11 or later**
   - Download from: https://wixtoolset.org/releases/
   - Follow the installation instructions

2. **Install WiX Toolset Visual Studio Extension** (optional but recommended)
   - In Visual Studio: Extensions > Manage Extensions
   - Search for "Wix Toolset Visual Studio Extension"
   - Install and restart Visual Studio

### Step-by-Step Installer Creation Process

1. **Build the Boola POS Project**
   - Open the "Page Navigation App.sln" solution in Visual Studio
   - Set the configuration to Release (important!)
   - Build the solution (Build > Build Solution)

2. **Generate the Component List**
   - Navigate to the BoolaPOSInstaller directory
   - Run Heat.bat (this creates FileComponents.wxs by scanning your build output)
   - If needed, edit the WIX_DIR variable in Heat.bat to point to your WiX Toolset installation

3. **Add FileComponents.wxs to the Project**
   - In Visual Studio, right-click on the BoolaPOSInstaller project
   - Select "Add > Existing Item"
   - Browse to and select FileComponents.wxs
   - Set its Build Action to "Compile" in Properties

4. **Build the Installer**
   - Right-click on the BoolaPOSInstaller project
   - Select "Build"
   - The installer (.msi file) will be created in the BoolaPOSInstaller\bin\Release folder

### Distributing the Installer

1. **Share the MSI file** with your customers for testing
   - The file will be named BoolaPOS_Installer.msi
   - It contains everything needed to run the application

2. **Installation Instructions for Customers**
   - Download the MSI file
   - Double-click to run the installer
   - Follow the on-screen instructions
   - The application will be installed in Program Files\Boola Systems\Boola POS
   - Shortcuts will be created on the desktop and in the Start menu

3. **First Run**
   - Launch the application using the desktop shortcut
   - Use one of the test accounts listed above to log in
   - The database is pre-configured with the application

### Alternative Deployment Methods

If you prefer not to use an installer, you can also:

1. **Publish as a Self-Contained Application**
   - In Visual Studio, right-click on the BoolaPOS project
   - Select "Publish..."
   - Choose "Folder" as the publish target
   - Select "Produce self-contained" and choose Windows as the target runtime
   - Click "Publish"
   - Share the entire published folder with your customers

2. **Create a ZIP Archive**
   - Build the application in Release mode
   - Navigate to bin\Release\net6.0-windows folder
   - Create a ZIP archive of all files
   - Share the ZIP file with instructions to extract all files before running

## Troubleshooting

### Installer Issues
- If WiX build fails, make sure you've installed WiX Toolset correctly
- If the installer fails to include all necessary files, check that Heat.bat ran successfully
- Verify the ProjectReference in BoolaPOSInstaller.wixproj points to the correct project ID

### Application Issues
- If the application fails to start, check that the database file is correctly installed
- If a user cannot log in, verify they are using the correct credentials from the table above
- For any database errors, ensure that the application has write permissions to its installation folder

For any additional assistance, please contact support.
