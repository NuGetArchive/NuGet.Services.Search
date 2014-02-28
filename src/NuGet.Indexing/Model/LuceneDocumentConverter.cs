using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Documents;
using Newtonsoft.Json;

namespace NuGet.Indexing.Model
{
    /// <summary>
    /// Handles storing and retrieving data in Lucene Documents
    /// </summary>
    public static class LuceneDocumentConverter
    {
        /// <summary>
        /// Convert the strongly-typed PackageDocument object into a Lucene Document for storage
        /// </summary>
        /// <param name="package">The document to store</param>
        /// <param name="boosts">The boosts to apply to the fields</param>
        /// <returns>A Lucene document</returns>
        public static Document ToLuceneDocument(PackageDocument package, BoostFactors boosts)
        {
            var doc = new Document();

            // Simple fields
            doc.Add("Id", package.Id, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS, boosts);
            doc.Add("TokenizedId", package.Id, Field.Store.NO, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS, boosts);
            doc.Add("ShingledId", package.Id, Field.Store.NO, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS, boosts);
            doc.Add("Title", package.Title ?? String.Empty, Field.Store.NO, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS, boosts);
            doc.Add("Version", package.Version, Field.Store.NO, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS, boosts);
            doc.Add("Tags", package.Tags ?? String.Empty, Field.Store.NO, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS, boosts);
            doc.Add("Description", package.Description ?? String.Empty, Field.Store.NO, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS, boosts);
            doc.Add("Authors", package.Authors ?? String.Empty, Field.Store.NO, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS, boosts);

            // Facets
            doc.Add("IsLatest", (package.IsLatest ? "1" : "0"), Field.Store.NO, Field.Index.NOT_ANALYZED, Field.TermVector.NO, boosts);
            doc.Add("IsLatestStable", (package.IsLatestStable ? "1" : "0"), Field.Store.NO, Field.Index.NOT_ANALYZED, Field.TermVector.NO, boosts);
            doc.Add("IsListed", (package.IsListed ? "1" : "0"), Field.Store.NO, Field.Index.NOT_ANALYZED, Field.TermVector.NO, boosts);

            // Sortable fields
            doc.Add(CreateDateField("Published", package.Published, boosts));
            doc.Add(CreateDateField("LastUpdated", package.LastUpdated, boosts));
            if (package.LastEdited != null)
            {
                doc.Add(CreateDateField("LastEdited", package.LastEdited.Value, boosts));
            }

            // Owners
            foreach (var owner in package.Owners)
            {
                doc.Add("Owners", owner, Field.Store.NO, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS, boosts);
            }

            // Feeds
            foreach (var feed in package.Feeds)
            {
                doc.Add("Feeds", feed, Field.Store.YES, Field.Index.NOT_ANALYZED, Field.TermVector.NO, boosts);
            }

            // Package Frameworks
            foreach (var fx in package.SupportedFrameworks)
            {
                doc.Add("SupportedFrameworks", fx, Field.Store.NO, Field.Index.NOT_ANALYZED, Field.TermVector.NO, boosts);
            }

            // Identity values
            doc.Add(new NumericField("Key", Field.Store.YES, index: true) { Boost = boosts["Key"] }.SetIntValue(package.Key));
            doc.Add(new NumericField("Checksum", Field.Store.YES, index: true) { Boost = boosts["Checksum"] }.SetIntValue(package.Checksum));

            // The actual payload goes along for the ride
            var payload = JsonConvert.SerializeObject(package.Payload, Formatting.None);
            doc.Add("Payload", payload, Field.Store.YES, Field.Index.NO, Field.TermVector.NO);

            return doc;
        }

        /// <summary>
        /// Load the payload (i.e. the part returned to users) from a Lucene Document
        /// </summary>
        /// <param name="doc">The Lucene document to load from</param>
        /// <returns>The package data stored in the payload</returns>
        public static PackageData LoadPayload(Document doc)
        {
            string json = doc.Get("Payload");
            if (String.IsNullOrEmpty(json))
            {
                return null;
            }
            return JsonConvert.DeserializeObject<PackageData>(json);
        }

        private static IFieldable CreateDateField(string name, DateTime value, BoostFactors boosts)
        {
            return new NumericField(name, Field.Store.YES, index: true)
            {
                Boost = boosts[name]
            }.SetLongValue(value.Date.Ticks);
        }
    }
}
