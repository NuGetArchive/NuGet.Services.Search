using Lucene.Net.Analysis;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Store.Azure;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

namespace NuGet.Indexing
{
    public class SecureSearcherManager : SearcherManager
    {
        IDictionary<string, Filter> _filters;
        IDictionary<string, JArray[]> _versionsByDoc;
        JArray[] _versionListsByDoc;

        public string IndexName { get; private set; }
        public IDictionary<string, Uri> RegistrationBaseAddress { get; private set; }

        public DateTime LastReopen { get; private set; }

        public SecureSearcherManager(string indexName, Lucene.Net.Store.Directory directory)
            : base(directory)
        {
            IndexName = indexName;

            RegistrationBaseAddress = new Dictionary<string, Uri>();
        }

        public static SecureSearcherManager CreateLocal(string path)
        {
            return new SecureSearcherManager(path, new SimpleFSDirectory(new DirectoryInfo(path)));
        }

        public static SecureSearcherManager CreateAzure(string storagePrimary, string searchIndexContainer)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storagePrimary);
            return new SecureSearcherManager(searchIndexContainer, new AzureDirectory(storageAccount, searchIndexContainer, new RAMDirectory()));
        }

        // ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////// 

        protected override void Warm(IndexSearcher searcher)
        {
            searcher.Search(new MatchAllDocsQuery(), 1);

            // Create the tenant filters
            _filters = new Dictionary<string, Filter>();
            IEnumerable<string> tenantIds = PackageTenantId.GetDistintTenantId(searcher.IndexReader);
            foreach (string tenantId in tenantIds)
            {
                _filters.Add(tenantId, new CachingWrapperFilter(new TenantFilter(tenantId)));
            }

            // Recalculate precalculated Versions arrays 
            PackageVersions packageVersions = new PackageVersions(searcher.IndexReader);
            
            _versionsByDoc = new Dictionary<string, JArray[]>();
            _versionsByDoc["http"] = packageVersions.CreateVersionsLookUp(null, RegistrationBaseAddress["http"]);
            _versionsByDoc["https"] = packageVersions.CreateVersionsLookUp(null, RegistrationBaseAddress["https"]);

            _versionListsByDoc = packageVersions.CreateVersionListsLookUp();

            LastReopen = DateTime.UtcNow;
        }

        public Filter GetFilter(string tenantId)
        {
            Filter publicTenantFilter = _filters["PUBLIC"];

            Filter tenantFilter;
            if (_filters.TryGetValue(tenantId, out tenantFilter))
            {
                Filter chainedFilter = new ChainedFilter(new Filter[] { publicTenantFilter, tenantFilter }, ChainedFilter.Logic.OR);
                return chainedFilter;
            }
            else
            {
                return publicTenantFilter;
            }
        }

        public JArray GetVersions(string scheme, int doc)
        {
            return _versionsByDoc[scheme][doc];
        }

        public JArray GetVersionLists(int doc)
        {
            return _versionListsByDoc[doc];
        }
    }
}