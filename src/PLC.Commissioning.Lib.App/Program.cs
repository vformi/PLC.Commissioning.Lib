using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Serilog;
using PLC.Commissioning.Lib.Enums;
using PLC.Commissioning.Lib.Abstractions;
using Siemens.Engineering.Download;
using Siemens.Engineering.HW;

namespace PLC.Commissioning.Lib.App
{
    class Program
    {
        static void Main(string[] args)
        {
            #region Registry Generation
            try
            {
                // Define paths
                string applicationPath = "C:\\Users\\Legion\\Documents\\CODING\\git\\projects\\dt.PLC.Commissioning.Lib\\src\\PLC.Commissioning.Lib.App\\bin\\Debug\\net48\\PLC.Commissioning.Lib.App.exe";
                string whitelistKeyName = "PLC.Commissioning.Lib.App.exe";
                string outputFilePath = "output.reg";

                // Generate and save registry entry
                string registryEntry = RegistryService.GenerateRegistryEntry(applicationPath, whitelistKeyName);
                RegistryService.SaveRegistryEntryToFile(registryEntry, outputFilePath);

                Log.Information("Registry entry generated and saved to {OutputFilePath}", outputFilePath);

                // Execute the registry file with admin privileges
                // commented out for now: RegistryService.ExecuteRegistryFile(outputFilePath);

                Log.Information("Registry file executed successfully.");
            }
            catch (Exception ex)
            {
                Log.Error("An error occurred during registry handling: {ErrorMessage}", ex.Message);
            }
            #endregion

            #region Application Workflow
            try
            {
                // 1) Configure logger for .NET usage:
                //    - console + file, logLevel=Information
                //    - no Python callback
                PLCFactory.ConfigureLogger(
                    pythonCallback: null,
                    writeToConsole: true,
                    writeToFile: true,
                    filePath: null,          // defaults to logs/log_...
                    logLevel: LogLevel.Debug);
                
                // Using statement to ensure proper disposal
                var plc = PLCFactory.CreateController<IPLCControllerSiemens>(Manufacturer.Siemens);
                {
                    // 1) Print GSD info (returns a Result)
                    var gsdPrintResult = plc.PrintGSDInformations(
                        @"C:\Users\Legion\Documents\CODING\git\projects\dt.PLC.Commissioning.Lib\src\submodules\Siemens\src\PLC.Commissioning.Lib.Siemens.Tests\TestData\gsd\GSDML-V2.42-LEUZE-RSL400P CU 4M12-20230816.xml");
                    if (gsdPrintResult.IsFailed)
                    {
                        Console.WriteLine($"PrintGSDInformations failed: {gsdPrintResult.Errors[0].Message}");
                        return;
                    }

                    // 2) Configure
                    var configureResult = plc.Configure(
                        @"C:\Users\Legion\Documents\CODING\git\projects\dt.PLC.Commissioning.Lib\src\PLC.Commissioning.Lib.App\configuration.json");
                    if (configureResult.IsFailed)
                    {
                        Console.WriteLine($"Configure failed: {configureResult.Errors[0].Message}");
                        return;
                    }

                    // 3) Initialize
                    var initResult = plc.Initialize(safety: false);
                    if (initResult.IsFailed)
                    {
                        Console.WriteLine($"Initialize failed: {initResult.Errors[0].Message}");
                        return;
                    }

                    // 4) Import devices
                    var gsdmlFiles = new List<string>
                    {
                        @"C:\Users\Legion\Documents\CODING\git\projects\dt.PLC.Commissioning.Lib\src\submodules\Siemens\src\PLC.Commissioning.Lib.Siemens.Tests\TestData\gsd\GSDML-V2.41-LEUZE-BCL248i-20211213.xml",
                        @"C:\Users\Legion\Documents\CODING\git\projects\dt.PLC.Commissioning.Lib\src\submodules\Siemens\src\PLC.Commissioning.Lib.Siemens.Tests\TestData\gsd\GSDML-V2.42-LEUZE-RSL400P CU 4M12-20230816.xml"
                        // add more if needed
                    };

                    var devicesResult = plc.ImportDevices(
                        @"C:\Users\Legion\Documents\CODING\git\projects\dt.PLC.Commissioning.Lib\src\submodules\Siemens\src\PLC.Commissioning.Lib.Siemens.Tests\TestData\aml\valid_multiple_devices.aml",
                        //@"C:\Users\Legion\Documents\School\Vysoka\Magistr\DIPLOMKA\semestral_project\same_devices.aml",
                        gsdmlFiles);
                    if (devicesResult.IsFailed)
                    {
                        Console.WriteLine($"ImportDevices failed: {devicesResult.Errors[0].Message}");
                        return;
                    }
                    
                    Dictionary<string, object> devices = devicesResult.Value;
                    object device = devices.Last().Value;
                    Console.WriteLine($"First device: {device}");

                    // 5) Delete device
                    // var deleteResult = plc.DeleteDevice(device);
                    // if (deleteResult.IsFailed)
                    // {
                    //     Console.WriteLine($"DeleteDevice failed: {deleteResult.Errors[0].Message}");
                    //     return;
                    // }
                    
                    // 6) Configure the device with IP/profinet name
                    var deviceParams = new Dictionary<string, object>
                    {
                        { "ipAddress", "192.168.60.100" },
                        { "profinetName", "dut" }
                    };
                    var configDevResult = plc.ConfigureDevice(device, deviceParams);
                    if (configDevResult.IsFailed)
                    {
                        Console.WriteLine($"ConfigureDevice failed: {configDevResult.Errors[0].Message}");
                        return;
                    }

                    // 7) Get device parameters
                    var getParamsResult = plc.GetDeviceParameters(device, "[M1] Safe signal", safety: true);
                    if (getParamsResult.IsFailed)
                    {
                        Console.WriteLine($"GetDeviceParameters failed: {getParamsResult.Errors[0].Message}");
                        return; 
                    }
                    
                    var paramDict = getParamsResult.Value;
                    Console.WriteLine("Retrieved parameters:");
                    foreach (var kvp in paramDict)
                    {
                        Console.WriteLine($"  {kvp.Key} => {kvp.Value}");
                    }

                    // 8) Set parameters
                    var parametersToSet = new Dictionary<string, object>
                    {
                        {"Automatic reading gate repeat", "yes"},
                        {"Reading gate end mode / completeness mode", 3},
                        {"Restart delay", 333},
                        {"Max. reading gate time when scanning", 762}
                    };
                    var setParamsResult = plc.SetDeviceParameters(device, "[M11] Reading gate control", parametersToSet);
                    if (setParamsResult.IsFailed)
                    {
                        Console.WriteLine($"SetDeviceParameters failed: {setParamsResult.Errors[0].Message}");
                        return;
                    }

                    // 9) Compile
                    var compileResult = plc.Compile();
                    if (compileResult.IsFailed)
                    {
                        Console.WriteLine($"Compile failed: {compileResult.Errors[0].Message}");
                        return;
                    }

                    // 10) Save project again
                    var saveAgainResult = plc.SaveProjectAs("V111");
                    if (saveAgainResult.IsFailed)
                    {
                        Console.WriteLine($"SaveProjectAs failed: {saveAgainResult.Errors[0].Message}");
                        return;
                    }

                    // 11) Read back parameters
                    var getParamsAgainResult = plc.GetDeviceParameters(device, "[M11] Reading gate control");
                    if (getParamsAgainResult.IsFailed)
                    {
                        Console.WriteLine($"GetDeviceParameters (again) failed: {getParamsAgainResult.Errors[0].Message}");
                        return;
                    }

                    // Done
                    Console.WriteLine("All operations completed successfully!");
                    // Optionally, Console.ReadKey(); if you want to pause
                }
            }
            catch (FileNotFoundException ex)
            {
                Log.Error("File not found: {FileName}", ex.FileName ?? "Unknown");
            }
            catch (Exception ex)
            {
                Log.Error("An error occurred: {ErrorMessage}", ex.Message);
            }
            #endregion

            Log.CloseAndFlush();
        }
    }
}
