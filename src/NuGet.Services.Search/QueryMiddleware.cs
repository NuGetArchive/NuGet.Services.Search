﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Microsoft.Owin;
using Newtonsoft.Json.Linq;
using NuGet.Indexing;
using Searcher = NuGet.Indexing.Searcher;

namespace NuGet.Services.Search
{
    public class QueryMiddleware
    {
        public static async Task Execute(IOwinContext context, PackageSearcherManager searcherManager)
        {
            Trace.TraceInformation("Search: {0}", context.Request.QueryString);

            string q = context.Request.Query["q"] ?? string.Empty;

            string projectType = context.Request.Query["projectType"] ?? string.Empty;

            string sortBy = context.Request.Query["sortBy"] ?? string.Empty;

            bool luceneQuery;
            if (!bool.TryParse(context.Request.Query["luceneQuery"], out luceneQuery))
            {
                luceneQuery = true;
            }

            bool includePrerelease;
            if (!bool.TryParse(context.Request.Query["prerelease"], out includePrerelease))
            {
                includePrerelease = false;
            }

            bool countOnly;
            if (!bool.TryParse(context.Request.Query["countOnly"], out countOnly))
            {
                countOnly = false;
            }

            string feed = context.Request.Query["feed"] ?? "none";

            int skip;
            if (!int.TryParse(context.Request.Query["skip"], out skip))
            {
                skip = 0;
            }

            int take;
            if (!int.TryParse(context.Request.Query["take"], out take))
            {
                take = 20;
            }

            bool includeExplanation;
            if (!bool.TryParse(context.Request.Query["explanation"], out includeExplanation))
            {
                includeExplanation = false;
            }

            bool ignoreFilter;
            if (!bool.TryParse(context.Request.Query["ignoreFilter"], out ignoreFilter))
            {
                ignoreFilter = false;
            }

            string fxName = context.Request.Query["supportedFramework"];
            FrameworkName supportedFramework = null;
            if (!string.IsNullOrEmpty(fxName))
            {
                supportedFramework = VersionUtility.ParseFrameworkName(fxName);
            }
            if (supportedFramework == null || !searcherManager.GetFrameworks().Contains(supportedFramework))
            {
                supportedFramework = FrameworksList.AnyFramework;
            }

            Query query;
            string content;
            int maxAgeSeconds = 0;

            try
            {
                query = LuceneQueryCreator.Parse(q, luceneQuery);

                if (query == null)
                {
                    query = new MatchAllDocsQuery();
                    maxAgeSeconds = 3600;
                }
                if (!ignoreFilter && !luceneQuery)
                {
                    string facet = includePrerelease ?
                        Facets.LatestPrereleaseVersion(supportedFramework) :
                        Facets.LatestStableVersion(supportedFramework);

                    var newQuery = new BooleanQuery();
                    newQuery.Add(query, Occur.MUST);
                    newQuery.Add(new TermQuery(new Term("Facet", facet)), Occur.MUST);
                    query = newQuery;
                }

                string args = string.Format("Searcher.Search(..., {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10})", q, countOnly, projectType, includePrerelease, feed, sortBy, skip, take, includeExplanation, ignoreFilter, luceneQuery);
                Trace.TraceInformation(args);

                content = Searcher.Search(
                    searcherManager,
                    query,
                    countOnly,
                    projectType,
                    includePrerelease,
                    feed,
                    sortBy,
                    skip,
                    take,
                    includeExplanation,
                    ignoreFilter);

                JObject result = JObject.Parse(content);
                result["answeredBy"] = "Search";
            }
            catch (Lucene.Net.QueryParsers.ParseException)
            {
                context.Response.StatusCode = 400;
                content = "Invalid query";
            }
            finally
            {
                context.Response.Headers.Add("Cache-Control", new[] { string.Format("private, max-age={0}", maxAgeSeconds) });
                context.Response.ContentType = "application/json";
            }
            await context.Response.WriteAsync(content);
        }
    }
}
