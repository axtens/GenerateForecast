using Google.Ads.GoogleAds;
using Google.Ads.GoogleAds.Config;
using Google.Ads.GoogleAds.Lib;
using Google.Ads.GoogleAds.Util;
using Google.Ads.GoogleAds.V14.Enums;
using Google.Ads.GoogleAds.V14.Resources;
using Google.Ads.GoogleAds.V14.Common;
using Google.Ads.GoogleAds.V14.Errors;
using Google.Ads.GoogleAds.V14.Services;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

using GoogleAdsException = Google.Ads.GoogleAds.V14.Errors.GoogleAdsException;
using KeywordPlanIdeaServiceClient = Google.Ads.GoogleAds.V14.Services.KeywordPlanIdeaServiceClient;

namespace GenerateForecast
{
    internal static class Program
    {
        private static string Me => typeof(Program).Assembly.GetName().Name;

        static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Requires settings.json and a credentials json and optional ccid");
                return 1;
            }
            var settingsJson = args[0];
            if (!File.Exists(settingsJson))
            {
                Console.WriteLine($"{settingsJson} not found.");
                return 1;
            }

            var credentialsJson = args[1];
            if (!File.Exists(credentialsJson))
            {
                Console.WriteLine($"{credentialsJson} not found.");
                return 1;
            }

