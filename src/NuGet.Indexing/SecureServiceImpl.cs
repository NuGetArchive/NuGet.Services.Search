using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Microsoft.Owin;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Collections.Generic;
using Lucene.Net.Analysis;

namespace NuGet.Indexing
{
    public static class SecureServiceImpl
    {
        public static JToken Query(IOwinContext context, SecureSearcherManager searcherManager, string tenantId)
        {
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

            bool countOnly;
            if (!bool.TryParse(context.Request.Query["countOnly"], out countOnly))
            {
                countOnly = false;
            }

            bool includePrerelease;
            if (!bool.TryParse(context.Request.Query["prerelease"], out includePrerelease))
            {
                includePrerelease = false;
            }

            bool includeExplanation = false;
            if (!bool.TryParse(context.Request.Query["explanation"], out includeExplanation))
            {
                includeExplanation = false;
            }

            string q = context.Request.Query["q"] ?? string.Empty;

            string scheme = context.Request.Uri.Scheme;

            return Search(searcherManager, tenantId, scheme, q, countOnly, includePrerelease, skip, take, includeExplanation);
        }

        public static JToken Search(SecureSearcherManager searcherManager, string tenantId, string scheme, string q, bool countOnly, bool includePrerelease, int skip, int take, bool includeExplanation)
        {
            IndexSearcher searcher = searcherManager.Get();
            try
            {
                Filter filter = searcherManager.GetFilter(tenantId);

                //TODO: uncomment these lines when we have an index that contains the appropriate @type field in every document
                //Filter typeFilter = new CachingWrapperFilter(new TypeFilter("http://schema.nuget.org/schema#NuGetClassicPackage"));
                //filter = new ChainedFilter(new Filter[] { filter, typeFilter }, ChainedFilter.Logic.AND);

                Query query = MakeQuery(q);

                TopDocs topDocs = searcher.Search(query, filter, skip + take);

                return MakeResult(searcher, scheme, topDocs, skip, take, searcherManager, includeExplanation, query);
            }
            finally
            {
                searcherManager.Release(searcher);
            }
        }

        public static Query MakeQuery(string q)
        {
            Query query = LuceneQueryCreator.Parse(q, false);
            return query;
        }

        public static JToken MakeResultData(IndexSearcher searcher, string scheme, TopDocs topDocs, int skip, int take, SecureSearcherManager searcherManager, bool includeExplanation, Query query)
        {
            Uri registrationBaseAddress = searcherManager.RegistrationBaseAddress[scheme];

            JArray array = new JArray();

            for (int i = skip; i < Math.Min(skip + take, topDocs.ScoreDocs.Length); i++)
            {
                ScoreDoc scoreDoc = topDocs.ScoreDocs[i];

                Document document = searcher.Doc(scoreDoc.Doc);

                string url = document.Get("Url");
                string id = document.Get("Id");
                string version = document.Get("Version");

                JObject obj = new JObject();
                obj["@id"] = new Uri(registrationBaseAddress, url).AbsoluteUri;
                obj["@type"] = "Package";
                obj["registration"] = new Uri(registrationBaseAddress, string.Format("{0}/index.json", id.ToLowerInvariant())).AbsoluteUri;
                obj["id"] = id;

                string tenantId = document.Get("TenantId");
                bool isPublic = (tenantId != null && tenantId == "PUBLIC");

                obj["isPublic"] = isPublic;

                AddField(obj, document, "domain", "Domain");
                AddField(obj, document, "description", "Description");
                AddField(obj, document, "summary", "Summary");
                AddField(obj, document, "title", "Title");
                AddField(obj, document, "iconUrl", "IconUrl");
                AddFieldAsArray(obj, document, "tags", "Tags");
                AddFieldAsArray(obj, document, "authors", "Authors");

                obj["version"] = version;
                obj["versions"] = searcherManager.GetVersions(scheme, scoreDoc.Doc);

                if (includeExplanation)
                {
                    Explanation explanation = searcher.Explain(query, scoreDoc.Doc);
                    obj["explanation"] = explanation.ToString();
                }

                array.Add(obj);
            }

            return array;
        }

        static void AddField(JObject obj, Document document, string to, string from)
        {
            string value = document.Get(from);
            if (value != null)
            {
                obj[to] = value;
            }
        }

        static void AddFieldAsArray(JObject obj, Document document, string to, string from)
        {
            string value = document.Get(from);
            if (value != null)
            {
                obj[to] = new JArray(value.Split(' '));
            }
        }

        static JToken MakeResult(IndexSearcher searcher, string scheme, TopDocs topDocs, int skip, int take, SecureSearcherManager searcherManager, bool includeExplanation, Query query)
        {
            JToken data = MakeResultData(searcher, scheme, topDocs, skip, take, searcherManager, includeExplanation, query);

            JObject result = new JObject();

            result.Add("@context", new JObject { { "@vocab", "http://schema.nuget.org/schema#" } });
            result.Add("totalHits", topDocs.TotalHits);
            result.Add("lastReopen", searcherManager.LastReopen.ToString("o"));
            result.Add("index", searcherManager.IndexName);
            result.Add("data", data);

            return result;
        }

        public static JToken Segments(SearcherManager searcherManager)
        {
            searcherManager.MaybeReopen();

            IndexSearcher searcher = searcherManager.Get();

            try
            {
                IndexReader indexReader = searcher.IndexReader;

                JArray segments = new JArray();
                foreach (ReadOnlySegmentReader segmentReader in indexReader.GetSequentialSubReaders())
                {
                    JObject segmentInfo = new JObject();
                    segmentInfo.Add("segment", segmentReader.SegmentName);
                    segmentInfo.Add("documents", segmentReader.NumDocs());
                    segments.Add(segmentInfo);
                }
                return segments;
            }
            finally
            {
                searcherManager.Release(searcher);
            }
        }

        public static JToken Stats(SecureSearcherManager searcherManager)
        {
            searcherManager.MaybeReopen();

            IndexSearcher searcher = searcherManager.Get();

            try
            {
                IndexReader indexReader = searcher.IndexReader;

                JObject result = new JObject();
                result.Add("numDocs", indexReader.NumDocs());
                result.Add("indexName", searcherManager.IndexName);
                result.Add("lastReopen", searcherManager.LastReopen.ToString("o"));
                return result;
            }
            finally
            {
                searcherManager.Release(searcher);
            }
        }
    }
}