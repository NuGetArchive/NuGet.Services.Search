using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGet.Services.Client;
using NuGet.Services.Search.Models;

namespace NuGet.Services.Search.Client
{
    public class SearchClient
    {
        private HttpClient _client;

        /// <summary>
        /// Create a search service client from the specified base uri and credentials.
        /// </summary>
        /// <param name="baseUri">The URL to the root of the service</param>
        /// <param name="handlers">Handlers to apply to the request in order from first to last</param>
        public SearchClient(Uri baseUri, params DelegatingHandler[] handlers)
            : this(baseUri, null, handlers)
        {
        }

        /// <summary>
        /// Create a search service client from the specified base uri and credentials.
        /// </summary>
        /// <param name="baseUri">The URL to the root of the service</param>
        /// <param name="credentials">The credentials to connect to the service with</param>
        /// <param name="handlers">Handlers to apply to the request in order from first to last</param>
        public SearchClient(Uri baseUri, ICredentials credentials, params DelegatingHandler[] handlers)
        {
            // Link the handlers
            HttpMessageHandler handler = new HttpClientHandler()
            {
                Credentials = credentials,
                AllowAutoRedirect = true,
                UseDefaultCredentials = credentials == null
            };

            foreach (var providedHandler in handlers.Reverse())
            {
                providedHandler.InnerHandler = handler;
                handler = providedHandler;
            }

            _client = new HttpClient(handler, disposeHandler: true);
            _client.BaseAddress = baseUri;
        }

        /// <summary>
        /// Create a search service client from the specified HttpClient. This client MUST have a valid
        /// BaseAddress, as the WorkClient will always use relative URLs to request work service APIs.
        /// The BaseAddress should point at the root of the service, NOT at the work service node.
        /// </summary>
        /// <param name="client">The client to use</param>
        public SearchClient(HttpClient client)
        {
            _client = client;
        }


        private static readonly Dictionary<SortOrder, string> _sortNames = new Dictionary<SortOrder, string>()
        {
            {SortOrder.LastEdited, "lastEdited"},
            {SortOrder.Relevance, "relevance"},
            {SortOrder.Published, "published"},
            {SortOrder.TitleAscending, "title-asc"},
            {SortOrder.TitleDescending, "title-desc"},
        };

        public async Task<ServiceResponse<SearchResults>> Search(
            string query,
            string projectTypeFilter = null,
            bool includePrerelease = false,
            string curatedFeed = null,
            SortOrder sortBy = SortOrder.Relevance,
            int skip = 0,
            int take = 10,
            bool isLuceneQuery = false,
            bool countOnly = false,
            bool explain = false,
            bool getAllVersions = false)
        {
            IDictionary<string, string> nameValue = new Dictionary<string, string>();
            nameValue.Add("q", query);
            nameValue.Add("skip", skip.ToString());
            nameValue.Add("take", take.ToString());
            nameValue.Add("sortBy", _sortNames[sortBy]);
            
            if (!String.IsNullOrEmpty(projectTypeFilter))
            {
                nameValue.Add("projectType", projectTypeFilter);
            }

            if (includePrerelease)
            {
                nameValue.Add("prerelease", "true");
            }

            if (!String.IsNullOrEmpty(curatedFeed))
            {
                nameValue.Add("feed", curatedFeed);
            }

            if (!isLuceneQuery)
            {
                nameValue.Add("luceneQuery", "false");
            }
            
            if (explain)
            {
                nameValue.Add("explanation", "true");
            }

            if (getAllVersions)
            {
                nameValue.Add("ignoreFilter", "true");
            }

            if (countOnly)
            {
                nameValue.Add("countOnly", "true");
            }

            FormUrlEncodedContent qs = new FormUrlEncodedContent(nameValue);

            return new ServiceResponse<SearchResults>(await _client.GetAsync("search/query?" + (await qs.ReadAsStringAsync())));
        }

        public async Task<ServiceResponse<IDictionary<int, int>>> GetChecksums(int minKey, int maxKey)
        {
            var response = await _client.GetAsync("search/range?min=" + minKey.ToString() + "&max=" + maxKey.ToString());
            return new ServiceResponse<IDictionary<int, int>>(
                response,
                async () => (await response.Content.ReadAsAsync<IDictionary<string, int>>())
                    .Select(pair => new KeyValuePair<int, int>(Int32.Parse(pair.Key), pair.Value))
                    .ToDictionary(pair => pair.Key, pair => pair.Value));
        }

        public async Task<ServiceResponse<IEnumerable<string>>> GetStoredFieldNames()
        {
            return new ServiceResponse<IEnumerable<string>>(
                await _client.GetAsync("search/fields"));
        }

        public async Task<ServiceResponse<JObject>> GetDiagnostics()
        {
            return new ServiceResponse<JObject>(
                await _client.GetAsync("search/diag"));
        }
    }
}
