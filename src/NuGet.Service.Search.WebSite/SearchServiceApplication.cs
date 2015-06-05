// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using Microsoft.Owin;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using Microsoft.Owin.StaticFiles.Infrastructure;
using Newtonsoft.Json.Linq;
using NuGet.Indexing;
using Owin;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Security;

namespace NuGet.Services.Search
{

    [assembly: OwinStartup("NuGet.Services.Search", typeof(NuGet.Services.Search.Startup))]
    public class Startup
    {
        private PackageSearcherManager _searcherManager;

        public Func<PackageSearcherManager> SearcherManagerBuilder { get; private set; }
        public string ServiceName = "/Search";


        //public SearchServiceApplication(Func<PackageSearcherManager> searcherManagerBuilder)
        //{

        //    SearcherManagerBuilder = searcherManagerBuilder;
        //}

        public void Configuration(IAppBuilder app)
        {
            // Configure the app
         //   app.UseErrorPage();
            _searcherManager = CreateSearcherManager();

            SharedOptions sharedStaticFileOptions = new SharedOptions()
            {
                RequestPath = new PathString("/console"),
                FileSystem = new EmbeddedResourceFileSystem(typeof(Startup).Assembly, "NuGet.Services.Search.Console")
            };
                    

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

            //// Public endpoint(s)
            //app.Use(typeof(QueryMiddleware), ServiceName, "/query", thunk);

            //// Admin endpoints
            //app.Use(typeof(DiagMiddleware), ServiceName, "/diag", thunk);
            //app.Use(typeof(FieldsMiddleware), ServiceName, "/fields", thunk);
            //app.Use(typeof(RangeMiddleware), ServiceName, "/range", thunk);
            //app.Use(typeof(SegmentsMiddleware), ServiceName, "/segments", thunk);

            app.Use(async (context, next) =>
            {
                // Handle root requests
                if (!context.Request.Path.HasValue || String.Equals(context.Request.Path.Value, "/"))
                {
                    JObject response = new JObject();
                    response.Add("name", ServiceName.ToString());
                    

                    JObject resources = new JObject();
                    response.Add("resources", resources);

                    resources.Add("range", MakeUri(context, "/range"));
                    resources.Add("fields", MakeUri(context, "/fields"));
                    resources.Add("console", MakeUri(context, "/console"));
                    resources.Add("diagnostics", MakeUri(context, "/diag"));
                    resources.Add("segments", MakeUri(context, "/segments"));
                    resources.Add("query", MakeUri(context, "/query"));                 
                    await SearchMiddleware.WriteResponse(context, response.ToString());                  
                }
                await next();
                
            });
            app.Run(Invoke);
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

        public async Task Invoke(IOwinContext context)
        {
            switch (context.Request.Path.Value)
            {
                case "/":
                    await context.Response.WriteAsync("OK");
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    break;              
                case "/search/query":
                    await QueryMiddleware.Execute(context, _searcherManager);
                    break;
                case "/search/range":
                    await RangeMiddleware.Execute(context, _searcherManager);
                    break;
                case "/search/diag":
                    await DiagMiddleware.Execute(context,_searcherManager);;
                    break;                
                default:
                    await context.Response.WriteAsync("unrecognized");
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    break;
            }
        }

         private PackageSearcherManager CreateSearcherManager()
        {
            Trace.TraceInformation("InitializeSearcherManager: new PackageSearcherManager");

          //  SearchConfiguration config = Configuration.GetSection<SearchConfiguration>();
            var searcher = GetSearcherManager();
            searcher.Open(); // Ensure the index is initially opened.
            IndexingEventSource.Log.LoadedSearcherManager();
            return searcher;
        }

        private PackageSearcherManager GetSearcherManager()
        {
            if (!String.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings.Get("Search.IndexPath")))
            {
                return PackageSearcherManager.CreateLocal(System.Configuration.ConfigurationManager.AppSettings.Get("Search.IndexPath"));
            }
            else
            {
                return PackageSearcherManager.CreateAzure(
                    System.Configuration.ConfigurationManager.AppSettings.Get("Storage.Primary"),
                    System.Configuration.ConfigurationManager.AppSettings.Get("Search.IndexContainer"),
                    System.Configuration.ConfigurationManager.AppSettings.Get("Search.IndexContainer"));
            }
        }
    }
}
