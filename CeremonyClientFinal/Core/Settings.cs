using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CeremonyClientFinal.Core
{
    public class Settings
    {
        public string server_url = "ceremony_server_url";

        public int nTotalSegments = 2179311;
        public static Settings Default = new Settings();
    }
}
