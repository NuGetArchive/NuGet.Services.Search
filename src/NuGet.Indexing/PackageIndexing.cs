using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGetGallery;
using Newtonsoft.Json;

namespace NuGet.Indexing
{
    public static class PackageIndexing
    {
        const int MaxDocumentsPerCommit = 800;      //  The maximum number of Lucene documents in a single commit. The min size for a segment.
        const int MergeFactor = 10;                 //  Define the size of a file in a level (exponentially) and the count of files that constitue a level
        const int MaxMergeDocs = 7999;              //  Except never merge segments that have more docs than this 

        public static TextWriter DefaultTraceWriter = Console.Out;

        public static void CreateFreshIndex(Lucene.Net.Store.Directory directory)
        {
            CreateNewEmptyIndex(directory);
        }

        //  this function will incrementally build an index from the gallery using a high water mark stored in the commit metadata
        //  this function is useful for building a fresh index as in that case it is more efficient than diff-ing approach

        public static void BuildIndex(string sqlConnectionString, Lucene.Net.Store.Directory directory, IndexComponents components, TextWriter log = null)
        {
            log = log ?? DefaultTraceWriter;

            while (true)
            {
                DateTime indexTime = DateTime.UtcNow;
                int highestPackageKey = GetHighestPackageKey(directory);

                log.WriteLine("get the checksums from the gallery");
                IDictionary<int, int> checksums = GalleryExport.FetchGalleryChecksums(sqlConnectionString, highestPackageKey);

                log.WriteLine("get curated feeds by PackageRegistration");
                IDictionary<int, IEnumerable<string>> feeds = GalleryExport.GetFeedsByPackageRegistration(sqlConnectionString, log, verbose: false);

                log.WriteLine("indexTime = {0} mostRecentPublished = {1}", indexTime, highestPackageKey);

                log.WriteLine("get packages from gallery where the Package.Key > {0}", highestPackageKey);
                List<Package> packages = GalleryExport.GetPublishedPackagesSince(sqlConnectionString, highestPackageKey, log, verbose: false);

                if (packages.Count == 0)
                {
                    break;
                }

                log.WriteLine("associate the feeds and checksum data with each packages");
                List<IndexDocumentData> indexDocumentData = MakeIndexDocumentData(packages, feeds, checksums);

                AddPackagesToIndex(indexDocumentData, directory, log, components);
            }

            log.WriteLine("all done");
        }

        private static void AddPackagesToIndex(List<IndexDocumentData> indexDocumentData, Lucene.Net.Store.Directory directory, TextWriter log, IndexComponents components)
        {
            log.WriteLine("About to add {0} packages", indexDocumentData.Count);

            for (int index = 0; index < indexDocumentData.Count; index += MaxDocumentsPerCommit)
            {
                int count = Math.Min(MaxDocumentsPerCommit, indexDocumentData.Count - index);

                List<IndexDocumentData> rangeToIndex = indexDocumentData.GetRange(index, count);

                AddToIndex(directory, rangeToIndex, log, components);
            }
        }

        private static void AddToIndex(Lucene.Net.Store.Directory directory, List<IndexDocumentData> rangeToIndex, TextWriter log, IndexComponents components)
        {
            log.WriteLine("begin AddToIndex");

            using (IndexWriter indexWriter = CreateIndexWriter(directory, false))
            {
                int highestPackageKey = -1;

                foreach (IndexDocumentData documentData in rangeToIndex)
                {
                    int currentPackageKey = documentData.Package.Key;

                    Document newDocument = CreateLuceneDocument(documentData, components);

                    indexWriter.AddDocument(newDocument);

                    if (currentPackageKey <= highestPackageKey)
                    {
                        throw new Exception("(currentPackageKey <= highestPackageKey) the data must not be ordered correctly");
                    }
                    
                    highestPackageKey = currentPackageKey;
                }

                log.WriteLine("about to commit {0} packages", rangeToIndex.Count);

                IDictionary<string, string> commitUserData = indexWriter.GetReader().CommitUserData;

                string lastEditsIndexTime = commitUserData["last-edits-index-time"];

                if (lastEditsIndexTime == null)
                {
                    //  this should never happen but if it did Lucene would throw 
                    lastEditsIndexTime = DateTime.MinValue.ToString();
                }

                indexWriter.Commit(PackageIndexing.CreateCommitMetadata(lastEditsIndexTime, highestPackageKey, rangeToIndex.Count, "add"));

                log.WriteLine("commit done");
            }

            log.WriteLine("end AddToIndex");
        }

