using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
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
    }
}
