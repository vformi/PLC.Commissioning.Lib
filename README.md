# PLC.Commissioning.Lib
This repository was developed for the purpose of automating the testing of PROFINET sensors as part of a diploma thesis by @vformi at Czech Technical University in Prague (CTU), within the Open Informatics program specializing in Software Engineering.

## Overview

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
git clone --recurse-submodules https://github.com/vformi/dt.PLC.Commissioning.Lib
```
To update the submodules later, use:
```bash
git submodule update --init --recursive
```
## Getting Started
Install .NET Standard 2.0 or later.
Restore dependencies:
dotnet restore
Usage
```csharp
using PLC.Commissioning.Lib;
using PLC.Commissioning.Lib.Abstractions;

var controller = PLCFactory.CreateController("Siemens");
// Configure, initialize, and start your PLC operations
```
For further usage examples see **PLC.Commissioning.Lib.App** which also includes an executable code that is using and utilizing most of the library.