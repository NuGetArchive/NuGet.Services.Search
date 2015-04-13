using Microsoft.Owin;
using NuGet.Indexing;
using NuGet.Services.ServiceModel;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NuGet.Services.Search
{
    public class RangeMiddleware : SearchMiddleware
    {
        public RangeMiddleware(OwinMiddleware next, ServiceName serviceName, string path,
            Func<PackageSearcherManager> searcherManagerThunk)
            : base(next, serviceName, path, searcherManagerThunk)
        {
        }

        protected override async Task Execute(IOwinContext context)
        {
            Trace.TraceInformation("Range: {0}", context.Request.QueryString);

            string min = context.Request.Query["min"];
            string max = context.Request.Query["max"];

            string content = "[]";

            int minKey;
            int maxKey;
            if (min != null && max != null && int.TryParse(min, out minKey) && int.TryParse(max, out maxKey))
            {
                Trace.TraceInformation("Searcher.KeyRangeQuery(..., {0}, {1})", minKey, maxKey);

                content = Searcher.KeyRangeQuery(SearcherManager, minKey, maxKey);
            }

            await WriteResponse(context, content);
        }
    }
}
