﻿using System;
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
        public string SqlConnectionString { get; set; }
        public bool WhatIf { get; set; }

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

        protected PackageSearcherManager GetSearcherManager()
        {
            if (!string.IsNullOrEmpty(Container))
            {
                return new PackageSearcherManager(
                    GetDirectory(),
                    new StorageRankings(StorageAccount));
            }
            else if (!string.IsNullOrEmpty(Folder))
            {
                return new PackageSearcherManager(
                    GetDirectory(),
                    new FolderRankings(Folder));
            }
            else
            {
                throw new Exception("You must specify either a folder or container");
            }
        }
    }
}
