using System;
using Serilog;
using PLC.Commissioning.Lib.Enums;
using PLC.Commissioning.Lib.Abstractions;
using Siemens.Engineering.Download;

namespace PLC.Commissioning.Lib.App
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
            // Main program execution
            try
            {
                using (var plc = PLCFactory.CreateController<IPLCControllerSiemens>(Manufacturer.Siemens))
                {
                    plc.Configure("C:\\Users\\Legion\\Documents\\CODING\\git\\projects\\dt.PLC.Commissioning.Lib\\src\\PLC.Commissioning.Lib.App\\configuration.json");
                    plc.Initialize(safety: false);
                    plc.Compile();
                    plc.Download(DownloadOptions.Hardware | DownloadOptions.Software);
                    plc.Stop();
                    
                    // Pause for the user at the end
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();

                    plc.Start(); 
                } // plc should be disposed here

            }
            catch (System.IO.FileNotFoundException ex)
            {
                Log.Error(
                    "File not found: {FileName}\n"+
                    "Ensure that the required file or assembly '{FileName}' is available and properly referenced in your project. " +
                    "This is typically caused by a missing or misconfigured dependency. If you are using TIA Portal, verify that the " +
                    "'Siemens.Engineering.dll' (Version 17.0.0.0) is installed and available in the expected location.",
                    ex.FileName ?? "Unknown", ex.FileName
                );
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
