using System;
using System.Configuration;
using System.IO;
using System.Diagnostics;

namespace GameServerService
{
    public class Logger 
    {
        private readonly string logFilePath;

        public Logger()
        {
            // Retrieve log file path from configuration
            logFilePath = ConfigurationManager.AppSettings["logFilePath"];

            // Ensure the log directory exists
            string directoryPath = Path.GetDirectoryName(logFilePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        public void Log(string message)
        {
            Log(message, EventLogEntryType.Information); // Default to Information
        }

        public void Log(string message, EventLogEntryType entryType)
        {
            try
            {
                // Append the log message with the entry type to the file
                File.AppendAllText(logFilePath, $"{DateTime.Now} [{entryType}]: {message}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                // If writing to file fails, fallback to Event Log
                try
                {
                    if (!EventLog.SourceExists("GameServerService"))
                    {
                        EventLog.CreateEventSource("GameServerService", "Application");
                    }

                    using (var eventLog = new EventLog("Application"))
                    {
                        eventLog.Source = "GameServerService";
                        eventLog.WriteEntry($"Failed to log to file: {ex.Message}", EventLogEntryType.Error);
                    }
                }
                catch
                {
                    // Suppress any exception here to avoid service crash
                }
            }
        }
    }
}
