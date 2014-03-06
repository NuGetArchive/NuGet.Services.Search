using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NuGet.Services.Search.Models
{
    public class SearchResults
    {
        public int TotalHits { get; set; }
        public ICollection<JObject> Data { get; set; }
    }
}
