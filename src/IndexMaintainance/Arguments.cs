using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Store;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json.Linq;
using NuGet.Indexing;
using PowerArgs;

namespace IndexMaintainance
{
    public class Arguments
    {
        [ArgActionMethod]
        public void IndexAdds(IndexAddsArgs args)
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

            IndexAddsTask task = new IndexAddsTask()
            {
                Clear = args.Clear,
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
                throw new NotImplementedException("Not yet implemented");
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
                args.SortBy ?? String.Empty, 
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
    }
}
