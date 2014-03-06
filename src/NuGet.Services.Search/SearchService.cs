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
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using Microsoft.Owin.StaticFiles.Infrastructure;
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

        private static readonly IList<PathString> _adminPaths = new List<PathString>()
        {
            new PathString("/diag"),
            new PathString("/fields"),
            new PathString("/range"),
            new PathString("/config"),
            new PathString("/console")
        };
        protected override void Configure(IAppBuilder app)
        {
            // Load middleware configuration
            _config = new SearchMiddlewareConfiguration(Configuration);

            // Configure the app
            app.UseErrorPage();
            app.Use(async (context, next) =>
            {
                if (!_adminPaths.Any(p => context.Request.Path.StartsWithSegments(p)) ||
                    await IsAdmin(context))
                {
                    await next();
                }
            });

            SharedOptions sharedStaticFileOptions = new SharedOptions()
            {
                RequestPath = new PathString("/console"),
                FileSystem = new EmbeddedResourceFileSystem("NuGet.Services.Search.Console")
            };
            
            // Public endpoint(s)
            app.Use(typeof(QueryMiddleware), "/query", _config);
               
            // Admin endpoints
            app.Use(typeof(DiagMiddleware), "/diag", _config);
            app.Use(typeof(FieldsMiddleware), "/fields", _config);
            app.Use(typeof(RangeMiddleware), "/range", _config);
            app.Use(typeof(ConfigMiddleware), "/config", _config);

            // Just a little bit of rewriting. Not the full UseDefaultFiles middleware, just a quick hack
            app.Use((context, next) =>
            {
                if (String.Equals(context.Request.Path.Value, "/console", StringComparison.OrdinalIgnoreCase))
                {
                    // Redirect to trailing slash to maintain relative links
                    context.Response.StatusCode = 301;
                    context.Response.Headers["Location"] = context.Request.PathBase + context.Request.Path + "/";
                    return Task.FromResult(0);
                }
                else if (String.Equals(context.Request.Path.Value, "/console/", StringComparison.OrdinalIgnoreCase))
                {
                    context.Request.Path = new PathString("/console/Index.html");
                }
                return next();
            });
            app.UseStaticFiles(new StaticFileOptions(sharedStaticFileOptions));

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

                    if (await IsAdmin(context, challenge: false)) 
                    {
                        resources.Add("range", MakeUri(context, "/range"));
                        resources.Add("fields", MakeUri(context, "/fields"));
                        resources.Add("config", MakeUri(context, "/config"));
                        resources.Add("console", MakeUri(context, "/console"));
                        resources.Add("diagnostics", MakeUri(context, "/diag"));
                    }
                    resources.Add("query", MakeUri(context, "/query"));

                    await SearchMiddleware.WriteResponse(context, response.ToString());
                }
            });
        }

        public static Task<bool> IsAdmin(IOwinContext context)
        {
            return IsAdmin(context, challenge: true);
        }

        public static async Task<bool> IsAdmin(IOwinContext context, bool challenge)
        {
            await context.Authentication.AuthenticateAsync("AdminKey");
            if (context.Request.User != null && context.Request.User.IsInRole(Roles.Admin))
            {
                return true;
            }
            else
            {
                if (challenge)
                {
                    context.Authentication.Challenge("AdminKey");
                }
                return false;
            }
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