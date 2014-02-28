using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using NuGet.Indexing.Analysis;
using NuGet.Indexing.Model;
using Xunit;

namespace NuGet.Indexing.Facts
{
    public class PackageIndexFacts
    {
        public class TheLoadMetadataMethod
        {
            [Fact]
            public void SetsInitializedAndExistsToFalseIfIndexDoesNotExist()
            {
                // Arrange
                var dir = new RAMDirectory();
                var index = new PackageIndex(dir);

                // Act
                index.LoadMetadata();

                // Assert
                Assert.False(index.Exists);
            }

            [Fact]
            public void SetsExistsToTrueIfIndexExists()
            {
                // Arrange
                var dir = new RAMDirectory();
                using (var writer = new IndexWriter(dir, new NuGetAnalyzer(), IndexWriter.MaxFieldLength.UNLIMITED))
                {
                    writer.Commit(new CommitMetadata("Created"));
                }

                var index = new PackageIndex(dir);

                // Act
                index.LoadMetadata();

                // Assert
                Assert.True(index.Exists);
            }

            [Fact]
            public void LoadsMostRecentCommitMetadataIfAny()
            {
                // Arrange
                var dir = new RAMDirectory();
                var now = DateTime.UtcNow;
                using (var writer = new IndexWriter(dir, new NuGetAnalyzer(), IndexWriter.MaxFieldLength.UNLIMITED))
                {
                    writer.AddDocument(new Document());
                    writer.Commit(new CommitMetadata("Created", now));
                }

                var index = new PackageIndex(dir);

                // Act
                index.LoadMetadata();

                // Assert
                Assert.NotNull(index.LatestCommit);
                Assert.Equal("Created", index.LatestCommit.Message);
                Assert.Equal(now, index.LatestCommit.TimestampUtc);
            }
        }

        public class TheCommitDocumentsMethod
        {
            [Fact]
            public void GivenPackageDocuments_ItAddsDocumentsToTheIndex()
            {
                // Arrange
                var dir = new RAMDirectory();
                var index = new PackageIndex(dir);

                // Act
                var packages = new[] { 
                    new PackageDocument() { Id = "Package.One", Version = "1.0.0", Title = "The first package" },
                    new PackageDocument() { Id = "Package.Two", Version = "2.0.0", Title = "The second package" },
                    new PackageDocument() { Id = "Package.Three", Version = "3.0.0", Title = "The third package" }
                };
                index.CommitDocuments(packages, "Test Commit");

                // Assert
                var expectedIds = new HashSet<string>(packages.Select(p => p.Id), StringComparer.OrdinalIgnoreCase);
                using (var reader = IndexReader.Open(dir, readOnly: true))
                {
                    Assert.Equal(3, reader.NumDocs());

                    var actualIds = Enumerable
                        .Range(0, reader.NumDocs())
                        .Select(i => reader.Document(i))
                        .Select(d => d.Get("Id"));

                    foreach (var id in actualIds)
                    {
                        Assert.True(expectedIds.Contains(id));
                        expectedIds.Remove(id);
                    }
                    Assert.Empty(expectedIds);
                }
            }
        }
    }
}
