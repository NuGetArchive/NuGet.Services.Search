using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGet.Services.TestFramework;
using Xunit;

namespace NuGet.Services.Search.Test
{
    public class RelevanceTests : HttpTest
    {
        public RelevanceTests(RunContext context) : base(context) { }


        /// <summary>
        /// Tests that when a particular <paramref name="query"/> is executed, 
        /// there is a package with an ID that matches the regex specified in <paramref name="match"/>,
        /// within <paramref name="threshold"/> items of the top
        /// </summary>
        /// <param name="query">The query to execute</param>
        /// <param name="match">The regex to use for matching. Case-sensitive unless you specify a Regex option http://msdn.microsoft.com/en-us/library/yd1hzczs(v=vs.110).aspx</param>
        /// <param name="threshold">How far down the result that matches the regex can be</param>
        
        // Some Regex tips:
        //  "(\..+)?" will match either NOTHING, OR a literal "." followed by at least something. Good for matching ID prefixes
        //  Remember! "." is a special character in Regex, you need to escape it with "\."
        //  ALSO! If you use an @"" string in C#, you don't have to worry about double-escaping "\" itself.
        //  If you want case-insensitive matching, you can use the "(?i)" global option at the very beginning of your Regex
        [Theory]
        [InlineData("owner:Microsoft id:aspnet", @"^Microsoft\.AspNet(\..+)?$", 3)]
        [InlineData("owner:Microsoft id:aspnet mvc", @"^Microsoft\.AspNet\.Mvc(\..+)?$", 1)]
        [InlineData("Microsoft ASP.NET Web API 2 Core", @"^Microsoft.AspNet.WebApi.Core(\..+)?$", 1)]
        [InlineData("C++", @"^cpprestsdk$", 1)]
        [InlineData("C#", @"^Facebook\.CSharp\.SDK$", 3)]
        [InlineData("Breeze Client and Server (obsolete)", @"^Breeze\.WebApi(\..+)?$", 3)]
        [InlineData("identity mongodb", @"^MongoDB\.AspNet\.Identity(\..+)?$", 3)]
        [InlineData("Backload. A professional full featured ASP.NET file upload controller (MVC, Web API, WebForms)", @"^Backload(\..+)?$", 3)]
        [InlineData("Fare - Finite Automata and Regular Expressions", @"^Fare(\..+)?$", 3)]
        [InlineData("glimpse", @"^Glimpse$", 3)]
        [InlineData("less", @"^Less$", 3)]
        [InlineData("appinsights", @"^Microsoft\.ApplicationInsights(\..+)?$", 3)]
        [InlineData("application insights", @"^Microsoft\.ApplicationInsights(\..+)?$", 3)]
        [InlineData("riaservices server", @"^RIAServices.Server(\..+)?$", 3)]
        [InlineData("rdf", @"^dotNetRDF$", 3)]
        [InlineData("webapi", @"(?i)^.*WebApi.*$", 1)] // Top result should include WebAPI somewhere :)
        [InlineData("web api client", @"(?i)^.*WebApi.*$", 1)]
        [InlineData("signalr sample", @"(?i)^.*SignalR.*Sample.*$", 1)]
        [InlineData("request validation", @"^DisableRequestValidation$", 3)]
        [InlineData("attribute routing scaffolding", @"^Microsoft\.AspNet\.Mvc\.ScaffolderTemplates\.AttributeRouting(\..+)?$", 3)]
        [InlineData("tag:jquery", @"^jquery(\..+)?$", 1)] // First result should probably have jquery in it :)
        [InlineData("packageid:entityframework", @"^EntityFramework$", 1)] // Exact match!
        [InlineData("author:steffen forkmann", @"^FSharp.Testing$", 3)]
        [InlineData("author:\"colin blair\"", @"^RiaServicesContrib(\..+)?$", 3)]
        [InlineData("description:ria", @"(?i)^.*Ria.*$", 1)] // First result should have RIA in it!
        [InlineData("id:riaservices", @"(?i)^.*RiaServices.*$", 1)] // First result should have RiaServices in it!
        [InlineData("id:silverlight unittest", @"^Silverlight.UnitTest(\..+)?$", 3)]
        [InlineData("samjudson", @"^FlickrNet(\..+)?$", 3)] // samjudson is author of FlickerNet.* packages
        [InlineData("Nuget.core", @"^Nuget.Core$", 1)]
        [InlineData("EntityFramework", @"^EntityFramework$", 1)]
        [InlineData("Microsoft", @"^Microsoft(\..+)?$", 1)]
        [InlineData("Author:Jörn Zaefferer", @"^jQuery.Validation(\..+)?$", 1)]
        [InlineData("Author:\"Jörn Zaefferer\"", @"^jQuery.Validation(\..+)?$", 1)]
        public async Task RelevanceTest(string query, string match, int threshold)
        {
            var result = await Context.GetJson<JObject>("/search/query?luceneQuery=false&q=" + WebUtility.UrlEncode(query));
            
            Assert.True(result.Value<int>("totalHits") > 0);

            var hits = result.Value<JArray>("data");
            Assert.True(hits.Count > 0);

            // Take only <threshold> items
            Regex r = new Regex(match);
            foreach (var item in hits.Take(threshold))
            {
                string id = item["PackageRegistration"].Value<string>("Id");
                if (r.IsMatch(id))
                {
                    return;
                }
            }
            Assert.True(false, "No packages in the first " + threshold + " results matched the regex!");
        }
    }
}
