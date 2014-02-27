using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Documents;
using Lucene.Net.Index;

namespace NuGet.Indexing
{
    public static class LuceneExtensions
    {
        public static void Commit(this IndexWriter self, CommitMetadata metadata)
        {
            self.Commit(metadata.ToDictionary());
        }

        public static void Add(this Document self, string name, string value, Field.Store store, Field.Index index, Field.TermVector termVector)
        {
            Add(self, name, value, store, index, termVector, boost: 1.0f);
        }

        public static void Add(this Document self, string name, string value, Field.Store store, Field.Index index, Field.TermVector termVector, float boost)
        {
            self.Add(new Field(name, value, store, index, termVector)
            {
                Boost = boost
            });
        }

        public static void Add(this Document self, string name, string value, Field.Store store, Field.Index index, Field.TermVector termVector, BoostFactors boosts)
        {
            Add(self, name, value, store, index, termVector, boosts[name]);
        }
    }
}
