// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Hosting;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Owin;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using Microsoft.Owin.StaticFiles.Infrastructure;
using Newtonsoft.Json.Linq;
using NuGet.Indexing;
using Owin;

[assembly: OwinStartup("NuGet.Services.Search", typeof(NuGet.Services.Search.Startup))]

namespace NuGet.Services.Search
{
    public class Startup
    {
        private PackageSearcherManager _searcherManager;

        public Func<PackageSearcherManager> SearcherManagerBuilder { get; private set; }
        public string ServiceName = "Search";

        public void Configuration(IAppBuilder app)
        {
            var instrumentationKey = System.Configuration.ConfigurationManager.AppSettings.Get("Telemetry.InstrumentationKey");
            if (!string.IsNullOrEmpty(instrumentationKey))
            {
                // set it as early as possible to avoid losing telemetry
                TelemetryConfiguration.Active.InstrumentationKey = instrumentationKey;
            }

            _searcherManager = CreateSearcherManager();

            //test console
            app.Use(async (context, next) =>
            {
                if (string.Equals(context.Request.Path.Value, "/console", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.Redirect(context.Request.PathBase + context.Request.Path + "/");
                    context.Response.StatusCode = 301;
                    return;
                }
                else if (string.Equals(context.Request.Path.Value, "/console/", StringComparison.OrdinalIgnoreCase))
                {
                    context.Request.Path = new PathString("/console/Index.html");
                }
                await next();
            });

            app.UseStaticFiles(new StaticFileOptions(new SharedOptions
            {
                RequestPath = new PathString("/console"),
                FileSystem = new EmbeddedResourceFileSystem(typeof(QueryMiddleware).Assembly, "NuGet.Services.Search.Console")
            }));

            app.Run(Invoke);
        }

        private static void TrackTelemetryOnIndexReopened(object sender, SegmentInfoEventArgs e)
        {
            var telemetryClient = new TelemetryClient();

            // track the index reopened event
            var eventTelemetry = new EventTelemetry("Index Reopened");
            eventTelemetry.Metrics.Add("Segment Count", e.Segments.Count());
            eventTelemetry.Metrics.Add("Segment Size (Sum)", e.Segments.Sum(s => s.NumDocs));

            telemetryClient.TrackEvent(eventTelemetry);


            foreach (var segment in e.Segments)
            {
                var metricTelemetry = new MetricTelemetry("Segment Size", segment.NumDocs);
                metricTelemetry.Properties.Add("Segment Name", segment.Name);

                telemetryClient.TrackMetric(metricTelemetry);
            }

            telemetryClient.Flush();
        }

        public async Task Invoke(IOwinContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            bool success = false;

            try
            {
                switch (context.Request.Path.Value.ToLowerInvariant())
                {
                    case "/":
                        JObject response = new JObject();
                        response.Add("name", ServiceName);
                        JObject resources = new JObject();
                        response.Add("resources", resources);
                        resources.Add("range", MakeUri(context, "/search/range"));
                        resources.Add("fields", MakeUri(context, "/search/fields"));
                        resources.Add("console", MakeUri(context, "/console"));
                        resources.Add("diagnostics", MakeUri(context, "/search/diag"));
                        resources.Add("segments", MakeUri(context, "/search/segments"));
                        resources.Add("query", MakeUri(context, "/search/query"));
                        context.Response.StatusCode = (int) HttpStatusCode.OK;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(response.ToString());
                        break;
                    case "/search/query":
                        await QueryMiddleware.Execute(context, _searcherManager);
                        break;
                    case "/search/range":
                        await RangeMiddleware.Execute(context, _searcherManager);
                        break;
                    case "/search/diag":
                        await DiagMiddleware.Execute(context, _searcherManager);
                        break;
                    case "/search/segments":
                        await SegmentsMiddleware.Execute(context, _searcherManager);
                        break;
                    case "/search/fields":
                        await FieldsMiddleware.Execute(context, _searcherManager);
                        break;
                    default:
                        await context.Response.WriteAsync("unrecognized");
                        context.Response.StatusCode = (int) HttpStatusCode.NotFound;
                        break;
                }

                success = true;
            }
            catch
            {
                success = false;
            }
            finally
            {
                stopwatch.Stop();
                var telemetryClient = new TelemetryClient();
                var requestTelemetry = new RequestTelemetry(context.Request.Path.Value, DateTimeOffset.UtcNow, stopwatch.Elapsed, context.Response.StatusCode.ToString(CultureInfo.InvariantCulture), success: success);
                telemetryClient.TrackRequest(requestTelemetry);
            }
        }

        private static PackageSearcherManager CreateSearcherManager()
        {
            Trace.TraceInformation("InitializeSearcherManager: new PackageSearcherManager");

            var searcher = GetSearcherManager();
            
            if (!string.IsNullOrEmpty(TelemetryConfiguration.Active.InstrumentationKey))
            {
                searcher.IndexReopened += TrackTelemetryOnIndexReopened;
            }

            // Ensure the index is initially opened.
            searcher.Open();
            IndexingEventSource.Log.LoadedSearcherManager();
            return searcher;
        }

        private static PackageSearcherManager GetSearcherManager()
        {
            var searchIndexPath = System.Configuration.ConfigurationManager.AppSettings.Get("Search.IndexPath");
            if (!string.IsNullOrEmpty(searchIndexPath))
            {
                return PackageSearcherManager.CreateLocal(searchIndexPath);
            }
            else
            {
                return PackageSearcherManager.CreateAzure(
                    System.Configuration.ConfigurationManager.AppSettings.Get("Storage.Primary"),
                    System.Configuration.ConfigurationManager.AppSettings.Get("Search.IndexContainer"),
                    System.Configuration.ConfigurationManager.AppSettings.Get("Search.DataContainer"));
            }
        }

        private static string MakeUri(IOwinContext context, string path)
        {
            return new UriBuilder(context.Request.Uri)
            {
                Path = (context.Request.PathBase + new PathString(path)).Value
            }.Uri.AbsoluteUri;
        }
    }
}
