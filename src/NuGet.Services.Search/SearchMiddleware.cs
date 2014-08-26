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
using NuGet.Services.ServiceModel;

namespace NuGet.Services.Search
{
    public abstract class SearchMiddleware : OwinMiddleware
    {
        private Func<PackageSearcherManager> _searcherManagerThunk;

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
    }
}
