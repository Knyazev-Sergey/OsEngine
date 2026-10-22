using Grpc.Core;
using OsEngine.Entity;
using OsEngine.Logging;
using OsEngine.Market;
using OsEngine.Market.Servers;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using System.Threading;

namespace OsEngine.Robots.AlexBots
{
    [Bot("TwapBot")]
    public class TwapBot : BotPanel
    {
        #region Constructor

        public TwapBot(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            BotTab = TabsSimple[0];

            this.DeleteEvent += TwapBot_DeleteEvent;

            BotTab.ManualPositionSupport.DisableManualSupport();

            Thread thread = new Thread(MainThread);
            thread.Start();
        }

        #endregion

        #region Parameters

        private bool _deleteBot = false;

        public decimal SlipageValue;

        public decimal RestrictionPrice;

        public TypeRestriction TypeRestriction;

        public bool IsMarketOrder = false;

        public QuantityVolume VolumeType;

        public decimal NeedQuantity;

        public decimal NeedVolume;

        public decimal NeedVolumeInLot;

        public decimal OpenVolumeInLot;

        public DateTime TimeServerCurrent;

        public Position CurrentPosition;

        public DateTime StartTime = DateTime.UtcNow.AddHours(3);

        public DateTime EndTime = DateTime.UtcNow.AddHours(3);

        public bool StartWithCurrentTime;

        public IntervalType IntervalParameters;

        public int QuantityIterations;

        public int IterationTime;

        public int VolumeRange = 0;

        public bool UseLog;

        public decimal AveragePrice;

        public BotTabSimple BotTab;
        private StrategyParameterButton _regime;
        public BotMode NeedTwapMode = BotMode.Off;

        public BotMode CurrentTwapMode = BotMode.Off;

        public OrderDirection OrderDirection = OrderDirection.Buy;

        public List<ArbitrationStep> ArbitrationStep;

        public event Action ArbitrationStepUpdateEvent;

        public event Action TimeUpdateEvent;

        private ArbitrationStep CurrentStep;

        public int NumberTwapStatistics = 0;

        public TwapStatistic TwapActiveStatistics;

        public List<TwapStatistic> TwapFinishStatistics = new List<TwapStatistic>();

        public List<TwapStatistic> TwapCancelStatistics = new List<TwapStatistic>();

        #endregion

        #region Main thread

