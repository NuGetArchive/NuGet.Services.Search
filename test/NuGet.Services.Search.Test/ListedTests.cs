using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGet.Services.TestFramework;
using Xunit;

namespace NuGet.Services.Search.Test
{
    public class ListedTests : HttpTest
    {
        public ListedTests(RunContext context) : base(context) { }

        [Theory]
        [InlineData("nugettest.TestListed", "nugettest.TestListed", "2.0.0")]
        public async Task ListedFilterShouldApplyToLatestSelection(string query, string expectedId, string expectedVersionRange)
        {
            // Arrange
            var spec = VersionUtility.ParseVersionSpec(expectedVersionRange);

            // Act
            var result = await Context.GetJson<JObject>("/search/query?q=" + query + "&luceneQuery=false");

            // Assert
            var firstResult = (JObject)result.Value<JArray>("data")[0];
            Assert.Equal(expectedId, firstResult.Value<JObject>("PackageRegistration").Value<string>("Id"));

            SemanticVersion version = SemanticVersion.Parse(firstResult.Value<string>("NormalizedVersion"));
            Assert.True(spec.Satisfies(version), String.Format("Version {0} does not match expected range {1}", version, VersionUtility.PrettyPrint(spec)));
        }
    }
}
