using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xunit;

namespace NuGet.Services.TestFramework
{
    public class RunContext
    {
        public HttpClient HttpClient { get; private set; }
        public RunConfiguration Config { get; private set; }

        public RunContext()
        {
            Config = RunConfiguration.FromEnvironment();
            HttpClient = new HttpClient(new TestTracingHandler(new HttpClientHandler()))
            {
                BaseAddress = Config.ServiceRoot
            };
        }

        public Task<JToken> GetJson(string url)
        {
            return GetJson<JToken>(url);
        }

        public async Task<T> GetJson<T>(string url) where T : JToken
        {
            var response = await HttpClient.GetAsync(url);
            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);

            return (T)JToken.Parse(await response.Content.ReadAsStringAsync());
        }
    }
}
