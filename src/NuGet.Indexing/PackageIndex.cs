using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Store;
using NuGet.Indexing.Analysis;
using NuGet.Indexing.Model;
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

        /// <summary>
        /// Gets the parameters used to manage the index
        /// </summary>
        public PackageIndexParameters Parameters { get; private set; }

        public PackageIndex(Directory directory) : this(directory, new PackageIndexParameters()) { }
        public PackageIndex(Directory directory, PackageIndexParameters parameters)
        {
            Parameters = parameters;

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

        /// <summary>
        /// Adds a batch of documents to the index in one or more Lucene commits
        /// </summary>
        /// <remarks>
        /// Depending on the value of Max
        /// </remarks>
        /// <param name="documents">The documents to commit</param>
        /// <param name="message">A message to add to the commit</param>
        public void CommitDocuments(IEnumerable<PackageDocument> documents, string message)
        {
            using (var writer = new IndexWriter(_directory, new NuGetAnalyzer(), IndexWriter.MaxFieldLength.UNLIMITED))
            {
                foreach (var doc in documents)
                {
                    writer.AddDocument(LuceneDocumentConverter.ToLuceneDocument(doc, Parameters.Boosts));
                }
                var commit = new CommitMetadata(message);
                writer.Commit(commit);
                LatestCommit = commit;
            }
        }
    }
}