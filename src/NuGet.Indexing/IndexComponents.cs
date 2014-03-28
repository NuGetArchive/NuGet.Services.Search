using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.Indexing
{
    [Flags]
    public enum IndexComponents
    {
        Unspecified = 0x0,  // 00
        Data = 0x1,         // 01
        Typeahead = 0x2,    // 10
        All = 0x3           // 11
    }
}
