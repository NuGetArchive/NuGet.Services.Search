using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Store;

// Just pull one type in from System.IO because Directory conflicts between System.IO and Lucene.Net.Store.
using InvalidDataException = System.IO.InvalidDataException;

namespace NuGet.Indexing
{
    /// <summary>
    /// Represents a Search Index for NuGet packages
    /// </summary>
    public class PackageIndex
    {
        private Directory _directory;
        
        /// <summary>
        /// Gets a boolean indicating if the index existed AT THE TIME Open was called.
        /// </summary>
        public bool Exists { get; private set; }

        /// <summary>
        /// Gets the metadata for the most recent commit to the index
        /// </summary>
        public CommitMetadata LatestCommit { get; private set; }

        public PackageIndex(Directory directory)
        {
            _directory = directory;
        }

        /// <summary>
        /// Loads metadata for the directory and prepares it for use
        /// </summary>
        public void LoadMetadata()
        {
            Exists = IndexReader.IndexExists(_directory);
            
            if (Exists)
            {
                var dict = IndexReader.GetCommitUserData(_directory);
                if (dict == null)
                {
                    throw new InvalidDataException(Strings.PackageIndex_IndexHasNoCommits);
                }
                LatestCommit = CommitMetadata.FromDictionary(dict);
            }
        }
    }
}