        private void MainThread()
        {
            while (!_deleteBot)
            {
                try
                {
                    Thread.Sleep(1000);

                    TimeServerCurrent = DateTime.UtcNow.AddHours(ShiftTime);

                    if (NeedTwapMode == BotMode.Start && CurrentTwapMode == BotMode.Pause)
                    {
                        CurrentTwapMode = BotMode.Start;
                        NeedRecalculateArbitrationStep = true;
                    }
                    else if (NeedTwapMode == BotMode.Start && CurrentTwapMode == BotMode.Off)
                    {
                        CurrentTwapMode = BotMode.Start;

                        ArbitrationStep = null;
                        OpenVolumeInLot = 0;
                        NeedVolumeInLot = 0;
                        AveragePrice = 0;

                        NumberTwapStatistics++;

                        ArbitrationStepUpdateEvent?.Invoke();
                    }
                    else if (NeedTwapMode == BotMode.Pause && CurrentTwapMode == BotMode.Start)
                    {
                        CurrentTwapMode = BotMode.Pause;
                    }
                    else if (NeedTwapMode == BotMode.Off && (CurrentTwapMode == BotMode.Start || CurrentTwapMode == BotMode.Pause))
                    {
                        if (IsCancelTwapOrder == true)
                        {
                            IsCancelTwapOrder = false;
                            TwapActiveStatistics = null;
                            TwapCancelStatistics.Add(CreateTwapStatistic());
                        }

                        CurrentTwapMode = BotMode.Off;

                        ArbitrationStep = null;
                        OpenVolumeInLot = 0;
                        NeedVolumeInLot = 0;
                        AveragePrice = 0;

                        ArbitrationStepUpdateEvent?.Invoke();
                    }

                    if (CurrentTwapMode == BotMode.Off || CurrentTwapMode == BotMode.Pause)
                    {
                        if (StartWithCurrentTime)
                        {
                            StartTime = TimeServerCurrent;
                        }
                    }

                    TimeUpdateEvent?.Invoke();

                    if (BotTab.IsReadyToTrade == false)
                    {
                        this.SendNewLogMessage("Бумага недоступна для торговли", LogMessageType.System);
                        continue;
                    }
                    else if (BotTab.Securiti == null)
                    {
                        this.SendNewLogMessage("Бумага недоступна для торговли", LogMessageType.System);
                        continue;
                    }

                    if (CurrentTwapMode == BotMode.Off || CurrentTwapMode == BotMode.Pause)
                    {
                        if (CurrentStep != null)
                        {
                            CheckStepStatus(ref CurrentStep, isLastStep: false);

                            if (CurrentStep.OrderStep != null &&
                                (CurrentStep.OrderStep.State == OrderStateType.Active || CurrentStep.OrderStep.State == OrderStateType.Partial))
                            {
                                CancelOrder(CurrentStep);
                            }
                        }

                        continue;
                    }

                    if (TimeServerCurrent >= EndTime)
                    {
                        if (CurrentTwapMode != BotMode.Off)
                        {
                            this.SendNewLogMessage($"Наступило время окончания. Бот выключен.", LogMessageType.Error);

                            TwapActiveStatistics = null;

                            TwapFinishStatistics.Add(CreateTwapStatistic());

                            NeedTwapMode = BotMode.Off;

                            ArbitrationStepUpdateEvent?.Invoke();
                        }

                        if (CurrentStep != null)
                        {
                            CheckStepStatus(ref CurrentStep, isLastStep: true);

                            if (CurrentStep.OrderStep != null &&
                                (CurrentStep.OrderStep.State == OrderStateType.Active || CurrentStep.OrderStep.State == OrderStateType.Partial))
                            {
                                CancelOrder(CurrentStep);
                            }
                        }

                        continue;
                    }

                    if (RestrictionPrice != 0 &&
                        (OrderDirection == OrderDirection.Buy && BotTab.PriceBestAsk > RestrictionPrice))
                    {
                        this.SendNewLogMessage($"Цена вышла за допустимое ограничение. Ограничение: {RestrictionPrice}, Цена: {BotTab.PriceBestAsk}. Бот перешел в режим паузы.", LogMessageType.Error);
                        NeedTwapMode = BotMode.Pause;
                        continue;
                    }
                    else if (RestrictionPrice != 0 &&
                        (OrderDirection == OrderDirection.Sell && BotTab.PriceBestBid < RestrictionPrice))
                    {
                        this.SendNewLogMessage($"Цена вышла за допустимое ограничение. Ограничение: {RestrictionPrice}, Цена: {BotTab.PriceBestBid}. Бот перешел в режим паузы.", LogMessageType.Error);
                        NeedTwapMode = BotMode.Pause;
                        continue;
                    }

                    // рассчитываем объемы

                    if (ArbitrationStep == null || NeedRecalculateArbitrationStep)
                    {
                        if (BotTab.PriceBestAsk == 0)
                        {
                            this.SendNewLogMessage($"По бумаге нет стакана. Бот не торгует.", LogMessageType.Error);
                            continue;
                        }

                        CheckVolumes();

                        if (NeedRecalculateArbitrationStep)
                        {
                            NeedRecalculateArbitrationStep = false;
                        }
                    }

                    // торговая логика

                    TradeLogic();
                }
                catch (Exception ex)
                {
                    this.SendNewLogMessage(ex.ToString(), LogMessageType.Error);
                    Thread.Sleep(5000);
                }
            }
        }

