using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.Indexing
{
    public class PackageRegistration
    {
        public IEnumerable<string> Owners { get; set; }
    }
}
