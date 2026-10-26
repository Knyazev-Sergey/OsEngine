using System.Collections.Generic;

namespace OsEngine.Market.Servers.Atp.Entities
{
    public class MassiveWebSocketMessage
    {
        public string ev { get; set; }
        public string status { get; set; }
        public string message { get; set; }
        public string sym { get; set; }
        public string p { get; set; }
        public string s { get; set; }
        public string t { get; set; }
        public string bp { get; set; }
        public string bs { get; set; }
        public string ap { get; set; }
        public string @as { get; set; }
    }

    public class MassiveWebSocketMessageList : List<MassiveWebSocketMessage>
    {
    }
}
