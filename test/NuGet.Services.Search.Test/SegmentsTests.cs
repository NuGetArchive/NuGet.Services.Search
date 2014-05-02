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
    public class SegmentsTests : HttpTest
    {
        public SegmentsTests(RunContext context) : base(context) { }

        [Fact]
        public async Task GivenNoQueryString_ItReturnsSegmentsData()
        {
            var result = await Context.GetJson<JArray>("/search/segments");

            Assert.True(result.Count > 0);

            var firstSegment = result[0].Value<JObject>();
            Assert.NotNull(firstSegment.Property("segment"));
            Assert.NotNull(firstSegment.Property("documents"));
        }
    }
}
