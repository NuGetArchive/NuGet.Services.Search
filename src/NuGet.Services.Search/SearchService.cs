using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin;
using NuGet.Services.Http;
using NuGet.Services.ServiceModel;

namespace NuGet.Services.Search
{
    public class SearchService : NuGetApiService
    {
        public override PathString BasePath
        {
            get { return new PathString("search"); }
        }

        protected override Task OnRun()
        {
            // TODO: Indexer!
        }
    }
}