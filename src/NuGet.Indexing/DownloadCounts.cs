using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace NuGet.Indexing
{
    public abstract class DownloadCounts
    {
        public abstract string Path { get; }
        protected abstract JObject LoadJson();

        public IDictionary<int, DownloadCountRecord> Load()
        {
            JObject obj = LoadJson();

            IDictionary<int, DownloadCountRecord> result = new Dictionary<int, DownloadCountRecord>();

            foreach (JProperty prop in obj.Properties())
            {
                dynamic val = prop.Value;
                result.Add(Int32.Parse(prop.Name), new DownloadCountRecord()
                {
                    Downloads = val.Dwn,
                    RegistrationDownloads = val.AllDwn,
                    Installs = val.Inst,
                    Updates = val.Upd
                });
            }

            return result;
        }
    }

    public class DownloadCountRecord
    {
        public int Downloads { get; set; }
        public int RegistrationDownloads { get; set; }
        public int Installs { get; set; }
        public int Updates { get; set; }
    }
}
