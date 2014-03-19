using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Store;
using Lucene.Net.Store.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json.Linq;
using NuGet.Indexing;
using NuGet.Services.Search.Client;
using PowerArgs;

namespace IndexMaintainance
{
    public class Arguments
    {
        [ArgActionMethod]
        public void FullBuild(FullBuildArgs args)
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
                Log = Console.Out,
                SqlConnectionString = args.ConnectionString,
                StorageAccount = acct,
                WhatIf = args.WhatIf
            };
            task.Execute();
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
            // Load index
            Directory dir = null;
            Rankings rank = null;
            if (!String.IsNullOrEmpty(args.Folder))
            {
                rank = new FolderRankings(args.Folder);
                dir = new SimpleFSDirectory(new System.IO.DirectoryInfo(args.Folder));
            }
            else
            {
                CloudStorageAccount acct = CloudStorageAccount.Parse(args.StorageAccountConnectionString);
                CloudBlobContainer container = acct.CreateCloudBlobClient().GetContainerReference(args.Container ?? "ng-search");
                rank = new StorageRankings(container);
                dir = new AzureDirectory(acct, container.Name, new RAMDirectory());
            }

            if (!args.IsLuceneQuery && !String.IsNullOrEmpty(args.Query))
            {
                args.Query = LuceneQueryCreator.Parse(args.Query);
            }

            // Load Searcher Manager
            PackageSearcherManager manager = new PackageSearcherManager(dir, rank);

            // Perform the query
            string result = Searcher.Search(
                manager, 
                args.Query ?? String.Empty, 
                args.CountOnly, 
                args.ProjectType ?? String.Empty, 
                args.IncludePrerelease, 
                args.Feed ?? "none",
                args.Skip, 
                args.Take, 
                args.IncludeExplanation, 
                args.IgnoreFilter);
            dynamic json = JObject.Parse(result);

            Console.WriteLine("{0} hits", (int)json.totalHits);
            foreach (dynamic hit in json.data)
            {
                Console.WriteLine(" {0} {1} ", hit.PackageRegistration.Id, hit.Version);
            }
        }

        [ArgActionMethod]
        public void PerfTest(PerfTestArgs args)
        {
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
            var warmMax = data.Select(d => d.Aggregate(maxAgg)).Aggregate(maxAgg);
            var warmMin = data.Select(d => d.Aggregate(minAgg)).Aggregate(minAgg);
            var warmAvg = data.SelectMany(run => run).Average(t => t.Item2);
            Console.WriteLine("-- Warm Run Aggregates --");
            Console.WriteLine("Maximum: {0:0.00}ms for {1}", warmMax.Item2, warmMax.Item1);
            Console.WriteLine("Minimum: {0:0.00}ms for {1}", warmMin.Item2, warmMin.Item1);
            Console.WriteLine("Average: {0:0.00}ms", warmAvg);
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
    }
}
