using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGet.Services.TestFramework;
using Xunit;

namespace NuGet.Services.Search.Test
{
    public class SupportedFrameworkTests : HttpTest
    {
        public SupportedFrameworkTests(RunContext context) : base(context) { }

        [Theory]
        [InlineData("EntityFramework", "net20", "EntityFramework", "[4.1.10715.0]")]
        [InlineData("EntityFramework", "net40", "EntityFramework", "6.1.1")]
        public async Task SupportedFrameworkFilterShouldWorkForKnownFrameworks(string query, string framework, string expectedId, string expectedVersionRange)
        {
            // Arrange
            var spec = VersionUtility.ParseVersionSpec(expectedVersionRange);

            // Act
            var result = await Context.GetJson<JObject>("/search/query?q=" + query + "&supportedFramework=" + framework + "&luceneQuery=false");
            
            // Assert
            var firstResult = (JObject)result.Value<JArray>("data")[0];
            Assert.Equal(expectedId, firstResult.Value<JObject>("PackageRegistration").Value<string>("Id"));

            SemanticVersion version = SemanticVersion.Parse(firstResult.Value<string>("NormalizedVersion"));
            Assert.True(spec.Satisfies(version), String.Format("Version {0} does not match expected range {1}", version, VersionUtility.PrettyPrint(spec)));
        }

        [Theory]
        [InlineData("EntityFramework", "wibblewobble20", "EntityFramework", "6.1.1")]
        public async Task SupportedFrameworkFilterShouldIgnoreUnknownFrameworks(string query, string framework, string expectedId, string expectedVersionRange)
        {
            // Arrange
            var spec = VersionUtility.ParseVersionSpec(expectedVersionRange);

            // Act
            var result = await Context.GetJson<JObject>("/search/query?q=" + query + "&supportedFramework=" + framework + "&luceneQuery=false");

            // Assert
            var firstResult = (JObject)result.Value<JArray>("data")[0];
            Assert.Equal(expectedId, firstResult.Value<JObject>("PackageRegistration").Value<string>("Id"));

            SemanticVersion version = SemanticVersion.Parse(firstResult.Value<string>("NormalizedVersion"));
            Assert.True(spec.Satisfies(version), String.Format("Version {0} does not match expected range {1}", version, VersionUtility.PrettyPrint(spec)));
        }
    }
}
