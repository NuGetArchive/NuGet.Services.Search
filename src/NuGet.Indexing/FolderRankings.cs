using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Indexing
{
    public class FolderRankings : Rankings
    {
        string _folder;

        public FolderRankings(string folder)
        {
            _folder = folder;
        }

        protected override JObject LoadJson()
        {
            string json;
            using (TextReader reader = new StreamReader(_folder.Trim('\\') + "\\data\\rankings.v1.json"))
            {
                json = reader.ReadToEnd();
            }
            JObject obj = JObject.Parse(json);
            return obj;
        }
    }
}
