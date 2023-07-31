using Google.Ads.GoogleAds;
using Google.Ads.GoogleAds.Config;
using Google.Ads.GoogleAds.Lib;
using Google.Ads.GoogleAds.Util;
using Google.Ads.GoogleAds.V14.Common;
using Google.Ads.GoogleAds.V14.Services;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;

using Newtonsoft.Json;

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
                Console.WriteLine("Requires settings.json and a credentials json");
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

            (GoogleAdsClient client, UserCredential _) = Authorise(credentialsJson, settings);
            var (ok, body) = Forecast(client, settings);
            if (ok == null)
            {
                Console.WriteLine(JsonConvert.SerializeObject(body));
                return 0;
            }
            else
            {
                Console.WriteLine(JsonConvert.SerializeObject(ok.Failure));
                return 1;
            }
        }

        private static (GoogleAdsException err, KeywordForecastMetrics body) Forecast(GoogleAdsClient authorisation, Settings settings)
        {
            var ctfc = new CampaignToForecastClass();
            ctfc.AddLanguageConstant(settings.Language);
            ctfc.AddGeoModifier(ctfc.CreateCriterionBidModifier(settings.Location));
            ctfc.KeywordPlanNetwork = (Google.Ads.GoogleAds.V14.Enums.KeywordPlanNetworkEnum.Types.KeywordPlanNetwork)ctfc.CreateKeywordPlanNetwork(settings.Network);
            ctfc.BiddingStrategy = ctfc.CreateCampaignBiddingStrategy("ManualCpcBiddingStrategy", 500 * 1_000_000, 100_000L * 1_000_000);
            ctfc.AddBiddableKeyword(ctfc.CreateBiddableKeyword("Exact", settings.Keywords, 100_000L * 1_000_000));
            //ctfc.AddNegativeKeyword(ctfc.CreateKeywordInfo("Broad", "nothing"));
            ctfc.AddForecastAdGroup(ctfc.CreateForecastAdGroup(ctfc.BiddableKeywords, ctfc.NegativeKeywords, 10_000L * 1_000_000));
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
                LoginCustomerId = settings.LoginCustomerId.Replace("-",""),
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

        public static (Google.Ads.GoogleAds.V14.Errors.GoogleAdsException exception, KeywordForecastMetrics generated) GenerateKeywordForecast(GoogleAdsClient client, string ccid, DateRange dateRange, CampaignToForecast ctfc, bool debug = false)
        {
            if (debug) Debugger.Launch();

            KeywordPlanIdeaServiceClient keywordPlanIdeaService =
                client.GetService(Services.V14.KeywordPlanIdeaService);

            var req = new GenerateKeywordForecastMetricsRequest
            {
                Campaign = ctfc,
                ForecastPeriod = dateRange,
                CustomerId = ccid.Replace("-","")
            };

            try
            {
                // Generate keyword ideas based on the specified parameters.
                var response =
                    keywordPlanIdeaService.GenerateKeywordForecastMetrics(req);

                GenerateKeywordForecastMetricsResponse generateKeywordIdeaResults = response;
                return (null, generateKeywordIdeaResults.CampaignForecastMetrics);
            }
            catch (GoogleAdsException e)
            {
                return (e, null);
            }
        }
    }
}
