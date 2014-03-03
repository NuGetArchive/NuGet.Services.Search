using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PowerArgs;

namespace IndexMaintainance
{
    public class IndexAddsArgs : IndexWriteArgs
    {
        [ArgShortcut("-f")]
        [ArgDescription("When using blob storage force unlock the index for write")]
        public bool Force { get; set; }

        [ArgShortcut("-c")]
        [ArgDescription("The clear the current index before starting")]
        public bool Clear { get; set; }
    }
}
