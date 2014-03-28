using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.NGram;

namespace NuGet.Indexing
{
    public class TypeaheadAnalyzer : DescriptionAnalyzer
    {
        public override TokenStream TokenStream(string fieldName, System.IO.TextReader reader)
        {
            // Do all the DescriptionAnalyzer stuff, then build NGrams
            return new RemoveDuplicatesTokenFilter(
                new EdgeNGramTokenFilter(base.TokenStream(fieldName, reader), Side.FRONT, minGram: 2, maxGram: 10));
        }
    }
}
