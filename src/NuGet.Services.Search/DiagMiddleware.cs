// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Owin;
using NuGet.Indexing;
using NuGet.Services.ServiceModel;

namespace NuGet.Services.Search
{
    public class DiagMiddleware
    {
        public static async Task Execute(IOwinContext context, PackageSearcherManager SearcherManager)
        {
            Trace.TraceInformation("Diag");
            context.Response.Headers.Add("Pragma", new[] { "no-cache" });
            context.Response.Headers.Add("Cache-Control", new[] { "no-cache" });
            context.Response.Headers.Add("Expires", new[] { "0" });
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(IndexAnalyzer.Analyze(SearcherManager));
        }
    }
}