        public static void CreateNewEmptyIndex(Lucene.Net.Store.Directory directory)
        {
            using (IndexWriter indexWriter = CreateIndexWriter(directory, true))
            {
                indexWriter.Commit(PackageIndexing.CreateCommitMetadata(DateTime.MinValue, 0, 0, "creation"));
            }
        }

        private static IndexWriter CreateIndexWriter(Lucene.Net.Store.Directory directory, bool create)
        {
            IndexWriter indexWriter = new IndexWriter(directory, new PackageAnalyzer(), create, IndexWriter.MaxFieldLength.UNLIMITED);
            indexWriter.MergeFactor = MergeFactor;
            indexWriter.MaxMergeDocs = MaxMergeDocs;

            indexWriter.SetSimilarity(new CustomSimilarity());

            //StreamWriter streamWriter = new StreamWriter(Console.OpenStandardOutput());
            //indexWriter.SetInfoStream(streamWriter);
            //streamWriter.Flush();

            // this should theoretically work but appears to cause empty commit commitMetadata to not be saved
            //((LogMergePolicy)indexWriter.MergePolicy).SetUseCompoundFile(false);
            return indexWriter;
        }

        private static IDictionary<string, string> CreateCommitMetadata(DateTime lastEditsIndexTime, int highestPackageKey, int count, string description)
        {
            return CreateCommitMetadata(lastEditsIndexTime.ToString(), highestPackageKey, count, description);
        }

        private static IDictionary<string, string> CreateCommitMetadata(string lastEditsIndexTime, int highestPackageKey, int count, string description)
        {
            IDictionary<string, string> commitMetadata = new Dictionary<string, string>();

            commitMetadata.Add("commit-time-stamp",  DateTime.UtcNow.ToString());
            commitMetadata.Add("commit-description", description ?? string.Empty);
            commitMetadata.Add("commit-document-count", count.ToString());

            commitMetadata.Add("highest-package-key", highestPackageKey.ToString());
            commitMetadata.Add("last-edits-index-time", lastEditsIndexTime ?? DateTime.MinValue.ToString());

            commitMetadata.Add("MaxDocumentsPerCommit", MaxDocumentsPerCommit.ToString());
            commitMetadata.Add("MergeFactor", MergeFactor.ToString());
            commitMetadata.Add("MaxMergeDocs", MaxMergeDocs.ToString());
            
            return commitMetadata;
        }

        private static DateTime GetLastEditsIndexTime(Lucene.Net.Store.Directory directory)
        {
            IDictionary<string, string> commitMetadata = IndexReader.GetCommitUserData(directory);

            string lastEditsIndexTime;
            if (commitMetadata.TryGetValue("last-edits-index-time", out lastEditsIndexTime))
            {
                return DateTime.Parse(lastEditsIndexTime);
            }

            return DateTime.MinValue;
        }

        private static int GetHighestPackageKey(Lucene.Net.Store.Directory directory)
        {
            IDictionary<string, string> commitMetadata = IndexReader.GetCommitUserData(directory);

            string highestPackageKey;
            if (commitMetadata.TryGetValue("highest-package-key", out highestPackageKey))
            {
                return int.Parse(highestPackageKey);
            }

            return 0;
        }

        private static void Add(Document doc, string name, string value, Field.Store store, Field.Index index, Field.TermVector termVector, float boost = 1.0f)
        {
            if (value == null)
            {
                return;
            }

            Field newField = new Field(name, value, store, index, termVector);
            newField.Boost = boost;
            doc.Add(newField);
        }

        private static void Add(Document doc, string name, int value, Field.Store store, Field.Index index, Field.TermVector termVector, float boost = 1.0f)
        {
            Add(doc, name, value.ToString(CultureInfo.InvariantCulture), store, index, termVector, boost);
        }

        // ----------------------------------------------------------------------------------------------------------------------------------------

        private static Document CreateLuceneDocument(IndexDocumentData documentData, IndexComponents components)
        {
            Document doc = new Document();

            //  Add key and checksum info (in order to support the synchronization with the gallery)

            doc.Add(new NumericField("Key", Field.Store.YES, true).SetIntValue(documentData.Package.Key));
            doc.Add(new NumericField("Checksum", Field.Store.YES, true).SetIntValue(documentData.Checksum));

            if (components.HasFlag(IndexComponents.Data))
            {
                AddDataToDocument(doc, documentData);
            }

            if (components.HasFlag(IndexComponents.Typeahead))
            {
                AddTypeaheadToDocument(doc, documentData);
            }

            return doc;
        }

