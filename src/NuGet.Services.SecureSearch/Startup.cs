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
            string aadInstance = ConfigurationManager.AppSettings["ida:AADInstance"];

            string metadataAddress = string.Format(aadInstance, tenant) + "/federationmetadata/2007-06/federationmetadata.xml";

            app.UseWindowsAzureActiveDirectoryBearerAuthentication(new WindowsAzureActiveDirectoryBearerAuthenticationOptions
            {
                TokenValidationParameters = new TokenValidationParameters
                {
                    ValidAudience = audience,
                    ValidateIssuer = true,
                    IssuerValidator = (string issuer, SecurityToken securityToken, TokenValidationParameters validationParameters) => { return issuer; }
                },
                Tenant = tenant,
                MetadataAddress = metadataAddress
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
                    await SecureQueryImpl.Query(context, _searcherManager, ServiceHelpers.GetTenant());
                    break;

                //TODO: temp fix to unblock web site development
                case "/owner":
                    await SecureQueryImpl.QueryByOwner(context, _searcherManager, ServiceHelpers.GetTenant());
                    break;

                case "/find":
                    await SecureFindImpl.Find(context, _searcherManager, ServiceHelpers.GetTenant());
                    break;
                
                case "/segments":
                    await ServiceInfoImpl.Segments(context, _searcherManager);
                    break;

                case "/stats":
                    await ServiceInfoImpl.Stats(context, _searcherManager);
                    break;
                
                default:
                    string storagePrimary = System.Configuration.ConfigurationManager.AppSettings.Get("Storage.Primary");
                    MetadataImpl.Access(context, string.Empty, _searcherManager, storagePrimary, 30);
                    break;
            }
        }
    }
}
