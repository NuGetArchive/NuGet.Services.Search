using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace NuGet.Indexing
{
    /// <summary>
    /// Represents a document and a parsed representation of the Facets contained within.
    /// </summary>
    public class FacetedDocument
    {
        private readonly ISet<string> _facets;
        
        public Document Doc { get; private set; }
        public IEnumerable<string> Facets { get { return _facets; } }
        public bool Dirty { get; private set; }

        public FacetedDocument(Document doc, bool dirty)
        {
            Doc = doc;
            Dirty = dirty;
            
            _facets = ParseFacets(doc);
        }

        public bool HasFacet(string facet)
        {
            return _facets.Contains(facet);
        }

        public void RemoveFacet(string facet)
        {
            if (_facets.Contains(facet))
            {
                _facets.Remove(facet);
                Dirty = true;
            }
        }

        public void AddFacets(IEnumerable<string> facets)
        {
            foreach (string facet in facets)
            {
                AddFacet(facet);
            }
        }

        public void AddFacet(string facet)
        {
            if (!_facets.Contains(facet))
            {
                _facets.Add(facet);
                Dirty = true;
            }
        }

        public void UpdateDocument()
        {
            if (Dirty)
            {
                Doc.RemoveFields("Facet");
                foreach (var facet in _facets)
                {
                    Doc.Add(new Field("Facet", facet, Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS, Field.TermVector.NO));
                }
            }
        }

        private ISet<string> ParseFacets(Document doc)
        {
 	        var fields = doc.GetFields("Facets");
            if (fields != null)
            {
                return new HashSet<string>(
                    fields.Select(f => f.StringValue),
                    StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                return new HashSet<string>();
            }
        }

        // Gets a query that returns exactly this document
        public Query GetQuery()
        {
            string id = Doc.GetField("Id").StringValue;
            string version = Doc.GetField("Version").StringValue;
            var qry = new BooleanQuery();
            qry.Add(new TermQuery(new Term("Id", id)), Occur.MUST);
            qry.Add(new TermQuery(new Term("Version", version)), Occur.MUST);
            return qry;
        }
    }
}
