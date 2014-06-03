using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services.Search
{
    public class SearchConfiguration
    {
        public string IndexPath { get; set; }
        
        [DefaultValue("ng-search")]
        public string IndexContainer { get; set; }
        
        public string StatsContainer { get; set; }
    }
}
