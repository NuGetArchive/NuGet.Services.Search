using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using Lucene.Net.Store;
using Microsoft.Owin;
using NuGet.Indexing;
using NuGet.Services.ServiceModel;
using Owin;

[assembly: OwinStartup(typeof(NuGet.Services.Search.MiniTester.Startup))]

namespace NuGet.Services.Search.MiniTester
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            SearchServiceApplication search = new SearchServiceApplication(
                new ServiceName(
                    ServiceHostInstanceName.Parse("nuget-local-0-search_IN0"),
                    "search"),
                CreateSearcherManager);
            search.ReloadIndex();
            search.Configure(app);
        }

        private PackageSearcherManager CreateSearcherManager()
        {
            var index = WebConfigurationManager.AppSettings["IndexLocation"];
            if(String.IsNullOrEmpty(index))
            {
                throw new Exception("You must specify the location of the index in the IndexLocation appSetting");
            }
            return new PackageSearcherManager(
                new SimpleFSDirectory(new System.IO.DirectoryInfo(index)),
                new FolderRankings(index),
                new FolderDownloadCounts(index));
        }
    }
}