        private void CheckVolumes()
        {
            try
            {
                if (ArbitrationStep == null)
                {
                    ArbitrationStep = new List<ArbitrationStep>();
                }

                TimeSpan duration = EndTime.TimeOfDay - StartTime.TimeOfDay;
                Random random = new Random();

                if (VolumeType == QuantityVolume.Volume && IntervalParameters == IntervalType.IterationTime)
                {
                    NeedVolumeInLot = GetVolume();
                    (int stepsCount, TimeSpan lifetime) = GetStepParameters(IntervalType.IterationTime, duration);
                    List<ArbitrationStep> steps = GenerateArbitrationSteps(NeedVolumeInLot, stepsCount, lifetime, VolumeRange, random);
                    ArbitrationStep.AddRange(steps);
                }
                else if (VolumeType == QuantityVolume.Volume && IntervalParameters == IntervalType.QuantityIterations)
                {
                    NeedVolumeInLot = GetVolume();
                    (int stepsCount, TimeSpan lifetime) = GetStepParameters(IntervalType.QuantityIterations, duration);
                    List<ArbitrationStep> steps = GenerateArbitrationSteps(NeedVolumeInLot, stepsCount, lifetime, VolumeRange, random);
                    ArbitrationStep.AddRange(steps);
                }
                else if (VolumeType == QuantityVolume.Quantity && IntervalParameters == IntervalType.IterationTime)
                {
                    NeedVolumeInLot = NeedQuantity;
                    (int stepsCount, TimeSpan lifetime) = GetStepParameters(IntervalType.IterationTime, duration);
                    List<ArbitrationStep> steps = GenerateArbitrationSteps(NeedQuantity, stepsCount, lifetime, VolumeRange, random);
                    ArbitrationStep.AddRange(steps);
                }
                else if (VolumeType == QuantityVolume.Quantity && IntervalParameters == IntervalType.QuantityIterations)
                {
                    NeedVolumeInLot = NeedQuantity;
                    (int stepsCount, TimeSpan lifetime) = GetStepParameters(IntervalType.QuantityIterations, duration);
                    List<ArbitrationStep> steps = GenerateArbitrationSteps(NeedQuantity, stepsCount, lifetime, VolumeRange, random);
                    ArbitrationStep.AddRange(steps);
                }

                ArbitrationStepUpdateEvent?.Invoke();
            }
            catch (Exception ex)
            {
                this.SendNewLogMessage(ex.ToString(), LogMessageType.Error);
                Thread.Sleep(1000);
            }
        }

        private (int steps, TimeSpan lifetime) GetStepParameters(IntervalType intervalType, TimeSpan duration)
        {
            if (intervalType == IntervalType.IterationTime)
            {
                int steps = (int)Math.Round(duration.TotalSeconds / IterationTime);
                return (steps, TimeSpan.FromSeconds(IterationTime));
            }
            else
            {
                return (QuantityIterations, TimeSpan.FromTicks(duration.Ticks / QuantityIterations));
            }
        }

