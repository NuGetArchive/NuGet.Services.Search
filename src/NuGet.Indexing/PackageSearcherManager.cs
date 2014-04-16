using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace NuGet.Indexing
{
    public class PackageSearcherManager : SearcherManager
    {
        public static readonly TimeSpan RankingRefreshRate = TimeSpan.FromHours(24);
        public static readonly TimeSpan DownloadCountRefreshRate = TimeSpan.FromMinutes(5);

        IndexData<IDictionary<string, IDictionary<string, int>>> _currentRankings;
        IndexData<IDictionary<int, DownloadCountRecord>> _currentDownloadCounts;

        public Rankings Rankings { get; private set; }
        public DownloadCounts DownloadCounts { get; private set; }
        public Guid Id { get; private set; }

        public PackageSearcherManager(Lucene.Net.Store.Directory directory, Rankings rankings, DownloadCounts downloadCounts)
            : base(directory)
        {
            Rankings = rankings;
            DownloadCounts = downloadCounts;

            _currentDownloadCounts = new IndexData<IDictionary<int, DownloadCountRecord>>(
                "DownloadCounts",
                DownloadCounts.Path,
                DownloadCounts.Load,
                DownloadCountRefreshRate);
            _currentRankings = new IndexData<IDictionary<string, IDictionary<string, int>>>(
                "Rankings",
                Rankings.Path,
                Rankings.Load,
                RankingRefreshRate);
        
            Id = Guid.NewGuid(); // Used for identifying changes to the searcher manager at runtime.
        }

        protected override void Warm(IndexSearcher searcher)
        {
            searcher.Search(new MatchAllDocsQuery(), 1);

            // Reload download counts and rankings synchronously
            _currentDownloadCounts.Reload();
            _currentRankings.Reload();
        }

        public IDictionary<string, int> GetRankings(string context)
        {
            _currentRankings.MaybeReload();

            // Capture the current value
            var tempRankings = _currentRankings.Value;

            if (tempRankings == null)
            {
                return new Dictionary<string, int>();
            }

            IDictionary<string, int> rankings;
            if (tempRankings.TryGetValue(context, out rankings))
            {
                return rankings;
            }

            return tempRankings["Rank"];
        }

        public DownloadCountRecord GetDownloadCounts(int packageKey)
        {
            _currentDownloadCounts.MaybeReload();

            // Capture the current value and use it
            var downloadCounts = _currentDownloadCounts.Value;
            if (downloadCounts != null)
            {
                DownloadCountRecord record;
                if (downloadCounts.TryGetValue(packageKey, out record))
                {
                    return record;
                }
            }

            return null;
        }

        private class IndexData<T> where T : class
        {
            private Func<T> _loader;
            private object _lock = new object();
            private T _value;

            public string Name { get; private set; }
            public string Path { get; private set; }
            public T Value { get { return _value; } }
            public DateTime LastUpdatedUtc { get; private set; }
            public TimeSpan UpdateInterval { get; private set; }
            public bool Updating { get; private set; }

            public IndexData(string name, string path, Func<T> loader, TimeSpan updateInterval)
            {
                _loader = loader;

                Name = name;
                Path = path;
                LastUpdatedUtc = DateTime.MinValue;
                UpdateInterval = updateInterval;
                Updating = false;
            }

            public void MaybeReload()
            {
                lock (_lock)
                {
                    if ((Value == null || ((DateTime.UtcNow - LastUpdatedUtc) > UpdateInterval)) && !Updating)
                    {
                        // Start updating
                        Updating = true;
                        Task.Factory.StartNew(Reload);
                    }
                }
            }

            public void Reload()
            {
                IndexingEventSource.Log.ReloadingData(Name, Path);
                var newValue = _loader();
                lock (_lock)
                {
                    Updating = false;
                    LastUpdatedUtc = DateTime.UtcNow;

                    // The lock doesn't cover Value, so we need to change it using Interlocked.Exchange.
                    Interlocked.Exchange(ref _value, newValue);
                }
                IndexingEventSource.Log.ReloadedData(Name);
            }
        }
    }
}