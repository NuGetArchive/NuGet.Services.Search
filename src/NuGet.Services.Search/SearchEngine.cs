using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Store.Azure;
using NuGet.Services.Configuration;
using NuGet.Services.Search.Analysis;
using NuGet.Services.Search.Store;

namespace NuGet.Services.Search
{
    public class SearchEngine
    {
        private Directory _dir;

        public ConfigurationHub Config { get; private set; }
        public LuceneDirectoryManager DirectoryManager { get; private set; }

        public SearchEngine(ConfigurationHub config, LuceneDirectoryManager directoryManager)
        {
            Config = config;
            DirectoryManager = directoryManager;
        }

        public Task Update()
        {
            throw new NotImplementedException();
        }

        public void Load()
        {
            // Open the directory, creating if it does not exist
            _dir = DirectoryManager.Open(createIfNotExists: true);

            // Get commit metadata
            var metadata = IndexReader.GetCommitUserData(_dir);

            // Check for existing metadata
            if (metadata == null)
            {
                // Initialize the index
                InitializeIndex();
            }
        }

        private void InitializeIndex()
        {
            using (var writer = OpenWriter())
            {

            }
        }

        private IndexWriter OpenWriter()
        {
            return IndexWriter
        }

        private IndexReader OpenReader(bool readOnly)
        {
            return IndexReader.Open(_dir, readOnly);
        }
    }
}
