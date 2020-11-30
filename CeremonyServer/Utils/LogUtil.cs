using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CeremonyServer
{
    internal sealed partial class LogUtil
    {
        public static LogUtil instance;

        private string log_dictionary;

        public static LogUtil Default
        {
            get
            {
                if (instance == null)
                    instance = new LogUtil();

                return instance;
            }
        }

        public LogUtil()
        {
            log_dictionary = Path.Combine(AppContext.BaseDirectory, "Logs");
        }

        public void Log(string message)
        {
            DateTime now = DateTime.Now;
            string line = $"[{now.TimeOfDay:hh\\:mm\\:ss}] {message}";
            Console.WriteLine(line);
            if (string.IsNullOrEmpty(log_dictionary)) return;
            lock (log_dictionary)
            {
                Directory.CreateDirectory(log_dictionary);
                string path = Path.Combine(log_dictionary, $"{now:yyyy-MM-dd}.log");
                File.AppendAllLines(path, new[] { line });
            }
        }
    }
}
