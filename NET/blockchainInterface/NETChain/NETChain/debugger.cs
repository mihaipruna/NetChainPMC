using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace NETChain
{
    internal static class debugger
    {
        static List<string> loglines;
        internal static void logev(string evlog)
        {
            if (loglines == null)
            {
                loglines = new List<string>();

            }
            loglines.Add(DateTime.Now.ToString() + " " + evlog);
            string dtf = utils.AssemblyDirectory + @"\"+ DateTime.Today.ToString().Replace(@"/", "_").Replace(@"\", "_").Replace(":", "-").Replace(";", "-").Replace(".", "_") + "_log.txt";

            System.IO.File.WriteAllLines(dtf, loglines.ToArray());
        }

        internal static void initlog()
        {
            string dtf = utils.AssemblyDirectory + @"\" + DateTime.Today.ToString().Replace(@"/", "_").Replace(@"\", "_").Replace(":", "-").Replace(";", "-").Replace(".", "_") + "_log.txt";
            if (File.Exists(dtf))
            {
                loglines = File.ReadAllLines(dtf).ToList();
            }
        }
    }
        
        
        
}
