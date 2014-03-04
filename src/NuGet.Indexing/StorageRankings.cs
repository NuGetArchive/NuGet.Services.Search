﻿using Microsoft.WindowsAzure.Storage;
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

        public StorageRankings(string connectionString) : this(CloudStorageAccount.Parse(connectionString))
        {
        }

        public StorageRankings(CloudStorageAccount storageAccount) : this(
            storageAccount.CreateCloudBlobClient().GetContainerReference("ng-search"))
        {
        }

        public StorageRankings(CloudBlobContainer container) : this(container.GetBlockBlobReference(@"data\rankings.v1.json"))
        {
        }

        public StorageRankings(CloudBlockBlob blob)
        {
            _blob = blob;
        }

        protected override JObject LoadJson()
        {
            string json = _blob.DownloadText();
            JObject obj = JObject.Parse(json);
            return obj;
        }
    }
}
