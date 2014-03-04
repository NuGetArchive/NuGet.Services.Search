using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Indexing;
using NuGetGallery;

namespace NuGet.Indexing
{
    public class IndexDocumentData
    {
        public Package Package { get; set; }
        public int Checksum {get; set; }
        public IEnumerable<string> Feeds { get; set; }
    }
}
