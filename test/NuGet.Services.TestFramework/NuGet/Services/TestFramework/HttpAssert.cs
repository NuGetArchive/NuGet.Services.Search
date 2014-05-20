using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace NuGet.Services.TestFramework
{
    public class HttpAssert
    {
        public RunContext Context { get; private set; }

        public HttpAssert(RunContext context)
        {
            Context = context;
        }

        public async Task StatusCode(HttpStatusCode expected, string url)
        {
            var resp = await Context.HttpClient.GetAsync(url);
            Assert.Equal(expected, resp.StatusCode);
        }
    }
}