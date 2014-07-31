using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Store;
using Lucene.Net.Store.Azure;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Formatters;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json.Linq;
using NuGet.Indexing;
using NuGet.Services.Search.Client;
using PowerArgs;
using IndexMaintenance;
using System.IO;
using System.Diagnostics.Tracing;

namespace IndexMaintainance
{
    public class Arguments
    {
        [ArgShortcut("t")]
        [ArgDescription("The level of tracing to display")]
        [DefaultValue(EventLevel.Informational)]
        public EventLevel TraceLevel { get; set; }

        [ArgActionMethod]
        public void FullBuild(FullBuildArgs args)
        {
            using (StartTracing())
            {
                if (!String.IsNullOrEmpty(args.LocalDbName))
                {
                    args.ConnectionString = String.Format(@"Data Source=(LocalDB)\v11.0;Initial Catalog={0};Integrated Security=True", args.LocalDbName);
                }
                CloudStorageAccount acct = null;
                if (!String.IsNullOrEmpty(args.StorageAccountConnectionString))
                {
                    acct = CloudStorageAccount.Parse(args.StorageAccountConnectionString);
                }

                FullBuildTask task = new FullBuildTask()
                {
                    Container = args.Container,
                    Folder = args.Folder,
                    Force = args.Force,
                    FrameworksFile = args.FrameworksFile,
                    Log = TextWriter.Null,
                    SqlConnectionString = args.ConnectionString,
                    StorageAccount = acct,
                    WhatIf = args.WhatIf
                };
                task.Execute();
            }
        }

        [ArgActionMethod]
        public void UpdateIndex(UpdateIndexArgs args)
        {
            if (!String.IsNullOrEmpty(args.LocalDbName))
            {
                args.ConnectionString = String.Format(@"Data Source=(LocalDB)\v11.0;Initial Catalog={0};Integrated Security=True", args.LocalDbName);
            }
            CloudStorageAccount acct = null;
            if (!String.IsNullOrEmpty(args.StorageAccountConnectionString))
            {
                acct = CloudStorageAccount.Parse(args.StorageAccountConnectionString);
            }

            UpdateIndexTask task = new UpdateIndexTask()
            {
                Container = args.Container,
                Folder = args.Folder,
                Log = Console.Out,
                SqlConnectionString = args.ConnectionString,
                StorageAccount = acct,
                WhatIf = args.WhatIf
            };
            task.Execute();
        }

        [ArgActionMethod]
        public void Query(QueryArgs args)
        {
            // Open client
            var client = new SearchClient(new Uri(args.ServiceUrl));

            // Perform the query
            var result = client.Search(
                args.Query,
                args.ProjectType ?? String.Empty,
                args.IncludePrerelease,
                args.Feed,
                args.SortOrder,
                args.Skip,
                args.Take, 
                args.IsLuceneQuery,
                args.CountOnly,
                args.IncludeExplanation,
                args.IgnoreFilter).Result;
            if (!result.IsSuccessStatusCode)
            {
                Console.WriteLine("{0} from service!", (int)result.StatusCode);
            }
            else
            {
                var content = result.ReadContent().Result;
                Console.WriteLine("Hits: {0}", content.TotalHits);
                if (content.IndexTimestamp != null)
                {
                    Console.WriteLine("Index Timestamp: {0}", content.IndexTimestamp.Value.ToLocalTime());
                }
                foreach (dynamic hit in content.Data)
                {
                    Console.WriteLine(" {0} {1} (Published: {2})", hit.Title ?? hit.PackageRegistration.Id, hit.Version, hit.Published);
                }
            }
        }

        [ArgActionMethod]
        public void PerfTest(PerfTestArgs args)
        {
            ServicePointManager.ServerCertificateValidationCallback = (_, __, ___, ____) => true;

            // Load the query file
            IList<string> queries = (args.Queries ?? System.IO.File.ReadAllLines(args.QueryList)).ToList();

            // Open the client
            var client = new SearchClient(args.TargetServiceUri);

            // Collect data
            Console.WriteLine("Running tests...");
            IList<List<Tuple<string,double>>> data = Enumerable.Range(0, args.Runs)
                .Select(run =>
                {
                    Console.WriteLine("Run #{0} underway", run + 1);
                    return queries.Select(query =>
                    {
                        DateTime start = DateTime.UtcNow;
                        client.Search(query).Wait();
                        return Tuple.Create(query, (DateTime.UtcNow - start).TotalMilliseconds);
                    }).ToList();
                })
                .ToList();

            // Display data
            Console.WriteLine("-- Run #1 --");
            RenderRunData(data[0]);

            for(int i = 1; i < data.Count; i++)
            {
                Console.WriteLine("-- Run #{0} --", i + 1);
                RenderRunData(data[i]);
            }
            
            // Calculate aggregate warm time
            var maxAgg = TupleAggregate(max: true);
            var minAgg = TupleAggregate(max: false);
            var warmMax = data.Skip(1).Select(d => d.Aggregate(maxAgg)).Aggregate(maxAgg);
            var warmMin = data.Skip(1).Select(d => d.Aggregate(minAgg)).Aggregate(minAgg);
            var warmAvg = data.Skip(1).SelectMany(run => run).Average(t => t.Item2);
            Console.WriteLine("-- Warm Run Aggregates --");
            Console.WriteLine("Maximum: {0:0.00}ms for {1}", warmMax.Item2, warmMax.Item1);
            Console.WriteLine("Minimum: {0:0.00}ms for {1}", warmMin.Item2, warmMin.Item1);
            Console.WriteLine("Average: {0:0.00}ms", warmAvg);
        }

        [ArgActionMethod]
        public void ParseQuery(ParseQueryArgs args)
        {
            Console.WriteLine("---- Converted Lucene Query ----");
            Console.WriteLine(LuceneQueryCreator.Parse(args.Query, args.IsLuceneQuery));
            Console.WriteLine("-- End Converted Lucene Query --");
        }

        private void RenderRunData(IList<Tuple<string, double>> run)
        {
            // Find the interesting values
            Tuple<string, double> max = run.Aggregate(TupleAggregate(max: true));
            Tuple<string, double> min = run.Aggregate(TupleAggregate(max: false));
            double avg = run.Average(t => t.Item2);
            Console.WriteLine("Maximum: {0:0.00}ms for {1}", max.Item2, max.Item1);
            Console.WriteLine("Minimum: {0:0.00}ms for {1}", min.Item2, min.Item1);
            Console.WriteLine("Average: {0:0.00}ms", avg);
        }

        // Helper function because the built-in max/min don't make it easy to bring the associated object back
        private Func<Tuple<string, double>, Tuple<string, double>, Tuple<string, double>> TupleAggregate(bool max)
        {
            return (l, r) =>
            {
                if (l == null)
                {
                    return r;
                }
                else if ((max && r.Item2 > l.Item2) || (!max && r.Item2 < l.Item2))
                {
                    return r;
                }
                else
                {
                    return l;
                }
            };
        }

        private IDisposable StartTracing()
        {
            var listener = ConsoleLog.CreateListener(
                new ConsoleEventFormatter(), new DefaultConsoleColorMapper());
            listener.EnableEvents(IndexingEventSource.Log, TraceLevel);

            return listener;
        }
    }
}
