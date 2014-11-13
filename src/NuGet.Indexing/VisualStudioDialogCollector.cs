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
        string _supportedFramework;
        IDictionary<string, ISet<string>> _frameworkCompatibility;

        IDictionary<string, Tuple<string, SemanticVersion, float, int, Document>> _docs;

        public VisualStudioDialogCollector(bool includePrerelease, string supportedFramework, IDictionary<string,ISet<string>> frameworkCompatibility)
        {
            _docs = new Dictionary<string,Tuple<string, SemanticVersion, float, int, Document>>();
            _includePrerelease = includePrerelease;
            _supportedFramework = supportedFramework;
            _frameworkCompatibility = frameworkCompatibility;
        }

        public override bool AcceptsDocsOutOfOrder
        {
            get { return true; }
        }

        public override void Collect(int doc)
        {
            Document document = _reader.Document(doc);
            float score = _scorer.Score();

            string id = document.GetField("Id").StringValue;
            string lowerId = id.ToLowerInvariant();
            SemanticVersion ver = new SemanticVersion(document.GetField("Version").StringValue);

            if (IsCompatible(doc) && (_includePrerelease || string.IsNullOrEmpty(ver.SpecialVersion)))
            {
                Tuple<string, SemanticVersion, float, int, Document> item;
                if (!_docs.TryGetValue(lowerId, out item) || item.Item2 < ver)
                {
                    _docs[lowerId] = Tuple.Create(id, ver, _scorer.Score(), doc + _docBase, document);
                }
            }
        }

        private bool IsCompatible(int doc)
        {
            if (_supportedFramework == null || _supportedFramework == "any") return true;

            string supportedFrameworkName = _supportedFramework; //VersionUtility.ParseFrameworkName(_supportedFramework).ToString();

            Document document = _reader.Document(doc);
            Field[] frameworks = document.GetFields("TargetFramework");

            if (frameworks.Length == 0) return true;

            ISet<string> compatibleFrameworks;
            if (!_frameworkCompatibility.TryGetValue(_supportedFramework, out compatibleFrameworks)) return true;
            foreach (Field frameworkField in frameworks)
            {
                string framework = frameworkField.StringValue;
                if (framework == "any" || framework == "agnostic") return true;

                if (compatibleFrameworks.Contains(framework)) return true;
            }

            return false;
        }

        public IEnumerable<ScoreDoc> PopulateResults()
        {
            return _docs.Select(x => x.Value).OrderByDescending(x => x.Item3).Select(x => new ScoreDoc(x.Item4, x.Item3));
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
