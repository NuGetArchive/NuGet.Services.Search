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
        public static readonly SearchServiceEventSource Log = new WorkServiceEventSource();
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

        public static class Tasks
        {
            public const EventTask Startup = (EventTask)1;
        }
    }
}
