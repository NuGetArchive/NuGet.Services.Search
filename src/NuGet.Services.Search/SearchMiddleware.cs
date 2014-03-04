using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Store;
using Lucene.Net.Store.Azure;
using Microsoft.Owin;
using Microsoft.WindowsAzure.Storage;
using NuGet.Indexing;

namespace NuGet.Services.Search
{
    public abstract class SearchMiddleware : OwinMiddleware
    {
        public SearchMiddlewareConfiguration Config { get; private set; }
        public PathString BasePath { get; private set; }

        protected SearchMiddleware(OwinMiddleware next, string basePath, SearchMiddlewareConfiguration config)
            : base(next)
        {
            Config = config;
            BasePath = new PathString(basePath);
        }

        public sealed override Task Invoke(IOwinContext context)
        {
            if (context.Request.Path.StartsWithSegments(BasePath))
            {
                return Execute(context);
            }
            else
            {
                // Not a request for our base path, ignore it.
                return Next.Invoke(context);
            }
        }

        public static Task WriteResponse(IOwinContext context, string content)
        {
            context.Response.Headers.Add("Pragma", new string[] { "no-cache" });
            context.Response.Headers.Add("Cache-Control", new string[] { "no-cache" });
            context.Response.Headers.Add("Expires", new string[] { "0" });
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync(content);
        }

        protected abstract Task Execute(IOwinContext context);

        protected void TraceException(Exception e)
        {
            Trace.TraceError(e.GetType().Name);
            Trace.TraceError(e.Message);
            Trace.TraceError(e.StackTrace);

            if (e.InnerException != null)
            {
                TraceException(e.InnerException);
            }
        }

        // Doing this every request in order to allow config changes to be live without restarting the site.
        // If this becomes a perf issue, we can use a straightforward caching technique to handle it using the RoleEnvironment.Changing event:
        //  http://msdn.microsoft.com/en-us/library/microsoft.windowsazure.serviceruntime.roleenvironment.changing.aspx
        protected PackageSearcherManager GetSearcherManager()
        {
            Trace.TraceInformation("InitializeSearcherManager: new PackageSearcherManager");

            Lucene.Net.Store.Directory directory = GetDirectory();
            Rankings rankings = GetRankings();
            return new PackageSearcherManager(directory, rankings);
        }

        public static Task<bool> IsAdmin(IOwinContext context)
        {
            return IsAdmin(context, challenge: true);
        }

        public static async Task<bool> IsAdmin(IOwinContext context, bool challenge)
        {
            await context.Authentication.AuthenticateAsync("AdminKey");
            if (context.Request.User != null && context.Request.User.IsInRole(Roles.Admin))
            {
                return true;
            }
            else
            {
                if (challenge)
                {
                    context.Authentication.Challenge("AdminKey");
                }
                return false;
            }
        }

        private Lucene.Net.Store.Directory GetDirectory()
        {
            if (Config.UseStorage)
            {
                CloudStorageAccount storageAccount = Config.StorageAccount;

                Trace.TraceInformation("GetDirectory using storage. Container: {0}", Config.StorageContainer);

                return new AzureDirectory(storageAccount, Config.StorageContainer, new RAMDirectory());
            }
            else
            {
                string fileSystemPath = Config.LocalIndexPath;

                Trace.TraceInformation("GetDirectory using filesystem. Folder: {0}", fileSystemPath);

                return new SimpleFSDirectory(new DirectoryInfo(fileSystemPath));
            }
        }

        private Rankings GetRankings()
        {
            if (Config.UseStorage)
            {
                CloudStorageAccount storageAccount = Config.StorageAccount;

                Trace.TraceInformation("Rankings from storage.");

                return new StorageRankings(storageAccount);
            }
            else
            {
                string folder = Config.LocalIndexPath;

                Trace.TraceInformation("Rankings from folder.");

                return new FolderRankings(folder);
            }
        }
    }
}
