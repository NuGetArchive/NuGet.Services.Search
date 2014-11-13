using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using NuGet;
using System;
using System.Collections.Generic;

namespace NuGet.Indexing
{
    public class FrameworkCompatibilityFilter : Filter
    {
        ISet<string> _compatibleFrameworks;
        bool _includePrerelease;
        IndexReader _reader;
        IDictionary<string, OpenBitSet> _bitSetLookup;

        public FrameworkCompatibilityFilter(ISet<string> compatibleFrameworks, bool includePrerelease)
        {
            _compatibleFrameworks = compatibleFrameworks;
            _includePrerelease = includePrerelease;
            _bitSetLookup = null;
        }

        public void SetIndexReader(IndexReader reader)
        {
            _reader = reader;
        }

        public override DocIdSet GetDocIdSet(IndexReader segmentReader)
        {
            lock (this)
            {
                //TODO: possibly the correct behavior here is to refresh the filters in the SearcherManager.Warm override
                if (_bitSetLookup == null)
                {
                    _bitSetLookup = CreateBitSetLookup(_reader, _compatibleFrameworks, _includePrerelease);
                }
            }

            string segmentName = ((SegmentReader)segmentReader).SegmentName;

            return _bitSetLookup[segmentName];
        }

        static IDictionary<string, OpenBitSet> CreateBitSetLookup(IndexReader reader, ISet<string> compatibleFrameworks, bool includePrerelease)
        {
            IDictionary<string, Tuple<SemanticVersion, string, int>> matchingDocs = new Dictionary<string, Tuple<SemanticVersion, string, int>>();
            foreach (SegmentReader segmentReader in reader.GetSequentialSubReaders())
            {
                UpdateMatchingDocs(matchingDocs, segmentReader, compatibleFrameworks, includePrerelease);
            }

            IDictionary<string, OpenBitSet> result = new Dictionary<string, OpenBitSet>();
            foreach (SegmentReader segmentReader in reader.GetSequentialSubReaders())
            {
                result.Add(segmentReader.SegmentName, new OpenBitSet());
            }

            foreach (Tuple<SemanticVersion, string, int> matchingDoc in matchingDocs.Values)
            {
                result[matchingDoc.Item2].Set(matchingDoc.Item3);
            }
            
            return result;
        }

        static void UpdateMatchingDocs(IDictionary<string, Tuple<SemanticVersion, string, int>> matchingDocs, SegmentReader reader, ISet<string> compatibleFrameworks, bool includePrerelease)
        {
            for (int doc = 0; doc < reader.MaxDoc; doc++)
            {
                if (reader.IsDeleted(doc))
                {
                    continue;
                }

                Document document = reader.Document(doc);

                string id = document.GetField("Id").StringValue;
                SemanticVersion version = new SemanticVersion(document.GetField("Version").StringValue);

                Field[] frameworks = document.GetFields("TargetFramework");

                bool isCompatible;

                if (compatibleFrameworks == null)
                {
                    isCompatible = true;
                }
                else
                {
                    isCompatible = false;

                    foreach (Field frameworkField in frameworks)
                    {
                        string framework = frameworkField.StringValue;
                        if (framework == "any" || framework == "agnostic" || compatibleFrameworks.Contains(framework))
                        {
                            isCompatible = true;
                        }
                    }
                }

                if (isCompatible && (includePrerelease || string.IsNullOrEmpty(version.SpecialVersion)))
                {
                    if (!matchingDocs.ContainsKey(id) || matchingDocs[id].Item1 < version)
                    {
                        matchingDocs[id] = Tuple.Create(version, reader.SegmentName, doc);
                    }
                }
            }
        }
    }
}
