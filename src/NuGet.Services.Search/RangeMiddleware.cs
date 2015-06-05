// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using Microsoft.Owin;
using NuGet.Indexing;
using NuGet.Services.ServiceModel;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NuGet.Services.Search
{
    public class RangeMiddleware
    {  
        public static async Task Execute(IOwinContext context,PackageSearcherManager SearcherManager)
        {
            Trace.TraceInformation("Range: {0}", context.Request.QueryString);

            string min = context.Request.Query["min"];
            string max = context.Request.Query["max"];

            string content = "[]";

            int minKey;
            int maxKey;
            if (min != null && max != null && int.TryParse(min, out minKey) && int.TryParse(max, out maxKey))
            {
                Trace.TraceInformation("Searcher.KeyRangeQuery(..., {0}, {1})", minKey, maxKey);

                content = Searcher.KeyRangeQuery(SearcherManager, minKey, maxKey);
            }

            context.Response.Headers.Add("Pragma", new[] { "no-cache" });
            context.Response.Headers.Add("Cache-Control", new[] { "no-cache" });
            context.Response.Headers.Add("Expires", new[] { "0" });
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(content);
        }
    }
}
