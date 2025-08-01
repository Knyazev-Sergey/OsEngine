﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Market.Servers.AscendexSpot.Json
{

    class AscendexSpotDepthResponse
    {
        public string m { get; set; } // "depth-snapshot"

        public string symbol { get; set; }

        public AscendexSpotDepthtData data { get; set; }
    }

    class AscendexSpotDepthtData
    {
        public string seqnum { get; set; }

        public string ts { get; set; }

        public List<List<string>> asks { get; set; }

        public List<List<string>> bids { get; set; }
    }
}
