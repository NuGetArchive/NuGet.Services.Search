using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;

namespace NuGet.Indexing.Analysis
{
    public class NuGetAnalyzer : PerFieldAnalyzerWrapper
    {
        public NuGetAnalyzer()
            : base(
                new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30), CreateFieldAnalyzers()) { }

        private static IEnumerable<KeyValuePair<string, Analyzer>> CreateFieldAnalyzers()
        {
            yield break;
        }
    }
}
