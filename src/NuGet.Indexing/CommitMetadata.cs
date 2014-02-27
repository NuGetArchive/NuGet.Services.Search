using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Indexing
{
    public class CommitMetadata
    {
        public DateTime TimestampUtc { get; set; }
        public string Description { get; set; }
        public int DocumentCount { get; set; }
        public int HighestPackageKey { get; set; }
        public DateTime LastEditUtc { get; set; }
        public int MaxDocumentsPerCommit { get; set; }
        public int MergeFactor { get; set; }
        public int MaxMergeDocs { get; set; }

        private CommitMetadata()
        {

        }

        public CommitMetadata(string description) : this()
        {
            Description = description;
        }

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>() {
                {"TimestampUtc", TimestampUtc.ToString("O")},
                {"LastEditUtc", LastEditUtc.ToString("O")},
                {"Description", Description ?? String.Empty},
                {"DocumentCount", DocumentCount.ToString()},
                {"HighestPackageKey", HighestPackageKey.ToString()},
                {"MaxDocumentsPerCommit", MaxDocumentsPerCommit.ToString()},
                {"MergeFactor", MergeFactor.ToString()},
                {"MaxMergeDocs", MaxMergeDocs.ToString()}
            };
        }

        public static CommitMetadata FromDictionary(IDictionary<string, string> dict)
        {
            var meta = new CommitMetadata();
            meta.TimestampUtc = GetOrDefault<DateTime>(dict, "TimestampUtc", s => DateTime.Parse(s, CultureInfo.CurrentCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal));
            meta.LastEditUtc = GetOrDefault<DateTime>(dict, "LastEditUtc", s => DateTime.Parse(s, CultureInfo.CurrentCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal));
            meta.Description = GetOrDefault(dict, "Description");
            meta.DocumentCount = GetOrDefault(dict, "DocumentCount", Int32.Parse);
            meta.HighestPackageKey = GetOrDefault(dict, "HighestPackageKey", Int32.Parse);
            meta.MaxDocumentsPerCommit = GetOrDefault(dict, "MaxDocumentsPerCommit", Int32.Parse);
            meta.MergeFactor = GetOrDefault(dict, "MergeFactor", Int32.Parse);
            meta.MaxMergeDocs = GetOrDefault(dict, "MaxMergeDocs", Int32.Parse);
            return meta;
        }

        private static string GetOrDefault(IDictionary<string, string> dict, string key)
        {
            string ret;
            if (dict.TryGetValue(key, out ret))
            {
                return ret;
            }
            return null;
        }

        private static T GetOrDefault<T>(IDictionary<string, string> dict, string key, Func<string, T> converter)
        {
            string strVal = GetOrDefault(dict, key);
            if (String.IsNullOrEmpty(strVal))
            {
                return default(T);
            }
            return converter(strVal);
        }
    }
}
