using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Documents;

namespace NuGet.Indexing.Model
{
    /// <summary>
    /// Represents a package document in the Lucene Index
    /// </summary>
    /// <remarks>
    /// This is the object that is used to carry values within the Lucene Index. Any 
    /// </remarks>
    public class PackageDocument
    {
        /// <summary>
        /// Gets or sets the unique key for this package in the database
        /// </summary>
        public int Key { get; set; }

        /// <summary>
        /// Gets or sets a checksum value used to detect changes from the database
        /// </summary>
        public int Checksum { get; set; }

        public string Id { get; set; }
        public string Version { get; set; }
        public string Title { get; set; }
        public string Tags { get; set; }
        public string Description { get; set; }
        public string Authors { get; set; }
        public bool IsLatest { get; set; }
        public bool IsLatestStable { get; set; }
        public bool IsListed { get; set; }
        public DateTime LastUpdated { get; set; }
        public DateTime Published { get; set; }
        public DateTime? LastEdited { get; set; }

        public PackageData Payload { get; set; }

        /// <summary>
        /// Gets a list of usernames representing the owners of the package
        /// </summary>
        public IList<string> Owners { get; private set; }

        /// <summary>
        /// Gets a list of feed names representing feeds WITHIN the service that contain the package
        /// </summary>
        public IList<string> Feeds { get; private set; }

        /// <summary>
        /// Gets a list of supported target frameworks for this package
        /// </summary>
        public IList<string> SupportedFrameworks { get; private set; }

        public PackageDocument()
        {
            Owners = new List<string>();
            Feeds = new List<string>();
            SupportedFrameworks = new List<string>();
        }
    }
}
