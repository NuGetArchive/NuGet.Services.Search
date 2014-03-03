using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PowerArgs;

namespace IndexMaintainance
{
    public class QueryArgs : IndexArgs
    {
        [ArgShortcut("-q")]
        [ArgDescription("The query to execute")]
        public string Query { get; set; }

        [ArgShortcut("-l")]
        [ArgDescription("The query is a raw lucene query and should not be pre-parsed")]
        public bool IsLuceneQuery { get; set; }

        [ArgShortcut("-c")]
        [ArgDescription("Only fetch the count of matching rows instead of executing the full query")]
        public bool CountOnly { get; set; }

        [ArgShortcut("-prj")]
        [ArgDescription("Filter relevance data by project type")]
        public string ProjectType { get; set; }

        [ArgShortcut("-pre")]
        [ArgDescription("Include Pre-release packages")]
        public bool IncludePrerelease { get; set; }

        [ArgShortcut("-f")]
        [ArgDescription("Filter by curated feed")]
        public string Feed { get; set; }

        [ArgShortcut("-s")]
        [ArgDescription("Sort by a specific field")]
        public string SortBy { get; set; }
        
        [ArgShortcut("-sk")]
        [ArgDescription("Skips the specified number of records")]
        public int Skip { get; set; }

        [ArgShortcut("-ta")]
        [DefaultValue(10)]
        [ArgDescription("Takes the specified number of records")]
        public int Take { get; set; }

        [ArgShortcut("-explain")]
        [ArgDescription("Includes explanation data in the results")]
        public bool IncludeExplanation { get; set; }

        [ArgShortcut("-if")]
        [ArgDescription("Ignores filters")]
        public bool IgnoreFilter { get; set; }
    }
}
