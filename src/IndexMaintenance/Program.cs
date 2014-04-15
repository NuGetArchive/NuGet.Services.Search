using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PowerArgs;

namespace IndexMaintainance
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0 && String.Equals(args[0], "dbg", StringComparison.OrdinalIgnoreCase))
            {
                args = args.Skip(1).ToArray();
                Debugger.Launch();
            }

            if (args.Length == 0)
            {
                WriteUsage();
            }
            else
            {
                Args.InvokeAction<Arguments>(args);
            }
        }

        private static void WriteUsage()
        {
            ArgUsage.GetStyledUsage<Arguments>().Write();
        }
    }
}
