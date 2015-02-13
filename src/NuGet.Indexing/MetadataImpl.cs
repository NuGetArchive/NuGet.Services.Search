using Lucene.Net.Index;
using Lucene.Net.Search;
using Microsoft.Owin;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Indexing
{
    public static class MetadataImpl
    {
        public static void CheckAccess(IOwinContext context, string tenantId, NuGetSearcherManager searcherManager, string connectionString)
        {
            searcherManager.MaybeReopen();

            IndexSearcher searcher = searcherManager.Get();

            try
            {
                string relativeUri = context.Request.Path.ToString();

                string[] parts = relativeUri.Split('/');

                string containerName = parts[parts.Length - 2];
                string blobName = parts[parts.Length - 1];

                CloudStorageAccount account = CloudStorageAccount.Parse(connectionString);
                CloudBlobClient client = account.CreateCloudBlobClient();
                CloudBlobContainer container = client.GetContainerReference(containerName);
                CloudBlockBlob blob = container.GetBlockBlobReference(blobName);

                SharedAccessBlobPolicy sharedPolicy = new SharedAccessBlobPolicy()
                {
                    SharedAccessExpiryTime = DateTime.UtcNow.AddSeconds(10),
                    Permissions = SharedAccessBlobPermissions.Read
                };

                string sharedAccessSignature = blob.GetSharedAccessSignature(sharedPolicy);

                context.Response.Redirect(blob.Uri + sharedAccessSignature);
            }
            finally
            {
                searcherManager.Release(searcher);
            }
        }
    }
}
