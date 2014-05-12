using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.Services.TestFramework
{
    public class TestTracingHandler : DelegatingHandler
    {
        public TestTracingHandler() : base() { }
        public TestTracingHandler(HttpClientHandler inner) : base(inner) { }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Trace.TraceInformation("http -> {0} {1}", request.Method.Method, request.RequestUri);
            var response = await base.SendAsync(request, cancellationToken);
            Trace.TraceInformation("http <- {0} {1}", response.StatusCode, response.RequestMessage.RequestUri);
            return response;
        }
    }
}
