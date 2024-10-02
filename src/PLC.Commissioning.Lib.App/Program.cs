using System;
using Serilog;
using System.Collections.Generic;
using PLC.Commissioning.Lib.Enums;
using PLC.Commissioning.Lib.Abstractions;
using Siemens.Engineering.HW;
using System.Linq;

namespace PLC.Commissioning.Lib.Siemens.App
{
    class Program
    {
        static void Main(string[] args)
        {
            // logger 
            string logFileName = $"logs/log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()               // Set the minimum log level to Debug
            .WriteTo.Console()                  // Log to the console
            .WriteTo.File(logFileName,          // Log to a file with a dynamic name
                          fileSizeLimitBytes: 10_000_000,       // limit the file size to 10 MB
                          rollOnFileSizeLimit: true)            // create a new file when the limit is reached
            .CreateLogger();
            // variables 
            Dictionary<string, object> parametersToSet = new Dictionary<string, object>
            {
                {"Mode", "With ACK"},
            };

            List<string> parametersToRead = new List<string> {"Mode"};
            Dictionary<string, object> DAPparametersToSet = new Dictionary<string, object>
            {
                {"Code type 1", "Code39"},
                {"Code type 2", "Code32"},
                {"Number of digits 1", 11},
                {"Number of digits 1_1", 14},
            };
            List<string> DAPparametersToRead = new List<string> { "Code type 1" };

            string gsdPath = "C:\\Users\\vformane\\Documents\\coding\\git\\PLC_Automated_System_Testing\\plc-commisioning-lib\\src\\submodules\\Siemens\\src\\PLC.Commissioning.Lib.Siemens.Tests\\configuration_files\\gsd\\GSDML-V2.41-LEUZE-BCL348i-20211213.xml";
            // Main program execution
            try
            {
                using (var plc = PLCFactory.CreateController<IPLCControllerSiemens>(Manufacturer.Siemens))
                {
                    plc.PrintGSDInformations("C:\\Users\\vformane\\Desktop\\files\\", "DAP");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();

                    plc.Configure("C:\\Users\\vformane\\Documents\\coding\\git\\PLC_Automated_System_Testing\\plc-commisioning-lib\\src\\PLC.Commissioning.Lib.App\\configuration.json");
                    plc.Initialize(safety: true);
                    var importedDevices = plc.ImportDevice("C:\\Users\\vformane\\OneDrive - Leuze electronic GmbH + Co. KG\\Osobní\\Diplomka\\Siemens\\Blank_project_BCL.aml")
                        as Dictionary<string, Device>;
                    plc.Compile();

                    // Get the first element of the dictionary
                    /*
                    var firstDeviceKeyValue = importedDevices.First();
                    var firstDeviceKey = firstDeviceKeyValue.Key;
                    var firstDevice = firstDeviceKeyValue.Value;
                    */

                    var bcl = plc.GetDeviceByName("BCL348i");
                    plc.GetDeviceParameters(bcl, gsdPath, "DAP");
                    plc.GetDeviceParameters(bcl, gsdPath, "[M10] Activation");

                    plc.SetDeviceParameters(bcl, gsdPath, "DAP", DAPparametersToSet);
                    plc.GetDeviceParameters(bcl, gsdPath, "DAP", DAPparametersToRead);

                    plc.Download("safety");

                    //siemensPlc.SetDeviceParameters(bcl, gsdPath, "[M10] Activation", parametersToSet);
                    //siemensPlc.GetDeviceParameters(bcl, gsdPath, "[M10] Activation", parametersToRead);
                    // Pause for the user at the end
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                } // plc should be disposed here

            }
            catch (Exception ex)
            {
                Log.Error($"An error occurred: {ex}");
            }

        // Ensure to flush and close the log
        Log.CloseAndFlush();
        }
    }
}
