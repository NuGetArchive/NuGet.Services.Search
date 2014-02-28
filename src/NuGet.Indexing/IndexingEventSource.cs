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
            Level = EventLevel.Informational,
            Message = "Loading metadata from Lucene Directory of type {0}",
            Task = Tasks.LoadingMetadata,
            Opcode = EventOpcode.Start)]
        private void LoadingMetadata(string directoryType) { WriteEvent(1, directoryType); }

        [NonEvent]
        public void LoadingMetadata(Type directoryType) { LoadingMetadata(directoryType.FullName); }

        [Event(
            eventId: 2,
            Level = EventLevel.Informational,
            Message = "Loaded metadata from Lucene Directory of type {0}",
            Task = Tasks.LoadingMetadata,
            Opcode = EventOpcode.Stop)]
        private void LoadedMetadata(string directoryType) { WriteEvent(2, directoryType); }

        [NonEvent]
        public void LoadedMetadata(Type directoryType) { LoadedMetadata(directoryType.FullName); }

        public static class Tasks
        {
            public const EventTask LoadingMetadata = (EventTask)0x01;
            public const EventTask CommittingDocument = (EventTask)0x02;
        }
    }
}