        private List<ArbitrationStep> GenerateArbitrationSteps(decimal totalVolume, int stepsCount, TimeSpan lifetime, decimal volumeRange, Random random)
        {
            List<ArbitrationStep> steps = new List<ArbitrationStep>();
            decimal remainingVolume = totalVolume;

            decimal baseStep = 1;

            if (stepsCount == 0)
            {
                return steps;
            }

            if (totalVolume / stepsCount < 1)
            {
                baseStep = Math.Ceiling(totalVolume / stepsCount);
            }
            else
            {
                baseStep = Math.Round(totalVolume / stepsCount);
            }

            decimal lastDelta = 0;
            DateTime startTime = StartTime;
            int startNumber = 0;

            if (ArbitrationStep.Count > 0)
            {
                decimal openVolume = 0;
                for (int i = 0; i < ArbitrationStep.Count; i++)
                {
                    ArbitrationStep step = ArbitrationStep[i];

                    if (step.Status == OrderStateType.Done || step.Status == OrderStateType.Partial || step.Status == OrderStateType.Cancel)
                    {
                        openVolume += step.OpenVolume;
                    }

                    if (step.Status == OrderStateType.None)
                    {
                        ArbitrationStep.RemoveAt(i);
                        i--;
                    }
                }

                startTime = TimeServerCurrent;
                TimeSpan duration = EndTime.TimeOfDay - startTime.TimeOfDay;
                int newStepsCount = stepsCount;
                TimeSpan newLifetime = lifetime;

                if (IntervalParameters == IntervalType.IterationTime)
                {
                    newStepsCount = (int)Math.Round(duration.TotalSeconds / IterationTime);
                    newLifetime = TimeSpan.FromSeconds(IterationTime);
                }
                else
                {
                    newStepsCount = QuantityIterations - ArbitrationStep.Count;
                    newLifetime = TimeSpan.FromTicks(duration.Ticks / newStepsCount);
                }

                OpenVolumeInLot = openVolume;

                stepsCount = newStepsCount;
                lifetime = newLifetime;

                remainingVolume -= openVolume;

                if (remainingVolume / stepsCount < 1)
                {
                    baseStep = Math.Ceiling(remainingVolume / stepsCount);
                }
                else
                {
                    baseStep = Math.Round(remainingVolume / stepsCount);
                }

                stepsCount += ArbitrationStep.Count;
                startNumber = ArbitrationStep.Count;
            }

            bool clearDelta = false;

            for (int i = startNumber; i < stepsCount; i++)
            {
                ArbitrationStep step = new ArbitrationStep
                {
                    Status = OrderStateType.None,
                    StartTime = startTime,
                    EndTime = startTime + lifetime,
                    NumberStep = i + 1,
                    OpenVolume = 0,
                    OrderStep = null,
                    Lifetime = lifetime,
                    OrderDirection = OrderDirection
                };

                if (i == stepsCount - 1)
                {
                    step.EndTime = EndTime.AddSeconds(-1);
                    step.VolumeStep = Math.Round(remainingVolume);
                }
                else
                {
                    decimal delta = random.Next(-(int)volumeRange, (int)volumeRange);
                    if (delta == 0) delta = 1;
                    delta = 1 + (delta / 100);

                    decimal volume = 0;

                    if (volumeRange != 0 && lastDelta != 0)
                    {
                        volume = baseStep - lastDelta;

                        if (volume < 1)
                        {
                            step.VolumeStep = Math.Ceiling(volume);
                        }
                        else
                        {
                            step.VolumeStep = Math.Round(volume);
                        }

                        clearDelta = true;
                    }
                    else
                    {
                        if (volumeRange == 0)
                        {
                            volume = baseStep;
                        }
                        else
                        {
                            volume = baseStep * delta;
                        }

                        if (volume < 1)
                        {
                            step.VolumeStep = baseStep;
                        }
                        else
                        {
                            step.VolumeStep = Math.Round(volume);

                            if (step.VolumeStep >= baseStep * 2)
                            {
                                step.VolumeStep = baseStep;
                            }

                            if (step.VolumeStep != baseStep)
                            {
                                clearDelta = false;
                            }
                        }
                    }
                }

                if (clearDelta)
                {
                    lastDelta = 0;
                }
                else
                {
                    lastDelta = step.VolumeStep - baseStep;
                }

                remainingVolume -= step.VolumeStep;

                startTime = startTime + lifetime;

                if (remainingVolume < 0)
                {
                    step.EndTime = EndTime.AddSeconds(-1);
                    step.VolumeStep = Math.Ceiling(-remainingVolume);
                    steps.Add(step);
                    break;
                }
                else if (remainingVolume == 0)
                {
                    step.EndTime = EndTime.AddSeconds(-1);
                    steps.Add(step);
                    break;
                }

                steps.Add(step);
            }

            decimal volumeFull = 0;
            for (int i = 0; i < steps.Count; i++)
            {
                volumeFull += steps[i].VolumeStep;
            }

            return steps;
        }

