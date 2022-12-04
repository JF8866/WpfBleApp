using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfBleApp.Utils
{
    public class Log
    {
        private static string Timestamp => $"{DateTime.Now:HH:mm:ss.fff}";

        public static void Info(string msg)
        {
            Console.WriteLine($"{Timestamp} - {msg}");
        }
    }
}
