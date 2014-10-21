using Lucene.Net.Index;
using Lucene.Net.Search;
using Microsoft.Owin;
using NuGet.Indexing;
using NuGet.Services.ServiceModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services.Search
{
    public class AutocompleteQueryMiddleware: SearchMiddleware
    {
        public AutocompleteQueryMiddleware(OwinMiddleware next, ServiceName serviceName, string path, Func<PackageSearcherManager> searcherManagerThunk) : base(next, serviceName, path, searcherManagerThunk) { }

        protected override async Task Execute(IOwinContext context)
        {
            Trace.TraceInformation("Search: {0}", context.Request.QueryString);

            string q = context.Request.Query["q"] ?? String.Empty;
            q = q.ToLowerInvariant();
            string id = context.Request.Query["id"] ?? String.Empty;
            id = id.ToLowerInvariant();

            IList<string> fxValues = context.Request.Query.GetValues("supportedFramework");
            string fxName = fxValues != null ? fxValues.FirstOrDefault() : null;
            FrameworkName supportedFramework = null;
            if (!String.IsNullOrEmpty(fxName))
            {
                supportedFramework = VersionUtility.ParseFrameworkName(fxName);
            }

            string resultString = "";

            IndexSearcher searcher = NuGet.Indexing.Searcher.GetLatestSearcher(SearcherManager);

            if (!string.IsNullOrEmpty(q))
            {
                Query query = new TermQuery(new Term("IdAutocomplete", q));
                TopDocs results = searcher.Search(query, 1000);
                resultString = string.Join("\",\"", results.ScoreDocs.Select(x => searcher.Doc(x.Doc)).Select(x => x.GetField("Id").StringValue).OrderBy(x => x).Distinct());
            }
            else if (!string.IsNullOrEmpty(id))
            {
                Query query = new TermQuery(new Term("Id", id));
                TopDocs results = searcher.Search(query, 1000);
                resultString = string.Join("\",\"", results.ScoreDocs.Select(x => searcher.Doc(x.Doc)).Select(x => x.GetField("Version").StringValue).Select(x => new SemanticVersion(x)).OrderBy(x => x));
            }

            StringBuilder strBldr = new StringBuilder();

            string timestamp;
            if (!searcher.IndexReader.CommitUserData.TryGetValue("commit-time-stamp", out timestamp))
            {
                timestamp = null;
            }

            strBldr.AppendFormat("{{\"@context\":{{\"@vocab\":\"http://schema.nuget.org/schema#\"}},\"totalHits\":{0},\"timeTakenInMs\":{1},\"index\":\"{2}\"", 0 /*topDocs.TotalHits*/, 0/*elapsed*/, SearcherManager.IndexName);
            if (!String.IsNullOrEmpty(timestamp))
            {
                strBldr.AppendFormat(",\"indexTimestamp\":\"{0}\"", timestamp);
            }

            strBldr.Append(",\"data\":[");

            if (!string.IsNullOrEmpty(resultString))
            {
                strBldr.Append("\"");
                strBldr.Append(resultString);
                strBldr.Append("\"");
            }

            strBldr.Append("]}");

            await WriteResponse(context, strBldr.ToString());
        }
    }
}
