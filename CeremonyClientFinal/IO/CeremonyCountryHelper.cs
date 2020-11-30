using Ceremony.IO.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace Ceremony.IO
{
    public class CeremonyCountryHelper
    {
        private string CountryFile = "countryInfo";
        public JObject GetCountries()
        {
            ResourceManager rm = new ResourceManager("CeremonyClientFinal.Ceremony", Assembly.GetExecutingAssembly());
            string text = System.Text.Encoding.UTF8.GetString( (byte[])rm.GetObject(CountryFile) );
            return JArray.Parse(text);
        }
    }
}
