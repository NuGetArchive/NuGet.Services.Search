using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Store;

namespace NuGet.Services.Search.Store
{
    public abstract class LuceneDirectoryManager
    {
        public abstract Directory Open(bool createIfNotExists);
    }
}
