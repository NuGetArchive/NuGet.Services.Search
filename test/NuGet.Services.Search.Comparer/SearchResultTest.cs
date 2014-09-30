using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using Xunit;

namespace NuGet.Services.Search.Comparer
{
    public class SearchResultTest
    {
        private int _count = 50;
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
        public void CompareSearchResultsForReductions(string targetFramework, CuratedFeed feed)
        {
            // Get the list of packages that are missing from the new search results
            List<IPackage> reductions = GetChangesInSearchResults(targetFramework, feed).Item1;

            // Assert packages from the redutions list are not compatible with the project's targetframework 
            AssertPackageListOutcome(targetFramework, reductions, false);
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
        public void CompareSearchResultsForAdditions(string targetFramework, CuratedFeed feed)
        {
            // Get the list of packages that are added to the new search results
            List<IPackage> additions = GetChangesInSearchResults(targetFramework, feed).Item2;

            // Assert packages from the additions list are compatible with the project's targetframework 
            AssertPackageListOutcome(targetFramework, additions, true);
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
        public void CompareSearchResultsForVersionUpdates(string targetFramework, CuratedFeed feed)
        {
            // Get the list of packages that have different versions in the new result results
            List<IPackage> versionUpdates = GetChangesInSearchResults(targetFramework, feed).Item3;

            // Assert packages from the versionUpdates list are compatible with the project's targetframework 
            AssertPackageListOutcome(targetFramework, versionUpdates, true);
        }

        #region Helper methods
        private void AssertPackageListOutcome(string targetFramework, IList<IPackage> list, bool expectCompatible)
        {
            FrameworkName projFramework = new FrameworkName(targetFramework);
            foreach (IPackage package in list)
            {
                List<FrameworkName> packageFrameworks = FindSupportedTargetFrameworksForPackage(package);
                bool isCompatible = IsCompatible(projFramework, packageFrameworks);
                string output = BuildOutputString(package, projFramework, packageFrameworks, isCompatible, expectCompatible);
                if (expectCompatible)
                {
                    Assert.True(isCompatible, output);
                }
                else
                {
                    Assert.False(isCompatible, output);
                }
            }
        }
        private string BuildOutputString(IPackage package, FrameworkName projFramework, List<FrameworkName> packageFrameworks, bool isCompatible, bool expectTrue)
        {
            string packageFrameworksList = (packageFrameworks == null || packageFrameworks.Count == 0) ? "Supporing All Frameworks" : string.Join(", ", packageFrameworks); 
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(string.Format("Package: {0}, {1}", package.Id, package.Version));
            builder.AppendLine(string.Format("Project targetframework is {0}; Package targetframeworks are {1}.", projFramework, packageFrameworksList));
            builder.AppendLine(string.Format("Expected compatibility: {0}; Actual: {1}", expectTrue, isCompatible));
            builder.AppendLine();
            return builder.ToString();
        }

        private bool IsCompatible(FrameworkName projectFramework, List<FrameworkName> packageFrameworks)
        {
            bool compat = NuGet.VersionUtility.IsCompatible(projectFramework, packageFrameworks);
            return compat;
        }

        private Tuple<List<IPackage>, List<IPackage>, List<IPackage>> GetChangesInSearchResults(string targetFramework, CuratedFeed feed)
        {
            List<string> frameworks = new List<string>();
            frameworks.Add(targetFramework);
            // Get prod search results
            List<IPackage> prodResults = ReturnSearchResults(FeedSource.Prod, feed, "", frameworks, false, Count);
            // Get Int search results
            List<IPackage> intResults = ReturnSearchResults(FeedSource.Int, feed, "", frameworks, false, Count);

            List<IPackage> reductions = new List<IPackage>();
            List<IPackage> additions = new List<IPackage>();
            List<IPackage> versionUpdates = new List<IPackage>();

            for (int i = 0; i < prodResults.Count(); i++)
            {
                IPackage package = intResults.Where(p => p.Id == prodResults[i].Id).FirstOrDefault();
                if (package == null)
                {
                    reductions.Add(prodResults[i]);
                }
            }

            for (int i = 0; i < intResults.Count(); i++)
            {
                IPackage package = prodResults.Where(p => p.Id == intResults[i].Id).FirstOrDefault();
                if (package == null)
                {
                    additions.Add(intResults[i]);
                }
            }

            for (int i = 0; i < prodResults.Count(); i++)
            {
                IPackage packageA = prodResults[i];
                IPackage packageB = intResults.Where(p => p.Id == packageA.Id).FirstOrDefault();
                if (packageB != null && packageA.Version != packageB.Version)
                {
                    versionUpdates.Add(packageB);
                }
            }

            var resultTuple = new Tuple<List<IPackage>, List<IPackage>,List<IPackage>>(reductions, additions, versionUpdates);
            return resultTuple;
        }

        private List<FrameworkName> FindSupportedTargetFrameworksForPackage(IPackage package)
        {
            List<FrameworkName> frameworks = new List<FrameworkName>();
            try
            {
                frameworks = package.GetSupportedFrameworks().ToList();
            }
            catch (Exception)
            {
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
