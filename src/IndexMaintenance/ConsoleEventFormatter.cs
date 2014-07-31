using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Formatters;

namespace IndexMaintenance
{
    public class ConsoleEventFormatter : IEventTextFormatter
    {
        public void WriteEvent(EventEntry eventEntry, TextWriter writer)
        {
            writer.WriteLine(
                "[{0}]({1}/{2}) {3}",
                eventEntry.Schema.Level,
                eventEntry.Schema.ProviderName,
                eventEntry.Schema.EventName,
                eventEntry.FormattedMessage);
        }
    }
}
