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
        public string DataContainer { get; set; }
        public string Folder { get; set; }
        public string FrameworksFile { get; set; }
        public string SqlConnectionString { get; set; }
        public bool WhatIf { get; set; }

        public IndexTask()
        {
            Log = Console.Out;
        }

        protected PackageSearcherManager GetSearcherManager()
        {
            PackageSearcherManager manager;
            if (!string.IsNullOrEmpty(Container))
            {
                manager = PackageSearcherManager.CreateAzure(
                    StorageAccount,
                    Container,
                    DataContainer);
            }
            else if (!string.IsNullOrEmpty(Folder))
            {
                manager = PackageSearcherManager.CreateLocal(
                    Folder,
                    FrameworksFile);
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
