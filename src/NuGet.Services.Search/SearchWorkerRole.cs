using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using NuGet.Services.Hosting.Azure;

namespace NuGet.Services.Search
{
    public class SearchWorkerRole : SingleServiceWorkerRole<SearchService>
    {
    }
}
