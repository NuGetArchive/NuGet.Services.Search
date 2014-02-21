using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using NuGet.Services.Http;

namespace NuGet.Services.Search.Api
{
    public class SomethingController : NuGetApiController
    {
        [Route("test")]
        public string Test()
        {
            return "Search is up!";
        }
    }
}