        #endregion

        #region Trade

        private void TradeLogic()
        {
            try
            {
                if (ArbitrationStep == null)
                {
                    return;
                }

                if (CheckRestrictionPrice() == false)
                {
                    return;
                }

                for (int i = 0; i < ArbitrationStep.Count; i++)
                {
                    ArbitrationStep step = ArbitrationStep[i];

                    if (i == ArbitrationStep.Count - 1)
                    {
                        CheckStepStatus(ref step, isLastStep: true);
                    }
                    else
                    {
                        CheckStepStatus(ref step, isLastStep: false);
                    }

                    if (step.Status == OrderStateType.Done)
                    {
                        continue;
                    }

                    if (step.NeedCancelOrder)
                    {
                        CancelOrder(step);

                        IsCancelTwapOrder = true;

                        continue;
                    }

                    if (CurrentPosition == null)
                    {
                        if (TimeServerCurrent.TimeOfDay < StartTime.TimeOfDay)
                        {
                            break;
                        }

                        decimal price = GetOrderPrice();

                        if (OrderDirection == OrderDirection.Buy)
                        {
                            CurrentPosition = BotTab.BuyAtLimit(Math.Round(step.VolumeStep, BotTab.Securiti.DecimalsVolume), price);
                        }
                        else if (OrderDirection == OrderDirection.Sell)
                        {
                            CurrentPosition = BotTab.SellAtLimit(Math.Round(step.VolumeStep, BotTab.Securiti.DecimalsVolume), price);
                        }

                        if (CurrentPosition != null && CurrentPosition.OpenOrders != null && CurrentPosition.OpenOrders.Count > 0)
                        {
                            step.OrderStep = CurrentPosition.OpenOrders[0];
                            step.Status = step.OrderStep.State;
                        }

                        CurrentStep = step;

                        break;
                    }

                    if (step.Status == OrderStateType.Active)
                    {
                        if (step.EndTime < TimeServerCurrent)
                        {
                            CancelOrder(step);
                        }
                    }
                    else if (step.OrderStep == null &&
                            (ArbitrationStep[i - 1].Status == OrderStateType.Done))
                    {
                        if (step.StartTime > TimeServerCurrent)
                        {
                            break;
                        }

                        if (!step.IgnoreRecalculation && step.EndTime < TimeServerCurrent)
                        {
                            if (UseLog)
                            {
                                this.SendNewLogMessage($"Время жизни ордера в шаге {step.NumberStep} некорректно. Пересчет всех шагов. Торговля продолжится в штатном режиме.", LogMessageType.Error);
                            }

                            step.IgnoreRecalculation = true;
                            NeedRecalculateArbitrationStep = true;
                            break;
                        }

                        decimal price = GetOrderPrice();

                        if (OrderDirection == OrderDirection.Buy)
                        {
                            BotTab.BuyAtLimitToPosition(CurrentPosition, price, Math.Round(step.VolumeStep, BotTab.Securiti.DecimalsVolume));
                        }
                        else if (OrderDirection == OrderDirection.Sell)
                        {
                            BotTab.SellAtLimitToPosition(CurrentPosition, price, Math.Round(step.VolumeStep, BotTab.Securiti.DecimalsVolume));
                        }

                        if (CurrentPosition != null && CurrentPosition.OpenOrders != null)
                        {
                            step.OrderStep = CurrentPosition.OpenOrders[CurrentPosition.OpenOrders.Count - 1];
                        }

                        CurrentStep = step;
                    }
                    else if (step.OrderStep == null &&
                            (ArbitrationStep[i - 1].Status == OrderStateType.Cancel))
                    {
                        if (i - 1 < ArbitrationStep.Count)
                        {
                            if (ArbitrationStep[i - 1].OrderStep.VolumeExecute > 0)
                            {
                                step.VolumeStep += ArbitrationStep[i - 1].OrderStep.Volume - ArbitrationStep[i - 1].OrderStep.VolumeExecute;
                            }
                            else if (ArbitrationStep[i - 1].OrderStep.VolumeExecute == 0)
                            {
                                step.VolumeStep += ArbitrationStep[i - 1].OrderStep.Volume;
                            }
                        }

                        if (step.StartTime > TimeServerCurrent)
                        {
                            break;
                        }

                        if (!step.IgnoreRecalculation && step.EndTime < TimeServerCurrent)
                        {
                            if (UseLog)
                            {
                                this.SendNewLogMessage($"Время жизни ордера в шаге {step.NumberStep} некорректно. Пересчет всех шагов. Торговля продолжится в штатном режиме.", LogMessageType.Error);
                            }

                            step.IgnoreRecalculation = true;
                            NeedRecalculateArbitrationStep = true;
                            break;
                        }

                        decimal price = GetOrderPrice();

                        if (OrderDirection == OrderDirection.Buy)
                        {
                            BotTab.BuyAtLimitToPosition(CurrentPosition, price, Math.Round(step.VolumeStep, BotTab.Securiti.DecimalsVolume));
                        }
                        else if (OrderDirection == OrderDirection.Sell)
                        {
                            BotTab.SellAtLimitToPosition(CurrentPosition, price, Math.Round(step.VolumeStep, BotTab.Securiti.DecimalsVolume));
                        }

                        if (CurrentPosition != null && CurrentPosition.OpenOrders != null)
                        {
                            step.OrderStep = CurrentPosition.OpenOrders[CurrentPosition.OpenOrders.Count - 1];
                        }

                        CurrentStep = step;
                    }
                }

                if (CurrentPosition != null)
                {
                    AveragePrice = (CurrentPosition.OpenVolume != 0)
                        ? Math.Round(CurrentPosition.EntryPrice, BotTab.Securiti.Decimals)
                        : 0;
                }
                else
                {
                    AveragePrice = 0;
                }

                if (NeedTwapMode != BotMode.Off)
                {
                    TwapActiveStatistics = CreateTwapStatistic();
                    ArbitrationStepUpdateEvent?.Invoke();
                }
            }
            catch (Exception ex)
            {
                this.SendNewLogMessage(ex.ToString(), LogMessageType.Error);
                Thread.Sleep(1000);
            }
        }

