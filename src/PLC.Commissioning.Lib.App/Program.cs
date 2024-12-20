using System;
using System.IO;
using Serilog;
using PLC.Commissioning.Lib.Enums;
using PLC.Commissioning.Lib.Abstractions;

namespace PLC.Commissioning.Lib.App
{
    class Program
    {
        static void Main(string[] args)
        {
            // Setup Serilog
            string logFileName = $"logs/log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(logFileName, fileSizeLimitBytes: 10_000_000, rollOnFileSizeLimit: true)
                .CreateLogger();

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
                RegistryService.ExecuteRegistryFile(outputFilePath);

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
                using (var plc = PLCFactory.CreateController<IPLCControllerSiemens>(Manufacturer.Siemens))
                {
                    plc.PrintGSDInformations(
                        @"C:\Users\Legion\Documents\CODING\git\projects\dt.PLC.Commissioning.Lib\src\submodules\Siemens\src\PLC.Commissioning.Lib.Siemens.Tests\TestData\gsd\GSDML-V2.41-LEUZE-BCL248i-20211213.xml");
                    // Console.ReadKey();
                    plc.Configure(@"C:\Users\Legion\Documents\CODING\git\projects\dt.PLC.Commissioning.Lib\src\PLC.Commissioning.Lib.App\configuration.json");
                    plc.Initialize(safety: true);
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
