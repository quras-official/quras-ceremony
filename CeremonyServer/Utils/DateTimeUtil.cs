using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CeremonyServer
{
    internal sealed partial class DateTimeUtil
    {
        public static DateTimeUtil instance;

        public static DateTimeUtil Default
        {
            get
            {
                if (instance == null)
                    instance = new DateTimeUtil();

                return instance;
            }
        }

        public uint GetTimeStamp(DateTime datetime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            uint timestamp = ((uint)(datetime.ToUniversalTime() - epoch).TotalSeconds);

            return timestamp;
        }
    }
}
