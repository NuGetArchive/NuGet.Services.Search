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
        const int MAX_NGRAM_LENGTH = 8;

        public AutocompleteQueryMiddleware(OwinMiddleware next, ServiceName serviceName, string path, Func<PackageSearcherManager> searcherManagerThunk) : base(next, serviceName, path, searcherManagerThunk) { }

        protected override async Task Execute(IOwinContext context)
        {
            Trace.TraceInformation("Autocomplete: {0}", context.Request.QueryString);

            string q = context.Request.Query["q"];
            q = (q == null ? null : q.ToLowerInvariant());
            string id = context.Request.Query["id"];
            id = (id == null ? null : id.ToLowerInvariant());

            if (q == null && id == null)
            {
                q = string.Empty;
            }

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

            IList<string> fxValues = context.Request.Query.GetValues("supportedFramework");
            string fxName = fxValues != null ? fxValues.FirstOrDefault() : null;
            FrameworkName supportedFramework = null;
            if (!String.IsNullOrEmpty(fxName))
            {
                supportedFramework = VersionUtility.ParseFrameworkName(fxName);
            }

            string resultString = "";

            IndexSearcher searcher = NuGet.Indexing.Searcher.GetLatestSearcher(SearcherManager);

            if (q != null)
            {
                IDictionary<string, int> rankings = SearcherManager.GetRankings("");

                Query query = new MatchAllDocsQuery();

                if (!string.IsNullOrEmpty(q))
                {
                    query = new TermQuery(new Term("IdAutocomplete", q.Length < 8 ? q : q.Substring(0, MAX_NGRAM_LENGTH)));
                }
                Query boostedQuery = new RankingScoreQuery(query, rankings);

                VisualStudioDialogCollector coll = new VisualStudioDialogCollector(includePrerelease: true);

                searcher.Search(boostedQuery, coll);

                IEnumerable<ScoreDoc> results = coll.PopulateResults();

                IEnumerable<string> resultStrings = results.Select(x => searcher.Doc(x.Doc)).Select(x => x.GetField("Id").StringValue);
                if (q.Length > MAX_NGRAM_LENGTH)
                {
                    resultStrings = resultStrings.Where(x => x.ToLowerInvariant().Contains(q));
                }
                resultString = string.Join("\",\"", resultStrings.Skip(skip).Take(take));
            }
            else if (id != null)
            {
                Query query = new TermQuery(new Term("Id", id));
                TopDocs results = searcher.Search(query, 1000);
                resultString = string.Join("\",\"", results.ScoreDocs.Select(x => searcher.Doc(x.Doc)).Select(x => x.GetField("Version").StringValue).Select(x => new SemanticVersion(x)).OrderBy(x => x).Skip(skip).Take(take));
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

            strBldr.Append("],");
            strBldr.AppendFormat("\"answeredBy\":\"{0}\"", ServiceName);
            strBldr.Append("}");

            await WriteResponse(context, strBldr.ToString());
        }
    }
}
