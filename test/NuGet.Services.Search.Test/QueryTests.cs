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
    public class QueryTests : HttpTest
    {
        public QueryTests(RunContext context) : base(context) { }

        [Fact]
        public async Task GivenNoQueryString_ItReturnsTotalHitCount()
        {
            var result = await Context.GetJson<JObject>("/search/query");

            Assert.True(result.Value<int>("totalHits") >= result.Value<JArray>("data").Count);
        }

        [Fact]
        public async Task GivenNoQueryString_ItReturnsTimeTakenInMilliseconds()
        {
            var result = await Context.GetJson<JObject>("/search/query");

            // This is NOT a perf test, so just using an arbitrary number
            Assert.True(result.Value<int>("timeTakenInMs") < 1000);
        }

        [Fact]
        public async Task GivenNoQueryString_ItReturnsIndexTimestamp()
        {
            var result = await Context.GetJson<JObject>("/search/query");

            // Again, this is NOT a liveness test, so just verify we get a date
            Assert.True(result.Value<DateTime>("indexTimestamp") <= DateTime.UtcNow);
        }

        [Fact]
        public async Task GivenNoQueryString_ItReturnsExpectedDataFields()
        {
            var result = await Context.GetJson<JObject>("/search/query");

            // Not about accuracy, just about the presence of expected fields
            var firstResult = (JObject)result.Value<JArray>("data")[0];
            Assert.NotNull(firstResult.Property("Key"));
            Assert.NotNull(firstResult.Property("PackageRegistrationKey"));
            Assert.NotNull(firstResult.Property("PackageRegistration"));
            Assert.NotNull(firstResult.Value<JObject>("PackageRegistration").Property("Key"));
            Assert.NotNull(firstResult.Value<JObject>("PackageRegistration").Property("Id"));
            Assert.NotNull(firstResult.Value<JObject>("PackageRegistration").Property("DownloadCount"));
            Assert.NotNull(firstResult.Property("Version"));
            Assert.NotNull(firstResult.Property("NormalizedVersion"));
            Assert.NotNull(firstResult.Property("Title"));
            Assert.NotNull(firstResult.Property("Description"));
            Assert.NotNull(firstResult.Property("IsLatest"));
            Assert.NotNull(firstResult.Property("IsLatestStable"));
            Assert.NotNull(firstResult.Property("Listed"));
            Assert.NotNull(firstResult.Property("DownloadCount"));
            Assert.NotNull(firstResult.Property("Hash"));
            Assert.NotNull(firstResult.Property("PackageFileSize"));
            Assert.NotNull(firstResult.Property("Installs"));
            Assert.NotNull(firstResult.Property("Updates"));
        }

        [Fact]
        public async Task GivenAQuery_ItReturnsData()
        {
            var result = await Context.GetJson<JObject>("/search/query?q=EntityFramework");

            Assert.True(result.Value<int>("totalHits") > 0);
        }

        [Fact]
        public async Task GivenASortOrderOfLastEdited_ItReturnsDataOrderedByLastEdited()
        {
            var result = await Context.GetJson<JObject>("/search/query?sortBy=lastEdited");

            Assert.True(result.Value<int>("totalHits") > 0);
            var dates = result.Value<JArray>("data").Values<JObject>().Select(jobj => 
                jobj.Value<DateTime?>("LastEdited") ??
                jobj.Value<DateTime?>("Published"));

            DateTime? previous = null;
            foreach (var date in dates)
            {
                if (previous.HasValue && date.HasValue)
                {
                    Assert.True(date.Value.Date <= previous.Value.Date);
                }
                previous = date;
            }
        }

        [Fact]
        public async Task GivenASortOrderOfPublished_ItReturnsDataOrderedByPublished()
        {
            var result = await Context.GetJson<JObject>("/search/query?sortBy=published");

            Assert.True(result.Value<int>("totalHits") > 0);
            var dates = result.Value<JArray>("data").Values<JObject>().Select(jobj =>
                jobj.Value<DateTime?>("Published"));

            DateTime? previous = null;
            foreach (var date in dates)
            {
                if (previous.HasValue && date.HasValue)
                {
                    Assert.True(date.Value.Date <= previous.Value.Date);
                }
                previous = date;
            }
        }

        [Fact]
        public async Task GivenASortOrderOfTitleAsc_ItReturnsDataOrderedByTitleAscending()
        {
            var result = await Context.GetJson<JObject>("/search/query?sortBy=title-asc");

            Assert.True(result.Value<int>("totalHits") > 0);
            var titles = result.Value<JArray>("data").Values<JObject>().Select(jobj =>
                jobj.Value<string>("Title"));

            string previous = null;
            foreach (var title in titles)
            {
                if (previous != null)
                {
                    Assert.True(String.Compare(title, previous, StringComparison.OrdinalIgnoreCase) >= 0);
                }
                previous = title;
            }
        }

        [Fact]
        public async Task GivenASortOrderOfTitleDesc_ItReturnsDataOrderedByTitleDescending()
        {
            var result = await Context.GetJson<JObject>("/search/query?sortBy=title-desc");

            Assert.True(result.Value<int>("totalHits") > 0);
            var titles = result.Value<JArray>("data").Values<JObject>().Select(jobj =>
                jobj.Value<string>("Title"));

            string previous = null;
            foreach (var title in titles)
            {
                if (previous != null)
                {
                    Assert.True(String.Compare(title, previous, StringComparison.OrdinalIgnoreCase) <= 0);
                }
                previous = title;
            }
        }
    }
}
