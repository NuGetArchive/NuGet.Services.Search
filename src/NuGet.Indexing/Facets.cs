using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Indexing
{
    public static class Facets
    {
        private const string CompatibleName = "Compatible";
        private const string LatestStableVersionName = "LatestStableVersion";
        private const string LatestPrereleaseVersionName = "LatestPrereleaseVersion";

        public static string Compatible(FrameworkName framework)
        {
            return Create(CompatibleName, framework.FullName);
        }

        public static string LatestStableVersion(FrameworkName framework)
        {
            return Create(LatestStableVersionName, framework.FullName);
        }

        public static string LatestPrereleaseVersion(FrameworkName framework)
        {
            return Create(LatestPrereleaseVersionName, framework.FullName);
        }

        internal static string Create(string name, string parameter)
        {
            return name + "(" + parameter + ")";
        }
    }
}
