using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowBot
{
    internal class Utils
    {
        public static readonly HttpClient GlobalHttpClient = 
            new HttpClient();

        public static void PressAnyKeyToContinue()
        {
            Console.WriteLine("Press any key to continue");
            Console.ReadKey(true);
        }
    }
}