        private static void AddTypeaheadToDocument(Document doc, IndexDocumentData documentData)
        {
            //  Add data for typeahead search
            Package package = documentData.Package;
            string typeahead = package.PackageRegistration.Id;
            if (!String.IsNullOrEmpty(package.Title) && !String.Equals(package.Title, typeahead, StringComparison.OrdinalIgnoreCase))
            {
                // Add the title if it contributes anything new
                typeahead += " " + package.Title;
            }

            // Store the typeahead without term vectors, so we don't care about order or position of terms
            Add(doc, "Typeahead", typeahead, Field.Store.NO, Field.Index.ANALYZED, Field.TermVector.NO);

            // Is there a data field?
            if (doc.GetField("Data") == null)
            {
                // Add a limited set of data for rendering only
                string json = PackageJson.ToJson(package, minimal: true).ToString(Formatting.None);
                Add(doc, "Data", json, Field.Store.YES, Field.Index.NO, Field.TermVector.NO);
            }
        }

        private static void AddDataToDocument(Document doc, IndexDocumentData documentData)
        {
            Package package = documentData.Package;
            
            //  Query Fields
            float titleBoost = 3.0f;
            float idBoost = 2.0f;

            if (package.Title == null)
            {
                idBoost += titleBoost;
            }

            Add(doc, "Id", package.PackageRegistration.Id, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS, idBoost);
            Add(doc, "TokenizedId", package.PackageRegistration.Id, Field.Store.NO, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS, idBoost);
            Add(doc, "ShingledId", package.PackageRegistration.Id, Field.Store.NO, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS, idBoost);
            Add(doc, "Version", package.Version, Field.Store.NO, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS, idBoost);
            Add(doc, "Title", package.Title, Field.Store.NO, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS, titleBoost);
            Add(doc, "Tags", package.Tags, Field.Store.NO, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS, 1.5f);
            Add(doc, "Description", package.Description, Field.Store.NO, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS);
            Add(doc, "Authors", package.FlattenedAuthors, Field.Store.NO, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS);

            foreach (User owner in package.PackageRegistration.Owners)
            {
                Add(doc, "Owners", owner.Username, Field.Store.NO, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS);
            }

            //  Sorting:

            doc.Add(new NumericField("PublishedDate", Field.Store.YES, true).SetIntValue(int.Parse(package.Published.ToString("yyyyMMdd"))));

            DateTime lastEdited = package.LastEdited ?? package.Published;
            doc.Add(new NumericField("EditedDate", Field.Store.YES, true).SetIntValue(int.Parse(lastEdited.ToString("yyyyMMdd"))));

            string displayName = String.IsNullOrEmpty(package.Title) ? package.PackageRegistration.Id : package.Title;
            displayName = displayName.ToLower(CultureInfo.CurrentCulture);
            Add(doc, "DisplayName", displayName, Field.Store.NO, Field.Index.NOT_ANALYZED, Field.TermVector.NO);

            //  Facets:

            Add(doc, "IsLatest", package.IsLatest ? 1 : 0, Field.Store.NO, Field.Index.NOT_ANALYZED, Field.TermVector.NO);
            Add(doc, "IsLatestStable", package.IsLatestStable ? 1 : 0, Field.Store.NO, Field.Index.NOT_ANALYZED, Field.TermVector.NO);
            Add(doc, "Listed", package.Listed ? 1 : 0, Field.Store.NO, Field.Index.NOT_ANALYZED, Field.TermVector.NO);

            if (documentData.Feeds != null)
            {
                foreach (string feed in documentData.Feeds)
                {
                    //  Store this to aid with debugging
                    Add(doc, "CuratedFeed", feed, Field.Store.YES, Field.Index.NOT_ANALYZED, Field.TermVector.NO);
                }
            }

            foreach (PackageFramework packageFramework in package.SupportedFrameworks)
            {
                Add(doc, "SupportedFramework", packageFramework.TargetFramework, Field.Store.NO, Field.Index.NOT_ANALYZED, Field.TermVector.NO);
            }

            //  Data we want to store in index - these cannot be queried

            JObject obj = PackageJson.ToJson(package);
            string data = obj.ToString(Formatting.None);

            Add(doc, "Data", data, Field.Store.YES, Field.Index.NO, Field.TermVector.NO);
        }

