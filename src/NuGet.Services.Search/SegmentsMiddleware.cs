using Microsoft.Owin;
using NuGet.Indexing;
using NuGet.Services.ServiceModel;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NuGet.Services.Search
{
    public class SegmentsMiddleware : SearchMiddleware
    {
        public SegmentsMiddleware(OwinMiddleware next, ServiceName serviceName, string path, Func<PackageSearcherManager> searcherManagerThunk) 
            : base(next, serviceName, path, searcherManagerThunk)
        {
        }

        protected override async Task Execute(IOwinContext context)
        {
            Trace.TraceInformation("Segments");

            await WriteResponse(context, IndexAnalyzer.GetSegments(SearcherManager));
        }
    }
}
