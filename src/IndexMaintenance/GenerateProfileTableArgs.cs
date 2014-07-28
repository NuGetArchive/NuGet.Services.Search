using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PowerArgs;

namespace IndexMaintenance
{
    public class GenerateProfileTableArgs
    {
        [ArgRequired]
        [ArgPosition(0)]
        [ArgShortcut("d")]
        [ArgDescription("The file to write the table to")]
        public string Destination { get; set; }
    }
}
