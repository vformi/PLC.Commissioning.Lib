using System;
using System.Collections.Generic;
using System.IO;
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
                using (var plc = PLCFactory.CreateController<IPLCControllerSiemens>(
                           Manufacturer.Siemens,
                           LogLevel.Debug))
                {
                    string gsdFilePath =
                        @"C:\Users\Legion\Documents\CODING\git\projects\dt.PLC.Commissioning.Lib\src\submodules\Siemens\src\PLC.Commissioning.Lib.Siemens.Tests\TestData\gsd\GSDML-V2.41-LEUZE-BCL248i-20211213.xml";
                    plc.PrintGSDInformations(gsdFilePath);
                    // configure
                    plc.Configure(@"C:\Users\Legion\Documents\CODING\git\projects\dt.PLC.Commissioning.Lib\src\PLC.Commissioning.Lib.App\configuration.json");
                    // initialize 
                    plc.Initialize(safety: false);
                    // import aml 
                    object device = plc.ImportDevice(
                        @"C:\Users\Legion\Documents\School\Vysoka\Magistr\DIPLOMKA\semestral_project\bcl248i_M10_M11.aml");
                    // change ip address and profinet name
                    var deviceParams = new Dictionary<string, object>
                    {
                        { "ipAddress", "192.168.60.100" },
                        { "profinetName", "dut" }
                    };
                    plc.ConfigureDevice(device, deviceParams);
                    // get parameters for module 
                    plc.GetDeviceParameters(device, gsdFilePath, "[M11] Reading gate control");
                    // set parameters in module 
                    var parametersToSet = new Dictionary<string, object>
                    {
                        {"Automatic reading gate repeat", "yes"},
                        {"Reading gate end mode / completeness mode", 3},
                        {"Restart delay", 333},
                        {"Max. reading gate time when scanning", 762}
                    };
                    plc.SetDeviceParameters(device, gsdFilePath, "[M11] Reading gate control", parametersToSet);
                    // compile 
                    plc.Compile();
                    // save project as (to see the changes) /Documents/Openness/Saved_projects
                    plc.SaveProjectAs("V1"); // make sure this is deleted beforehand
                    // debug to see if it was set properly
                    plc.GetDeviceParameters(device, gsdFilePath, "[M11] Reading gate control");
                    // Console.ReadKey(); // waiting point for safety showcase
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
