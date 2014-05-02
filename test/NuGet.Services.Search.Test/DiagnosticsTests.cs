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
    public class DiagnosticsTests : HttpTest
    {
        public DiagnosticsTests(RunContext context) : base(context) { }

        [Fact]
        public async Task GivenNoQueryString_ItReturnsDiagnosticData()
        {
            var result = await Context.GetJson<JObject>("/search/diag");

            Assert.NotNull(result.Property("NumDocs"));
            Assert.NotNull(result.Property("SearcherManagerIdentity"));
            Assert.NotNull(result.Property("Index"));
            Assert.NotNull(result.Property("CommitUserData"));
            Assert.NotNull(result.Property("RankingsUpdated"));
            Assert.NotNull(result.Property("DownloadCountsUpdated"));
        }
    }
}
