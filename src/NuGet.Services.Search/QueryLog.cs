using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Services.Storage;

namespace NuGet.Services.Search
{
    public class QueryLog
    {
        private ConcurrentQueue<SearchQueryLogEntry> _records = new ConcurrentQueue<SearchQueryLogEntry>();

        /// <summary>
        /// Records a new query in the queue
        /// </summary>
        /// <param name="query">The query text to record</param>
        /// <param name="projectType">The project type guids used</param>
        /// <param name="feed">The feed queried</param>
        /// <param name="userAgent">The User Agent that invoked the query</param>
        /// <param name="timeTakenInMs">The duration of the query in milliseconds</param>
        public void RecordQuery(string query, string projectType, string feed, string userAgent, int timeTakenInMs)
        {
            _records.Enqueue(new SearchQueryLogEntry(DateTime.UtcNow, query, projectType, feed, userAgent, timeTakenInMs));
        }

        /// <summary>
        /// Retrieves a batch of query records from the system
        /// </summary>
        /// <param name="maxSize">The maximum number of records to retrieve</param>
        /// <returns>The records retrieved. Always non-null, may be empty.</returns>
        public IList<SearchQueryLogEntry> GetBatch(int? maxSize = null)
        {
            // Fetch as many as we can, or until we have maxSize items
            IList<SearchQueryLogEntry> results = new List<SearchQueryLogEntry>();
            SearchQueryLogEntry record;
            while ((maxSize == null || results.Count < maxSize.Value) && _records.TryDequeue(out record))
            {
                results.Add(record);
            }

            return results;
        }
    }

    public class SearchQueryLogEntry : AzureTableEntity
    {
        public string Query { get; private set; }
        public string ProjectTypeGuids { get; private set; }
        public string Feed { get; private set; }
        public string UserAgent { get; private set; }
        public int TimeTakenInMs { get; private set; }
        
        public SearchQueryLogEntry(DateTimeOffset timeStamp, string query, string projectTypeGuids, string feed, string userAgent, int timeTakenInMs)
            : base(GetPartitionKey(timeStamp), GetRowKey(timeStamp), timeStamp)
        {
            Query = query;
            ProjectTypeGuids = projectTypeGuids;
            Feed = feed;
            UserAgent = userAgent;
            TimeTakenInMs = timeTakenInMs;
        }

        private static string GetRowKey(DateTimeOffset timeStamp)
        {
            return (DateTime.MaxValue.Ticks - timeStamp.UtcDateTime.Ticks).ToString("D19") +
                "_" +
                Guid.NewGuid().ToString("N");
        }

        private static string GetPartitionKey(DateTimeOffset timeStamp)
        {
            return (DateTime.MaxValue.Ticks - timeStamp.UtcDateTime.Date.Ticks).ToString("D19");
        }
    }
}
