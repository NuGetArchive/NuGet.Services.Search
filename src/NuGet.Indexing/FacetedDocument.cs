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
        private SemanticVersion _version;
        private string _id;
        private int? _key = null;
        
        public Document Doc { get; private set; }
        public IEnumerable<string> DocFacets { get { return _facets; } }
        public bool Dirty { get; private set; }
        public bool IsNew { get; private set; }
        public SemanticVersion Version
        {
            get
            {
                if (_version == null)
                {
                    _version = SemanticVersion.Parse(Doc.GetField("Version").StringValue);
                }
                return _version;
            }
        }
        public string Id
        {
            get
            {
                if (_id == null)
                {
                    _id = Doc.GetField("Id").StringValue;
                }
                return _id;
            }
        }

        public int Key
        {
            get
            {
                if (_key == null)
                {
                    _key = Int32.Parse(Doc.GetFieldable("Key").StringValue);
                }
                return _key.Value;
            }
        }

        public FacetedDocument(Document doc, bool isNew)
        {
            Doc = doc;
            Dirty = IsNew = isNew;
            
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
                _version = null;
                _id = null;
                Doc.RemoveFields(Facets.FieldName);
                foreach (var facet in _facets)
                {
                    Doc.Add(new Field(Facets.FieldName, facet, Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS, Field.TermVector.NO));
                }
            }
        }

        private ISet<string> ParseFacets(Document doc)
        {
            var fields = doc.GetFields(Facets.FieldName);
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
            var keyField = Doc.GetFieldable("Key");
            if (keyField != null)
            {
                int val = Int32.Parse(keyField.StringValue);
                return NumericRangeQuery.NewIntRange("Key", val, val, minInclusive: true, maxInclusive: true);
            }
            else
            {
                string id = Doc.GetField("Id").StringValue.ToLowerInvariant();
                string version = Doc.GetField("Version").StringValue;
                var qry = new BooleanQuery();
                qry.Add(new TermQuery(new Term("Id", id)), Occur.MUST);
                qry.Add(new TermQuery(new Term("Version", version)), Occur.MUST);
                return qry;
            }
        }
    }
}
