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
    public class StoredFieldsListTests : HttpTest
    {
        public StoredFieldsListTests(RunContext context) : base(context) { }

        [Fact]
        public async Task GivenNoQueryString_ItReturnsStoredFields()
        {
            var result = await Context.GetJson<JArray>("/search/fields");

            Assert.True(result.Count > 0);
            Assert.Contains("Data", result.Values().Select(t => t.Value<string>()));
        }
    }
}
