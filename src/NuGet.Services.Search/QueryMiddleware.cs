using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Microsoft.Owin;
using Newtonsoft.Json.Linq;
using NuGet.Indexing;
using NuGet.Services.ServiceModel;

namespace NuGet.Services.Search
{
    public class QueryMiddleware : SearchMiddleware
    {
        public QueryMiddleware(OwinMiddleware next, ServiceName serviceName, string path, Func<PackageSearcherManager> searcherManagerThunk) : base(next, serviceName, path, searcherManagerThunk) { }

        protected override async Task Execute(IOwinContext context)
        {
            Trace.TraceInformation("Search: {0}", context.Request.QueryString);

            string q = context.Request.Query["q"] ?? String.Empty;

            string projectType = context.Request.Query["projectType"] ?? String.Empty;

            string sortBy = context.Request.Query["sortBy"] ?? String.Empty;

            bool luceneQuery;
            if (!bool.TryParse(context.Request.Query["luceneQuery"], out luceneQuery))
            {
                luceneQuery = true;
            }

            bool includePrerelease;
            if (!bool.TryParse(context.Request.Query["prerelease"], out includePrerelease))
            {
                includePrerelease = false;
            }

            bool countOnly;
            if (!bool.TryParse(context.Request.Query["countOnly"], out countOnly))
            {
                countOnly = false;
            }

            string feed = context.Request.Query["feed"] ?? "none";

            int skip;
            if (!int.TryParse(context.Request.Query["skip"], out skip))
            {
                skip = 0;
            }

            int take;
            if (!int.TryParse(context.Request.Query["take"], out take))
            {
                take = 20;
            }

            bool includeExplanation = false;
            if (!bool.TryParse(context.Request.Query["explanation"], out includeExplanation))
            {
                includeExplanation = false;
            }

            bool ignoreFilter;
            if (!bool.TryParse(context.Request.Query["ignoreFilter"], out ignoreFilter))
            {
                ignoreFilter = false;
            }

            string callback = context.Request.Query["callback"];

            IList<string> fxValues = context.Request.Query.GetValues("supportedFramework");
            string fxName = fxValues != null ? fxValues.FirstOrDefault() : null;
            FrameworkName supportedFramework = null;
            if (!String.IsNullOrEmpty(fxName))
            {
                supportedFramework = VersionUtility.ParseFrameworkName(fxName);
            }
            if (supportedFramework == null || !SearcherManager.GetFrameworks().Contains(supportedFramework))
            {
                supportedFramework = FrameworksList.AnyFramework;
            }

            var query = LuceneQueryCreator.Parse(q, luceneQuery);
            if (!ignoreFilter && !luceneQuery)
            {
                string facet = includePrerelease ?
                    Facets.LatestPrereleaseVersion(supportedFramework) :
                    Facets.LatestStableVersion(supportedFramework);

                var newQuery = new BooleanQuery();
                newQuery.Add(query, Occur.MUST);
                newQuery.Add(new TermQuery(new Term("Facet", facet)), Occur.MUST);
                query = newQuery;
            }

            string args = string.Format("Searcher.Search(..., {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10})", q, countOnly, projectType, includePrerelease, feed, sortBy, skip, take, includeExplanation, ignoreFilter, luceneQuery);
            Trace.TraceInformation(args);

            string content = NuGet.Indexing.Searcher.Search(
                SearcherManager, 
                query, 
                countOnly, 
                projectType, 
                includePrerelease, 
                feed,
                sortBy, 
                skip, 
                take, 
                includeExplanation, 
                ignoreFilter);

            JObject result = JObject.Parse(content);
            result["answeredBy"] = ServiceName.ToString();

            string resultString;

            if (string.IsNullOrEmpty(callback))
            {
                resultString = result.ToString();
            }
            else
            {
                resultString = callback + "(" + result.ToString() + ");";
            }

            await WriteResponse(context, resultString);
        }
    }
}