        private void CancelOrder(ArbitrationStep step)
        {
            try
            {
                if (step.IsSendToCancel
                    && step.LastCancelTryLocalTime.AddSeconds(3) > TimeServerCurrent)
                    return;

                if (!string.IsNullOrEmpty(step.OrderStep.NumberMarket))
                {
                    BotTab.CloseOrder(step.OrderStep);
                    step.IsSendToCancel = true;
                    step.LastCancelTryLocalTime = TimeServerCurrent;
                }
            }
            catch (Exception ex)
            {
                this.SendNewLogMessage(ex.ToString(), LogMessageType.Error);
                Thread.Sleep(1000);
            }
        }

        #endregion

        #region Helpers

        private bool CheckStepStatus(ref ArbitrationStep step, bool isLastStep)
        {
            try
            {
                if (step.OrderStep != null && step.OrderStep.State != step.Status)
                {
                    step.Status = step.OrderStep.State;
                    step.OpenVolume = step.OrderStep.VolumeExecute;

                    if (step.NeedCancelOrder && step.Status == OrderStateType.Cancel)
                    {
                        NeedTwapMode = BotMode.Off;
                    }
                    else if (step.Status == OrderStateType.Partial)
                    {
                        OpenVolumeInLot += step.OpenVolume;
                    }
                    else if (step.Status == OrderStateType.Done)
                    {
                        OpenVolumeInLot += step.OpenVolume;

                        if (UseLog)
                        {
                            this.SendNewLogMessage($"Шаг {step.NumberStep} завершен. Набрано {step.OpenVolume} из {step.VolumeStep}", LogMessageType.Error);
                        }
                    }
                    else if (step.Status == OrderStateType.Cancel)
                    {
                        if (UseLog)
                        {
                            this.SendNewLogMessage($"Шаг {step.NumberStep} отменен. Набрано {step.OpenVolume} из {step.VolumeStep}", LogMessageType.Error);
                        }
                    }
                    else if (step.Status == OrderStateType.Active)
                    {
                        if (UseLog)
                        {
                            this.SendNewLogMessage($"Шаг {step.NumberStep} выставлен. Набрано {step.OpenVolume} из {step.VolumeStep}", LogMessageType.Error);
                        }
                    }

                    if (isLastStep && step.Status == OrderStateType.Done)
                    {
                        if (UseLog)
                        {
                            this.SendNewLogMessage($"Бот выключен. Набрано {OpenVolumeInLot} из {NeedVolumeInLot}", LogMessageType.Error);
                        }

                        BotTab.CloseAtFake(CurrentPosition, CurrentPosition.OpenVolume, BotTab.PriceBestAsk, TimeServerCurrent);

                        TwapActiveStatistics = null;

                        TwapFinishStatistics.Add(CreateTwapStatistic());

                        NeedTwapMode = BotMode.Off;
                    }

                    ArbitrationStepUpdateEvent?.Invoke();
                }

                return true;
            }
            catch (Exception ex)
            {
                this.SendNewLogMessage(ex.ToString(), LogMessageType.Error);
                Thread.Sleep(1000);
                return false;
            }
        }

