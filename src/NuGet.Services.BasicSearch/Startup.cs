using Microsoft.Owin;
using Newtonsoft.Json.Linq;
using NuGet.Indexing;
using Owin;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

[assembly: OwinStartup(typeof(NuGet.Services.BasicSearch.Startup))]

namespace NuGet.Services.BasicSearch
{
    public class Startup
    {
        Timer _timer;
        NuGetSearcherManager _searcherManager;
        int _gate;

        public void Configuration(IAppBuilder app)
        {
            app.UseErrorPage();

            _searcherManager = CreateSearcherManager();

            _searcherManager.Open();

            _gate = 0;
            _timer = new Timer(new TimerCallback(ReopenCallback), 0, 0, 180 * 1000);

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

        public NuGetSearcherManager CreateSearcherManager()
        {
            NuGetSearcherManager searcherManager;

            string luceneDirectory = System.Configuration.ConfigurationManager.AppSettings.Get("Local.Lucene.Directory");
            if (!string.IsNullOrEmpty(luceneDirectory))
            {
                string dataDirectory = System.Configuration.ConfigurationManager.AppSettings.Get("Local.Data.Directory");
                searcherManager = NuGetSearcherManager.CreateLocal(luceneDirectory, dataDirectory);
            }
            else
            {
                string storagePrimary = System.Configuration.ConfigurationManager.AppSettings.Get("Storage.Primary");
                string searchIndexContainer = System.Configuration.ConfigurationManager.AppSettings.Get("Search.IndexContainer");
                string searchDataContainer = System.Configuration.ConfigurationManager.AppSettings.Get("Search.DataContainer");

                searcherManager = NuGetSearcherManager.CreateAzure(storagePrimary, searchIndexContainer, searchDataContainer);
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
                case "/find":
                    await ServiceHelpers.WriteResponse(context, HttpStatusCode.OK, ServiceImpl.Find(context, _searcherManager));
                    break;
                case "/query":
                    await ServiceHelpers.WriteResponse(context, HttpStatusCode.OK, ServiceImpl.Query(context, _searcherManager));
                    break;
                case "/autocomplete":
                    await ServiceHelpers.WriteResponse(context, HttpStatusCode.OK, ServiceImpl.AutoComplete(context, _searcherManager));
                    break;
                case "/targetframeworks":
                    await ServiceInfoImpl.TargetFrameworks(context, _searcherManager);
                    break;
                case "/segments":
                    await ServiceInfoImpl.Segments(context, _searcherManager);
                    break;
                case "/stats":
                    await ServiceInfoImpl.Stats(context, _searcherManager);
                    break;
                default:
                    await context.Response.WriteAsync("unrecognized");
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    break;
            }
        }
    }
}
