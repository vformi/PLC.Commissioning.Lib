# PLC.Commissioning.Lib
This repository was developed for the purpose of automating the testing of PROFINET sensors as part of a diploma thesis by @vformi at Czech Technical University in Prague (CTU), within the Open Informatics program specializing in Software Engineering.

## Overview
This repository contains the main library for commissioning PLCs and includes links to manufacturer-specific libraries as Git submodules. The structure is designed to enable modular, automated testing across different PLC manufacturers, with each submodule providing manufacturer-specific functionality.

## Prerequisites
- **Operating System**: **Windows** (required)
    - The TIA Portal is "only" supported on Windows, so this library requires a Windows platform to function properly.
- **TIA Portal and TIA Portal Openness**: TIA Portal (with TIA Openness enabled) must be installed on the system, as the library interacts directly with the TIA Portal software for PLC commissioning and testing.
    - Ensure that you have the necessary permissions, including membership in the **Siemens TIA Openness** Windows group, to allow programmatic access to TIA Portal functionalities. 
  > **Note**: The library was developed on TIA Portal V17, but exploratory testing was conducted against versions ranging from V17 up to V20. While not a deep test of all methods, the core functionality was verified to work in these newer versions, 
  > suggesting it should be compatible in most cases.

This repository contains the main library for commissioning PLCs and includes links to manufacturer-specific libraries and other functionalities as Git submodules. The structure is designed to enable modular, automated testing across different PLC manufacturers, with each submodule providing manufacturer-specific functionality.

## Repositories Structure
The project is divided across several repositories:
- **[PLC.Commissioning.Lib](https://github.com/vformi/PLC.Commissioning.Lib)** (this repository)
- **[PLC.Commissioning.Lib.Abstractions](https://github.com/vformi/PLC.Commissioning.Lib.Abstractions)**
- **[PLC.Commissioning.Lib.Siemens](https://github.com/vformi/PLC.Commissioning.Lib.Siemens)**
- **[PLC.Commissioning.Lib.Siemens.Webserver](https://github.com/vformi/PLC.Commissioning.Lib.Siemens.Webserver)**
- Further manufacturers (Rockwell, Beckhoff) in development 

## Cloning the Project
To clone the main repository with all submodules, use:
```bash
git clone --recurse-submodules https://github.com/vformi/PLC.Commissioning.Lib
```
To update the submodules later, use:
```bash
git submodule update --init --recursive
```

## Solution Overview
### dt.PLC.Commissioning.Lib.sln
- This is the main solution containing all projects, including the core library, abstractions, Manufacturer-specific implementations, tests, and the **PLC.Commissioning.Lib.App** project.
---
Use this solution if you want to:
- Explore the full source code of the library.
- Run comprehensive examples via **PLC.Commissioning.Lib.App**, which demonstrates most of the library’s features in an executable application.
- Contribute to the library or customize it for your needs.

#### Usage
```csharp
using PLC.Commissioning.Lib.Enums;
using PLC.Commissioning.Lib.Abstractions;

var controller = PLCFactory.CreateController<IPLCControllerSiemens>(Manufacturer.Siemens);
// Configure, initialize, and start your PLC operations
```
For further usage examples see **PLC.Commissioning.Lib.App** as advised above.

### dt.PLC.Commissioning.Lib.Standalone.sln
This solution focuses on showcasing the library’s standalone capability as a single DLL (PLC.Commissioning.Lib.Merged.dll). 
- **PLC.Commissioning.Lib.App.Standalone**: A full-featured example demonstrating extensive functionality using the same DLL. 
--- 
Use this solution if you want to:
- See how to integrate and use the library as a single DLL in your own projects.
- Test the standalone deployment without needing the full source code.

For further usage examples see **PLC.Commissioning.Lib.App.Standalone** as advised above.

#### Which Solution Should You Choose?
- For Developers and Explorers: Open **dt.PLC.Commissioning.Lib.sln** in editor to access the complete library source, including the **PLC.Commissioning.Lib.App** project for detailed examples.
- For End Users and Integrators: Open **dt.PLC.Commissioning.Lib.Standalone.sln** to see how to use the pre-built **PLC.Commissioning.Lib.Merged.dll** in standalone applications, with minimal examples provided.

## Troubleshooting
#### Engineering Security Exception
If you encounter the following error:
```bash
[12:25:45 ERR] EngineeringSecurityException: Ensure the user 'USER' is a member of the Siemens TIA Openness group.
```
This indicates that the user running the application is not a member of the Siemens TIA Openness Windows group, which is required for accessing certain TIA Portal functionality. To resolve this issue:
1. Add the User to the Siemens TIA Openness Group:
   - Open Local Users and Groups by pressing `Win + R`, typing `lusrmgr.msc`, and pressing Enter.
   - In the Groups section, locate **Siemens TIA Openness**.
   - Right-click on **Siemens TIA Openness** and select **Add to Group**.
   - Add your user to the group.
   - Save and close, then restart your computer for the changes to take effect.
2. Refer to Siemens Support Detailed Guide:
   - For more information, refer to the [Siemens Support Detailed Guide](https://support.industry.siemens.com/cs/mdm/109773802?c=101778035467&lc=en-DE).

#### FileNotFoundException: Siemens.Engineering.dll
If you encounter the following error:
```bash
Unhandled exception: System.IO.FileNotFoundException: Could not load file or assembly Siemens.Engineering, Version=17.0.0.0, Culture=neutral, PublicKeyToken=d29ec89bac048f84 or one of its dependencies. The system cannot find the file specified.
```
This indicates the required Siemens.Engineering.dll file is missing. To resolve:
- Verify that TIA Portal is installed and matches the required version.
- Ensure the Siemens.Engineering.dll file is properly referenced in your project.
- You have the following options to start the TIA Portal with a TIA Portal Openness application:
  - Use an application configuration file (recommended in most use cases).
  - Use the "AssemblyResolve" method (recommended when you use copy deploy etc.).
  - Copy the Siemens.Engineering.dll  in the TIA Portal Openness application directory.

For more information, refer to the [TIA Portal Openness Documentation (Page 81)](https://cache.industry.siemens.com/dl/files/533/109798533/att_1069908/v1/TIAPortalOpennessenUS_en-US.pdf). 
You can take a look at the **PLC.Commissioning.Lib.Standalone.sln** where you can see how the copying of the Siemens.Engineering files is handled. 

# Single dll publish workflow
1. Install ILRepack
```bash
dotnet tool install -g dotnet-ilrepack 
```

2. Ensure your project and all dependencies are built successfully and output DLLs are in the bin\Release\netstandard2.0 folder:
```bash
dotnet build -c Release
```
After running the build, a ilrepack.rsp file will be created in the `bin\Release\netstandard2.0` directory.
This file contains the list of all .dll files to be merged, excluding the already merged output.
### Steps to reproduce
1. Navigate to proper directory
```bash
cd PLC.Commissioning.Lib
```
2. Build the project:
```bash
dotnet build -c Release
```
3. Navigate to the output directory:
```bash
cd bin\Release\netstandard2.0
```
4. Run the ilrepack command:
```bash
ilrepack /out:PLC.Commissioning.Lib.Merged.dll "@ilrepack.rsp"
```
---
Note: The merged DLL (`PLC.Commissioning.Lib.Merged.dll`) includes only this project’s code and MIT-licensed dependencies. Proprietary Siemens Openness DLLs (e.g., Siemens.Engineering.dll) remain external and must be provided by the user’s TIA Portal installation.