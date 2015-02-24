using Lucene.Net.Search;
using Microsoft.Owin;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Net;

namespace NuGet.Indexing
{
    public static class MetadataImpl
    {
        public static void Access(IOwinContext context, string tenantId, SecureSearcherManager searcherManager, string connectionString, int accessDuration)
        {
            searcherManager.MaybeReopen();

            IndexSearcher searcher = searcherManager.Get();

            try
            {
                string relativeUri = context.Request.Path.ToString();

                string[] parts = relativeUri.Split('/');

                if (parts.Length >= 3)
                {
                    string containerName = parts[parts.Length - 2];
                    string blobName = parts[parts.Length - 1];

                    CloudStorageAccount account = CloudStorageAccount.Parse(connectionString);
                    CloudBlobClient client = account.CreateCloudBlobClient();
                    CloudBlobContainer container = client.GetContainerReference(containerName);
                    CloudBlockBlob blob = container.GetBlockBlobReference(blobName);

                    SharedAccessBlobPolicy sharedPolicy = new SharedAccessBlobPolicy()
                    {
                        SharedAccessExpiryTime = DateTime.UtcNow.AddSeconds(accessDuration),
                        Permissions = SharedAccessBlobPermissions.Read
                    };

                    string sharedAccessSignature = blob.GetSharedAccessSignature(sharedPolicy);

                    context.Response.Redirect(blob.Uri + sharedAccessSignature);
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                }
            }
            finally
            {
                searcherManager.Release(searcher);
            }
        }
    }
}
