using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;

namespace NuGet.Indexing
{
    public class RemoveDuplicatesTokenFilter : TokenFilter
    {
        private ITermAttribute _termAttribute;
        private HashSet<string> _seenTerms = new HashSet<string>(StringComparer.Ordinal);

        public RemoveDuplicatesTokenFilter(TokenStream inner) : base(inner)
        {
            _termAttribute = AddAttribute<ITermAttribute>();
        }

        public override bool IncrementToken()
        {
            // Reset attributes
            ClearAttributes();

            // Skip terms where we've already seen them
            string term = null;
            do
            {
                // Get the input token
                if (!input.IncrementToken())
                {
                    return false; // No more tokens anyway!
                }

                // Get the term value
                term = input.GetAttribute<ITermAttribute>().Term;
            } while (_seenTerms.Contains(term));

            // We haven't seen it before, return it!
            _termAttribute.SetTermBuffer(term);
            _seenTerms.Add(term);
            return true;
        }
    }
}
