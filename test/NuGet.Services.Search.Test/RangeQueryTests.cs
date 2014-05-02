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
    public class RangeQueryTests : HttpTest
    {
        public RangeQueryTests(RunContext context) : base(context) { }

        [Fact]
        public async Task GivenNoQueryString_ItReturnsAnEmptyArray()
        {
            var result = await Context.GetJson<JArray>("/search/range");

            Assert.Equal(0, result.Count);
        }

        [Fact]
        public async Task GivenMinAndMax_ItReturnsADictionaryOfKeysBetweenMinAndMaxInclusive()
        {
            var result = await Context.GetJson<JObject>("/search/range?min=100000&max=100004");

            Assert.Equal(
                new[] { "100000", "100001", "100002", "100003", "100004" },
                result.Properties().Select(p => p.Name).ToArray());
        }

        [Fact]
        public async Task GivenMinAndMaxWithNoKeysInRange_ItReturnsAnEmptyDictionary()
        {
            var result = await Context.GetJson<JObject>("/search/range?min=0&max=0");

            Assert.Equal(0, result.Count);
        }
    }
}
