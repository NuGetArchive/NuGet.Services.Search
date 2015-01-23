using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Store.Azure;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;

namespace NuGet.Indexing
{
    public class NuGetSearcherManager : SearcherManager
    {
        Tuple<IDictionary<string, Filter>, IDictionary<string, Filter>> _filters;
        IDictionary<string, JArray[]> _versionsByDoc;

        public static readonly TimeSpan FrameworksRefreshRate = TimeSpan.FromHours(24);
        public static readonly TimeSpan PortableFrameworksRefreshRate = TimeSpan.FromHours(24);
        public static readonly TimeSpan RankingRefreshRate = TimeSpan.FromHours(24);
        public static readonly TimeSpan DownloadCountRefreshRate = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan FrameworkCompatibilityRefreshRate = TimeSpan.FromHours(24);

        IndexData<IDictionary<string, IDictionary<string, int>>> _currentRankings;
        IndexData<IDictionary<string, IDictionary<string, int>>> _currentDownloadCounts;
        IndexData<IList<FrameworkName>> _currentFrameworkList;
        IndexData<IDictionary<string, ISet<string>>> _currentFrameworkCompatibility;

        public Rankings Rankings { get; private set; }
        public DownloadLookup DownloadCounts { get; private set; }
        public FrameworksList Frameworks { get; private set; }
        public FrameworkCompatibility FrameworkCompatibility { get; private set; }

        public string IndexName { get; private set; }
        public IDictionary<string, Uri> RegistrationBaseAddress { get; private set; }

        public DateTime LastReopen { get; private set; }

        public NuGetSearcherManager(string indexName, Lucene.Net.Store.Directory directory, Rankings rankings, DownloadLookup downloadCounts, FrameworksList frameworks, FrameworkCompatibility frameworkCompatibility)
            : base(directory)
        {
            Rankings = rankings;
            DownloadCounts = downloadCounts;
            Frameworks = frameworks;
            IndexName = indexName;
            FrameworkCompatibility = frameworkCompatibility;

            RegistrationBaseAddress = new Dictionary<string, Uri>();

            _currentDownloadCounts = new IndexData<IDictionary<string, IDictionary<string, int>>>(
                "DownloadCounts",
                DownloadCounts.Path,
                DownloadCounts.Load,
                DownloadCountRefreshRate);
            _currentRankings = new IndexData<IDictionary<string, IDictionary<string, int>>>(
                "Rankings",
                Rankings.Path,
                Rankings.Load,
                RankingRefreshRate);
            _currentFrameworkList = new IndexData<IList<FrameworkName>>(
                "FrameworkList",
                Frameworks.Path,
                Frameworks.Load,
                FrameworksRefreshRate);
            _currentFrameworkCompatibility = new IndexData<IDictionary<string,ISet<string>>>(
                "FrameworkCompatibility",
                FrameworkCompatibility.Path,
                FrameworkCompatibility.Load,
                FrameworkCompatibilityRefreshRate
                );
        }

        // /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public static NuGetSearcherManager CreateAzure(
            string storageConnectionString,
            string indexContainer = null,
            string dataContainer = null)
        {
            return CreateAzure(
                CloudStorageAccount.Parse(storageConnectionString),
                indexContainer,
                dataContainer);
        }
        public static NuGetSearcherManager CreateAzure(
            CloudStorageAccount storageAccount,
            string indexContainer = null,
            string dataContainer = null)
        {
            if (String.IsNullOrEmpty(indexContainer))
            {
                indexContainer = "ng-search-index";
            }

            string dataPath = String.Empty;
            if (String.IsNullOrEmpty(dataContainer))
            {
                dataContainer = indexContainer;
                dataPath = "data/";
            }

            return new NuGetSearcherManager(
                indexContainer,
                new AzureDirectory(storageAccount, indexContainer, new RAMDirectory()),
                new StorageRankings(storageAccount, dataContainer, dataPath + Rankings.FileName),
                new StorageDownloadLookup(storageAccount, dataContainer, dataPath + DownloadLookup.FileName),
                new StorageFrameworksList(storageAccount, dataContainer, dataPath + FrameworksList.FileName),
                new StorageFrameworkCompatibility(storageAccount, dataContainer, dataPath + FrameworkCompatibility.FileName));
        }

        // /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public static NuGetSearcherManager CreateLocal(string luceneDirectory, string dataDirectory)
        {
            string frameworksFile = Path.Combine(dataDirectory, FrameworksList.FileName);
            string rankingsFile = Path.Combine(dataDirectory, Rankings.FileName);
            string downloadCountsFile = Path.Combine(dataDirectory, DownloadLookup.FileName);
            string frameworkCompatibilityFile = Path.Combine(dataDirectory, FrameworkCompatibility.FileName);

            return new NuGetSearcherManager(
                luceneDirectory,
                new SimpleFSDirectory(new DirectoryInfo(luceneDirectory)),
                new LocalRankings(rankingsFile),
                new LocalDownloadLookup(downloadCountsFile),
                new LocalFrameworksList(frameworksFile),
                new LocalFrameworkCompatibility(frameworkCompatibilityFile));
        }

        // ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////// 

        protected override void Warm(IndexSearcher searcher)
        {
            searcher.Search(new MatchAllDocsQuery(), 1);

            // Reload download counts and rankings synchronously
            _currentDownloadCounts.Reload();
            _currentRankings.Reload();
            _currentFrameworkList.Reload();
            _currentFrameworkCompatibility.Reload();

            _filters = Compatibility.Warm(searcher.IndexReader, _currentFrameworkCompatibility.Value);

            _versionsByDoc = new Dictionary<string, JArray[]>();
            _versionsByDoc["http"] = CreateVersionsLookUp(searcher.IndexReader, _currentDownloadCounts.Value, RegistrationBaseAddress["http"]);
            _versionsByDoc["https"] = CreateVersionsLookUp(searcher.IndexReader, _currentDownloadCounts.Value, RegistrationBaseAddress["https"]);

            LastReopen = DateTime.UtcNow;
        }

        public IDictionary<string, int> GetRankings(string name = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = "Rank";
            }
            
            return _currentRankings.Value[name];
        }

        public Filter GetFilter(bool includePrerelease, string supportedFramework)
        {
            IDictionary<string, Filter> lookUp = includePrerelease ? _filters.Item2 : _filters.Item1;

            string frameworkFullName;

            if (string.IsNullOrEmpty(supportedFramework))
            {
                frameworkFullName = "any";
            }
            else
            {
                FrameworkName frameworkName = VersionUtility.ParseFrameworkName(supportedFramework);
                frameworkFullName = frameworkName.FullName;
                if (frameworkFullName == "Unsupported,Version=v0.0")
                {
                    try
                    {
                        frameworkName = new FrameworkName(supportedFramework);
                        frameworkFullName = frameworkName.FullName;
                    }
                    catch (ArgumentException)
                    {
                        frameworkFullName = "any";
                    }
                }
            }

            Filter filter;
            if (lookUp.TryGetValue(frameworkFullName, out filter))
            {
                return filter;
            }

            return null;
        }

        public JArray GetVersions(string scheme, int doc)
        {
            return _versionsByDoc[scheme][doc];
        }

        static JArray[] CreateVersionsLookUp(IndexReader reader, IDictionary<string, IDictionary<string, int>> downloadLookup, Uri registrationBaseAddress)
        {
            IDictionary<string, IList<string>> registrations = new Dictionary<string, IList<string>>();

            for (int i = 0; i < reader.MaxDoc; i++)
            {
                if (reader.IsDeleted(i))
                {
                    continue;
                }

                Document document = reader[i];

                string id = document.Get("Id").ToLowerInvariant();
                string version = document.Get("Version");

                IList<string> versions;
                if (!registrations.TryGetValue(id, out versions))
                {
                    versions = new List<string>();
                    registrations.Add(id, versions);
                }

                versions.Add(version);
            }

            IDictionary<string, JArray> versionsById = new Dictionary<string, JArray>();

            foreach (KeyValuePair<string, IList<string>> registration in registrations)
            {
                IDictionary<string, int> downloadsByVersion = null;
                downloadLookup.TryGetValue(registration.Key, out downloadsByVersion);

                JArray versions = new JArray();

                foreach (string version in registrations[registration.Key].OrderByDescending(v => new SemanticVersion(v)))
                {
                    JObject versionObj = new JObject();
                    versionObj.Add("version", version);

                    int downloads = 0;
                    if (downloadsByVersion != null)
                    {
                        downloadsByVersion.TryGetValue(version, out downloads);
                    }
                    versionObj.Add("downloads", downloads);

                    Uri versionUri = new Uri(registrationBaseAddress, string.Format("{0}/{1}.json", registration.Key, version).ToLowerInvariant());
                    versionObj.Add("@id", versionUri.AbsoluteUri);

                    versions.Add(versionObj);
                }

                versionsById.Add(registration.Key, versions);
            }

            JArray[] versionsByDoc = new JArray[reader.MaxDoc];

            for (int i = 0; i < reader.MaxDoc; i++)
            {
                if (reader.IsDeleted(i))
                {
                    continue;
                }

                Document document = reader[i];
                
                string id = document.Get("Id").ToLowerInvariant();

                versionsByDoc[i] = versionsById[id];
            }

            return versionsByDoc;
        }
    }
}