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
        internal static void Start(string startMessage)
        {
            string startText = $"""

                -------------------------------------------------------------------------------------------------

                  xxxxxxxxxxxxxxx   xxxxxxxxxxxxxxxxx     xxxxxxxxxxxxx     xxxxxxxxxxxxxxx     xxxxxxxxxxxxxxxxx
                 xxxxxxxxxxxxxxxx   xxxxxxxxxxxxxxxxx    xxxxxxxxxxxxxxx    xxxxxxxxxxxxxxxx    xxxxxxxxxxxxxxxxx
                xxxxx                     xxxxx         xxxxxx     xxxxxx   xxxxxx     xxxxxx         xxxxx      
                xxxxx                     xxxxx         xxxxx       xxxxx   xxxxx       xxxxx         xxxxx      
                 xxxxxxxxxxxxxx           xxxxx         xxxxxx     xxxxxx   xxxxxx     xxxxx          xxxxx      
                  xxxxxxxxxxxxxx          xxxxx         xxxxxxxxxxxxxxxxx   xxxxxxxxxxxxxxx           xxxxx      
                            xxxxx         xxxxx         xxxxxxxxxxxxxxxxx   xxxxxxxxxxxxxxx           xxxxx      
                            xxxxx         xxxxx         xxxxx       xxxxx   xxxxx      xxxxx          xxxxx      
                xxxxxxxxxxxxxxxx          xxxxx         xxxxx       xxxxx   xxxxx       xxxxx         xxxxx      
                xxxxxxxxxxxxxxx           xxxxx         xxxxx       xxxxx   xxxxx       xxxxx         xxxxx      

                {startMessage}


                """;

            if (!File.Exists("cloudflare.log"))
                startText = startText.TrimStart();

            File.AppendAllText("cloudflare.log", startText);
        }

        internal static void LogInfo(string logMessage)
        {
            Log("INFO", logMessage);
        }

        internal static void LogError(string errorMessage)
        {
            Log("ERROR", errorMessage);
        }

        private static void Log(string logType, string message)
        {
            string newLog = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {logType,-10} - {message}\n";
            File.AppendAllText("cloudflare.log", newLog);

            Console.WriteLine(message);
        }
    }
}
