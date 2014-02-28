using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Documents;
using Newtonsoft.Json;
using NuGet.Indexing.Model;
using Xunit;

namespace NuGet.Indexing.Facts.Model
{
    public class LuceneDocumentConverterFacts
    {
        public class TheFromDocumentMethod
        {
            [Fact]
            public void GivenValidDocument_ItCanLoadPayload()
            {
                // Arrange
                var doc = new Document();
                doc.Add("Payload", "{Id:'DataId'}", Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS);

                // Act
                var data = LuceneDocumentConverter.LoadData(doc);

                // Assert
                Assert.NotNull(data);
                Assert.Equal("DataId", data.Id);
            }
        }

        public class TheToDocumentMethod
        {
            public static readonly PackageDocument TestPackage = new PackageDocument()
            {
                Id = "Id",
                Title = "Title",
                Version = "1.0.0",
                Tags = "tag1 tag2",
                Description = "Description",
                Authors = "author1 author2",
                IsLatest = true,
                IsLatestStable = false,
                IsListed = true,
                Published = new DateTime(2014, 01, 01, 00, 42, 00, DateTimeKind.Utc),
                LastUpdated = new DateTime(2014, 02, 02, 00, 24, 00, DateTimeKind.Utc),
                LastEdited = null,
                Key = 42,
                Checksum = 24,
                Payload = new PackageData()
                {
                    Id = "DataId"
                }
            };

            public static readonly PackageDocument TestEditedPackage = new PackageDocument()
            {
                Id = "Id",
                Title = "Title",
                Version = "1.0.0",
                Tags = "tag1 tag2",
                Description = "Description",
                Authors = "author1 author2",
                IsLatest = true,
                IsLatestStable = false,
                IsListed = true,
                Published = new DateTime(2014, 01, 01, 00, 42, 00, DateTimeKind.Utc),
                LastUpdated = new DateTime(2014, 02, 02, 00, 24, 00, DateTimeKind.Utc),
                LastEdited = new DateTime(2014, 03, 03, 00, 00, 00, DateTimeKind.Utc),
                Key = 42,
                Checksum = 24,
                Payload = new PackageData()
                {
                    Id = "DataId"
                }
            };

            [Fact]
            public void GivenSimplePackageWithoutLastEditedDate_ItReturnsDocumentWithoutLastEditedField()
            {
                // Act
                var doc = LuceneDocumentConverter.ToLuceneDocument(TestPackage, new BoostFactors());

                // Assert
                Assert.Null(doc.fields_ForNUnit.FirstOrDefault(f => String.Equals(f.Name, "LastEdited")));
            }

            [Fact]
            public void GivenPackageWithLastEditedDate_ItHasLastEditedField()
            {
                // Act
                var doc = LuceneDocumentConverter.ToLuceneDocument(TestEditedPackage, new BoostFactors());

                // Assert
                Assert.NotNull(doc.fields_ForNUnit.FirstOrDefault(f => String.Equals(f.Name, "LastEdited")));
            }

            [Fact]
            public void GivenPackage_ItStoresDataAsJson()
            {
                // Act
                var doc = LuceneDocumentConverter.ToLuceneDocument(TestPackage, new BoostFactors());

                // Assert
                var field = doc.fields_ForNUnit.FirstOrDefault(f => String.Equals(f.Name, "Payload"));
                Assert.NotNull(field);
                var data = JsonConvert.DeserializeObject<PackageData>(field.StringValue);
                Assert.Equal("DataId", data.Id);
            }

            [Fact]
            public void GivenOwners_ItStoresEachOwnerInField()
            {
                var package = new PackageDocument() {
                    Id = "Id",
                    Version = "1.0.0"
                };
                package.Owners.Add("a");
                package.Owners.Add("b");
                package.Owners.Add("c");

                // Act
                var doc = LuceneDocumentConverter.ToLuceneDocument(package, new BoostFactors());

                // Assert
                var docOwners = doc.fields_ForNUnit.Where(f => String.Equals(f.Name, "Owners")).Select(f => f.StringValue).ToList();
                Assert.Empty(package.Owners.Except(docOwners));
                Assert.Empty(docOwners.Except(package.Owners));
            }

