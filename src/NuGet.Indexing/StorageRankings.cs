using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Indexing
{
    public class StorageRankings : Rankings
    {
        CloudBlockBlob _blob;
        
        public override string Path { get { return _blob.Uri.AbsoluteUri; } }

        public StorageRankings(CloudStorageAccount account, string container)
            : this(GetBlob(account, container, folder: null)) { }
        public StorageRankings(CloudStorageAccount account, string container, string folder)
            : this(GetBlob(account, container, folder)) { }

        public StorageRankings(CloudBlockBlob blob)
        {
            _blob = blob;
        }

        protected override JObject LoadJson()
        {
            if (!_blob.Exists())
            {
                return null;
            }
            string json = _blob.DownloadText();
            JObject obj = JObject.Parse(json);
            return obj;
        }

        private static CloudBlockBlob GetBlob(CloudStorageAccount account, string containerName, string folder)
        {
            var container = account.CreateCloudBlobClient().GetContainerReference(containerName);
            return container.GetBlockBlobReference(
                String.IsNullOrEmpty(folder) ?
                    ReportName :
                    (folder + "/" + ReportName));
        }
    }
}
