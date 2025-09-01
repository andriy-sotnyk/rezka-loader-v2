using System;
using System.Net;
using Newtonsoft.Json;

namespace rezka_loader_v2
{
    internal class Versioning
    {
        public const int CURRENT_VESION_SIGNED = 3;

        public const string CURRENT_VESION = "1.1b";

        private const string MANIFEST_URL = "https://raw.githubusercontent.com/Zaba-web/rezka-loader-v2/refs/heads/main/other/manifest.json";

        public static VersionData CheckUpdate()
        {
            var json = new WebClient().DownloadString(MANIFEST_URL);
            VersionData parsed = JsonConvert.DeserializeObject<VersionData>(json);
            
            if (CURRENT_VESION_SIGNED < parsed.version_sign)
            {
                return parsed;
            }

            return null;
        }
    }
}
