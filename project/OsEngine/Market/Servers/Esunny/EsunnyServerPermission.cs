namespace OsEngine.Market.Servers.Esunny
{
    public class EsunnyServerPermission : IServerPermission
    {
        public ServerType ServerType => ServerType.Esunny;

        public bool DataFeedTf1SecondCanLoad => false;
        public bool DataFeedTf2SecondCanLoad => false;
        public bool DataFeedTf5SecondCanLoad => false;
        public bool DataFeedTf10SecondCanLoad => false;
        public bool DataFeedTf15SecondCanLoad => false;
        public bool DataFeedTf20SecondCanLoad => false;
        public bool DataFeedTf30SecondCanLoad => false;
        public bool DataFeedTf1MinuteCanLoad => true;
        public bool DataFeedTf2MinuteCanLoad => true;
        public bool DataFeedTf5MinuteCanLoad => true;
        public bool DataFeedTf10MinuteCanLoad => true;
        public bool DataFeedTf15MinuteCanLoad => true;
        public bool DataFeedTf30MinuteCanLoad => true;
        public bool DataFeedTf1HourCanLoad => true;
        public bool DataFeedTf2HourCanLoad => true;
        public bool DataFeedTf4HourCanLoad => true;
        public bool DataFeedTfDayCanLoad => true;
        public bool DataFeedTfTickCanLoad => true;
        public bool DataFeedTfMarketDepthCanLoad => true;

        public bool MarketOrdersIsSupport => true;
        public bool IsCanChangeOrderPrice => false;
        public bool IsUseLotToCalculateProfit => true;
        public TimeFramePermission TradeTimeFramePermission => null;
        public int WaitTimeSecondsAfterFirstStartToSendOrders => 2;
        public bool UseStandardCandlesStarter => true;
        public bool ManuallyClosePositionOnBoard_IsOn => false;
        public string[] ManuallyClosePositionOnBoard_ValuesForTrimmingName => null;
        public string[] ManuallyClosePositionOnBoard_ExceptionPositionNames => null;
        public bool CanQueryOrdersAfterReconnect => false;
        public bool CanQueryOrderStatus => false;
        public bool CanGetOrderLists => false;
        public bool HaveOnlyMakerLimitsRealization => false;

        public bool IsNewsServer => false;
        public bool IsSupports_CheckDataFeedLogic => false;
        public string[] CheckDataFeedLogic_ExceptionSecuritiesClass => null;
        public int CheckDataFeedLogic_NoDataMinutesToDisconnect => 0;
        public bool IsSupports_MultipleInstances => false;
        public bool IsSupports_ProxyFor_MultipleInstances => false;
        public bool IsSupports_AsyncOrderSending => false;
        public int AsyncOrderSending_RateGateLimitMls => 0;
        public bool IsSupports_AsyncCandlesStarter => false;
        public int AsyncCandlesStarter_RateGateLimitMls => 0;
        public string[] IpAddressServer => null;
        public bool Leverage_IsSupports => false;
        public decimal Leverage_StandardValue => 1;
        public string[] Leverage_SupportClasses => null;
        public bool CanChangeOrderMarketNumber => false;
        public OrderLifeTimePermission OrdersLifeTimeRealization => null;
    }
}
