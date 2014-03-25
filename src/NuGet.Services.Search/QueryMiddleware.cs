using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Index;
using Microsoft.Owin;
using NuGet.Indexing;

namespace NuGet.Services.Search
{
    public class QueryMiddleware : SearchMiddleware
    {
        private QueryLog _log;

        public QueryMiddleware(OwinMiddleware next, string path, Func<PackageSearcherManager> searcherManagerThunk, QueryLog log) : base(next, path, searcherManagerThunk) {
            _log = log;
        }

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

            string args = string.Format("Searcher.Search(..., {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10})", q, countOnly, projectType, includePrerelease, feed, sortBy, skip, take, includeExplanation, ignoreFilter, luceneQuery);
            Trace.TraceInformation(args);

            string queryToExecute = q;
            if (!luceneQuery)
            {
                queryToExecute = LuceneQueryCreator.Parse(q);
            }

            Stopwatch sw = new Stopwatch();
            sw.Start();
            string content = Searcher.Search(SearcherManager, queryToExecute, countOnly, projectType, includePrerelease, feed, sortBy, skip, take, includeExplanation, ignoreFilter);
            sw.Stop();

            // Queue the request for recording
            _log.RecordQuery(q, projectType, feed, context.Request.Headers.Get("User-Agent"), (int)sw.ElapsedMilliseconds);

            await WriteResponse(context, content);
        }
    }
}
