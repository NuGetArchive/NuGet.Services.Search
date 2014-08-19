using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IndexMaintainance;
using PowerArgs;

namespace IndexMaintenance
{
    public class PushIndexArgs
    {
        [ArgShortcut("-dir")]
        [ArgDescription("The file system folder containing the index to push")]
        public string SourceFolder { get; set; }

        [ArgShortcut("-st")]
        [ArgDescription("The connection string to the storage server to push the index to")]
        public string DestinationStorage { get; set; }

        [ArgShortcut("-cont")]
        [ArgDescription("The Blob Storage Container to push the index to")]
        public string Container { get; set; }
    }
}
