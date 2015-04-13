using Microsoft.Owin;
using Microsoft.WindowsAzure.ServiceRuntime;
using NuGet.Indexing;
using NuGet.Services.Http;
using NuGet.Services.ServiceModel;
using Owin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;

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
        private static readonly object _unit = new object();
        private Task WaitForShutdown()
        {
            var tcs = new TaskCompletionSource<object>();
            Host.ShutdownToken.Register(() => tcs.SetResult(_unit)); // Don't want to return null, just a useless object
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
            if (!String.IsNullOrEmpty(config.IndexPath))
            {
                return PackageSearcherManager.CreateLocal(config.IndexPath);
            }
            else
            {
                return PackageSearcherManager.CreateAzure(
                    Configuration.Storage.Primary,
                    config.IndexContainer,
                    config.DataContainer);
            }
        }
    }
}