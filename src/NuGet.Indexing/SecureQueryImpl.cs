using Lucene.Net.Documents;
using Lucene.Net.Search;
using Microsoft.Owin;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Threading.Tasks;

namespace NuGet.Indexing
{
    public static class SecureQueryImpl
    {
        public static async Task Query(IOwinContext context, SecureSearcherManager searcherManager, string tenantId)
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

            JToken result = Search(searcherManager, tenantId, scheme, q, countOnly, includePrerelease, skip, take, includeExplanation);

            await ServiceHelpers.WriteResponse(context, HttpStatusCode.OK, result);
        }

        public static JToken Search(SecureSearcherManager searcherManager, string tenantId, string scheme, string q, bool countOnly, bool includePrerelease, int skip, int take, bool includeExplanation)
        {
            IndexSearcher searcher = searcherManager.Get();
            try
            {
                Filter filter = searcherManager.GetFilter(tenantId, "http://schema.nuget.org/schema#ApiAppPackage");

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
                obj["@type"] = document.Get("@type"); ;
                obj["registration"] = new Uri(registrationBaseAddress, string.Format("{0}/index.json", id.ToLowerInvariant())).AbsoluteUri;
                obj["id"] = id;

                ServiceHelpers.AddField(obj, document, "packageContent", "PackageContent");
                ServiceHelpers.AddField(obj, document, "catalogEntry", "CatalogEntry");

                ServiceHelpers.AddField(obj, document, "tenantId", "TenantId");
                ServiceHelpers.AddField(obj, document, "namespace", "Namespace");
                ServiceHelpers.AddField(obj, document, "visibility", "Visibility");
                ServiceHelpers.AddField(obj, document, "description", "Description");
                ServiceHelpers.AddField(obj, document, "summary", "Summary");
                ServiceHelpers.AddField(obj, document, "title", "Title");
                ServiceHelpers.AddField(obj, document, "iconUrl", "IconUrl");
                ServiceHelpers.AddFieldAsArray(obj, document, "tags", "Tags");
                ServiceHelpers.AddFieldAsArray(obj, document, "authors", "Authors");

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
    }
}