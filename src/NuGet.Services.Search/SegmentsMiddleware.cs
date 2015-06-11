// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Owin;
using NuGet.Indexing;

namespace NuGet.Services.Search
{
    public class SegmentsMiddleware
    {
        public static async Task Execute(IOwinContext context, PackageSearcherManager searcherManager)
        {
            Trace.TraceInformation("Segments");
            context.Response.Headers.Add("Pragma", new[] { "no-cache" });
            context.Response.Headers.Add("Cache-Control", new[] { "no-cache" });
            context.Response.Headers.Add("Expires", new[] { "0" });
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(IndexAnalyzer.GetSegments(searcherManager));
        }
    }
}
