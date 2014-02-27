using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.Indexing.Model
{
    /// <summary>
    /// Represents non-indexed data relating to a package registration
    /// </summary>
    public class PackageRegistrationData
    {
        public int Key { get; set; }
        public string Id { get; set; }
        public int DownloadCount { get; set; }
        public IList<string> Owners { get; private set; }
    }
}
