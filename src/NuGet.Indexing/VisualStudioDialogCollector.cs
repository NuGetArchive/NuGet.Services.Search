using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Newtonsoft.Json.Linq;
using NuGetGallery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Indexing
{
    public class VisualStudioDialogCollector: Collector
    {
        IndexReader _reader;
        int _docBase;
        Scorer _scorer;

        bool _includePrerelease;

        List<Tuple<string, SemanticVersion, float, int, Document>> _docs;

        public VisualStudioDialogCollector(bool includePrerelease)
        {
            _docs = new List<Tuple<string, SemanticVersion, float, int, Document>>();
            _includePrerelease = includePrerelease;
        }

        public override bool AcceptsDocsOutOfOrder
        {
            get { return true; }
        }

        public override void Collect(int doc)
        {
            Package  pacakge;
            Document document = _reader.Document(doc);
            float score = _scorer.Score();

            string id = document.GetField("Id").StringValue;
            SemanticVersion ver = new SemanticVersion(document.GetField("Version").StringValue);

            if (IsCompatible(doc) && (_includePrerelease || string.IsNullOrEmpty(ver.SpecialVersion)))
            {
                int index = _docs.FindIndex(x => x.Item1 == id);
                if (index < 0)
                {
                    _docs.Add(Tuple.Create(id, ver, _scorer.Score(), doc + _docBase, document));
                    return;
                }

                // TODO: handle pre-release
                if (_docs[index].Item2 < ver)
                {
                    _docs[index] = Tuple.Create(id, ver, _scorer.Score(), doc + _docBase, document);
                    return;
                }
            }
        }

        private bool IsCompatible(int doc)
        {
            return true;
        }

        public IEnumerable<ScoreDoc> PopulateResults()
        {
            return _docs.OrderBy(x => x.Item3).Select(x => new ScoreDoc(x.Item4, x.Item3));
        }

        public override void SetNextReader(Lucene.Net.Index.IndexReader reader, int docBase)
        {
            _reader = reader;
            _docBase = docBase;
        }

        public override void SetScorer(Lucene.Net.Search.Scorer scorer)
        {
            _scorer = scorer;
        }
    }
}
