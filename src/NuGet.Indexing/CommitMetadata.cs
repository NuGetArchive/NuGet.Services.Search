﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Indexing
{
    /// <summary>
    /// Metadata stored every time a batch of documents are committed to the index.
    /// </summary>
    public class CommitMetadata
    {
        public DateTime TimestampUtc { get; private set; }
        public string Message { get; private set; }
        
        private CommitMetadata()
        {

        }

        public CommitMetadata(string message, DateTime timestampUtc)
            : this()
        {
            Message = message;
            TimestampUtc = timestampUtc;
        }

        public CommitMetadata(string message)
            : this(message, DateTime.UtcNow)
        {
        }

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>() {
                {"TimestampUtc", TimestampUtc.ToString("O")},
                {"Message", Message ?? String.Empty},
            };
        }

        public static CommitMetadata FromDictionary(IDictionary<string, string> dict)
        {
            var meta = new CommitMetadata();
            meta.TimestampUtc = GetOrDefault<DateTime>(dict, "TimestampUtc", s => DateTime.Parse(s, CultureInfo.CurrentCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal));
            meta.Message = GetOrDefault(dict, "Message");
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
