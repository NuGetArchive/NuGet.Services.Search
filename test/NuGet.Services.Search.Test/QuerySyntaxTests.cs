using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGet.Services.TestFramework;
using Xunit;

namespace NuGet.Services.Search.Test
{
    /// <summary>
    /// Tests that ensure all query syntax behaves as expected.
    /// </summary>
    public class QuerySyntaxTests : HttpTest
    {
        public QuerySyntaxTests(RunContext context) : base(context) { }

        [Theory]
        [InlineData(@"id: testpackage")]
        [InlineData(@"riaservices*")]
        [InlineData(@"jquery-")]
        [InlineData(@"Ctrl +")]
        [InlineData(@"#$%^&*()")]
        [InlineData(@"Id: , tag:")]
        [InlineData(@":config")]
        [InlineData(@"[id:]")]
        [InlineData(@"(:)")]
        [InlineData(@"\, +, -, !, (, ), :, ^, ], {, }, ~,")]
        [InlineData(@"#")]
        [InlineData(@"$")]
        [InlineData(@"%")]
        [InlineData(@"^")]
        [InlineData(@"&")]
        [InlineData(@"*")]
        [InlineData(@"\")]
        [InlineData(@"+")]
        [InlineData(@"-")]
        [InlineData(@"!")]
        [InlineData(@"(")]
        [InlineData(@")")]
        [InlineData(@":")]
        [InlineData(@"^")]
        [InlineData(@"]")]
        [InlineData(@"{")]
        [InlineData(@"}")]
        [InlineData(@"~")]
        public async Task GivenBadNuGetSyntax_ItDoesNotReturn500(string query)
        {
            var result = await Context.GetJson<JObject>("/search/query?luceneQuery=false&q=" + WebUtility.UrlEncode(query));

            Assert.NotNull(result);
            Assert.NotNull(result["totalHits"]);
        }

        [Theory]
        [InlineData(@"")]
        [InlineData(@"abcdef")]
        [InlineData(@"id:jquery")]
        [InlineData(@"ID:jquery")]
        [InlineData(@"iD:jquery")]
        [InlineData(@"Id:jquery")]
        [InlineData(@"PackageId:jquery")]
        [InlineData(@"id:jquery tags:validation")]
        [InlineData("id:\"jquery.ui\"")]
        [InlineData("modern UI javascript")]
        [InlineData("\"modern UI\" package")]
        public async Task GivenValidNuGetSyntax_ItDoesNotReturn500(string query)
        {
            var result = await Context.GetJson<JObject>("/search/query?luceneQuery=false&q=" + WebUtility.UrlEncode(query));

            Assert.NotNull(result);
            Assert.NotNull(result["totalHits"]);
        }

        [Fact]
        public async Task GivenTagFilters_AllTagsArePresentInAllResults()
        {
            var result = await Context.GetJson<JObject>("/search/query?luceneQuery=false&q=tag:jquery tag:validation");

            Assert.True(result.Value<int>("totalHits") > 0);
            Assert.True(result
                .Value<JArray>("data")
                .Cast<JObject>()
                .Select(j => j.Value<string>("Tags"))
                .All(s =>
                    // Have to use IndexOf because Contains doesn't take a StringComparison.
                    s.IndexOf("jquery", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    s.IndexOf("validation", StringComparison.OrdinalIgnoreCase) >= 0));
        }

        [Fact]
        public async Task GivenTagPhraseQuery_AllTagsArePresentInAllResults()
        {
            var result = await Context.GetJson<JObject>("/search/query?luceneQuery=false&q=tag:\"jquery validation\"");

            Assert.True(result.Value<int>("totalHits") > 0);
            Assert.True(result
                .Value<JArray>("data")
                .Cast<JObject>()
                .Select(j => j.Value<string>("Tags"))
                .All(s =>
                    // Have to use IndexOf because Contains doesn't take a StringComparison.
                    s.IndexOf("jquery", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    s.IndexOf("validation", StringComparison.OrdinalIgnoreCase) >= 0));
        }
    }
}
