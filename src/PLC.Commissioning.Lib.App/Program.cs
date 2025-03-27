using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog;
using PLC.Commissioning.Lib.Enums;
using PLC.Commissioning.Lib.Abstractions;
using Newtonsoft.Json;
using Siemens.Engineering.Download;

namespace PLC.Commissioning.Lib.App
{
    class Program
    {
        static void Main(string[] args)
        {
            #region Application Workflow
            try
            {
                // Configure logger
                PLCFactory.ConfigureLogger(
                    pythonCallback: null,
                    writeToConsole: true,
                    writeToFile: true,
                    filePath: null,
                    logLevel: LogLevel.Debug);

                using (var plc = PLCFactory.CreateController<IPLCControllerSiemens>(Manufacturer.Siemens))
                {
                    // Define file paths from samples folder
                    string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "samples");
                    string gsdFile = Path.Combine(basePath, "GSDML-V2.41-LEUZE-BCL248i-20211213.xml");
                    string deviceList = Path.GetFullPath(Path.Combine(basePath, "sample_device.aml")); // Convert to absolute path
                    string projectFile = Path.Combine(basePath, "Blank_project_S71200_20250303_2247.zap17");
                    string configFile = Path.Combine(basePath, "sample_config.json");
                    
                    // Create sample_config.json
                    var configData = new Dictionary<string, string>
                    {
                        { "projectPath", projectFile },
                        { "networkCard", "Realtek USB GbE Family Controller" }
                    };
                    string jsonConfig = JsonConvert.SerializeObject(configData, Formatting.Indented);
                    File.WriteAllText(configFile, jsonConfig);
                    Log.Information("Created sample_config.json at {ConfigFilePath}", configFile);
                    
                    // 1) Print GSD info
                    var gsdPrintResult = plc.PrintGSDInformations(gsdFile);
                    if (gsdPrintResult.IsFailed)
                    {
                        Console.WriteLine($"PrintGSDInformations failed: {gsdPrintResult.Errors[0].Message}");
                        return;
                    }

                    // 2) Configure
                    var configureResult = plc.Configure(configFile);
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
                    var gsdmlFiles = new List<string> { gsdFile };
                    var devicesResult = plc.ImportDevices(deviceList, gsdmlFiles);
                    if (devicesResult.IsFailed)
                    {
                        Console.WriteLine($"ImportDevices failed: {devicesResult.Errors[0].Message}");
                        return;
                    }

                    Dictionary<string, object> devices = devicesResult.Value;
                    object device = devices.First().Value;
                    Console.WriteLine($"First device: {device}");

                    // 5) Configure device
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

                    // 6) Get parameters before set
                    var paramsBefore = plc.GetDeviceParameters(device, "[M11] Reading gate control");
                    if (paramsBefore.IsFailed)
                    {
                        Console.WriteLine($"GetDeviceParameters failed: {paramsBefore.Errors[0].Message}");
                        return;
                    }

                    // 7) Set parameters
                    var parametersToSet = new Dictionary<string, object>
                    {
                        {"Automatic reading gate repeat", "yes"},
                        {"Reading gate end mode / completeness mode", "Ident List dependent"},
                        {"Restart delay", 333},
                        {"Max. reading gate time when scanning", 762}
                    };
                    var setParamsResult = plc.SetDeviceParameters(device, "[M11] Reading gate control", parametersToSet);
                    if (setParamsResult.IsFailed)
                    {
                        Console.WriteLine($"SetDeviceParameters failed: {setParamsResult.Errors[0].Message}");
                        return;
                    }

                    // 8) Get parameters after set
                    var paramsAfter = plc.GetDeviceParameters(device, "[M11] Reading gate control");
                    if (paramsAfter.IsFailed)
                    {
                        Console.WriteLine($"GetDeviceParameters failed: {paramsAfter.Errors[0].Message}");
                        return;
                    }
                    Console.WriteLine($"Retrieved device parameters after update: {paramsAfter.Value}");

                    // 9) Compile
                    var compileResult = plc.Compile();
                    if (compileResult.IsFailed)
                    {
                        Console.WriteLine($"Compile failed: {compileResult.Errors[0].Message}");
                        return;
                    }
                    
                    // 10) Download
                    var downloadResult = plc.Download(DownloadOptions.Hardware | DownloadOptions.Software);
                    if (downloadResult.IsFailed)
                    {
                        Console.WriteLine($"Download failed: {downloadResult.Errors[0].Message}");
                        return;
                    }

                    // 11) Save project
                    var saveResult = plc.SaveProjectAs("DotnetTest");
                    if (saveResult.IsFailed)
                    {
                        Console.WriteLine($"SaveProjectAs failed: {saveResult.Errors[0].Message}");
                        return;
                    }

                    Console.WriteLine("All operations completed successfully!");
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