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
        public override PathString BasePath
        {
            get { return new PathString("/search"); }
        }

        public SearchServiceApplication App { get; private set; }

        public SearchService(ServiceName name, ServiceHost host)
            : base(name, host)
        {
            App = new SearchServiceApplication(name, CreateSearcherManager);
        }

        protected override Task<bool> OnStart()
        {
            // Load the index
            App.ReloadIndex();

            // Set up reloading
            try
            {
                if (RoleEnvironment.IsAvailable)
                {
                    RoleEnvironment.Changing += (_, __) =>
                    {
                        App.ReloadIndex();
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
            App.Configure(app);
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
            Directory dir = CreateDirectory(config);
            Rankings rankings = CreateRankings(config);
            DownloadCounts downloadCounts = CreateDownloadCounts(config);
            return new PackageSearcherManager(dir, rankings, downloadCounts);
        }

        private Rankings CreateRankings(SearchConfiguration config)
        {
            if (!String.IsNullOrEmpty(config.StatsContainer))
            {
                return new StorageRankings(Configuration.Storage.Primary, config.StatsContainer);
            }
            else if(String.IsNullOrEmpty(config.IndexPath))
            {
                return new StorageRankings(Configuration.Storage.Primary, config.IndexContainer, "data");
            } 
            else 
            {
                return new FolderRankings(config.IndexPath);
            }
        }

        private DownloadCounts CreateDownloadCounts(SearchConfiguration config)
        {
            if (!String.IsNullOrEmpty(config.StatsContainer))
            {
                return new StorageDownloadCounts(Configuration.Storage.Primary, config.StatsContainer);
            }
            else if(String.IsNullOrEmpty(config.IndexPath))
            {
                return new StorageDownloadCounts(Configuration.Storage.Primary, config.IndexContainer, "data");
            } 
            else
            {
                return new FolderDownloadCounts(config.IndexPath);
            }
        }

        private Directory CreateDirectory(SearchConfiguration config)
        {
            Directory dir;
            if (String.IsNullOrEmpty(config.IndexPath))
            {
                CloudStorageAccount storageAccount = Configuration.Storage.Primary;

                string url = storageAccount.CreateCloudBlobClient().GetContainerReference(config.IndexContainer).Uri.AbsoluteUri;

                IndexingEventSource.Log.LoadingSearcherManager(url);

                dir = new AzureDirectory(storageAccount, config.IndexContainer, new RAMDirectory());
            }
            else
            {
                IndexingEventSource.Log.LoadingSearcherManager(config.IndexPath);

                dir = new SimpleFSDirectory(new System.IO.DirectoryInfo(config.IndexPath));
            }
            return dir;
        }
    }
}