        private decimal GetVolume()
        {
            try
            {
                decimal contractPrice = BotTab.PriceBestAsk;
                decimal volume = NeedVolume / contractPrice;

                IServerPermission serverPermission = ServerMaster.GetServerPermission(BotTab.Connector.ServerType);

                if (serverPermission != null &&
                    serverPermission.IsUseLotToCalculateProfit &&
                BotTab.Securiti.Lot != 0 &&
                    BotTab.Securiti.Lot > 1)
                {
                    volume = NeedVolume / (contractPrice * BotTab.Securiti.Lot);
                }

                volume = Math.Round(volume, BotTab.Securiti.DecimalsVolume);

                return volume;
            }
            catch (Exception ex)
            {
                this.SendNewLogMessage(ex.ToString(), LogMessageType.Error);
            }

            return 0;
        }

        private decimal GetOrderPrice()
        {
            decimal price = 0;

            if (IsMarketOrder)
            {
                if (OrderDirection == OrderDirection.Sell)
                {
                    price = BotTab.PriceBestBid;
                }
                else if (OrderDirection == OrderDirection.Buy)
                {
                    price = BotTab.PriceBestAsk;
                }
            }
            else if (TypeRestriction == TypeRestriction.Percent)
            {
                if (OrderDirection == OrderDirection.Sell)
                {
                    price = BotTab.PriceBestBid * (1 + (1 * (-SlipageValue)));
                }
                else if (OrderDirection == OrderDirection.Buy)
                {
                    price = BotTab.PriceBestAsk * (1 + (1 * SlipageValue));
                }
            }
            else if (TypeRestriction == TypeRestriction.Point)
            {
                if (OrderDirection == OrderDirection.Sell)
                {
                    price = BotTab.PriceBestBid - SlipageValue;
                }
                else if (OrderDirection == OrderDirection.Buy)
                {
                    price = BotTab.PriceBestAsk + SlipageValue;
                }
            }

            return price;
        }

