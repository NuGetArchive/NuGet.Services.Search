using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services.Search
{
    [EventSource(Name = "Outercurve-NuGet-Search-Service")]
    public class SearchServiceEventSource : EventSource
    {
        public static readonly SearchServiceEventSource Log = new SearchServiceEventSource();
        private SearchServiceEventSource() { }

        [Event(
            eventId: 1,
            Level = EventLevel.Critical,
            Task = Tasks.Startup,
            Opcode = EventOpcode.Stop,
            Message = "Search Service encountered a fatal startup error: {0}")]
        private void StartupError(string exception) { WriteEvent(1, exception); }

        [NonEvent]
        public void StartupError(Exception ex) { StartupError(ex.ToString()); }

        [Event(
            eventId: 2,
            Level = EventLevel.Critical,
            Task = Tasks.Request,
            Message = "Error during request to '{0}': {1}")]
        public void RequestError(string url, string exception) { WriteEvent(2, url, exception); }

        [Event(
            eventId: 3,
            Level = EventLevel.Informational,
            Task = Tasks.ReloadingIndex,
            Message = "Reloading Index")]
        public void ReloadingIndex() { WriteEvent(3); }

        [Event(
            eventId: 4,
            Level = EventLevel.Informational,
            Task = Tasks.ReloadingIndex,
            Message = "Reloaded Index")]
        public void ReloadedIndex() { WriteEvent(4); }

        public static class Tasks
        {
            public const EventTask Startup = (EventTask)1;
            public const EventTask Request = (EventTask)2;
            public const EventTask ReloadingIndex = (EventTask)3;
        }
    }
}
