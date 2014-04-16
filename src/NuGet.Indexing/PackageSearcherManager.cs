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

        IDictionary<string, IDictionary<string, int>> _currentRankings;
        IDictionary<int, DownloadCountRecord> _currentDownloadCounts;
        long _rankingsTimeStampUtcTicks;
        long _downloadCountsTimeStampUtcTicks;

        public Rankings Rankings { get; private set; }
        public DownloadCounts DownloadCounts { get; private set; }
        public Guid Id { get; private set; }

        public PackageSearcherManager(Lucene.Net.Store.Directory directory, Rankings rankings, DownloadCounts downloadCounts)
            : base(directory)
        {
            Rankings = rankings;
            DownloadCounts = downloadCounts;
        
            Id = Guid.NewGuid(); // Used for identifying changes to the searcher manager at runtime.
        }

        protected override void Warm(IndexSearcher searcher)
        {
            searcher.Search(new MatchAllDocsQuery(), 1);
        }

        public IDictionary<string, int> GetRankings(string context)
        {
            if (_currentRankings == null || (DateTime.UtcNow.Ticks - _rankingsTimeStampUtcTicks) > RankingRefreshRate.Ticks)
            {
                IndexingEventSource.Log.DataExpiredReloading("Rankings", Rankings.Path);
                Task.Factory.StartNew(() =>
                {
                    IndexingEventSource.Log.ReloadingData("Rankings", Rankings.Path);
                    var newRankings = Rankings.Load();
                    Interlocked.Exchange(ref _rankingsTimeStampUtcTicks, DateTime.UtcNow.Ticks);
                    Interlocked.Exchange(ref _currentRankings, newRankings);
                    IndexingEventSource.Log.ReloadedData("Rankings");
                });
            }

            // Capture the current value
            var tempRankings = _currentRankings;

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
            if (_currentDownloadCounts == null || (DateTime.UtcNow.Ticks - _downloadCountsTimeStampUtcTicks) > DownloadCountRefreshRate.Ticks)
            {
                IndexingEventSource.Log.DataExpiredReloading("DownloadCounts", DownloadCounts.Path);
                Task.Factory.StartNew(() =>
                {
                    IndexingEventSource.Log.ReloadingData("DownloadCounts", DownloadCounts.Path);
                    var newCounts = DownloadCounts.Load();
                    Interlocked.Exchange(ref _downloadCountsTimeStampUtcTicks, DateTime.UtcNow.Ticks);
                    Interlocked.Exchange(ref _currentDownloadCounts, newCounts);
                    IndexingEventSource.Log.ReloadedData("DownloadCounts");
                });
            }

            // Capture the current value and use it
            var downloadCounts = _currentDownloadCounts;
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
    }
}