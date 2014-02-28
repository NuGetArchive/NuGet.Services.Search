using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.Indexing.Model
{
    /// <summary>
    /// Represents non-index data associated with the package. Designed to be the primary piece of data
    /// actually sent back to clients.
    /// </summary>
    public class PackageData
    {
        public int Key { get; set; }
        public PackageRegistrationData Registration { get; set; }
        public string Id { get; set; }
        public string Version { get; set; }
        public string NormalizedVersion { get; set; }

        public string Authors { get; set; }
        public string Copyright { get; set; }
        public DateTime Created { get; set; }
        public string Dependencies { get; set; }
        public string Description { get; set; }
        public string IconUrl { get; set; }
        public bool IsLatestVersion { get; set; }
        public bool IsAbsoluteLatestVersion { get; set; }
        public bool IsPrerelease { get; set; }
        public string Language { get; set; }
        public DateTime LastUpdated { get; set; }
        public DateTime Published { get; set; }
        public string PackageHash { get; set; }
        public string PackageHashAlgorithm { get; set; }
        public long PackageSize { get; set; }
        public string ProjectUrl { get; set; }
        public string ReportAbuseUrl { get; set; }
        public string ReleaseNotes { get; set; }
        public bool RequireLicenseAcceptance { get; set; }
        public string Summary { get; set; }
        public string Tags { get; set; }
        public string Title { get; set; }
        public string MinClientVersion { get; set; }
        public int DownloadCount { get; set; }
        public DateTime? LastEdited { get; set; }

        // License Report Information
        public string LicenseUrl { get; set; }
        public string LicenseNames { get; set; }
        public string LicenseReportUrl { get; set; }
    }
}
