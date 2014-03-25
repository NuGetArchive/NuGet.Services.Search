using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NuGet.Indexing;
using PowerArgs;

namespace IndexMaintainance
{
    public abstract class IndexArgs
    {
        [ArgShortcut("-st")]
        [ArgDescription("The connection string to the storage server")]
        public string StorageAccountConnectionString { get; set; }

        [ArgShortcut("-cont")]
        [ArgDescription("The Blob Storage Container")]
        public string Container { get; set; }

        [ArgShortcut("-dir")]
        [ArgDescription("The file system folder")]
        public string Folder { get; set; }
    }

    public abstract class IndexWriteArgs : IndexArgs
    {
        [ArgShortcut("-db")]
        [ArgDescription("Connection string to the relevant database server")]
        public string ConnectionString { get; set; }

        [ArgShortcut("-ldb")]
        [ArgDescription("Instead of -db, use this parameter to connect to a SQL LocalDb database of the specified name")]
        public string LocalDbName { get; set; }

        [ArgShortcut("-c")]
        [ArgDescription("The components to include in the index: Data, Typeahead or All")]
        [DefaultValue(IndexComponents.Data)]
        public IndexComponents Components { get; set; }

        [ArgShortcut("-n")]
        [ArgShortcut("-!")]
        [ArgDescription("Shows what would be executed without running it")]
        public bool WhatIf { get; set; }
    }
}
