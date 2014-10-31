using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json.Linq;

namespace NuGet.Indexing
{
    public abstract class FrameworkCompatibility
    {
        public static readonly FrameworkName AnyFramework = new FrameworkName("Any", new Version(0, 0));
        public static readonly string FileName = "frameworkCompat.v1.json";

        public static FrameworkCompatibility Empty = new EmptyFrameworkCompatibility();

        public abstract string Path { get; }
        protected abstract JObject LoadJson();

        public IDictionary<string,ISet<string>> Load()
        {
            JObject obj = LoadJson();
            
            Dictionary<string,ISet<string>> dict = new Dictionary<string, ISet<string>>();
            if (obj == null)
            {
                return dict;
            }

            var data = obj.Value<JObject>("data");

            foreach (var val in data)
            {
                dict[val.Key] = new HashSet<string>(((IDictionary<string, JToken>)val.Value).Select(x => x.Key));
            }

            return dict;
        }

        private class EmptyFrameworkCompatibility : FrameworkCompatibility
        {
            public override string Path
            {
                get { return "<empty>"; }
            }

            protected override JObject LoadJson()
            {
                return null;
            }
        }
    }

    public class LocalFrameworkCompatibility : FrameworkCompatibility
    {
        string _path;

        public override string Path { get { return _path; } }

        public LocalFrameworkCompatibility(string path)
        {
            _path = path;
        }

        protected override JObject LoadJson()
        {
            if (!File.Exists(Path))
            {
                return null;
            }

            string json;
            using (TextReader reader = new StreamReader(Path))
            {
                json = reader.ReadToEnd();
            }
            JObject obj = JObject.Parse(json);
            return obj;
        }

        public static string GetFileName(string folder)
        {
            return folder.Trim('\\') + "\\data\\" + FileName;
        }
    }

    public class StorageFrameworkCompatibility : FrameworkCompatibility
    {
        CloudBlockBlob _blob;
        private CloudStorageAccount storageAccount;
        private string frameworksContainer;
        private string path;

        public override string Path { get { return _blob.Uri.AbsoluteUri; } }

        public StorageFrameworkCompatibility(string connectionString)
            : this(CloudStorageAccount.Parse(connectionString))
        {
        }

        public StorageFrameworkCompatibility(CloudStorageAccount storageAccount)
            : this(storageAccount, "ng-search")
        {
        }

        public StorageFrameworkCompatibility(CloudStorageAccount storageAccount, string containerName)
            : this(storageAccount.CreateCloudBlobClient().GetContainerReference(containerName))
        {
        }

        public StorageFrameworkCompatibility(CloudBlobContainer container)
            : this(container.GetBlockBlobReference(@"data/" + FileName))
        {
        }

        public StorageFrameworkCompatibility(CloudBlockBlob blob)
        {
            _blob = blob;
        }

        public StorageFrameworkCompatibility(CloudStorageAccount storageAccount, string containerName, string path)
            : this(storageAccount.CreateCloudBlobClient().GetContainerReference(containerName).GetBlockBlobReference(path))
        {
        }

        protected override JObject LoadJson()
        {
            if (!_blob.Exists())
            {
                return null;
            }
            string json = _blob.DownloadText();
            JObject obj = JObject.Parse(json);
            return obj;
        }
    }
}
