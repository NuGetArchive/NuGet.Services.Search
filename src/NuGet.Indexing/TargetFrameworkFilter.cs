using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace NuGet.Indexing
{
    public class TargetFrameworkFilter : Filter
    {
        private FrameworkName _targetFramework;
        private NetPortableProfileTable _portableProfileTable;
        private Filter _innerFilter;

        public TargetFrameworkFilter(FrameworkName targetFramework, NetPortableProfileTable portableProfileTable, Filter innerFilter)
        {
            _targetFramework = targetFramework;
            _portableProfileTable = portableProfileTable;
            _innerFilter = innerFilter;
        }

        public override DocIdSet GetDocIdSet(IndexReader reader)
        {
            // Get the starting document set
            var startSet = _innerFilter.GetDocIdSet(reader);
            if (startSet == null)
            {
                // Null indicates that nothing matched the inner filter, so return null
                return null;
            }

            // Filter documents by framework
            return new FrameworkFilteredDocIdSet(_targetFramework, _portableProfileTable, reader, startSet);
        }
        private class FrameworkFilteredDocIdSet : FilteredDocIdSet
        {
            private FrameworkName _targetFramework;
            private NetPortableProfileTable _portableProfileTable;
            private IndexReader _reader;

            public FrameworkFilteredDocIdSet(FrameworkName targetFramework, NetPortableProfileTable portableProfileTable, IndexReader reader, DocIdSet startSet)
                : base(startSet)
            {
                _targetFramework = targetFramework;
                _portableProfileTable = portableProfileTable;
                _reader = reader;
            }

            public override bool Match(int docid)
            {
                var doc = _reader.Document(docid);
                var supportedFrameworks = doc.GetFields("SupportedFramework").Select(f => new FrameworkName(f.StringValue)).ToList();
                bool compatible = VersionUtility.IsCompatible(_targetFramework, supportedFrameworks, _portableProfileTable);
                return compatible;
            }
        }
    }
}