using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Indexing
{
    /// <summary>
    /// Parameters used to control the Lucene index
    /// </summary>
    public class PackageIndexParameters
    {
        public static readonly int DefaultMergeFactor = 10;
        public static readonly int DefaultMaxDocumentsPerCommit = 800;
        public static readonly int DefaultMaxMergeDocuments = 7999;

        /// <summary>
        /// Define the size of a file in a level (exponentially) and the count of files that constitue a level
        /// </summary>
        public int MergeFactor { get; set; }
        
        /// <summary>
        /// The maximum number of Lucene documents in a single commit. The min size for a segment.
        /// </summary>
        public int MaxDocumentsPerCommit { get; set; }

        /// <summary>
        /// Never merge segments that have more docs than this 
        /// </summary>
        public int MaxMergeDocuments { get; set; }

        /// <summary>
        /// Boost factors to apply to fields in this index
        /// </summary>
        public BoostFactors Boosts { get; set; }

        internal bool NeverDeleteCommits { get; set; }

        public PackageIndexParameters() : this(DefaultMergeFactor, DefaultMaxDocumentsPerCommit, DefaultMaxMergeDocuments, new BoostFactors())
        {
        }

        public PackageIndexParameters(int mergeFactor, int maxDocumentsPerCommit, int maxMergeDocuments, BoostFactors boosts)
        {
            MergeFactor = mergeFactor;
            MaxDocumentsPerCommit = maxDocumentsPerCommit;
            MaxMergeDocuments = maxMergeDocuments;
            Boosts = boosts;
            NeverDeleteCommits = false;
        }
    }
}
