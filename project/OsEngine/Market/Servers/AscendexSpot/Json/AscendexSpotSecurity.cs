﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Market.Servers.AscendexSpot.Json
{
    public class AscendexSpotSecurityResponse
    {
        public string code { get; set; }

        public List<AscendexSpotSecurityData> data { get; set; }
    }

    public class AscendexSpotSecurityData
    {
        public string symbol { get; set; }

        public string displayName { get; set; }

        public string domain { get; set; }

        public string tradingStartTime { get; set; }

        public string collapseDecimals { get; set; }

        public string minQty { get; set; }

        public string maxQty { get; set; }

        public string minNotional { get; set; }

        public string maxNotional { get; set; }

        public string statusCode { get; set; }

        public string statusMessage { get; set; }

        public string tickSize { get; set; }

        public string useTick { get; set; }

        public string lotSize { get; set; }

        public string useLot { get; set; }

        public string commissionType { get; set; }

        public string commissionReserveRate { get; set; }

        public string qtyScale { get; set; }

        public string priceScale { get; set; }

        public string notionalScale { get; set; }
    }
}