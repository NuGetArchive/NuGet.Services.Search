using Lucene.Net.Search;

namespace NuGet.Indexing
{
    public class CachingFrameworkCompatibilityFilter : CachingWrapperFilter
    {
        public CachingFrameworkCompatibilityFilter(FrameworkCompatibilityFilter filter)
            : base(filter)
        {
            Inner = filter;
        }

        public FrameworkCompatibilityFilter Inner { get; private set; }
    }
}
