using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NuGet.Services.Search.Comparer
{
    public class SearchResultTest
    {
        private int _count = 100;
        private string _rootPath = Environment.CurrentDirectory;

        public int Count
        {
            get { return _count; }
            set { _count = value; }
        }

        public string RootPath
        {
            get { return _rootPath; }
            set { _rootPath = value; }
        }

        [Theory]
        [InlineData(".NETFramework,Version=v2.0", CuratedFeed.None)]
        [InlineData(".NETFramework,Version=v3.0", CuratedFeed.DotNet)]
        [InlineData(".NETFramework,Version=v3.5,Profile=Full", CuratedFeed.None)]
        [InlineData(".NETFramework,Version=v4.0", CuratedFeed.None)]
        [InlineData(".NETFramework,Version=v4.0", CuratedFeed.WebMatrix)]
        [InlineData(".NETFramework,Version=v4.5", CuratedFeed.Win8Express)]
        [InlineData("Silverlight,Version=v4.0", CuratedFeed.None)]
        [InlineData("Silverlight,Version=v5.0", CuratedFeed.None)]
        [InlineData(".NETFramework,Version=v4.0,Profile=Client", CuratedFeed.None)]
        [InlineData(".NETCore,Version=v4.5.1", CuratedFeed.None)]
        [InlineData("WindowsPhone,Version=v8.1", CuratedFeed.None)]
        [InlineData(".NETPortable,Version=v0.0,Profile=wp8+netcore45+net45+wp81+wpa81", CuratedFeed.None)]
        [InlineData("Unsupported,Version=v0.0", CuratedFeed.None)]
        [InlineData("MonoAndroid,Version=v0.0", CuratedFeed.None)]
        [InlineData("MonoTouch,Version=v0.0", CuratedFeed.None)]
        [InlineData("Silverlight,Version=v4.0,Profile=windowsphone71", CuratedFeed.None)]
        [InlineData("Windows,Version=v8.0", CuratedFeed.Win8Express)]
        [InlineData("WindowsPhoneApp,Version=v8.1", CuratedFeed.None)]
        [InlineData(".NETFramework,Version=v3.5,Profile=CompactFramework", CuratedFeed.None)]
        [InlineData(".NETMicroFramework,Version=v4.1", CuratedFeed.DotNet)]
        [InlineData("ASP.Net,Version=v5.0", CuratedFeed.None)]
        [InlineData("ASP.NetCore,Version=v5.0", CuratedFeed.None)]
        [InlineData("Windows, Version=8.1", CuratedFeed.None)]
        public void CompareSearchResultsBeforeAndAfterChanges(string targetFramework, CuratedFeed feed)
        {
            List<string> frameworks = new List<string>();
            frameworks.Add(targetFramework);
            // Get prod search results
            List<IPackage> prodResults = ReturnSearchResults(FeedSource.Prod, feed, "", frameworks, false, Count);
            // Get Int search results
            List<IPackage> intResults = ReturnSearchResults(FeedSource.Int, feed, "", frameworks, false, Count);

            // Compare the intResults against prodResults and get their differences
            List<IPackage> reductions;
            List<IPackage> additions;
            List<IPackage> versionUpdates;
            GetChangesInSearchResults(prodResults, intResults, out reductions, out additions, out versionUpdates);

            // Assert that the changes in search results are legitimate
            FrameworkName projFramework = new FrameworkName(targetFramework);

            foreach (IPackage package1 in reductions)
            {
                List<FrameworkName> packageFrameworks = FindSupportedTargetFrameworksForPackage(package1);
                Assert.True(!IsCompatible(projFramework, packageFrameworks));
            }

            foreach (IPackage package2 in additions)
            {
                List<FrameworkName> packageFrameworks = FindSupportedTargetFrameworksForPackage(package2);
                Assert.True(IsCompatible(projFramework, packageFrameworks));
            }

            foreach (IPackage package3 in versionUpdates)
            {
                List<FrameworkName> packageFrameworks = FindSupportedTargetFrameworksForPackage(package3);
                Assert.True(IsCompatible(projFramework, packageFrameworks));
            }
        }

        #region Helper methods
        private void GetChangesInSearchResults(List<IPackage> listA, List<IPackage> listB, out List<IPackage> reductions, out List<IPackage> additions, out List<IPackage> versionUpdates)
        {
            reductions = new List<IPackage>();
            additions = new List<IPackage>();
            versionUpdates = new List<IPackage>();

            for (int i = 0; i < listA.Count(); i++)
            {
                IPackage package = listB.Where(p => p.Id == listA[i].Id).FirstOrDefault();
                if (package == null)
                {
                    reductions.Add(package);
                }
            }

            for (int i = 0; i < listB.Count(); i++)
            {
                IPackage package = listA.Where(p => p.Id == listB[i].Id).FirstOrDefault();
                if (package == null)
                {
                    additions.Add(package);
                }
            }

            for (int i = 0; i < listA.Count(); i++)
            {
                IPackage packageA = listA[i];
                IPackage packageB = listB.Where(p => p.Id == packageA.Id).FirstOrDefault();
                if (packageB != null && packageA.Version != packageB.Version)
                {
                    versionUpdates.Add(packageB);
                }
            }
        }

        private bool IsCompatible(FrameworkName projectFramework, List<FrameworkName> packageFrameworks)
        {
            bool compat = NuGet.VersionUtility.IsCompatible(projectFramework, packageFrameworks);
            return compat;
        }

        private List<FrameworkName> FindSupportedTargetFrameworksForPackage(IPackage package)
        {
            List<FrameworkName> frameworks = new List<FrameworkName>();
            try
            {
                frameworks = package.GetSupportedFrameworks().ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return frameworks;
        }

        public List<IPackage> ReturnSearchResults(FeedSource source, CuratedFeed subFeed, string searchTerm, List<string> targetFrameworks, bool allowPrerelease, int count)
        {
            try
            {
                string packageSource = GetPackageSourceURL(source, subFeed);
                IPackageRepository repo = PackageRepositoryFactory.Default.CreateRepository(packageSource);
                IQueryable<IPackage> packages = repo.Search(searchTerm, targetFrameworks, allowPrerelease);
                var query = packages.OrderBy(p => p.DownloadCount).Take(count);
                return query.ToList().Take(count).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public string GetPackageSourceURL(FeedSource source, CuratedFeed type)
        {
            string rootURL = string.Empty;
            string curatedURL = string.Empty;
            switch (source)
            {
                case FeedSource.Prod:
                    {
                        rootURL = "https://www.nuget.org/api/v2/";
                        break;
                    }
                case FeedSource.Int:
                    {
                        rootURL = "https://int.nugettest.org/api/v2/";
                        break;
                    }
                default:
                    break;
            }

            switch (type)
            {
                case CuratedFeed.None:
                    {
                        curatedURL = "";
                        break;
                    }
                case CuratedFeed.Win8Express:
                    {
                        curatedURL = "curated-feeds/windows8-packages/";
                        break;
                    }
                case CuratedFeed.WebMatrix:
                    {
                        curatedURL = "curated-feeds/webmatrix/";
                        break;
                    }
                case CuratedFeed.DotNet:
                    {
                        curatedURL = "curated-feeds/microsoftdotnet/";
                        break;
                    }
                default:
                    break;
            }

            return rootURL + curatedURL;
        }
        #endregion
    }

    public enum FeedSource
    {
        Prod,
        Int
    }

    public enum CuratedFeed
    {
        None,
        Win8Express,
        WebMatrix,
        DotNet

    }
}
