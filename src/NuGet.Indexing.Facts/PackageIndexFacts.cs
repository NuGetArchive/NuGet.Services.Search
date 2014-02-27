using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using NuGet.Indexing.Analysis;
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
                    writer.Commit(new CommitMetadata("Created")
                    {
                        DocumentCount = 42,
                        TimestampUtc = now
                    });
                }

                var index = new PackageIndex(dir);

                // Act
                index.LoadMetadata();

                // Assert
                Assert.NotNull(index.LatestCommit);
                Assert.Equal("Created", index.LatestCommit.Description);
                Assert.Equal(42, index.LatestCommit.DocumentCount);
                Assert.Equal(now, index.LatestCommit.TimestampUtc);
            }
        }
    }
}
