using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace CloudflareDNSUpdater
{
    internal static class Logger
    {
        internal static void LogInfo(string logFilePath, string logMessage)
        {
            Log(logFilePath, "INFO", logMessage);
        }

        internal static void LogError(string logFilePath, string errorMessage)
        {
            Log(logFilePath, "ERROR", errorMessage);
        }

        private static void Log(string logFilePath, string logType, string message)
        {
            string newLog = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {logType,-10} - {message}\n";
            File.AppendAllText(logFilePath, newLog);

            Console.WriteLine(message);
        }
    }
}
