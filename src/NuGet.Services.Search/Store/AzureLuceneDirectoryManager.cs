using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Store;

namespace NuGet.Services.Search.Store
{
    public class AzureLuceneDirectoryManager : LuceneDirectoryManager
    {
        public override Directory Open()
        {
            throw new NotImplementedException();
        }
    }
}
