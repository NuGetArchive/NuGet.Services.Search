using Microsoft.Owin;
using NuGet.Indexing;
using NuGet.Services.ServiceModel;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NuGet.Services.Search
{
    public abstract class SearchMiddleware : OwinMiddleware
    {
        private readonly Func<PackageSearcherManager> _searcherManagerThunk;

        public PackageSearcherManager SearcherManager { get { return _searcherManagerThunk(); } }

        public PathString BasePath { get; private set; }
        public ServiceName ServiceName { get; private set; }

        protected SearchMiddleware(OwinMiddleware next, ServiceName serviceName, string basePath, Func<PackageSearcherManager> searcherManagerThunk)
            : base(next)
        {
            BasePath = new PathString(basePath);
            ServiceName = serviceName;

            _searcherManagerThunk = searcherManagerThunk;
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
            context.Response.Headers.Add("Pragma", new[] { "no-cache" });
            context.Response.Headers.Add("Cache-Control", new[] { "no-cache" });
            context.Response.Headers.Add("Expires", new[] { "0" });
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
    }
}
