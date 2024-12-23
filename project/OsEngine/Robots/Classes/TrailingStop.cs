using OsEngine.Entity;
using OsEngine.Logging;
using OsEngine.OsTrader.Panels.Tab;
using System;

namespace OsEngine.Robots.Classes
{
    public class TrailingStop
    {
        private BotTabSimple _tab;
        private string _orderType;
        private decimal _stepStop;
        private decimal _minDist;
        private decimal _quantityStepsPrices;
        private string _pointOrPercent;

        public TrailingStop(BotTabSimple tab, string orderType, decimal stepStop, decimal minDist, decimal quantityStepsPrices, string pointOrPercent)
        {
            // инициализируем экземпляр объекта
            _tab = tab;
            _orderType = orderType;
            _stepStop = stepStop;
            _minDist = minDist;
            _quantityStepsPrices = quantityStepsPrices;
            _pointOrPercent = pointOrPercent;
        }

        public void SetTrailingStop(decimal lastPrice)
        {
            try
            {
                // перебираем открытые позиции
                for (int i = 0; i < _tab.PositionsOpenAll.Count; i++) 
                {                    
                    Position pos = _tab.PositionsOpenAll[i];
                                        
                    decimal minDist = GetMinDist(pos);
                    decimal stepStop = GetStepStop(pos);

                    decimal priceActivation = 0;
                    decimal priceOrder = 0;
                    decimal slippageOrder = _tab.Securiti.PriceStep * _quantityStepsPrices; 
                    
                    if (pos.State != PositionStateType.Open)
                    {
                        continue;
                    }

                    if (pos.OpenOrders[0].Side == Side.Buy)
                    {
                        // если еще нет открытого трейлинг стопа, открываем стоп по цене открытия минус минимальная дистанция
                        if (pos.StopOrderRedLine == 0) 
                        {
                            priceActivation = pos.EntryPrice - minDist;  
                            priceOrder = priceActivation - slippageOrder;
                        }
                        else // если есть активный трейлинг стоп
                        {
                            // если цена не прошла мминимальную дистанцию, то переходим к следующей позиции
                            if (lastPrice - minDist < pos.StopOrderRedLine)
                            {
                                continue;
                            }
                            
                            // если цена не прошла кратную величину шага установления стопа, то переходим к следующей позиции
                            if (stepStop != 0)
                            {
                                if (Math.Abs(lastPrice - pos.EntryPrice) / stepStop < 1)
                                {
                                    continue;
                                }
                            }

                            // если цена прошла минимальную дистаницю и прошла дистанцию кратной величины шага стопа
                            // то устанавливаем цену активации стопа
                            if (stepStop != 0)
                            {
                                if (lastPrice - minDist >= pos.StopOrderRedLine &&
                                Math.Abs(lastPrice - pos.EntryPrice) / stepStop >= 1)
                                {
                                    priceActivation = Math.Floor((lastPrice - pos.EntryPrice) / stepStop) * stepStop - minDist + pos.EntryPrice;
                                    priceOrder = priceActivation - slippageOrder;
                                }
                            }
                            else
                            {
                                // если шаг стопа равен 0, то цена стопа исходит от минимальной дистанции
                                if (lastPrice - minDist >= pos.StopOrderRedLine)
                                {
                                    priceActivation = lastPrice - minDist;
                                    priceOrder = priceActivation - slippageOrder;
                                }
                            }                            
                        }
                    }

                    else if (pos.OpenOrders[0].Side == Side.Sell)
                    {
                        // если еще нет открытого трейлинг стопа, открываем стоп по цене открытия плюс минимальная дистанция
                        if (pos.StopOrderRedLine == 0)
                        {
                           
                            priceActivation = pos.EntryPrice + minDist;
                            priceOrder = priceActivation + slippageOrder;
                        }
                        else // если есть активный трейлинг стоп
                        {
                            // если цена не прошла мминимальную дистанцию, то переходим к следующей позиции
                            if (lastPrice + minDist > pos.StopOrderRedLine)
                            {
                                continue;
                            }

                            // если цена не прошла кратную величину шага установления стопа, то переходим к следующей позиции
                            if (stepStop != 0)
                            {
                                if (Math.Abs(lastPrice - pos.EntryPrice) / stepStop < 1)
                                {
                                    continue;
                                }
                            }

                            // если цена прошла минимальную дистаницю и прошла дистанцию кратной величины шага стопа
                            // то устанавливаем цену активации стопа
                            if (stepStop != 0)
                            {
                                if (lastPrice + minDist <= pos.StopOrderRedLine &&
                                Math.Abs(lastPrice - pos.EntryPrice) / stepStop >= 1)
                                {
                                    priceActivation = Math.Ceiling((lastPrice - pos.EntryPrice) / stepStop) * stepStop + minDist + pos.EntryPrice;
                                    priceOrder = priceActivation + slippageOrder;
                                }
                            }
                            else
                            {
                                // если шаг стопа равен 0, то цена стопа исходит от минимальной дистанции
                                if (lastPrice + minDist >= pos.StopOrderRedLine)
                                {
                                    priceActivation = lastPrice + minDist;
                                    priceOrder = priceActivation + slippageOrder;
                                }
                            }
                        }
                    }

                    // если по какой-то причине мы не получили цены для стопа, переходим к следующей позиции
                    if (priceActivation == 0 && 
                        priceOrder == 0)
                    {
                        continue;
                    }

                    if (priceActivation == pos.StopOrderRedLine)
                    {
                        continue;
                    }

                    // выставляем лимитный трейлинг стоп
                    if (_orderType == "Limit")
                    {
                        _tab.CloseAtTrailingStop(_tab.PositionsOpenAll[i], priceActivation, priceOrder);

                        //посылаем сообщение в лог
                        _tab.SetNewLogMessage("Set Limit Trailing Stop. Price activation = " + priceActivation + ", Price Order = " + priceOrder, LogMessageType.Trade);
                    }

                    // выставляем рыночный трейлинг стоп
                    else if (_orderType == "Market")
                    {
                        _tab.CloseAtTrailingStopMarket(_tab.PositionsOpenAll[i], priceActivation);

                        //посылаем сообщение в лог
                        _tab.SetNewLogMessage("Set Market Trailing Stop. Price activation = " + priceActivation, LogMessageType.Trade);                        
                    }
                }
            }
            catch (Exception ex)
            {
                _tab.SetNewLogMessage("SetTrailingStop: " + ex.Message, LogMessageType.Error);
            }
        }

        private decimal GetMinDist(Position pos)
        {
            decimal minDist = 0;
            
            if (_pointOrPercent == "Percent")
            {
                minDist = pos.EntryPrice * _minDist / 100;
            }
            else
            {
                minDist = _minDist;
            }
            return minDist;
        }

        private decimal GetStepStop(Position pos)
        {
            decimal stepStop = 0;

            if (_pointOrPercent == "Percent")
            {
                stepStop = pos.EntryPrice * _stepStop / 100;
            }
            else
            {
                stepStop = _stepStop;
            }
            return stepStop;
        }
    }
}
