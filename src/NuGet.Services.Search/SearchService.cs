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

        public SearchService(ServiceName name, ServiceHost host) : base(name, host) {}

        protected override Task<bool> OnStart()
        {
            throw new NotImplementedException();
        }

        protected override async Task OnRun()
        {
            throw new NotImplementedException();
        }

        public override void RegisterComponents(ContainerBuilder builder)
        {
            base.RegisterComponents(builder);
        }
    }
}