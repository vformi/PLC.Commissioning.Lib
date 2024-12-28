using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using Serilog;

namespace PLC.Commissioning.Lib.App
{
    public class RegistryService
    {
        /// <summary>
        /// Generates a registry entry string for a specified file path.
        /// </summary>
        /// <param name="applicationPath">The file path of the application.</param>
        /// <param name="whitelistKeyName">The key name for the whitelist entry.</param>
        /// <returns>Formatted registry entry string.</returns>
        public static string GenerateRegistryEntry(string applicationPath, string whitelistKeyName)
        {
            try
            {
                // Validate file existence
                if (!File.Exists(applicationPath))
                    throw new FileNotFoundException($"The specified file does not exist: {applicationPath}");

                // File information
                FileInfo fileInfo = new FileInfo(applicationPath);
                DateTime lastWriteTimeUtc = fileInfo.LastWriteTimeUtc;
                string lastWriteTimeUtcFormatted = lastWriteTimeUtc.ToString(@"yyyy/MM/dd HH:mm:ss.fff");

                // Compute SHA256 hash
                string convertedHash;
                using (HashAlgorithm hashAlgorithm = SHA256.Create())
                using (FileStream stream = File.OpenRead(applicationPath))
                {
                    byte[] hash = hashAlgorithm.ComputeHash(stream);
                    convertedHash = Convert.ToBase64String(hash);
                }

                // Build the registry entry
                string registryEntry =
                    $@"Windows Registry Editor Version 5.00
                    [HKEY_LOCAL_MACHINE\SOFTWARE\Siemens\Automation\Openness\17.0\Whitelist\{whitelistKeyName}]
                    [HKEY_LOCAL_MACHINE\SOFTWARE\Siemens\Automation\Openness\17.0\Whitelist\{whitelistKeyName}\Entry]
                    ""Path""=""{applicationPath.Replace("\\", "\\\\")}""
                    ""DateModified""=""{lastWriteTimeUtcFormatted}""
                    ""FileHash""=""{convertedHash}""";

                Log.Debug("Path: {Path}", applicationPath.Replace("\\", "\\\\"));
                Log.Debug("DateModified: {DateModified}", lastWriteTimeUtcFormatted);
                Log.Debug("Filehash: {FileHash}", convertedHash);

                return registryEntry;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Failed to generate registry entry.", ex);
            }
        }

        /// <summary>
        /// Saves the registry entry to a file.
        /// </summary>
        /// <param name="registryEntry">The registry entry content.</param>
        /// <param name="outputFilePath">The file path to save the registry entry.</param>
        public static void SaveRegistryEntryToFile(string registryEntry, string outputFilePath)
        {
            try
            {
                File.WriteAllText(outputFilePath, registryEntry);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Failed to save registry entry to file: {outputFilePath}", ex);
            }
        }

        /// <summary>
        /// Executes the specified .reg file with administrative privileges.
        /// </summary>
        /// <param name="filePath">The path to the .reg file.</param>
        public static void ExecuteRegistryFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("The .reg file does not exist.", filePath);
            }

            try
            {
                Log.Information("Requesting administrative privileges to execute registry file: {FilePath}", filePath);

                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "regedit.exe",
                        Arguments = $"/s \"{filePath}\"",
                        UseShellExecute = true,
                        Verb = "runas" // Ensures admin privileges
                    }
                };

                process.Start();
                process.WaitForExit();

                Log.Information("Successfully executed registry file: {FilePath}", filePath);
            }
            catch (System.ComponentModel.Win32Exception win32Ex) when (win32Ex.NativeErrorCode == 1223)
            {
                // Handle case when user cancels the UAC prompt
                Log.Warning("Registry execution canceled by the user.");
                throw new OperationCanceledException("Registry execution was canceled by the user.", win32Ex);
            }
            catch (Exception ex)
            {
                Log.Error("Failed to execute registry file. Error: {ErrorMessage}", ex.Message);
                throw new ApplicationException("Failed to execute the registry file. Ensure the program is running with administrative privileges.", ex);
            }
        }
    }
}
