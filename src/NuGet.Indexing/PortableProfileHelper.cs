using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace NuGet.Indexing
{
    public static class PortableProfileHelper
    {
        public static NetPortableProfileTable FromFolder(string indexFolder)
        {
            string profileData = Path.Combine(indexFolder, "data/profiles.v1.json");
            NetPortableProfileTable portableProfileTable;
            if (File.Exists(profileData))
            {
                using (var stream = new FileStream(profileData, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    return NetPortableProfileTable.Deserialize(stream);
                }
            }
            else
            {
                return NetPortableProfileTable.Default;
            }
        }

        public static NetPortableProfileTable FromStorage(CloudStorageAccount account, string containerName)
        {
            return FromStorage(account.CreateCloudBlobClient().GetContainerReference(containerName));
        }

        public static NetPortableProfileTable FromStorage(CloudBlobContainer container)
        {
            return FromStorage(container.GetBlockBlobReference("data/profiles.v1.json"));
        }

        public static NetPortableProfileTable FromStorage(CloudBlockBlob blob)
        {
            throw new NotImplementedException();
        }
    }
}
