using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Google.Ads.GoogleAds.V12.Services;
using Google.Ads.GoogleAds.Util;
using System.Diagnostics;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using System.Threading;
using Google.Ads.GoogleAds.Config;
using Google.Ads.GoogleAds.Lib;

namespace GenerateForecast
{
    internal static class Program
    {
        private static string Me => new StackTrace().GetFrame(1).GetMethod().Name;

        static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Requires credentials.json and settings.json");
                return 1;
            }
            var credentialsJson = args[0];
            var settingsJson = args[1];

            if (!File.Exists(credentialsJson))
            {
                Console.WriteLine($"{credentialsJson} not found.");
                return 1;
            }

            if (!File.Exists(settingsJson))
            {
                Console.WriteLine($"{settingsJson} not found.");
                return 1;
            }

            var credentials = JsonConvert.DeserializeObject<Root>(File.ReadAllText(credentialsJson));
            var settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(settingsJson));

            var authorisation = Authorise(credentials, settings);
            var report = Forecast(authorisation, settings);
            if (report.ok)
                Console.WriteLine(JsonConvert.SerializeObject(report.body));

            return 0;
        }

        private static (bool ok, object body) Forecast(object authorisation, Settings settings)
        {
            throw new NotImplementedException();
        }

        private static (GoogleAdsClient client, UserCredential credential) Authorise(Root credentials, Settings settings)
        {
            if (settings.Trace)
            {
                Directory.CreateDirectory(@"C:\Logs");
                TraceUtilities.Configure(TraceUtilities.DETAILED_REQUEST_LOGS_SOURCE, $@"C:\logs\{Me}_{System.Guid.NewGuid()}_trace.log", System.Diagnostics.SourceLevels.All);
            }

            var clientId = credentials.Installed.Client_id;
            var clientSecret = credentials.Installed.Client_secret;
            var devToken = settings.DevToken;

            var secrets = new ClientSecrets()
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
            };

            Task<UserCredential> task = GoogleWebAuthorizationBroker.AuthorizeAsync(
                secrets,
                new string[] { "https://www.googleapis.com/auth/adwords" },
                "user",
                CancellationToken.None,
                new FileDataStore("GenerateForecast", false));
            UserCredential credential = task.Result;

            var config = new GoogleAdsConfig
            {
                OAuth2RefreshToken = credential.Token.RefreshToken,
                DeveloperToken = devToken,
                LoginCustomerId = settings.AccountId,
                OAuth2ClientId = clientId,
                OAuth2ClientSecret = clientSecret
            };

            var client = new GoogleAdsClient(config);
            
            return (client, credential);
        }
    }
}
