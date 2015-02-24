using Microsoft.Owin;
using Microsoft.Owin.Security.ActiveDirectory;
using Newtonsoft.Json.Linq;
using NuGet.Indexing;
using Owin;
using System;
using System.Configuration;
using System.IdentityModel.Tokens;
using System.IO;
using System.Net;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

[assembly: OwinStartup(typeof(NuGet.Services.SecureSearch.Startup))]

namespace NuGet.Services.SecureSearch
{
    public class Startup
    {
        Timer _timer;
        SecureSearcherManager _searcherManager;
        int _gate;

        public void Configuration(IAppBuilder app)
        {
            app.UseErrorPage();

            string audience = ConfigurationManager.AppSettings["ida:Audience"];
            string tenant = ConfigurationManager.AppSettings["ida:Tenant"];

            app.UseWindowsAzureActiveDirectoryBearerAuthentication(
                new WindowsAzureActiveDirectoryBearerAuthenticationOptions
                {
                    TokenValidationParameters = new TokenValidationParameters { ValidAudience = audience },
                    Tenant = tenant
                });

            _searcherManager = CreateSearcherManager();

            _searcherManager.Open();

            string searchIndexRefresh = ConfigurationManager.AppSettings["Search.IndexRefresh"] ?? "180";
            int seconds;
            if (!int.TryParse(searchIndexRefresh, out seconds))
            {
                seconds = 180;
            }

            _gate = 0;
            _timer = new Timer(new TimerCallback(ReopenCallback), 0, 0, seconds * 1000);

            app.Run(Invoke);
        }

        void ReopenCallback(object obj)
        {
            int val = Interlocked.Increment(ref _gate);
            if (val > 1)
            {
                Interlocked.Decrement(ref _gate);
                return;
            }

            _searcherManager.MaybeReopen();
            Interlocked.Decrement(ref _gate);
            return;
        }

        public SecureSearcherManager CreateSearcherManager()
        {
            SecureSearcherManager searcherManager;

            string luceneDirectory = System.Configuration.ConfigurationManager.AppSettings.Get("Local.Lucene.Directory");
            if (!string.IsNullOrEmpty(luceneDirectory))
            {
                string dataDirectory = System.Configuration.ConfigurationManager.AppSettings.Get("Local.Data.Directory");
                searcherManager = SecureSearcherManager.CreateLocal(luceneDirectory);
            }
            else
            {
                string storagePrimary = System.Configuration.ConfigurationManager.AppSettings.Get("Storage.Primary");
                string searchIndexContainer = System.Configuration.ConfigurationManager.AppSettings.Get("Search.IndexContainer");

                searcherManager = SecureSearcherManager.CreateAzure(storagePrimary, searchIndexContainer);
            }

            string registrationBaseAddress = System.Configuration.ConfigurationManager.AppSettings.Get("Search.RegistrationBaseAddress");

            searcherManager.RegistrationBaseAddress["http"] = MakeRegistrationBaseAddress("http", registrationBaseAddress);
            searcherManager.RegistrationBaseAddress["https"] = MakeRegistrationBaseAddress("https", registrationBaseAddress);
            return searcherManager;
        }

        static Uri MakeRegistrationBaseAddress(string scheme, string registrationBaseAddress)
        {
            Uri original = new Uri(registrationBaseAddress);
            if (original.Scheme == scheme)
            {
                return original;
            }
            else
            {
                return new UriBuilder(original)
                {
                    Scheme = scheme,
                    Port = -1
                }.Uri;
            }
        }

        public async Task Invoke(IOwinContext context)
        {
            switch (context.Request.Path.Value)
            {
                case "/":
                    await context.Response.WriteAsync("OK");
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    break;
                case "/query":
                    await WriteResponse(context, SecureServiceImpl.Query(context, _searcherManager, string.Empty));
                    break;
                case "/secure/query":
                    if (IsAuthorized())
                    {
                        Claim tenantIdClaim = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid");
                        string tenantId = (tenantIdClaim != null) ? tenantIdClaim.Value : "PUBLIC";
                        await WriteResponse(context, SecureServiceImpl.Query(context, _searcherManager, tenantId));
                    }
                    else
                    {
                        await context.Response.WriteAsync("unauthorized");
                        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    }
                    break;
                case "/segments":
                    await WriteResponse(context, SecureServiceImpl.Segments(_searcherManager));
                    break;
                case "/stats":
                    await WriteResponse(context, SecureServiceImpl.Stats(_searcherManager));
                    break;
                default:
                    string storagePrimary = System.Configuration.ConfigurationManager.AppSettings.Get("Storage.Primary");
                    MetadataImpl.Access(context, string.Empty, _searcherManager, storagePrimary, 30);
                    break;
            }
        }

        public bool IsAuthorized()
        {
            Claim scopeClaim = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/scope");
            bool authorized = (scopeClaim != null && scopeClaim.Value == "user_impersonation");
            return authorized;
        }

        public static Task WriteResponse(IOwinContext context, JToken content)
        {
            string callback = context.Request.Query["callback"];

            string contentType;
            string responseString;
            if (string.IsNullOrEmpty(callback))
            {
                responseString = content.ToString();
                contentType = "application/json";
            }
            else
            {
                responseString = string.Format("{0}({1})", callback, content);
                contentType = "application/javascript";
            }

            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.Headers.Add("Pragma", new string[] { "no-cache" });
            context.Response.Headers.Add("Cache-Control", new string[] { "no-cache" });
            context.Response.Headers.Add("Expires", new string[] { "0" });
            context.Response.ContentType = contentType;

            return context.Response.WriteAsync(responseString);
        }
    }
}
