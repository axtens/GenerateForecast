using Google.Ads.GoogleAds.V14.Common;
using Google.Ads.GoogleAds.V14.Services;

using System;
using System.Collections.Generic;

using static Google.Ads.GoogleAds.V14.Enums.KeywordMatchTypeEnum.Types;
using static Google.Ads.GoogleAds.V14.Enums.KeywordPlanNetworkEnum.Types;
using static Google.Ads.GoogleAds.V14.Services.CampaignToForecast.Types;

namespace GenerateForecast
{
    public class CampaignToForecastClass
    {
        public List<LanguageInfo> LanguageConstants { get; set; }
        public List<CriterionBidModifier> GeoModifiers { get; set; }
        public KeywordPlanNetwork KeywordPlanNetwork { get; set; }
        public List<KeywordInfo> NegativeKeywords { get; set; }
        public List<BiddableKeyword> BiddableKeywords { get; set; }
        public CampaignBiddingStrategy BiddingStrategy { get; set; }
        public List<ForecastAdGroup> AdGroups { get; set; }
        public double ConversionRate { get; set; }

        public CampaignToForecastClass()
        {
            LanguageConstants = new List<LanguageInfo>();
            GeoModifiers = new List<CriterionBidModifier>();
            NegativeKeywords = new List<KeywordInfo>();
            BiddableKeywords = new List<BiddableKeyword>();
            AdGroups = new List<ForecastAdGroup>();
        }

        public CampaignToForecastClass AddLanguageConstant(string languageConstant)
        {
            LanguageConstants.Add(new LanguageInfo
            {
                LanguageConstant = languageConstant
            });
            return this;
        }

        public CriterionBidModifier CreateCriterionBidModifier(string geoTargetConstant, double? bidModifier = null)
        {
            CriterionBidModifier cbm = new CriterionBidModifier();
            if (bidModifier != null)
            {
                cbm.BidModifier = (double)bidModifier;
            }
            cbm.GeoTargetConstant = geoTargetConstant;
            return cbm;
        }

        public CampaignToForecastClass AddGeoModifier(CriterionBidModifier criterionBidModifier)
        {
            GeoModifiers.Add(criterionBidModifier);
            return this;
        }

        public KeywordPlanNetwork? CreateKeywordPlanNetwork(string keywordPlanNetwork)
        {
            bool flag = Enum.TryParse<KeywordPlanNetwork>(keywordPlanNetwork, out KeywordPlanNetwork kpn);
            if (flag)
            {
                return kpn;
            }
            return null;
        }

        public KeywordInfo CreateKeywordInfo(string keywordMatchType, string text)
        {
            bool flag = Enum.TryParse<KeywordMatchType>(keywordMatchType, out KeywordMatchType kmt);
            if (flag)
            {
                KeywordInfo keywordInfo = new KeywordInfo
                {
                    Text = text,
                    MatchType = (KeywordMatchType)kmt
                };
                return keywordInfo;
            }
            return null;
        }

        public BiddableKeyword CreateBiddableKeyword(string keywordMatchType, string text, long maxCpcBidMicros) => new BiddableKeyword
        {
            Keyword = CreateKeywordInfo(keywordMatchType, text),
            MaxCpcBidMicros = maxCpcBidMicros
        };

        public CampaignToForecastClass AddBiddableKeyword(BiddableKeyword biddableKeywordInfo)
        {
            BiddableKeywords.Add(biddableKeywordInfo);
            return this;
        }
        public CampaignToForecastClass AddNegativeKeyword(KeywordInfo negativeKeywordInfo)
        {
            NegativeKeywords.Add(negativeKeywordInfo);
            return this;
        }

        public CampaignBiddingStrategy CreateCampaignBiddingStrategy(string strategy, long a, long b)
        {
            var cbs = new CampaignBiddingStrategy();
            cbs.ClearBiddingStrategy();

            switch (strategy.ToLower())
            {
                case "manualcpcbiddingstrategy":
                    var mcbs = new ManualCpcBiddingStrategy
                    {
                        MaxCpcBidMicros = a,
                        DailyBudgetMicros = b
                    };
                    cbs.ManualCpcBiddingStrategy = mcbs;
                    break;
                case "maximizeclicksbiddingstrategy":
                    var xcbs = new MaximizeClicksBiddingStrategy
                    {
                        MaxCpcBidCeilingMicros = a,
                        DailyTargetSpendMicros = b
                    };
                    cbs.MaximizeClicksBiddingStrategy = xcbs;
                    break;
                case "maximizeconversionsbiddingstrategy":
                    var icsb = new MaximizeConversionsBiddingStrategy
                    {
                        DailyTargetSpendMicros = a
                    };
                    cbs.MaximizeConversionsBiddingStrategy = icsb;
                    break;
            }
            return cbs;
        }

        public ForecastAdGroup CreateForecastAdGroup(List<BiddableKeyword> biddableKeywords, List<KeywordInfo> negativeKeywords, long maxCpcBidMicros)
        {
            var forecastAdGroup = new ForecastAdGroup
            {
                MaxCpcBidMicros = maxCpcBidMicros
            };
            forecastAdGroup.BiddableKeywords.AddRange(biddableKeywords.ToArray());
            forecastAdGroup.NegativeKeywords.AddRange(negativeKeywords.ToArray());

            return forecastAdGroup;
        }
        public CampaignToForecastClass AddForecastAdGroup(ForecastAdGroup forecastAdGroup)
        {
            AdGroups.Add(forecastAdGroup);
            return this;
        }

        public CampaignToForecast Generate()
        {
            var ctf = new CampaignToForecast
            {
                BiddingStrategy = BiddingStrategy,
                ConversionRate = ConversionRate,
                KeywordPlanNetwork = KeywordPlanNetwork
            };
            ctf.GeoModifiers.AddRange(GeoModifiers.ToArray());
            //ctf.LanguageConstants.AddRange(LanguageConstants.ToArray());
            foreach (var lc in LanguageConstants)
            {
                ctf.LanguageConstants.Add(lc.LanguageConstant);
            }
            ctf.NegativeKeywords.AddRange(NegativeKeywords.ToArray());
            ctf.AdGroups.AddRange(AdGroups.ToArray());
            return ctf;
        }
    }
}