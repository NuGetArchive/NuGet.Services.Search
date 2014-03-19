using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PowerArgs;

namespace IndexMaintainance
{
    public class PerfTestArgs
    {
        [ArgExistingFile]
        [ArgShortcut("l")]
        [ArgDescription("A file containing a list of queries to use, separated by lines")]
        public string QueryList { get; set; }

        [ArgShortcut("q")]
        [ArgDescription("A list of queries to run")]
        public string[] Queries { get; set; }

        [DefaultValue(1)]
        [ArgShortcut("r")]
        [ArgDescription("Specifies how many times to run the query file (defaults 1)")]
        public int Runs { get; set; }

        [ArgRequired]
        [ArgShortcut("url")]
        [ArgDescription("The URL to run the test against")]
        public Uri TargetServiceUri { get; set; }
    }
}
