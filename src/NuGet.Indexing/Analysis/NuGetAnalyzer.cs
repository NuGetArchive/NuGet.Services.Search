using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;

namespace NuGet.Indexing.Analysis
{
    public class NuGetAnalyzer : Analyzer
    {
        public override TokenStream TokenStream(string fieldName, TextReader reader)
        {
            throw new NotImplementedException();
        }
    }
}
