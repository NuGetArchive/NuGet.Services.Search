using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Store;
using Lucene.Net.Store.Azure;
using Microsoft.WindowsAzure.Storage;

namespace NuGet.Indexing
{
    public abstract class IndexTask
    {
        public abstract void Execute();

        public TextWriter Log { get; set; }

        public CloudStorageAccount StorageAccount { get; set; }
        public string Container { get; set; }
        public string Folder { get; set; }
        public string FrameworksFile { get; set; }
        public string SqlConnectionString { get; set; }
        public bool WhatIf { get; set; }

        public IndexTask()
        {
            Log = Console.Out;
        }

        protected Lucene.Net.Store.Directory GetDirectory()
        {
            Lucene.Net.Store.Directory directory = null;

            if (!string.IsNullOrEmpty(Container))
            {
                directory = new AzureDirectory(StorageAccount, Container, new RAMDirectory());
            }
            else if (!string.IsNullOrEmpty(Folder))
            {
                directory = new SimpleFSDirectory(new DirectoryInfo(Folder));
            }

            if (directory == null)
            {
                throw new Exception("You must specify either a folder or container");
            }

            return directory;
        }

        protected FrameworksList GetFrameworksList()
        {
            if (!String.IsNullOrEmpty(FrameworksFile))
            {
                return new LocalFrameworksList(FrameworksFile);
            }

            if (String.IsNullOrEmpty(Container))
            {
                var path = LocalFrameworksList.GetFileName(Folder);
                if (!File.Exists(path))
                {
                    Log.WriteLine("WARNING: Could not find Frameworks list in '{0}'. Supported Framework Data may be inaccurate", path);
                }
                return new LocalFrameworksList(path);
            }
            else
            {
                return new StorageFrameworksList(StorageAccount, Container);
            }
        }

        protected PackageSearcherManager GetSearcherManager()
        {
            PackageSearcherManager manager;
            if (!string.IsNullOrEmpty(Container))
            {
                manager = new PackageSearcherManager(
                    GetDirectory(),
                    new StorageRankings(StorageAccount, Container),
                    new StorageDownloadCounts(StorageAccount, Container),
                    GetFrameworksList());
            }
            else if (!string.IsNullOrEmpty(Folder))
            {
                manager = new PackageSearcherManager(
                    GetDirectory(),
                    new FolderRankings(Folder),
                    new FolderDownloadCounts(Folder),
                    GetFrameworksList());
            }
            else
            {
                throw new Exception("You must specify either a folder or container");
            }

            manager.Open();
            return manager;
        }
    }
}
