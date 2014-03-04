﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Indexing
{
    /// <summary>
    /// Updates the index incrementally.
    /// </summary>
    public class UpdateIndexTask : IndexTask
    {
        public override void Execute()
        {
            IDictionary<int, int> database = GalleryExport.FetchGalleryChecksums(SqlConnectionString);

            Log.WriteLine("fetched {0} keys from database", database.Count);

            Tuple<int, int> minMax = GalleryExport.FindMinMaxKey(database);

            Log.WriteLine("min = {0}, max = {1}", minMax.Item1, minMax.Item2);

            // For now, use the in-memory Searcher client. But eventually this will use the original Search Service call below
            IDictionary<int, int> index = SearchServiceClient.ParseRangeResult(
                Searcher.KeyRangeQuery(GetSearcherManager(), minMax.Item1, minMax.Item2));
            //IDictionary<int, int> index = SearchServiceClient.GetRangeFromIndex(minMax.Item1, minMax.Item2, Host);

            Log.WriteLine("fetched {0} keys from index", index.Count);

            List<int> adds = new List<int>();
            List<int> updates = new List<int>();
            List<int> deletes = new List<int>();

            SortIntoAddsUpdateDeletes(database, index, adds, updates, deletes);

            Log.WriteLine("{0} adds", adds.Count);
            Log.WriteLine("{0} updates", updates.Count);
            Log.WriteLine("{0} deletes", deletes.Count);

            if (adds.Count == 0 && updates.Count == 0)
            {
                return;
            }

            IDictionary<int, IEnumerable<string>> feeds = GalleryExport.GetFeedsByPackageRegistration(SqlConnectionString);
            IDictionary<int, IndexDocumentData> packages = PackageIndexing.LoadDocumentData(SqlConnectionString, adds, updates, deletes, feeds, database);

            Lucene.Net.Store.Directory directory = GetDirectory();

            PackageIndexing.UpdateIndex(WhatIf, adds, updates, deletes, (key) => { return packages[key]; }, directory);
        }

        private void SortIntoAddsUpdateDeletes(IDictionary<int, int> database, IDictionary<int, int> index, List<int> adds, List<int> updates, List<int> deletes)
        {
            foreach (KeyValuePair<int, int> databaseItem in database)
            {
                int indexChecksum = 0;
                if (index.TryGetValue(databaseItem.Key, out indexChecksum))
                {
                    if (databaseItem.Value != indexChecksum)
                    {
                        updates.Add(databaseItem.Key);
                    }
                }
                else
                {
                    adds.Add(databaseItem.Key);
                }
            }

            foreach (KeyValuePair<int, int> indexItem in index)
            {
                if (!database.ContainsKey(indexItem.Key))
                {
                    deletes.Add(indexItem.Key);
                }
            }
        }
    }
}