        private bool CheckRestrictionPrice()
        {
            if (RestrictionPrice == 0)
            {
                return true;
            }

            if (OrderDirection == OrderDirection.Sell)
            {
                if (RestrictionPrice > BotTab.PriceBestBid)
                {
                    return false;
                }
            }
            else if (OrderDirection == OrderDirection.Buy)
            {
                if (RestrictionPrice < BotTab.PriceBestAsk)
                {
                    return false;
                }
            }

            return true;
        }

        public override string GetNameStrategyType()
        {
            return "TwapBot";
        }


        private TwapBotUi _ui;

        public bool NeedRecalculateArbitrationStep;

        public int ShiftTime = 3;
        internal bool IsCancelTwapOrder;

        public override void ShowIndividualSettingsDialog()
        {
            try
            {
                if (_ui == null)
                {
                    _ui = new TwapBotUi(this);
                    _ui.Closed += _ui_Closed;
                    _ui.Show();
                }
                else
                {
                    if (_ui.WindowState == System.Windows.WindowState.Minimized)
                    {
                        _ui.WindowState = System.Windows.WindowState.Normal;
                    }
                    _ui.Activate();
                }
            }
            catch (Exception ex)
            {
                this.SendNewLogMessage(ex.ToString(), LogMessageType.Error);
            }
        }

        private void TwapBot_DeleteEvent()
        {
            try
            {
                this.DeleteEvent -= TwapBot_DeleteEvent;

                if (_ui != null)
                {
                    _ui.Close();
                }

                _deleteBot = true;
            }
            catch (Exception ex)
            {
                this.SendNewLogMessage(ex.ToString(), LogMessageType.Error);
            }
        }

        private void _ui_Closed(object sender, EventArgs e)
        {
            _ui.Closed -= _ui_Closed;
            _ui = null;
        }

        public TwapStatistic CreateTwapStatistic()
        {
            TwapStatistic twapOrder = new TwapStatistic();
            twapOrder.Number = NumberTwapStatistics;
            twapOrder.StartTime = StartTime;
            twapOrder.EndTime = EndTime;
            twapOrder.Direction = OrderDirection;
            twapOrder.OpenVolumeInLot = OpenVolumeInLot;
            twapOrder.NeedVolumeInLot = NeedVolumeInLot;
            twapOrder.AveragePrice = AveragePrice;

            if (OpenVolumeInLot == 0 || NeedVolumeInLot == 0)
            {
                twapOrder.OpenVolumeInPercent = 0;
            }
            else
            {
                twapOrder.OpenVolumeInPercent = Math.Round(OpenVolumeInLot / NeedVolumeInLot * 100, 2);
            }

            return twapOrder;
        }
    }

    public class ArbitrationStep
    {
        public int NumberStep;

        public OrderStateType Status;

        public decimal VolumeStep;

        public decimal OpenVolume;

        public Order OrderStep;

        public TimeSpan Lifetime;

        public OrderDirection OrderDirection;

        public bool IsSendToCancel;

        public DateTime LastCancelTryLocalTime;

        public DateTime StartTime;

        public DateTime EndTime;

        public bool NeedCancelOrder;

        public bool IgnoreRecalculation;
    }

    public class TwapStatistic
    {
        public int Number;

        public DateTime StartTime;

        public DateTime EndTime;

        public decimal AveragePrice;

        public decimal OpenVolumeInLot;

        public decimal NeedVolumeInLot;

        public OrderDirection Direction;

        public decimal OpenVolumeInPercent;

        public TwapState TwapState;
    }

    public enum TwapState
    {
        Active,

        Finish,

        Cancel
    }

    public enum OrderDirection
    {
        Buy,

        Sell
    }

    public enum TypeRestriction
    {
        Point,

        Percent,
    }

    public enum QuantityVolume
    {
        Quantity,

        Volume
    }

    public enum IntervalType
    {
        IterationTime,

        QuantityIterations
    }

    public enum BotMode
    {
        Off,
        Start,
        Pause
    }

    #endregion
}
