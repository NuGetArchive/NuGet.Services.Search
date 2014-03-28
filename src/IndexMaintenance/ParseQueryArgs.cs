using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PowerArgs;

namespace IndexMaintainance
{
    public class ParseQueryArgs
    {
        [ArgRequired]
        [ArgPosition(0)]
        [ArgShortcut("q")]
        public string Query { get; set; }

        [ArgShortcut("l")]
        [ArgDescription("Set this flag to only do the basic parsing for raw lucene queries (basically just field name aliases)")]
        public bool IsLuceneQuery { get; set; }
    }
}
