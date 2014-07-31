using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Indexing
{
    [EventSource(Name = "Outercurve-NuGet-Search-Indexing")]
    public class IndexingEventSource : EventSource
    {
        public static readonly IndexingEventSource Log = new IndexingEventSource();
        private IndexingEventSource() { }

        [Event(
            eventId: 1,
            Message = "Reloading {0} from {1}...",
            Level = EventLevel.Informational,
            Opcode = EventOpcode.Start,
            Task = Tasks.ReloadingData)]
        public void ReloadingData(string data, string path) { WriteEvent(1, data, path); }

        [Event(
            eventId: 2,
            Message = "Reloaded {0}",
            Level = EventLevel.Informational,
            Opcode = EventOpcode.Stop,
            Task = Tasks.ReloadingData)]
        public void ReloadedData(string data) { WriteEvent(2, data); }

        [Event(
            eventId: 3,
            Message = "{0} data has expired, starting background reload from {1}",
            Level = EventLevel.Informational)]
        public void DataExpiredReloading(string data, string path) { WriteEvent(3, data, path); }

        [Event(
            eventId: 4,
            Message = "Loading Searcher Manager from data in {0}",
            Level = EventLevel.Informational,
            Opcode = EventOpcode.Start,
            Task = Tasks.LoadingSearcherManager)]
        public void LoadingSearcherManager(string path) { WriteEvent(4, path); }

        [Event(
            eventId: 5,
            Message = "Loaded Searcher Manager",
            Level = EventLevel.Informational,
            Opcode = EventOpcode.Stop,
            Task = Tasks.LoadingSearcherManager)]
        public void LoadedSearcherManager() { WriteEvent(5); }

        [Event(
            eventId: 6,
            Message = "Creating new, empty, {0} index",
            Level = EventLevel.Informational,
            Opcode = EventOpcode.Start,
            Task = Tasks.CreatingEmptyIndex)]
        public void CreatingEmptyIndex(string indexType) { WriteEvent(6, indexType); }

        [Event(
            eventId: 7,
            Message = "Created Index",
            Level = EventLevel.Informational,
            Opcode = EventOpcode.Stop,
            Task = Tasks.CreatingEmptyIndex)]
        public void CreatedEmptyIndex() { WriteEvent(7); }

        [Event(
            eventId: 8,
            Message = "Rebuilding index from data in {0}/{1} using frameworks list in {2}",
            Level = EventLevel.Informational,
            Opcode = EventOpcode.Start,
            Task = Tasks.RebuildingIndex)]
        public void RebuildingIndex(string server, string database, string frameworksListPath) { WriteEvent(8, server, database, frameworksListPath); }

        [Event(
            eventId: 9,
            Message = "Index Rebuilt",
            Level = EventLevel.Informational,
            Opcode = EventOpcode.Stop,
            Task = Tasks.RebuildingIndex)]
        public void RebuiltIndex() { WriteEvent(9); }

        [Event(
            eventId: 10,
            Message = "Indexing batch of {1} starting at key {0}",
            Level = EventLevel.Informational,
            Opcode = EventOpcode.Start,
            Task = Tasks.IndexingBatch)]
        public void IndexingBatch(int startKey, int batchSize) { WriteEvent(10, startKey, batchSize); }

        [Event(
            eventId: 11,
            Message = "Batch Indexed",
            Level = EventLevel.Informational,
            Opcode = EventOpcode.Stop,
            Task = Tasks.IndexingBatch)]
        public void IndexedBatch() { WriteEvent(11); }

        [Event(
            eventId: 12,
            Message = "Fetching batch of {1} checksums starting at {0}",
            Level = EventLevel.Verbose,
            Opcode = EventOpcode.Start,
            Task = Tasks.FetchingChecksums)]
        public void FetchingChecksums(int startKey, int batchSize) { WriteEvent(12, startKey, batchSize); }

        [Event(
            eventId: 13,
            Message = "Checksums Fetched",
            Level = EventLevel.Verbose,
            Opcode = EventOpcode.Stop,
            Task = Tasks.FetchingChecksums)]
        public void FetchedChecksums() { WriteEvent(13); }

        [Event(
            eventId: 14,
            Message = "Fetching curated feed membership data",
            Level = EventLevel.Verbose,
            Opcode = EventOpcode.Start,
            Task = Tasks.FetchingCuratedFeedMembership)]
        public void FetchingCuratedFeedMembership() { WriteEvent(14); }

        [Event(
            eventId: 15,
            Message = "Curated feed membership data fetched. Rows: {0}",
            Level = EventLevel.Verbose,
            Opcode = EventOpcode.Stop,
            Task = Tasks.FetchingCuratedFeedMembership)]
        public void FetchedCuratedFeedMembership(int count) { WriteEvent(15, count); }

        [Event(
            eventId: 16,
            Message = "Fetching {1} rows of package data, starting at {0}",
            Level = EventLevel.Verbose,
            Opcode = EventOpcode.Start,
            Task = Tasks.FetchingPackageData)]
        public void FetchingPackageData(int startKey, int batchSize) { WriteEvent(16, startKey, batchSize); }

        [Event(
            eventId: 17,
            Message = "Fetched package data",
            Level = EventLevel.Verbose,
            Opcode = EventOpcode.Stop,
            Task = Tasks.FetchingPackageData)]
        public void FetchedPackageData() { WriteEvent(17); }

        [Event(
            eventId: 18,
            Message = "Creating Lucene commit from {0} packages.",
            Level = EventLevel.Informational,
            Opcode = EventOpcode.Start,
            Task = Tasks.BuildingCommit)]
        public void BuildingCommit(int batchSize) { WriteEvent(18, batchSize); }

        [Event(
            eventId: 19,
            Message = "Commit complete",
            Level = EventLevel.Informational,
            Opcode = EventOpcode.Stop,
            Task = Tasks.BuildingCommit)]
        public void BuiltCommit() { WriteEvent(19); }

        [Event(
            eventId: 20,
            Message = "Grouping input packages by ID",
            Level = EventLevel.Verbose,
            Opcode = EventOpcode.Start,
            Task = Tasks.GroupingInputPackagesById)]
        public void GroupingInputPackagesById() { WriteEvent(20); }

        [Event(
            eventId: 21,
            Message = "Grouped packages",
            Level = EventLevel.Verbose,
            Opcode = EventOpcode.Stop,
            Task = Tasks.GroupingInputPackagesById)]
        public void GroupedInputPackagesById() { WriteEvent(21); }

        [Event(
            eventId: 22,
            Message = "Retrieving existing Lucene documents by Id: {0}",
            Level = EventLevel.Verbose,
            Opcode = EventOpcode.Start,
            Task = Tasks.RetrievingExistingDocumentsById)]
        public void RetrievingExistingDocumentsById(string id) { WriteEvent(22, id); }

        [Event(
            eventId: 23,
            Message = "Retrieved existing Lucene documents",
            Level = EventLevel.Verbose,
            Opcode = EventOpcode.Stop,
            Task = Tasks.RetrievingExistingDocumentsById)]
        public void RetrievedExistingDocumentsById() { WriteEvent(23); }

        [Event(
            eventId: 24,
            Message = "Updating Facets for packages with Id {0}",
            Level = EventLevel.Verbose,
            Opcode = EventOpcode.Start,
            Task = Tasks.UpdatingFacets)]
        public void UpdatingFacets(string id) { WriteEvent(24, id); }

        [Event(
            eventId: 25,
            Message = "Updated Facets",
            Level = EventLevel.Verbose,
            Opcode = EventOpcode.Stop,
            Task = Tasks.UpdatingFacets)]
        public void UpdatedFacets() { WriteEvent(25); }

        [Event(
            eventId: 26,
            Message = "Updating Latest Version Facets relating to framework {1} for packages with id {0}",
            Level = EventLevel.Verbose,
            Opcode = EventOpcode.Start,
            Task = Tasks.UpdatingLatestVersionFacets)]
        public void UpdatingLatestVersionFacets(string id, string framework) { WriteEvent(26, id, framework); }

        [Event(
            eventId: 27,
            Message = "Updated Latest Version Facets",
            Level = EventLevel.Verbose,
            Opcode = EventOpcode.Stop,
            Task = Tasks.UpdatingLatestVersionFacets)]
        public void UpdatedLatestVersionFacets() { WriteEvent(27); }

        [Event(
            eventId: 28,
            Message = "Updating {1} dirty documents for packages with id {0}",
            Level = EventLevel.Verbose,
            Opcode = EventOpcode.Start,
            Task = Tasks.UpdatingDocuments)]
        public void UpdatingDocuments(string id, int dirtyDocuments) { WriteEvent(28, id, dirtyDocuments); }

        [Event(
            eventId: 29,
            Message = "Updated dirty documents",
            Level = EventLevel.Verbose,
            Opcode = EventOpcode.Stop,
            Task = Tasks.UpdatingDocuments)]
        public void UpdatedDocuments() { WriteEvent(29); }

        [Event(
            eventId: 30,
            Message = "Committing batch of documents",
            Level = EventLevel.Verbose,
            Opcode = EventOpcode.Start,
            Task = Tasks.CommittingDocuments)]
        public void CommittingDocuments() { WriteEvent(30); }

        [Event(
            eventId: 31,
            Message = "Committed documents",
            Level = EventLevel.Verbose,
            Opcode = EventOpcode.Stop,
            Task = Tasks.CommittingDocuments)]
        public void CommittedDocuments() { WriteEvent(31); }

        [Event(
            eventId: 32,
            Message = "Indexing packages with id {0}",
            Level = EventLevel.Verbose,
            Opcode = EventOpcode.Start,
            Task = Tasks.IndexingIdGroup)]
        public void IndexingIdGroup(string id) { WriteEvent(32, id); }

        [Event(
            eventId: 33,
            Message = "Indexed packages",
            Level = EventLevel.Verbose,
            Opcode = EventOpcode.Stop,
            Task = Tasks.IndexingIdGroup)]
        public void IndexedIdGroup() { WriteEvent(33); }

        [Event(
            eventId: 34,
            Message = "Updating package document for {0} v{1}",
            Level = EventLevel.Verbose,
            Opcode = EventOpcode.Start,
            Task = Tasks.UpdatingPackageDocument)]
        public void UpdatingPackageDocument(string id, string version) { WriteEvent(34, id, version); }

        [Event(
            eventId: 35,
            Message = "Updated package document",
            Level = EventLevel.Verbose,
            Opcode = EventOpcode.Stop,
            Task = Tasks.UpdatingPackageDocument)]
        public void UpdatedPackageDocument() { WriteEvent(35); }

        [Event(
            eventId: 36,
            Message = "Updating Compatibility Facets for {0} v{1}",
            Level = EventLevel.Verbose,
            Opcode = EventOpcode.Start,
            Task = Tasks.UpdatingCompatibilityFacets)]
        public void UpdatingCompatibilityFacets(string id, string version) { WriteEvent(36, id, version); }

        [Event(
            eventId: 37,
            Message = "Updated Compatibility Facets",
            Level = EventLevel.Verbose,
            Opcode = EventOpcode.Stop,
            Task = Tasks.UpdatingCompatibilityFacets)]
        public void UpdatedCompatibilityFacets() { WriteEvent(37); }

        [Event(
            eventId: 38,
            Message = "Deleting existing document for {0} v{1}",
            Level = EventLevel.Verbose,
            Opcode = EventOpcode.Start,
            Task = Tasks.DeletingExistingDocument)]
        public void DeletingExistingDocument(string id, string version) { WriteEvent(38, id, version); }

        [Event(
            eventId: 39,
            Message = "Deleted existing document",
            Level = EventLevel.Verbose,
            Opcode = EventOpcode.Stop,
            Task = Tasks.DeletingExistingDocument)]
        public void DeletedExistingDocument() { WriteEvent(39); }

        [Event(
            eventId: 40,
            Message = "Adding Document for {0} v{1}",
            Level = EventLevel.Verbose,
            Opcode = EventOpcode.Start,
            Task = Tasks.AddingDocument)]
        public void AddingDocument(string id, string version) { WriteEvent(40, id, version); }

        [Event(
            eventId: 41,
            Message = "Added Document",
            Level = EventLevel.Verbose,
            Opcode = EventOpcode.Stop,
            Task = Tasks.AddingDocument)]
        public void AddedDocument() { WriteEvent(41); }

        public static class Tasks {
            public const EventTask ReloadingData = (EventTask)1;
            public const EventTask LoadingSearcherManager = (EventTask)2;
            public const EventTask CreatingEmptyIndex = (EventTask)3;
            public const EventTask RebuildingIndex = (EventTask)4;
            public const EventTask IndexingBatch = (EventTask)5;
            public const EventTask FetchingChecksums = (EventTask)6;
            public const EventTask FetchingCuratedFeedMembership = (EventTask)7;
            public const EventTask FetchingPackageData = (EventTask)8;
            public const EventTask BuildingCommit = (EventTask)9;
            public const EventTask GroupingInputPackagesById = (EventTask)10;
            public const EventTask RetrievingExistingDocumentsById = (EventTask)11;
            public const EventTask UpdatingFacets = (EventTask)12;
            public const EventTask UpdatingLatestVersionFacets = (EventTask)13;
            public const EventTask UpdatingDocuments = (EventTask)14;
            public const EventTask CommittingDocuments = (EventTask)15;
            public const EventTask IndexingIdGroup = (EventTask)16;
            public const EventTask UpdatingPackageDocument = (EventTask)17;
            public const EventTask UpdatingCompatibilityFacets = (EventTask)18;
            public const EventTask DeletingExistingDocument = (EventTask)19;
            public const EventTask AddingDocument = (EventTask)20;
        }
    }
}
