using System;
using System.Collections.Generic;
using System.Globalization;
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
            IndexingEventSource.Log.LoadingMetadata(_directory.GetType());
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
            IndexingEventSource.Log.LoadedMetadata(_directory.GetType());
        }

        /// <summary>
        /// Adds a batch of documents to the index in one or more Lucene commits
        /// </summary>
        /// <remarks>
        /// The documents must be in order and have a higher Key value than the current "HighestPackageKey"
        /// </remarks>
        /// <param name="documents">The documents to commit</param>
        /// <param name="message">A message to add to the commit</param>
        public void AddNewDocuments(IEnumerable<PackageDocument> documents, string message)
        {
            var docList = documents.ToList();
            
            using (var writer = OpenWriter())
            {
                // Break the list up in to batches based on max commit size
                bool pendingCommit = false;
                int batch = 0;
                int currentHighestKey = LatestCommit == null ? -1 : LatestCommit.HighestPackageKey;
                for(int i = 0; i < docList.Count; i++)
                {
                    AddNewDocument(writer, docList[i], currentHighestKey);
                    currentHighestKey = docList[i].Key;
                    pendingCommit = true;
                    if (((i + 1) % Parameters.MaxDocumentsPerCommit) == 0)
                    {
                        Commit(
                            writer, 
                            message + String.Format(CultureInfo.CurrentCulture, Strings.PackageIndex_BatchCommitMessageSuffix, batch),
                            currentHighestKey);
                        batch++;
                        pendingCommit = false;
                    }
                }
                if (pendingCommit)
                {
                    Commit(
                        writer,
                        message + String.Format(CultureInfo.CurrentCulture, Strings.PackageIndex_BatchCommitMessageSuffix, batch),
                        currentHighestKey);
                }
            }
        }

        private void AddNewDocument(IndexWriter writer, PackageDocument doc, int currentHighestKey)
        {
            // Check the document against the latest commit
            if (doc.Key <= currentHighestKey)
            {
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.PackageIndex_DataOutOfOrder,
                    doc.Key,
                    currentHighestKey));
            }
            writer.AddDocument(LuceneDocumentConverter.ToLuceneDocument(doc, Parameters.Boosts));
        }

        private IndexWriter OpenWriter()
        {
            return new IndexWriter(
                _directory, 
                new NuGetAnalyzer(), 
                Parameters.NeverDeleteCommits ? 
                    (IndexDeletionPolicy)new DebugDeletionPolicy() :
                    (IndexDeletionPolicy)new KeepOnlyLastCommitDeletionPolicy(), 
                IndexWriter.MaxFieldLength.UNLIMITED);
        }

        private void Commit(IndexWriter writer, string message, int highestPackageKey)
        {
            var commit = new CommitMetadata(message, highestPackageKey);
            writer.Commit(commit);
            LatestCommit = commit;
        }

        private class DebugDeletionPolicy : IndexDeletionPolicy
        {
            public void OnCommit<T>(IList<T> commits) where T : IndexCommit
            {
            }

            public void OnInit<T>(IList<T> commits) where T : IndexCommit
            {
            }
        }
    }
}