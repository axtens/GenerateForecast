using Newtonsoft.Json;

using System.Collections.Generic;

namespace GenerateForecast
{
    public class Installed
    {
        [JsonProperty("client_id")]
        public string Client_id { get; set; }

        [JsonProperty("project_id")]
        public string Project_id { get; set; }

        [JsonProperty("auth_uri")]
        public string Auth_uri { get; set; }

        [JsonProperty("token_uri")]
        public string Token_uri { get; set; }

        [JsonProperty("auth_provider_x509_cert_url")]
        public string Auth_provider_x509_cert_url { get; set; }

        [JsonProperty("client_secret")]
        public string Client_secret { get; set; }

        [JsonProperty("redirect_uris")]
        public List<string> Redirect_uris { get; set; }
    }

    public class Root
    {
        [JsonProperty("installed")]
        public Installed Installed { get; set; }
    }
}
