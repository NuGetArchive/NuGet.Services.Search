using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PowerArgs;

namespace IndexMaintainance
{
    public class ParseQueryArgs
    {
        [ArgRequired]
        [ArgPosition(0)]
        [ArgShortcut("q")]
        public string Query { get; set; }
    }
}
