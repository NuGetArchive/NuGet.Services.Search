using Lucene.Net.Analysis;
using Lucene.Net.Analysis.NGram;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Indexing
{
    public class IdentifierAutocompleteAnalyzer: Analyzer
    {
        public override TokenStream TokenStream(string fieldName, TextReader reader)
        {
            return new EdgeNGramTokenFilter(new LowerCaseFilter(new CamelCaseFilter(new DotTokenizer(reader))), Side.FRONT, 1, 8);
        }
    }
}