        public static void UpdateIndex(bool whatIf, List<int> adds, List<int> updates, List<int> deletes, Func<int, IndexDocumentData> fetch, Lucene.Net.Store.Directory directory, IndexComponents components, TextWriter log = null)
        {
            log = log ?? DefaultTraceWriter;

            if (whatIf)
            {
                log.WriteLine("WhatIf mode");

                Apply(adds, WhatIf_ApplyAdds, fetch, directory, components, log);
                Apply(updates, WhatIf_ApplyUpdates, fetch, directory, components, log);
                Apply(deletes, WhatIf_ApplyDeletes, fetch, directory, components, log);
            }
            else
            {
                Apply(adds, ApplyAdds, fetch, directory, components, log);
                Apply(updates, ApplyUpdates, fetch, directory, components, log);
                Apply(deletes, ApplyDeletes, fetch, directory, components, log);
            }
        }

        private static void Apply(List<int> packageKeys, Action<List<int>, Func<int, IndexDocumentData>, Lucene.Net.Store.Directory, IndexComponents, TextWriter> action, Func<int, IndexDocumentData> fetch, Lucene.Net.Store.Directory directory, IndexComponents components, TextWriter log)
        {
            for (int index = 0; index < packageKeys.Count; index += MaxDocumentsPerCommit)
            {
                int count = Math.Min(MaxDocumentsPerCommit, packageKeys.Count - index);
                List<int> range = packageKeys.GetRange(index, count);
                action(range, fetch, directory, components, log);
            }
        }

        private static void WhatIf_ApplyAdds(List<int> packageKeys, Func<int, IndexDocumentData> fetch, Lucene.Net.Store.Directory directory, IndexComponents components, TextWriter log)
        {
            log.WriteLine("[WhatIf] adding...");
            foreach (int packageKey in packageKeys)
            {
                IndexDocumentData documentData = fetch(packageKey);
                log.WriteLine("{0} {1} {2}", packageKey, documentData.Package.PackageRegistration.Id, documentData.Package.Version);
            }
        }

        private static void WhatIf_ApplyUpdates(List<int> packageKeys, Func<int, IndexDocumentData> fetch, Lucene.Net.Store.Directory directory, IndexComponents components, TextWriter log)
        {
            log.WriteLine("[WhatIf] updating...");
            foreach (int packageKey in packageKeys)
            {
                IndexDocumentData documentData = fetch(packageKey);
                log.WriteLine("{0} {1} {2}", packageKey, documentData.Package.PackageRegistration.Id, documentData.Package.Version);
            }
        }

        private static void WhatIf_ApplyDeletes(List<int> packageKeys, Func<int, IndexDocumentData> fetch, Lucene.Net.Store.Directory directory, IndexComponents components, TextWriter log)
        {
            log.WriteLine("[WhatIf] deleting...");
            foreach (int packageKey in packageKeys)
            {
                log.WriteLine("{0}", packageKey);
            }
        }
        
        private static void ApplyAdds(List<int> packageKeys, Func<int, IndexDocumentData> fetch, Lucene.Net.Store.Directory directory, IndexComponents components, TextWriter log)
        {
            log.WriteLine("ApplyAdds");

            using (IndexWriter indexWriter = CreateIndexWriter(directory, false))
            {
                int highestPackageKey = -1;
                foreach (int packageKey in packageKeys)
                {
                    IndexDocumentData documentData = fetch(packageKey);
                    int currentPackageKey = documentData.Package.Key;
                    Document newDocument = CreateLuceneDocument(documentData, components);
                    indexWriter.AddDocument(newDocument);
                    if (currentPackageKey <= highestPackageKey)
                    {
                        throw new Exception("(currentPackageKey <= highestPackageKey) the data must not be ordered correctly");
                    }
                    highestPackageKey = currentPackageKey;
                }

                IDictionary<string, string> commitUserData = indexWriter.GetReader().CommitUserData;
                string lastEditsIndexTime = commitUserData["last-edits-index-time"];
                if (lastEditsIndexTime == null)
                {
                    //  this should never happen but if it did Lucene would throw 
                    lastEditsIndexTime = DateTime.MinValue.ToString();
                }

                log.WriteLine("Commit {0} adds", packageKeys.Count);
                indexWriter.Commit(PackageIndexing.CreateCommitMetadata(lastEditsIndexTime, highestPackageKey, packageKeys.Count, "add"));
            }
        }

