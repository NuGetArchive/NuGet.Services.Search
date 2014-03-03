using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Indexing
{
    public class IndexAddsTask : IndexTask
    {
        public bool Force { get; set; }
        public bool Clear { get; set; }

        public override void Execute()
        {
            DateTime before = DateTime.Now;

            if (Force && StorageAccount != null && !string.IsNullOrEmpty(Container))
            {
                AzureDirectoryManagement.ForceUnlockAzureDirectory(StorageAccount, Container);
            }

            Lucene.Net.Store.Directory directory = GetDirectory();

            if (Clear)
            {
                PackageIndexing.CreateNewEmptyIndex(directory);
            }

            PackageIndexing.BuildIndex(SqlConnectionString, directory);

            DateTime after = DateTime.Now;
            Log.WriteLine("duration = {0} seconds", (after - before).TotalSeconds);
        }
    }
}
