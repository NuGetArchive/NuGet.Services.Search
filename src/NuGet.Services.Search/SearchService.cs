using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Owin;
using NuGet.Services.Http;
using NuGet.Services.ServiceModel;

namespace NuGet.Services.Search
{
    public class SearchService : NuGetApiService
    {
        public override PathString BasePath
        {
            get { return new PathString("/search"); }
        }

        public SearchEngine Engine { get; private set; }

        public SearchService(ServiceName name, ServiceHost host) : base(name, host) {}

        protected override Task<bool> OnStart()
        {
            // Synchronous, there's no task stuff going on in here :).
            try
            {
                // Resolve the index
                Engine = Container.Resolve<SearchEngine>();

                // Load the index
                Engine.Load();
            }
            catch (Exception ex)
            {
                SearchServiceEventSource.Log.StartupError(ex);
                return Task.FromResult(false);
            }
            return Task.FromResult(true);
        }

        protected override async Task OnRun()
        {
            // Update the index at a regular interval
            while (!Host.ShutdownToken.IsCancellationRequested)
            {
                await Engine.Update();
                await Task.Delay(Engine.Config.IndexInterval);
            }
        }

        public override void RegisterComponents(ContainerBuilder builder)
        {
            base.RegisterComponents(builder);

            builder
                .RegisterType<SearchEngine>()
                .AsSelf()
                .SingleInstance();
        }
    }
}