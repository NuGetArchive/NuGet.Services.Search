using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services.TestFramework
{
    public class RunConfiguration
    {
        public static readonly RunConfiguration Default = new RunConfiguration();

        public Uri ServiceRoot { get; private set; }

        private RunConfiguration()
        {
            ServiceRoot = new Uri("https://api.nuget.org");
        }

        private RunConfiguration(string root)
        {
            ServiceRoot = new Uri(root);
        }

        public static RunConfiguration FromEnvironment()
        {
            string root = Environment.GetEnvironmentVariable("NUGET_TEST_SERVICEROOT");
            if (String.IsNullOrEmpty(root))
            {
                return Default;
            }
            else
            {
                return new RunConfiguration(root);
            }
        }
    }
}
