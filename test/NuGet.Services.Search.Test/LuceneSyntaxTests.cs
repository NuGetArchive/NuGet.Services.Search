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
    /// Tests that ensure all lucene query syntax behaves as expected.
    /// </summary>
    public class LuceneSyntaxTests : HttpTest
    {
        public LuceneSyntaxTests(RunContext context) : base(context) { }

        [Theory]
        [InlineData("foo")]
        [InlineData("\"foo\"")]
        [InlineData("field:val")]
        [InlineData("field:\"phrase query\"")]
        [InlineData("field:\"phrase query\" AND boolean")]
        [InlineData("wild?card")]
        [InlineData("wild*card")]
        [InlineData("fuzzy~")]
        [InlineData("fuzzy~0.8")]
        [InlineData("\"fuzzy phrase\"~0.8")]
        [InlineData("range:[1 TO 10]")]
        [InlineData("range:{A TO Z}")]
        [InlineData("boost^4 query")]
        [InlineData("\"boost phrase\"^4 query")]
        [InlineData("this AND that")]
        [InlineData("this OR that")]
        [InlineData("NOT that")]
        [InlineData("+must +have those")]
        [InlineData("+must -not +have some")]
        [InlineData("group OR (of AND clauses)")]
        [InlineData("field:(+group +\"of clauses\")")]
        [InlineData(@"escaping \( of \* special \? characters \+")]
        public async Task ValidSyntaxProduces200(string query)
        {
            await HttpAssert.StatusCode(HttpStatusCode.OK, "/search/query?q=" + query);
        }

        [Theory]
        [InlineData("unclosed (paren")]
        [InlineData("empty field:")]
        [InlineData("range:[1 TO")]
        [InlineData("range:{Unclosed TO")]
        [InlineData("this AND OR NOT that")]
        public async Task InvalidSyntaxProduces400(string query)
        {
            await HttpAssert.StatusCode(HttpStatusCode.BadRequest, "/search/query?q=" + query);
        }
    }
}