            [Fact]
            public void GivenFeeds_ItStoresEachFeedInField()
            {
                var package = new PackageDocument()
                {
                    Id = "Id",
                    Version = "1.0.0"
                };
                package.Feeds.Add("a");
                package.Feeds.Add("b");
                package.Feeds.Add("c");

                // Act
                var doc = LuceneDocumentConverter.ToLuceneDocument(package, new BoostFactors());

                // Assert
                var docFeeds = doc.fields_ForNUnit.Where(f => String.Equals(f.Name, "Feeds")).Select(f => f.StringValue).ToList();
                Assert.Empty(package.Feeds.Except(docFeeds));
                Assert.Empty(docFeeds.Except(package.Feeds));
            }

            [Fact]
            public void GivenSupportedFrameworks_ItStoresEachSupportedFrameworkInField()
            {
                var package = new PackageDocument()
                {
                    Id = "Id",
                    Version = "1.0.0"
                };
                package.SupportedFrameworks.Add("a");
                package.SupportedFrameworks.Add("b");
                package.SupportedFrameworks.Add("c");

                // Act
                var doc = LuceneDocumentConverter.ToLuceneDocument(package, new BoostFactors());

                // Assert
                var docSupportedFrameworks = doc.fields_ForNUnit.Where(f => String.Equals(f.Name, "SupportedFrameworks")).Select(f => f.StringValue).ToList();
                Assert.Empty(package.SupportedFrameworks.Except(docSupportedFrameworks));
                Assert.Empty(docSupportedFrameworks.Except(package.SupportedFrameworks));
            }

            [Fact]
            public void GivenPackage_StoresIdentityValuesAsNumericFields()
            {
                // Arrange
                var package = new PackageDocument()
                {
                    Id = "Id",
                    Version = "1.0.0",
                    Key = 42,
                    Checksum = 24
                };

                // Act
                var doc = LuceneDocumentConverter.ToLuceneDocument(package, new BoostFactors());

                // Assert
                var keyField = doc.fields_ForNUnit.OfType<NumericField>().FirstOrDefault(f => String.Equals(f.Name, "Key"));
                var chkField = doc.fields_ForNUnit.OfType<NumericField>().FirstOrDefault(f => String.Equals(f.Name, "Checksum"));
                Assert.NotNull(keyField);
                Assert.NotNull(chkField);
                Assert.Equal(42, keyField.NumericValue);
                Assert.Equal(24, chkField.NumericValue);
            }

            [Fact]
            public void GivenBoosts_ItCanApplyThemToAllFields()
            {
                // Serialize once to get the list of fields
                var doc = LuceneDocumentConverter.ToLuceneDocument(TestEditedPackage, new BoostFactors());

                // Generate some random boost factors

                var fieldNames = new HashSet<string>(doc.fields_ForNUnit.Where(f => f.IsIndexed).Select(f => f.Name).Distinct());
                var boosts = new BoostFactors();
                var r = new Random();
                foreach (var field in fieldNames)
                {
                    boosts[field] = (float)(r.NextDouble() * 10);
                }
                
                // Serialize the doc again with the boosts
                doc = LuceneDocumentConverter.ToLuceneDocument(TestEditedPackage, boosts);

                // Check the boosts
                string[] failedBoosts = fieldNames
                    .SelectMany(name => doc.fields_ForNUnit.Where(f => String.Equals(f.Name, name)))
                    .Where(f => Math.Abs(f.Boost - boosts[f.Name]) >= 0.1)
                    .Select(f => f.Name)
                    .ToArray();
                Assert.Equal(new string[0], failedBoosts);
            }
        }
    }
}
