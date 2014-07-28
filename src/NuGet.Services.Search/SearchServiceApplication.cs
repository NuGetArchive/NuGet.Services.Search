using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Owin;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using Microsoft.Owin.StaticFiles.Infrastructure;
using Newtonsoft.Json.Linq;
using NuGet.Indexing;
using NuGet.Services.ServiceModel;
using Owin;

namespace NuGet.Services.Search
{
    public class SearchServiceApplication
    {
        private PackageSearcherManager _searcherManager;
        private bool _includeConsole;

        public Func<PackageSearcherManager> SearcherManagerBuilder { get; private set; }
        public ServiceName ServiceName { get; private set; }

        public SearchServiceApplication(ServiceName serviceName, Func<PackageSearcherManager> searcherManagerBuilder) : this(serviceName, searcherManagerBuilder, includeConsole: true) { }
        public SearchServiceApplication(ServiceName serviceName, Func<PackageSearcherManager> searcherManagerBuilder, bool includeConsole)
        {
            ServiceName = serviceName;
            SearcherManagerBuilder = searcherManagerBuilder;
            _includeConsole = includeConsole;
        }

        public void Configure(IAppBuilder app)
        {
            // Configure the app
            app.UseErrorPage();

            SharedOptions sharedStaticFileOptions = new SharedOptions()
            {
                RequestPath = new PathString("/console"),
                FileSystem = new EmbeddedResourceFileSystem(typeof(SearchServiceApplication).Assembly, "NuGet.Services.Search.Console")
            };

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

            var thunk = new Func<PackageSearcherManager>(() => _searcherManager);

            // Public endpoint(s)
            app.Use(typeof(QueryMiddleware), "/query", thunk);

            // Admin endpoints
            app.Use(typeof(DiagMiddleware), "/diag", thunk);
            app.Use(typeof(FieldsMiddleware), "/fields", thunk);
            app.Use(typeof(RangeMiddleware), "/range", thunk);
            app.Use(typeof(SegmentsMiddleware), "/segments", thunk);

            app.Use(async (context, next) =>
            {
                // Handle root requests
                if (!context.Request.Path.HasValue || String.Equals(context.Request.Path.Value, "/"))
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

        private string MakeUri(IOwinContext context, string path)
        {
            return new UriBuilder(context.Request.Uri)
            {
                Path = (context.Request.PathBase + new PathString(path)).Value
            }.Uri.AbsoluteUri;
        }

        public void ReloadIndex()
        {
            SearchServiceEventSource.Log.ReloadingIndex();
            PackageSearcherManager newIndex = SearcherManagerBuilder();
            Interlocked.Exchange(ref _searcherManager, newIndex);
            SearchServiceEventSource.Log.ReloadedIndex();
        }
    }
}
