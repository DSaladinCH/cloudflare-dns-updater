using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudflareDNSUpdater
{
    internal class Configuration
    {
        public string BearerToken { get; set; } = "";
        public List<string> ZoneIdentifiers { get; set; } = new();
    }
}
