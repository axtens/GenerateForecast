using Google.Ads.GoogleAds;
using Google.Ads.GoogleAds.Lib;
using Google.Ads.GoogleAds.V14.Common;
using Google.Ads.GoogleAds.V14.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static Google.Ads.GoogleAds.V14.Enums.DeviceEnum.Types;
using static Google.Ads.GoogleAds.V14.Enums.GenderTypeEnum.Types;
using static Google.Ads.GoogleAds.V14.Enums.ReachPlanAgeRangeEnum.Types;

namespace GenerateForecast
{
    internal static class Reach
    {
        internal static ReachPlanServiceClient GetReachPlanService(GoogleAdsClient client) => client.GetService(Services.V14.ReachPlanService);

        internal static List<PlannableLocation> GetPlannableLocations(ReachPlanServiceClient reachPlanService)
        {
            var list = new List<PlannableLocation>();
            ListPlannableLocationsRequest request = new ListPlannableLocationsRequest();
            ListPlannableLocationsResponse response = reachPlanService.ListPlannableLocations(
                request);
            list.AddRange(response.PlannableLocations);
            return list;
        }

        internal static List<ProductMetadata> GetPlannableProducts(ReachPlanServiceClient reachPlanService, string locationId)
        {
            ListPlannableProductsRequest request = new ListPlannableProductsRequest
            {
                PlannableLocationId = locationId
            };
            ListPlannableProductsResponse response = reachPlanService.ListPlannableProducts(
                request);

            var list = new List<ProductMetadata>();

            // Console.WriteLine($"Plannable Products for location {locationId}:");
            foreach (ProductMetadata product in response.ProductMetadata)
            {
                /*Console.WriteLine($"{product.PlannableProductCode}:");
                Console.WriteLine("Age Ranges:");
                foreach (ReachPlanAgeRange ageRange in product.PlannableTargeting.AgeRanges)
                {
                    Console.WriteLine($"\t- {ageRange}");
                }

                Console.WriteLine("Genders:");
                foreach (GenderInfo gender in product.PlannableTargeting.Genders)
                {
                    Console.WriteLine($"\t- {gender.Type}");
                }

                Console.WriteLine("Devices:");
                foreach (DeviceInfo device in product.PlannableTargeting.Devices)
                {
                    Console.WriteLine($"\t- {device.Type}");
                }*/
                list.Add(product);
            }
            return list;
        }

        internal static List<PlannedProductReachForecast> GetForecastMix(ReachPlanServiceClient reachPlanService, string customerId, string locationId, string currencyCode, long budgetMicros)
        {
            List<PlannedProduct> productMix = new List<PlannedProduct>();

            // Set up a ratio to split the budget between two products.
            const double trueviewAllocation = 0.15;
            const double bumperAllocation = 1 - trueviewAllocation;

            // See listPlannableProducts on ReachPlanService to retrieve a list
            // of valid PlannableProductCode's for a given location:
            // https://developers.google.com/google-ads/api/reference/rpc/latest/ReachPlanService
            productMix.Add(new PlannedProduct
            {
                PlannableProductCode = "TRUEVIEW_IN_STREAM",
                BudgetMicros = Convert.ToInt64(budgetMicros * trueviewAllocation)
            });
            productMix.Add(new PlannedProduct
            {
                PlannableProductCode = "BUMPER",
                BudgetMicros = Convert.ToInt64(budgetMicros * bumperAllocation)
            });

            GenerateReachForecastRequest request =
                BuildReachRequest(customerId, productMix, locationId, currencyCode);

            var list = new List<PlannedProductReachForecast>();

            GetReachCurve(reachPlanService, request, ref list);

            return list;
        }

        private static void GetReachCurve(ReachPlanServiceClient reachPlanService, GenerateReachForecastRequest request, ref List<PlannedProductReachForecast> list)
        {
            GenerateReachForecastResponse response = reachPlanService.GenerateReachForecast(
                request);
            //Console.WriteLine("Reach curve output:");
            //Console.WriteLine(
            //    "Currency, Cost Micros, On-Target Reach, On-Target Impressions, Total Reach," +
            //    " Total Impressions, Products");
            foreach (ReachForecast point in response.ReachCurve.ReachForecasts)
            {
                /*Console.Write($"{request.CurrencyCode}, ");
                Console.Write($"{point.CostMicros}, ");
                Console.Write($"{point.Forecast.OnTargetReach}, ");
                Console.Write($"{point.Forecast.OnTargetImpressions}, ");
                Console.Write($"{point.Forecast.TotalReach}, ");
                Console.Write($"{point.Forecast.TotalImpressions}, ");
                Console.Write("\"[");
                */
                foreach (PlannedProductReachForecast productReachForecast in
                    point.PlannedProductReachForecasts)
                {
                    //Console.Write($"(Product: {productReachForecast.PlannableProductCode}, ");
                    //Console.Write($"Budget Micros: {productReachForecast.CostMicros}), ");
                    list.Add(productReachForecast);
                }
                /*
                Console.WriteLine("]\"");*/

            }
        }

        private static GenerateReachForecastRequest BuildReachRequest(string customerId, List<PlannedProduct> productMix, string locationId, string currencyCode)
        {
            // Valid durations are between 1 and 90 days.
            CampaignDuration duration = new CampaignDuration
            {
                DurationInDays = 28
            };

            GenderInfo[] genders =
            {
                new GenderInfo {Type = GenderType.Female},
                new GenderInfo {Type = GenderType.Male}
            };

            DeviceInfo[] devices =
            {
                new DeviceInfo {Type = Device.Desktop},
                new DeviceInfo {Type = Device.Mobile},
                new DeviceInfo {Type = Device.Tablet}
            };

            Targeting targeting = new Targeting
            {
                PlannableLocationId = locationId,
                AgeRange = ReachPlanAgeRange.AgeRange1865Up,
            };
            targeting.Genders.AddRange(genders);
            targeting.Devices.AddRange(devices);

            // See the docs for defaults and valid ranges:
            // https://developers.google.com/google-ads/api/reference/rpc/latest/GenerateReachForecastRequest
            GenerateReachForecastRequest request = new GenerateReachForecastRequest
            {
                CustomerId = customerId,
                CurrencyCode = currencyCode,
                CampaignDuration = duration,
                Targeting = targeting,
                MinEffectiveFrequency = 1
            };

            request.PlannedProducts.AddRange(productMix);

            return request;
        }
    }
}
