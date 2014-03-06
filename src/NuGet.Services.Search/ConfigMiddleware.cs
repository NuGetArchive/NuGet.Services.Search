using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json.Linq;

namespace NuGet.Services.Search
{
    public class ConfigMiddleware : SearchMiddleware
    {
        public ConfigMiddleware(OwinMiddleware next, string path, SearchMiddlewareConfiguration config) : base(next, path, config) { }

        protected override async Task Execute(IOwinContext context)
        {
            Trace.TraceInformation("Where");

            JObject response = new JObject();

            if (Config.UseStorage)
            {
                string accountName = Config.StorageAccount.Credentials.AccountName;
                response.Add("AccountName", accountName);
                response.Add("StorageContainer", Config.StorageContainer);

                CloudBlobClient client = Config.StorageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = client.GetContainerReference(Config.StorageContainer);
                CloudBlockBlob blob = container.GetBlockBlobReference("segments.gen");

                response.Add("IndexExists", blob.Exists());
            }
            else
            {
                response.Add("LocalIndexPath", Config.LocalIndexPath);
            }

            await WriteResponse(context, response.ToString());
        }
    }
}