        private static void ApplyUpdates(List<int> packageKeys, Func<int, IndexDocumentData> fetch, Lucene.Net.Store.Directory directory, IndexComponents components, TextWriter log)
        {
            log.WriteLine("ApplyUpdates");

            PackageQueryParser queryParser = new PackageQueryParser(Lucene.Net.Util.Version.LUCENE_30, "Id", new PackageAnalyzer());

            using (IndexWriter indexWriter = CreateIndexWriter(directory, false))
            {
                IDictionary<string, string> commitUserData = indexWriter.GetReader().CommitUserData;

                foreach (int packageKey in packageKeys)
                {
                    IndexDocumentData documentData = fetch(packageKey);

                    Query query = NumericRangeQuery.NewIntRange("Key", packageKey, packageKey, true, true);
                    indexWriter.DeleteDocuments(query);

                    Document newDocument = PackageIndexing.CreateLuceneDocument(documentData, components);
                    indexWriter.AddDocument(newDocument);
                }

                commitUserData["count"] = packageKeys.Count.ToString();
                commitUserData["commit-description"] = "update";

                log.WriteLine("Commit {0} updates (delete and re-add)", packageKeys.Count);
                indexWriter.Commit(commitUserData);
            }
        }

        private static void ApplyDeletes(List<int> packageKeys, Func<int, IndexDocumentData> fetch, Lucene.Net.Store.Directory directory, IndexComponents components, TextWriter log)
        {
            log.WriteLine("ApplyDeletes");

            PackageQueryParser queryParser = new PackageQueryParser(Lucene.Net.Util.Version.LUCENE_30, "Id", new PackageAnalyzer());

            using (IndexWriter indexWriter = CreateIndexWriter(directory, false))
            {
                IDictionary<string, string> commitUserData = indexWriter.GetReader().CommitUserData;

                foreach (int packageKey in packageKeys)
                {
                    Query query = NumericRangeQuery.NewIntRange("Key", packageKey, packageKey, true, true);
                    indexWriter.DeleteDocuments(query);
                }

                commitUserData["count"] = packageKeys.Count.ToString();
                commitUserData["commit-description"] = "delete";

                log.WriteLine("Commit {0} deletes", packageKeys.Count);
                indexWriter.Commit(commitUserData);
            }
        }

        //  helper functions

        public static IDictionary<int, IndexDocumentData> LoadDocumentData(string connectionString, List<int> adds, List<int> updates, List<int> deletes, IDictionary<int, IEnumerable<string>> feeds, IDictionary<int, int> checksums, TextWriter log = null)
        {
            log = log ?? DefaultTraceWriter;

            IDictionary<int, IndexDocumentData> packages = new Dictionary<int, IndexDocumentData>();

            List<Package> addsPackages = GalleryExport.GetPackages(connectionString, adds, log, verbose: false);
            List<IndexDocumentData> addsIndexDocumentData = MakeIndexDocumentData(addsPackages, feeds, checksums);
            foreach (IndexDocumentData indexDocumentData in addsIndexDocumentData)
            {
                packages.Add(indexDocumentData.Package.Key, indexDocumentData);
            }

            List<Package> updatesPackages = GalleryExport.GetPackages(connectionString, updates, log, verbose: false);
            List<IndexDocumentData> updatesIndexDocumentData = MakeIndexDocumentData(updatesPackages, feeds, checksums);
            foreach (IndexDocumentData indexDocumentData in updatesIndexDocumentData)
            {
                packages.Add(indexDocumentData.Package.Key, indexDocumentData);
            }

            return packages;
        }

        private static List<IndexDocumentData> MakeIndexDocumentData(IList<Package> packages, IDictionary<int, IEnumerable<string>> feeds, IDictionary<int, int> checksums)
        {
            Func<int, IEnumerable<string>> GetFeeds = packageRegistrationKey =>
            {
                IEnumerable<string> ret = null;
                feeds.TryGetValue(packageRegistrationKey, out ret);
                return ret;
            };

            List<IndexDocumentData> result = packages
                .Select(p => new IndexDocumentData { Package = p, Checksum = checksums[p.Key], Feeds = GetFeeds(p.PackageRegistrationKey) })
                .ToList();

            return result;
        }
    }
}
