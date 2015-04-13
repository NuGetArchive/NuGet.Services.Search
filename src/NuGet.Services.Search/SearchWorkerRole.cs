using NuGet.Services.Hosting.Azure;

namespace NuGet.Services.Search
{
    public class SearchWorkerRole 
        : SingleServiceWorkerRole<SearchService>
    {
    }
}
