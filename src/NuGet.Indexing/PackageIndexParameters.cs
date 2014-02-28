using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Indexing
{
    public class PackageIndexParameters
    {
        public static readonly int DefaultMergeFactor = 10;                 //  Define the size of a file in a level (exponentially) and the count of files that constitue a level
        public static readonly int DefaultMaxDocumentsPerCommit = 800;      //  The maximum number of Lucene documents in a single commit. The min size for a segment.
        public static readonly int DefaultMaxMergeDocuments = 7999;         //  Except never merge segments that have more docs than this 

        public int MergeFactor { get; private set; }
        public int MaxDocumentsPerCommit { get; private set; }
        public int MaxMergeDocuments { get; private set; }
        public BoostFactors Boosts { get; private set; }

        public PackageIndexParameters() : this(DefaultMergeFactor, DefaultMaxDocumentsPerCommit, DefaultMaxMergeDocuments, new BoostFactors())
        {
        }

        public PackageIndexParameters(int mergeFactor, int maxDocumentsPerCommit, int maxMergeDocuments, BoostFactors boosts)
        {
            MergeFactor = mergeFactor;
            MaxDocumentsPerCommit = maxDocumentsPerCommit;
            maxMergeDocuments = maxMergeDocuments;
            Boosts = boosts;
        }
    }
}
