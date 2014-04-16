using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Lucene.Net.Store;
using Lucene.Net.Store.Azure;
using Microsoft.Owin;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using Microsoft.Owin.StaticFiles.Infrastructure;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json.Linq;
using NuGet.Indexing;
using NuGet.Services.Http;
using NuGet.Services.ServiceModel;
using Owin;

namespace NuGet.Services.Search
{
    public class SearchService : NuGetHttpService
    {
        private PackageSearcherManager _searcherManager;

        public override PathString BasePath
        {
            get { return new PathString("/search"); }
        }

        public PackageSearcherManager SearcherManager
        {
            get { return _searcherManager; }
        }

        public SearchService(ServiceName name, ServiceHost host)
            : base(name, host)
        {
        }

        protected override Task<bool> OnStart()
        {
            // Load the index
            ReloadIndex();

            // Set up reloading
            try
            {
                if (RoleEnvironment.IsAvailable)
                {
                    RoleEnvironment.Changing += (_, __) =>
                    {
                        ReloadIndex();
                    };
                }
            }
            catch (Exception)
            {
                // Ignore failures, they will only occur outside of Azure
            }

            return base.OnStart();
        }

        public override IEnumerable<EventSource> GetEventSources()
        {
            yield return SearchServiceEventSource.Log;
            yield return IndexingEventSource.Log;
        }

        protected override Task OnRun()
        {
            return WaitForShutdown();
        }

        // This could be moved up to the service platform
        private static readonly object Unit = new object();
        private Task WaitForShutdown()
        {
            var tcs = new TaskCompletionSource<object>();
            Host.ShutdownToken.Register(() => tcs.SetResult(Unit)); // Don't want to return null, just a useless object
            return tcs.Task;
        }

        protected override void Configure(IAppBuilder app)
        {
            // Configure the app
            app.UseErrorPage();
            
            SharedOptions sharedStaticFileOptions = new SharedOptions()
            {
                RequestPath = new PathString("/console"),
                FileSystem = new EmbeddedResourceFileSystem("NuGet.Services.Search.Console")
            };

            Func<PackageSearcherManager> searcherManagerThunk = () => SearcherManager;

            app.Use(async (context, next) =>
            {
                if (String.Equals(context.Request.Path.Value, "/reloadIndex", StringComparison.OrdinalIgnoreCase))
                {
                    if (context.Request.User == null || !context.Request.User.IsInRole(Roles.Admin))
                    {
                        context.Authentication.Challenge();
                    }
                    else
                    {
                        ReloadIndex();
                        context.Response.StatusCode = 200;
                        await context.Response.WriteAsync("Reload started.");
                        return;
                    }
                }
                await next();
            });

            // Just a little bit of rewriting. Not the full UseDefaultFiles middleware, just a quick hack
            app.Use(async (context, next) =>
            {
                if (String.Equals(context.Request.Path.Value, "/console", StringComparison.OrdinalIgnoreCase))
                {
                    // Redirect to trailing slash to maintain relative links
                    context.Response.Redirect(context.Request.PathBase + context.Request.Path + "/");
                    context.Response.StatusCode = 301;
                    return;
                }
                else if (String.Equals(context.Request.Path.Value, "/console/", StringComparison.OrdinalIgnoreCase))
                {
                    context.Request.Path = new PathString("/console/Index.html");
                }
                await next();
            });
            app.UseStaticFiles(new StaticFileOptions(sharedStaticFileOptions));
            
            // Public endpoint(s)
            app.Use(typeof(QueryMiddleware), "/query", searcherManagerThunk);
               
            // Admin endpoints
            app.Use(typeof(DiagMiddleware), "/diag", searcherManagerThunk);
            app.Use(typeof(FieldsMiddleware), "/fields", searcherManagerThunk);
            app.Use(typeof(RangeMiddleware), "/range", searcherManagerThunk);
            app.Use(typeof(SegmentsMiddleware), "/segments", searcherManagerThunk);

            app.Use(async (context, next) =>
            {
                // Handle root requests
                if (!context.Request.Path.HasValue)
                {
                    JObject response = new JObject();
                    response.Add("name", ServiceName.ToString());
                    response.Add("service", ServiceName.Name);

                    JObject resources = new JObject();
                    response.Add("resources", resources);

                    resources.Add("range", MakeUri(context, "/range"));
                    resources.Add("fields", MakeUri(context, "/fields"));
                    resources.Add("console", MakeUri(context, "/console"));
                    resources.Add("diagnostics", MakeUri(context, "/diag"));
                    resources.Add("segments", MakeUri(context, "/segments"));
                    resources.Add("query", MakeUri(context, "/query"));

                    if (context.Request.User != null && context.Request.User.IsInRole(Roles.Admin))
                    {
                        resources.Add("reloadIndex", MakeUri(context, "/reloadIndex"));
                    }

                    await SearchMiddleware.WriteResponse(context, response.ToString());
                }
            });
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

        private string MakeUri(IOwinContext context, string path)
        {
            return new UriBuilder(context.Request.Uri)
            {
                Path = (context.Request.PathBase + new PathString(path)).Value
            }.Uri.AbsoluteUri;
        }

        private PackageSearcherManager CreateSearcherManager()
        {
            Trace.TraceInformation("InitializeSearcherManager: new PackageSearcherManager");

            SearchConfiguration config = Configuration.GetSection<SearchConfiguration>();
            var searcher = GetSearcherManager(config);
            searcher.Open(); // Ensure the index is initially opened.
            IndexingEventSource.Log.LoadedSearcherManager();
            return searcher;
        }

        private PackageSearcherManager GetSearcherManager(SearchConfiguration config)
        {
            Directory dir;
            Rankings rankings;
            DownloadCounts downloadCounts;
            if (String.IsNullOrEmpty(config.IndexPath))
            {
                CloudStorageAccount storageAccount = Configuration.Storage.Primary;

                config.IndexContainer = String.IsNullOrEmpty(config.IndexContainer) ?
                    "ng-search" :
                    config.IndexContainer;

                string url = storageAccount.CreateCloudBlobClient().GetContainerReference(config.IndexContainer).Uri.AbsoluteUri;

                IndexingEventSource.Log.LoadingSearcherManager(url);

                dir = new AzureDirectory(storageAccount, config.IndexContainer, new RAMDirectory());
                rankings = new StorageRankings(storageAccount, config.IndexContainer);
                downloadCounts = new StorageDownloadCounts(storageAccount, config.IndexContainer);
            }
            else
            {
                IndexingEventSource.Log.LoadingSearcherManager(config.IndexPath);

                dir = new SimpleFSDirectory(new System.IO.DirectoryInfo(config.IndexPath));
                rankings = new FolderRankings(config.IndexPath);
                downloadCounts = new FolderDownloadCounts(config.IndexPath);
            }
            return new PackageSearcherManager(dir, rankings, downloadCounts);
        }

        private void ReloadIndex()
        {
            SearchServiceEventSource.Log.ReloadingIndex();
            PackageSearcherManager newIndex = CreateSearcherManager();
            Interlocked.Exchange(ref _searcherManager, newIndex);
            SearchServiceEventSource.Log.ReloadedIndex();
        }
    }
}