            var settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(settingsJson));
            if (!File.Exists(credentialsJson))
            {
                Console.WriteLine($"{credentialsJson} not found.");
                return 1;
            }

            settings.CcId = args.Length > 2 ? args[2].Replace("-", "") : settings.CcId.Replace("-", "");
            settings.BiddableKeywordsMatchType = args.Length > 3 ? args[3] : settings.BiddableKeywordsMatchType;

            (GoogleAdsClient client, UserCredential _) = Authorise(credentialsJson, settings);
            var (exception, response) = GetUserInterest(client, settings);

            var (ok, body) = Forecast(client, settings);
            if (ok == null)
            {
                var geos = GetGeoTargetConstants(client, settings).response;
                var res = new Result
                {
                    Locations = new Dictionary<long, string>()
                };
                var dets = CampaignDetails(client, settings).response;
                // res.CampaignCriteria = dets;
                foreach (var detail in dets)
                {
                    if (detail.HasNegative && !detail.Negative && detail.CriterionCase == Google.Ads.GoogleAds.V14.Resources.CampaignCriterion.CriterionOneofCase.Location)
                    {
                        var resId = long.Parse(detail.ResourceName.Split('~')[1]);
                        var g = (from geo in geos where geo.Id == resId select geo).FirstOrDefault();
                        res.Locations[resId] = g?.CanonicalName;
                    }
                }
                res.KeywordForecastMetrics = body;

                //var reachPlanService = Reach.GetReachPlanService(client);
                //res.PlannableLocations = Reach.GetPlannableLocations(reachPlanService);
                //res.ProductMetadata = Reach.GetPlannableProducts(reachPlanService, settings.Location.Split('/')[1]);
                //res.PlannedProductReachForecasts = Reach.GetForecastMix(reachPlanService, settings.CcId, settings.Location.Split('/')[1], "AUD", DoubleToMicros(settings.BiddingStrategyDailyBudgetDollars));
                Console.WriteLine(JsonConvert.SerializeObject(res, Formatting.Indented));
                return 0;
            }
            else
            {
                Console.WriteLine(JsonConvert.SerializeObject(ok.Failure, Formatting.Indented));
                return 1;
            }
        }

        private static (object exception, List<Google.Ads.GoogleAds.V14.Resources.UserInterest> response) GetUserInterest(GoogleAdsClient client, Settings settings)
        {
            // Get the GoogleAdsService.
            Google.Ads.GoogleAds.V14.Services.GoogleAdsServiceClient googleAdsService = client.GetService(
                Services.V14.GoogleAdsService);

            // Create a query that will retrieve all campaigns.
            var script = settings.GAQLScript;
            if (!File.Exists(script))
            {
                return (new FileNotFoundException(script), null);
            }
            string query = File.ReadAllText(script);

            try
            {
                var rows = new List<UserInterest>();

                // Issue a search request.
                googleAdsService.SearchStream(settings.CcId, query,
                    (SearchGoogleAdsStreamResponse resp) =>
                    {
                        foreach (GoogleAdsRow googleAdsRow in resp.Results)
                        {
                            //Console.WriteLine("Campaign with ID {0} and name '{1}' was found.",
                            //    googleAdsRow.Campaign.Id, googleAdsRow.Campaign.Name);
                            rows.Add(googleAdsRow.UserInterest);
                        }
                    }
                );
                return (null, rows);
            }
            catch (GoogleAdsException e)
            {
                return (e.Failure, null);
            }
        }

        private static (GoogleAdsFailure exception, List<Google.Ads.GoogleAds.V14.Resources.CampaignCriterion> response) CampaignDetails(GoogleAdsClient client, Settings settings)
        {
            // Get the GoogleAdsService.
            GoogleAdsServiceClient googleAdsService = client.GetService(
                Services.V14.GoogleAdsService);

            // Create a query that will retrieve all campaigns.
            string query = File.ReadAllText(@"C:\Temp\GenerateForecast.gaql");

            try
            {
                // Issue a search request.
                var rows = new List<Google.Ads.GoogleAds.V14.Resources.CampaignCriterion>();
                googleAdsService.SearchStream(settings.CcId, query,
                    (SearchGoogleAdsStreamResponse resp) =>
                    {
                        foreach (GoogleAdsRow googleAdsRow in resp.Results)
                        {
                            //Console.WriteLine("Campaign with ID {0} and name '{1}' was found.",
                            //    googleAdsRow.Campaign.Id, googleAdsRow.Campaign.Name);
                            rows.Add(googleAdsRow.CampaignCriterion);
                        }
                    }
                );
                return (null, rows);
            }
            catch (GoogleAdsException e)
            {
                return (e.Failure, null);
            }
        }

        private static (GoogleAdsFailure exception, List<Google.Ads.GoogleAds.V14.Resources.GeoTargetConstant> response) GetGeoTargetConstants(GoogleAdsClient client, Settings settings)
        {
            const string targetsJson = ".\\geo_target_constants.json";
            if (File.Exists(targetsJson))
            {
                var list = JsonConvert.DeserializeObject<List<Google.Ads.GoogleAds.V14.Resources.GeoTargetConstant>>(File.ReadAllText(targetsJson));
                return (null, list);
            }
            // Get the GoogleAdsService.
            GoogleAdsServiceClient googleAdsService = client.GetService(
                Services.V14.GoogleAdsService);

            // Create a query that will retrieve all campaigns.
            const string query = @"
SELECT 
  geo_target_constant.id,
  geo_target_constant.name,
  geo_target_constant.status,
  geo_target_constant.resource_name,
  geo_target_constant.parent_geo_target,
  geo_target_constant.target_type,
  geo_target_constant.country_code,
  geo_target_constant.canonical_name FROM geo_target_constant 
WHERE 
  geo_target_constant.status = 'ENABLED' 
";

            try
            {
                // Issue a search request.
                var rows = new List<Google.Ads.GoogleAds.V14.Resources.GeoTargetConstant>();
                googleAdsService.SearchStream(settings.CcId, query,
                    (SearchGoogleAdsStreamResponse resp) =>
                    {
                        foreach (GoogleAdsRow googleAdsRow in resp.Results)
                        {
                            //Console.WriteLine("Campaign with ID {0} and name '{1}' was found.",
                            //    googleAdsRow.Campaign.Id, googleAdsRow.Campaign.Name);
                            rows.Add(googleAdsRow.GeoTargetConstant);
                        }
                    }
                );
                File.WriteAllText(targetsJson,JsonConvert.SerializeObject(rows));
                return (null, rows);
            }
            catch (GoogleAdsException e)
            {
                return (e.Failure, null);
            }
        }
        private static long DoubleToMicros(double value) => (long)(value * 1_000_000);

        private static (GoogleAdsException err, KeywordForecastMetrics body) Forecast(GoogleAdsClient authorisation, Settings settings)
        {
            var ctfc = new CampaignToForecastClass();
            ctfc.AddLanguageConstant(settings.Language);
            ctfc.AddGeoModifier(ctfc.CreateCriterionBidModifier(settings.Location));
            ctfc.KeywordPlanNetwork = (Google.Ads.GoogleAds.V14.Enums.KeywordPlanNetworkEnum.Types.KeywordPlanNetwork)ctfc.CreateKeywordPlanNetwork(settings.Network);
            ctfc.BiddingStrategy = ctfc.CreateCampaignBiddingStrategy(
                "ManualCpcBiddingStrategy",
                DoubleToMicros(settings.BiddingStrategyDailyBudgetDollars),
                DoubleToMicros(settings.BiddingStrategyMaxCpcBidDollars));
            ctfc.AddBiddableKeyword(ctfc.CreateBiddableKeyword(
                settings.BiddableKeywordsMatchType,
                settings.BiddableKeywords,
                DoubleToMicros(settings.BiddableKeywordsMaxCpcBidDollars)));
            //ctfc.AddNegativeKeyword(ctfc.CreateKeywordInfo("Broad", "nothing"));
            ctfc.AddForecastAdGroup(
                ctfc.CreateForecastAdGroup(
                    ctfc.BiddableKeywords,
                    ctfc.NegativeKeywords,
                    DoubleToMicros(settings.AdGroupsMaxCpcBidDollar)));
            ctfc.SetConversionRate(settings.ConversionRate);
            var generated = ctfc.Generate();
            var result = GenerateKeywordForecast(authorisation, settings.CcId, CreateDataRange(settings.From, settings.To), generated);
            return (result.exception, result.generated);
        }

        private static (GoogleAdsClient client, UserCredential credential) Authorise(string credentialsJson, Settings settings)
        {
            if (settings.Trace)
            {
                Directory.CreateDirectory(@"C:\Logs");
                TraceUtilities.Configure(
                    TraceUtilities.DETAILED_REQUEST_LOGS_SOURCE,
                    $@"C:\logs\{Me}_{DateTime.Now:yyyy'-'MM'-'dd'-'HH'-'mm'-'ss'-'ff}_trace.log",
                    System.Diagnostics.SourceLevels.All);
            }

            var credentials = JsonConvert.DeserializeObject<Root>(File.ReadAllText(credentialsJson));

            var clientId = credentials.Installed.Client_id;
            var clientSecret = credentials.Installed.Client_secret;
            var devToken = settings.DevToken;

            Task<UserCredential> task = GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets()
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret,
                },
                new string[] { "https://www.googleapis.com/auth/adwords" },
                "user",
                CancellationToken.None,
                new FileDataStore($"{Me}_{Path.GetFileNameWithoutExtension(credentialsJson)}", false));

            UserCredential credential = task.Result;

            var client = new GoogleAdsClient(new GoogleAdsConfig
            {
                OAuth2RefreshToken = credential.Token.RefreshToken,
                DeveloperToken = devToken,
                LoginCustomerId = settings.LoginCustomerId.Replace("-", ""),
                OAuth2ClientId = clientId,
                OAuth2ClientSecret = clientSecret
            });

            return (client, credential);
        }

        public static DateRange CreateDataRange(string start, string end)
        {
            DateRange dateRange = new DateRange
            {
                StartDate = start,
                EndDate = end
            };
            return dateRange;
        }

        public static (GoogleAdsException exception, KeywordForecastMetrics generated) GenerateKeywordForecast(GoogleAdsClient client, string ccid, DateRange dateRange, CampaignToForecast ctfc, bool debug = false)
        {
            if (debug) Debugger.Launch();

            KeywordPlanIdeaServiceClient keywordPlanIdeaService =
                client.GetService(Services.V14.KeywordPlanIdeaService);

            var req = new GenerateKeywordForecastMetricsRequest
            {
                Campaign = ctfc,
                ForecastPeriod = dateRange,
                CustomerId = ccid.Replace("-", "")
            };

            try
            {
                // Generate keyword ideas based on the specified parameters.
                GenerateKeywordForecastMetricsResponse generateKeywordIdeaResults = keywordPlanIdeaService.GenerateKeywordForecastMetrics(req);
                return (null, generateKeywordIdeaResults.CampaignForecastMetrics);
            }
            catch (GoogleAdsException e)
            {
                return (e, null);
            }
        }
    }
}
