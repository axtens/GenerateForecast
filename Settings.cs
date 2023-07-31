using Newtonsoft.Json;

namespace GenerateForecast
{
    public class Settings
    {
        [JsonProperty("login-customer-id")]
        public string LoginCustomerId { get; set; }

        [JsonProperty("ccid")]
        public string CcId { get; set; }

        [JsonProperty("keywords")]
        public string Keywords { get; set; }

        [JsonProperty("language")]
        public string Language { get; set; }

        [JsonProperty("location")]
        public string Location { get; set; }

        [JsonProperty("network")]
        public string Network { get; set; }

        [JsonProperty("from")]
        public string From { get; set; }

        [JsonProperty("to")]
        public string To { get; set; }

        [JsonProperty("trace")]
        public bool Trace { get; set; }

        [JsonProperty("devtoken")]
        public string DevToken { get; set; }
    }
}
