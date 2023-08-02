using Google.Ads.GoogleAds.V14.Services;

using Newtonsoft.Json;

using System.Collections.Generic;

namespace GenerateForecast
{
    public class Settings
    {
        [JsonProperty("login-customer-id")]
        public string LoginCustomerId { get; set; }

        [JsonProperty("ccid")]
        public string CcId { get; set; }

        [JsonProperty("from")]
        public string From { get; set; }

        [JsonProperty("language")]
        public string Language { get; set; }

        [JsonProperty("location")]
        public string Location { get; set; }

        [JsonProperty("network")]
        public string Network { get; set; }

        [JsonProperty("to")]
        public string To { get; set; }

        [JsonProperty("devtoken")]
        public string DevToken { get; set; }

        [JsonProperty("trace")]
        public bool Trace { get; set; }

        [JsonProperty("biddingStrategyDailyBudgetDollars")]
        public double BiddingStrategyDailyBudgetDollars { get; set; }

        [JsonProperty("biddingStrategyMaxCpcBidDollars")]
        public double BiddingStrategyMaxCpcBidDollars { get; set; }

        [JsonProperty("conversionRate")]
        public double ConversionRate { get; set; }

        [JsonProperty("adGroupsMaxCpcBidDollar")]
        public double AdGroupsMaxCpcBidDollar { get; set; }

        [JsonProperty("biddableKeywords")]
        public string BiddableKeywords { get; set; }

        [JsonProperty("biddableKeywordsMaxCpcBidDollars")]
        public double BiddableKeywordsMaxCpcBidDollars { get; set; }

        [JsonProperty("biddableKeywordsMatchType")]
        public string BiddableKeywordsMatchType { get; set; }
    }

    public class Result
    {
        public KeywordForecastMetrics KeywordForecastMetrics { get; set; }
        public Dictionary<long,string> Locations { get; set; }
    }
}
