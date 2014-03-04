﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Indexing
{
    /// <summary>
    /// Rebuilds the index from scratch.
    /// </summary>
    public class FullBuildTask : IndexTask
    {
        public bool Force { get; set; }
        
        public override void Execute()
        {
            DateTime before = DateTime.Now;

            if (Force && StorageAccount != null && !string.IsNullOrEmpty(Container))
            {
                AzureDirectoryManagement.ForceUnlockAzureDirectory(StorageAccount, Container);
            }

            Lucene.Net.Store.Directory directory = GetDirectory();

            // Recreate the index
            PackageIndexing.CreateNewEmptyIndex(directory);
            
            PackageIndexing.BuildIndex(SqlConnectionString, directory);

            DateTime after = DateTime.Now;
            Log.WriteLine("duration = {0} seconds", (after - before).TotalSeconds);
        }
    }
}
