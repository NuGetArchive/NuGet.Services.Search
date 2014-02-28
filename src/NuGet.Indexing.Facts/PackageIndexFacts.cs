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
                    writer.Commit(new CommitMetadata("Created", 123));
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
                    writer.Commit(new CommitMetadata("Created", 123, now));
                }

                var index = new PackageIndex(dir);

                // Act
                index.LoadMetadata();

                // Assert
                Assert.NotNull(index.LatestCommit);
                Assert.Equal("Created", index.LatestCommit.Message);
                Assert.Equal(now, index.LatestCommit.TimestampUtc);
                Assert.Equal(123, index.LatestCommit.HighestPackageKey);
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
                    new PackageDocument() { Key = 0, Id = "Package.One", Version = "1.0.0", Title = "The first package" },
                    new PackageDocument() { Key = 1, Id = "Package.Two", Version = "2.0.0", Title = "The second package" },
                    new PackageDocument() { Key = 2, Id = "Package.Three", Version = "3.0.0", Title = "The third package" }
                };
                index.AddNewDocuments(packages, "Test Commit");

                // Assert
                var expectedIds = packages.Select(p => p.Id);
                AssertIdsPresent(expectedIds, 0, dir);
            }

            [Fact]
            public void GivenMorePackageDocumentsThanMaxPerCommit_ItAddsDocumentsToTheIndexInBatches()
            {
                // Arrange
                var dir = new RAMDirectory();
                var index = new PackageIndex(dir, new PackageIndexParameters()
                {
                    MaxDocumentsPerCommit = 2,
                    NeverDeleteCommits = true
                });

                // Act
                var packages = new[] { 
                    new PackageDocument() { Key = 0, Id = "Package.One", Version = "1.0.0", Title = "The first package" },
                    new PackageDocument() { Key = 1, Id = "Package.Two", Version = "2.0.0", Title = "The second package" },
                    new PackageDocument() { Key = 2, Id = "Package.Three", Version = "3.0.0", Title = "The third package" },
                    new PackageDocument() { Key = 3, Id = "Package.Four", Version = "4.0.0", Title = "The fourth package" },
                    new PackageDocument() { Key = 4, Id = "Package.Five", Version = "5.0.0", Title = "The fifth package" }
                };
                index.AddNewDocuments(packages, "Test Commit");

                // Assert
                var expectedIds = new HashSet<string>(packages.Select(p => p.Id), StringComparer.OrdinalIgnoreCase);
                var commits = IndexReader.ListCommits(dir).OrderBy(c => c.Timestamp).Skip(1).ToList();
                Assert.Equal(3, commits.Count);

                AssertIdsPresent(new[] { "Package.One", "Package.Two" }, 0, commits[0]);
                AssertIdsPresent(new[] { "Package.Three", "Package.Four" }, 2, commits[1]);
                AssertIdsPresent(new[] { "Package.Five" }, 4, commits[2]);
            }

            private static void AssertIdsPresent(IEnumerable<string> ids, int start, IndexCommit commit)
            {
                using (var reader = IndexReader.Open(commit, readOnly: true))
                {
                    AssertIdsPresent(ids, start, reader);
                }
            }

            private static void AssertIdsPresent(IEnumerable<string> ids, int start, Directory dir)
            {
                using (var reader = IndexReader.Open(dir, readOnly: true))
                {
                    AssertIdsPresent(ids, start, reader);
                }
            }

            private static void AssertIdsPresent(IEnumerable<string> ids, int start, IndexReader reader)
            {
                var expectedIds = new HashSet<string>(ids, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(expectedIds.Count + start, reader.NumDocs());

                var actualIds = Enumerable
                    .Range(start, expectedIds.Count)
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
