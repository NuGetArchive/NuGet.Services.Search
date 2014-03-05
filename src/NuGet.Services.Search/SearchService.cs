using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Lucene.Net.Store;
using Lucene.Net.Store.Azure;
using Microsoft.Owin;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json.Linq;
using NuGet.Indexing;
using NuGet.Services.Http;
using NuGet.Services.ServiceModel;
using Owin;

namespace NuGet.Services.Search
{
    public class SearchService : NuGetHttpService
    {
        private SearchMiddlewareConfiguration _config;

        public override PathString BasePath
        {
            get { return new PathString("/search"); }
        }

        public SearchService(ServiceName name, ServiceHost host)
            : base(name, host)
        {
            try
            {
                if (RoleEnvironment.IsAvailable)
                {
                    // In azure, set up config reloading
                    RoleEnvironment.Changing += (_, __) =>
                    {
                        if (_config != null)
                        {
                            _config.Reload();
                        }
                    };
                }
            }
            catch (Exception)
            {
                // Ignore failures, they will only occur outside of Azure
            }
        }

        protected override Task OnRun()
        {
            return WaitForShutdown();
        }

        // This could be moved up to the service platform
        private static readonly object Unit = new object();
        private Task WaitForShutdown()
        {
            var tcs = new TaskCompletionSource<object>();
            Host.ShutdownToken.Register(() => tcs.SetResult(Unit)); // Don't want to return null, just a useless object
            return tcs.Task;
        }

        protected override void Configure(IAppBuilder app)
        {
            // Load middleware configuration
            _config = new SearchMiddlewareConfiguration(Configuration);

            // Configure the app
            app.UseErrorPage();
            app.Use(typeof(DiagMiddleware), "/diag", _config);
            app.Use(typeof(FieldsMiddleware), "/fields", _config);
            app.Use(typeof(RangeMiddleware), "/range", _config);
            app.Use(typeof(QueryMiddleware), "/query", _config);
            app.Use(typeof(ConfigMiddleware), "/config", _config);
            app.Use(async (context, next) =>
            {
                // Handle root requests
                if (!context.Request.Path.HasValue)
                {
                    JObject response = new JObject();
                    response.Add("name", ServiceName.ToString());
                    response.Add("service", ServiceName.Name);

                    JObject resources = new JObject();
                    response.Add("resources", resources);

                    if (await SearchMiddleware.IsAdmin(context, challenge: false)) 
                    {
                        resources.Add("diagnostics", MakeUri(context, "/diag"));
                        resources.Add("fields", MakeUri(context, "/fields"));
                        resources.Add("range", MakeUri(context, "/range"));
                        resources.Add("config", MakeUri(context, "/config"));
                    }
                    resources.Add("query", MakeUri(context, "/query"));

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
    }
}