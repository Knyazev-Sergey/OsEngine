using OsEngine.Entity;
using OsEngine.Layout;
using OsEngine.Market;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OsEngine.Robots.AlexBots
{
    /// <summary>
    /// Interaction logic for TwapBotUi.xaml
    /// </summary>
    public partial class TwapBotUi : Window
    {
        private TwapBot _bot;
        private TwapBotTableOrders _tableOrders;

        public TwapBotUi(TwapBot bot)
        {
            InitializeComponent();

            _bot = bot;

            StickyBorders.Listen(this);
            StartupLocation.Start_MouseInCentre(this);

            GlobalGUILayout.Listen(this, "TwapBotUi " + bot.NameStrategyUniq);

            CreateTypeRestrictionComboBox();
            TypeRestrictionComboBox.SelectionChanged += TypeRestrictionComboBox_SelectionChanged;

            CreateTypeVolumeComboBox();
            TypeVolumeComboBox.SelectionChanged += TypeVolumeComboBox_SelectionChanged;

            CreateIntervalParametersComboBox();
            IntervalParametersComboBox.SelectionChanged += IntervalParametersComboBox_SelectionChanged;

            PriceRestrictionTextBox.Text = _bot.RestrictionPrice.ToString();
            PriceRestrictionTextBox.TextChanged += RestrictionPriceTextBox_TextChanged;

            SlipageTextBox.Text = _bot.SlipageValue.ToString();
            SlipageTextBox.TextChanged += SlipageTextBox_TextChanged;

            QuantityTextBox.Text = _bot.NeedQuantity.ToString();
            QuantityTextBox.TextChanged += QuantityTextBox_TextChanged;

            VolumeTextBox.Text = _bot.NeedVolume.ToString();
            VolumeTextBox.TextChanged += VolumeTextBox_TextChanged;

            VolumeRangeTextBox.Text = _bot.VolumeRange.ToString();
            VolumeRangeTextBox.TextChanged += VolumeRangeTextBox_TextChanged;

            HourStartTextBox.Text = _bot.StartTime.TimeOfDay.Hours.ToString();
            HourStartTextBox.TextChanged += HourStartTextBox_TextChanged;

            MinuteStartTextBox.Text = _bot.StartTime.TimeOfDay.Minutes.ToString();
            MinuteStartTextBox.TextChanged += MinuteStartTextBox_TextChanged;

            SecondStartTextBox.Text = _bot.StartTime.TimeOfDay.Seconds.ToString();
            SecondStartTextBox.TextChanged += SecondStartTextBox_TextChanged;

            HourEndTextBox.Text = _bot.EndTime.TimeOfDay.Hours.ToString();
            HourEndTextBox.TextChanged += HourEndTextBox_TextChanged;

            MinuteEndTextBox.Text = _bot.EndTime.TimeOfDay.Minutes.ToString();
            MinuteEndTextBox.TextChanged += MinuteEndTextBox_TextChanged;

            SecondEndTextBox.Text = _bot.EndTime.TimeOfDay.Seconds.ToString();
            SecondEndTextBox.TextChanged += SecondEndTextBox_TextChanged;

            TimeShiftTextBox.Text = _bot.ShiftTime.ToString();
            TimeShiftTextBox.TextChanged += TimeShiftTextBox_TextChanged;

            if (_bot.QuantityIterations == 0)
            {
                QuantityIterationsTextBox.Text = "";
            }
            else
            {
                QuantityIterationsTextBox.Text = _bot.QuantityIterations.ToString();
            }
            QuantityIterationsTextBox.TextChanged += QuantityIterationsTextBox_TextChanged;

            if (_bot.IterationTime == 0)
            {
                IterationTimeTextBox.Text = "";
            }
            else
            {
                IterationTimeTextBox.Text = _bot.IterationTime.ToString();
            }
            IterationTimeTextBox.TextChanged += IterationTimeTextBox_TextChanged;

            if (_bot.OrderDirection == OrderDirection.Buy)
            {
                ButtonBuy.Background = Brushes.DarkGreen;
                ButtonSell.Background = null;
            }
            else if (_bot.OrderDirection == OrderDirection.Sell)
            {
                ButtonBuy.Background = null;
                ButtonSell.Background = Brushes.DarkRed;
            }

            CurrentTimeCheckBox.IsChecked = _bot.StartWithCurrentTime;
            CurrentTimeCheckBox.Click += CurrentTimeCheckBox_Click;

            MarketCheckBox.IsChecked = _bot.IsMarketOrder;
            MarketCheckBox.Click += MarketCheckBox_Click;

            UseLogCheckBox.IsChecked = _bot.UseLog;
            UseLogCheckBox.Click += UseLogCheckBox_Click;

            CurrentModeTextBox.Text = _bot.NeedTwapMode.ToString();
            CurrentModeTextBox.IsEnabled = false;

            HourCurrentTimeTextBox.IsEnabled = false;
            MinuteCurrentTimeTextBox.IsEnabled = false;
            SecondCurrentTimeTextBox.IsEnabled = false;

            _bot.TimeUpdateEvent += _bot_TimeUpdateEvent;

            this.Closed += TwapBotUi_Closed;
        }

        private void TimeShiftTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(TimeShiftTextBox.Text, out int result))
            {
                _bot.ShiftTime = result;
            }
        }

        private void _bot_SecondStartTimeUpdateEvent()
        {
            if (!SecondStartTextBox.Dispatcher.CheckAccess())
            {
                SecondStartTextBox.Dispatcher.Invoke(new Action(_bot_SecondStartTimeUpdateEvent));
                return;
            }

            SecondStartTextBox.Text = _bot.StartTime.Second.ToString();

        }

        private void _bot_MinuteStartTimeUpdateEvent()
        {
            if (!MinuteStartTextBox.Dispatcher.CheckAccess())
            {
                MinuteStartTextBox.Dispatcher.Invoke(new Action(_bot_MinuteStartTimeUpdateEvent));
                return;
            }

            MinuteStartTextBox.Text = _bot.StartTime.Minute.ToString();
        }

        private void _bot_TimeUpdateEvent()
        {
            if (!HourStartTextBox.Dispatcher.CheckAccess())
            {
                HourStartTextBox.Dispatcher.Invoke(new Action(_bot_TimeUpdateEvent));
                return;
            }

            CurrentModeTextBox.Text = _bot.NeedTwapMode.ToString();

            HourStartTextBox.Text = _bot.StartTime.Hour.ToString();
            MinuteStartTextBox.Text = _bot.StartTime.Minute.ToString();
            SecondStartTextBox.Text = _bot.StartTime.Second.ToString();

            HourCurrentTimeTextBox.Text = _bot.TimeServerCurrent.Hour.ToString();
            MinuteCurrentTimeTextBox.Text = _bot.TimeServerCurrent.Minute.ToString();
            SecondCurrentTimeTextBox.Text = _bot.TimeServerCurrent.Second.ToString();

            EnableParameters();
        }

        private void SlipageTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(SlipageTextBox.Text))
                {
                    return;
                }

                decimal result;
                if (!TryParseDecimal(SlipageTextBox, out result))
                {
                    return;
                }

                if (result == 0)
                {
                    SlipageTextBox.Foreground = Brushes.Red;
                }
                else
                {
                    SlipageTextBox.ClearValue(TextBox.ForegroundProperty);
                }

                _bot.SlipageValue = result;
            }
            catch (Exception ex)
            {
                _bot.SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void UseLogCheckBox_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _bot.UseLog = UseLogCheckBox.IsChecked.Value;
            }
            catch (Exception ex)
            {
                _bot.SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void IterationTimeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (int.TryParse(IterationTimeTextBox.Text, out int iterationTimeTextBox) && iterationTimeTextBox > 0)
                {
                    _bot.IterationTime = iterationTimeTextBox;
                    IterationTimeTextBox.ClearValue(TextBox.ForegroundProperty);
                }
                else
                {
                    IterationTimeTextBox.Foreground = Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                _bot.SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void QuantityIterationsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (int.TryParse(QuantityIterationsTextBox.Text, out int quantityIterations) && quantityIterations > 0)
                {
                    _bot.QuantityIterations = quantityIterations;
                    QuantityIterationsTextBox.ClearValue(TextBox.ForegroundProperty);
                }
                else
                {
                    QuantityIterationsTextBox.Foreground = Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                _bot.SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void IntervalParametersComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IntervalParametersComboBox.SelectedIndex == 0)
            {
                _bot.IntervalParameters = IntervalType.IterationTime;
                QuantityIterationsTextBox.IsEnabled = false;
                IterationTimeTextBox.IsEnabled = true;
            }
            else if (IntervalParametersComboBox.SelectedIndex == 1)
            {
                _bot.IntervalParameters = IntervalType.QuantityIterations;
                QuantityIterationsTextBox.IsEnabled = true;
                IterationTimeTextBox.IsEnabled = false;
            }
        }

        private void CreateIntervalParametersComboBox()
        {
            IntervalParametersComboBox.Items.Clear();

            IntervalParametersComboBox.Items.Add(new ComboBoxItem
            {
                Content = "Время итерации"
            });

            IntervalParametersComboBox.Items.Add(new ComboBoxItem
            {
                Content = "Кол-во итерации",
            });

            if (_bot.IntervalParameters == IntervalType.IterationTime)
            {
                IntervalParametersComboBox.SelectedIndex = 0;
                QuantityIterationsTextBox.IsEnabled = false;
                IterationTimeTextBox.IsEnabled = true;
            }
            else if (_bot.IntervalParameters == IntervalType.QuantityIterations)
            {
                IntervalParametersComboBox.SelectedIndex = 1;
                QuantityIterationsTextBox.IsEnabled = true;
                IterationTimeTextBox.IsEnabled = false;
            }
        }

        private void CurrentTimeCheckBox_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _bot.StartWithCurrentTime = CurrentTimeCheckBox.IsChecked.Value;

                if (_bot.StartWithCurrentTime)
                {
                    _bot.StartTime = _bot.TimeServerCurrent;
                }

                HourStartTextBox.Text = _bot.StartTime.Hour.ToString();
                MinuteStartTextBox.Text = _bot.StartTime.Minute.ToString();
                SecondStartTextBox.Text = _bot.StartTime.Second.ToString();
            }
            catch (Exception ex)
            {
                _bot.SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void SecondEndTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                DateTime currentTime = _bot.EndTime;

                if (int.TryParse(SecondEndTextBox.Text, out int second))
                {
                    if (second >= 0 && second <= 59)
                    {
                        _bot.EndTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day,
                                                      currentTime.Hour, currentTime.Minute, second, currentTime.Millisecond);
                    }
                    else
                    {
                        // игнор
                    }
                }
            }
            catch (Exception ex)
            {
                _bot.SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void MinuteEndTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                DateTime currentTime = _bot.EndTime;

                if (int.TryParse(MinuteEndTextBox.Text, out int minute))
                {
                    if (minute >= 0 && minute <= 59)
                    {
                        _bot.EndTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day,
                                                      currentTime.Hour, minute, currentTime.Second, currentTime.Millisecond);
                    }
                    else
                    {
                        // игнор
                    }
                }
            }
            catch (Exception ex)
            {
                _bot.SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void HourEndTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                DateTime currentTime = _bot.EndTime;

                if (int.TryParse(HourEndTextBox.Text, out int hour))
                {
                    if (hour >= 0 && hour <= 23)
                    {
                        _bot.EndTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day,
                                                       hour, currentTime.Minute, currentTime.Second, currentTime.Millisecond);
                    }
                    else
                    {
                        // игнор
                    }
                }
            }
            catch (Exception ex)
            {
                _bot.SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void SecondStartTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                DateTime currentTime = _bot.StartTime;

                if (int.TryParse(SecondStartTextBox.Text, out int second))
                {
                    if (second >= 0 && second <= 59)
                    {
                        _bot.StartTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day,
                                                       currentTime.Hour, currentTime.Minute, second, currentTime.Millisecond);
                    }
                    else
                    {
                        // игнор
                    }
                }
            }
            catch (Exception ex)
            {
                _bot.SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void MinuteStartTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                DateTime currentTime = _bot.StartTime;

                if (int.TryParse(MinuteStartTextBox.Text, out int minute))
                {
                    if (minute >= 0 && minute <= 59)
                    {
                        _bot.StartTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day,
                                                       currentTime.Hour, minute, currentTime.Second, currentTime.Millisecond);
                    }
                    else
                    {
                        // игнор
                    }
                }
            }
            catch (Exception ex)
            {
                _bot.SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void HourStartTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                DateTime currentTime = _bot.StartTime;

                if (int.TryParse(HourStartTextBox.Text, out int hour))
                {
                    if (hour >= 0 && hour <= 23)
                    {
                        _bot.StartTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day,
                                                       hour, currentTime.Minute, currentTime.Second, currentTime.Millisecond);
                    }
                    else
                    {
                        // игнор
                    }
                }
            }
            catch (Exception ex)
            {
                _bot.SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void VolumeRangeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(VolumeRangeTextBox.Text))
                {
                    return;
                }

                int result;
                if (!int.TryParse(VolumeRangeTextBox.Text, out result))
                {
                    VolumeRangeTextBox.Foreground = Brushes.Red;
                    return;
                }
                else
                {
                    VolumeRangeTextBox.ClearValue(TextBox.ForegroundProperty);
                }

                if (result < 0)
                {
                    VolumeRangeTextBox.Foreground = Brushes.Red;
                    return;
                }
                else
                {
                    VolumeRangeTextBox.ClearValue(TextBox.ForegroundProperty);
                }

                _bot.VolumeRange = result;
            }
            catch (Exception ex)
            {
                _bot.SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }


        private void VolumeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(VolumeTextBox.Text))
                {
                    return;
                }

                decimal result;
                if (!TryParseDecimal(VolumeTextBox, out result))
                {
                    return;
                }

                if (result < 0)
                {
                    return;
                }

                _bot.NeedVolume = result;
            }
            catch (Exception ex)
            {
                _bot.SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void QuantityTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(QuantityTextBox.Text))
            {
                return;
            }

            decimal result;
            if (!TryParseDecimal(QuantityTextBox, out result))
            {
                return;
            }

            if (result < 0)
            {
                return;
            }

            _bot.NeedQuantity = result;
        }

        private void TypeVolumeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TypeVolumeComboBox.SelectedIndex == 0)
            {
                _bot.VolumeType = QuantityVolume.Quantity;
                VolumeTextBox.IsEnabled = false;
                QuantityTextBox.IsEnabled = true;
            }
            else if (TypeVolumeComboBox.SelectedIndex == 1)
            {
                _bot.VolumeType = QuantityVolume.Volume;
                VolumeTextBox.IsEnabled = true;
                QuantityTextBox.IsEnabled = false;
            }
        }

        private void CreateTypeVolumeComboBox()
        {
            TypeVolumeComboBox.Items.Clear();

            TypeVolumeComboBox.Items.Add(new ComboBoxItem
            {
                Content = "Количество"
            });

            TypeVolumeComboBox.Items.Add(new ComboBoxItem
            {
                Content = "Объем",
            });

            if (_bot.VolumeType == QuantityVolume.Quantity)
            {
                TypeVolumeComboBox.SelectedIndex = 0;
                VolumeTextBox.IsEnabled = false;
            }
            else if (_bot.VolumeType == QuantityVolume.Volume)
            {
                TypeVolumeComboBox.SelectedIndex = 1;
                QuantityTextBox.IsEnabled = false;
            }
        }

        private void TwapBotUi_Closed(object sender, EventArgs e)
        {
            TimeShiftTextBox.TextChanged -= TimeShiftTextBox_TextChanged;
            TypeRestrictionComboBox.SelectionChanged -= TypeRestrictionComboBox_SelectionChanged;
            PriceRestrictionTextBox.TextChanged -= RestrictionPriceTextBox_TextChanged;
            MarketCheckBox.Click -= MarketCheckBox_Click;
            TypeVolumeComboBox.SelectionChanged -= TypeVolumeComboBox_SelectionChanged;
            QuantityTextBox.TextChanged -= QuantityTextBox_TextChanged;
            HourStartTextBox.TextChanged -= HourStartTextBox_TextChanged;
            MinuteStartTextBox.TextChanged -= MinuteStartTextBox_TextChanged;
            SecondStartTextBox.TextChanged -= SecondStartTextBox_TextChanged;
            HourEndTextBox.TextChanged -= HourEndTextBox_TextChanged;
            MinuteEndTextBox.TextChanged -= MinuteEndTextBox_TextChanged;
            SecondEndTextBox.TextChanged -= SecondEndTextBox_TextChanged;
            CurrentTimeCheckBox.Click -= CurrentTimeCheckBox_Click;
            IntervalParametersComboBox.SelectionChanged -= IntervalParametersComboBox_SelectionChanged;
            QuantityIterationsTextBox.TextChanged -= QuantityIterationsTextBox_TextChanged;
            IterationTimeTextBox.TextChanged += IterationTimeTextBox_TextChanged;
            UseLogCheckBox.Click -= UseLogCheckBox_Click;
            SlipageTextBox.TextChanged -= SlipageTextBox_TextChanged;
            VolumeRangeTextBox.TextChanged -= VolumeRangeTextBox_TextChanged;
            _bot.TimeUpdateEvent -= _bot_TimeUpdateEvent;

            this.Closed -= TwapBotUi_Closed;
        }

        private void MarketCheckBox_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _bot.IsMarketOrder = MarketCheckBox.IsChecked.Value;

                if (_bot.IsMarketOrder)
                {
                    SlipageTextBox.IsEnabled = false;
                    TypeRestrictionComboBox.IsEnabled = false;
                }
                else
                {
                    SlipageTextBox.IsEnabled = true;
                    TypeRestrictionComboBox.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                _bot.SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void TypeRestrictionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (_bot.NeedTwapMode == BotMode.Start)
                {
                    CreateBanLogMessage();
                }

                if (TypeRestrictionComboBox.SelectedIndex == 0)
                {
                    _bot.TypeRestriction = TypeRestriction.Percent;
                }
                else if (TypeRestrictionComboBox.SelectedIndex == 1)
                {
                    _bot.TypeRestriction = TypeRestriction.Point;
                }
            }
            catch (Exception ex)
            {
                _bot.SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void CreateTypeRestrictionComboBox()
        {
            TypeRestrictionComboBox.Items.Clear();

            TypeRestrictionComboBox.Items.Add(new ComboBoxItem
            {
                Content = "Проценты",
            });

            TypeRestrictionComboBox.Items.Add(new ComboBoxItem
            {
                Content = "Пункты"
            });

            if (_bot.TypeRestriction == TypeRestriction.Percent)
            {
                TypeRestrictionComboBox.SelectedIndex = 0;
            }
            else if (_bot.TypeRestriction == TypeRestriction.Point)
            {
                TypeRestrictionComboBox.SelectedIndex = 1;
            }
        }

        private void RestrictionPriceTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(PriceRestrictionTextBox.Text))
                {
                    return;
                }

                decimal result;
                if (!TryParseDecimal(PriceRestrictionTextBox, out result))
                {
                    return;
                }

                _bot.RestrictionPrice = result;
            }
            catch (Exception ex)
            {
                _bot.SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void ButtonBuy_Click(object sender, RoutedEventArgs e)
        {
            if (_bot.NeedTwapMode == BotMode.Start)
            {
                CreateBanLogMessage();
                return;
            }

            ButtonBuy.Background = Brushes.DarkGreen;
            ButtonSell.Background = null;

            _bot.OrderDirection = OrderDirection.Buy;
        }

        private void ButtonSell_Click(object sender, RoutedEventArgs e)
        {
            if (_bot.NeedTwapMode == BotMode.Start)
            {
                CreateBanLogMessage();
                return;
            }

            ButtonBuy.Background = null;
            ButtonSell.Background = Brushes.DarkRed;

            _bot.OrderDirection = OrderDirection.Sell;
        }

        private void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            if (_bot.NeedTwapMode == BotMode.Start)
            {
                _bot.SendNewLogMessage("Twap ордер уже активен. Поставьте на паузу или отмените twap ордер", Logging.LogMessageType.Error);
                return;
            }
            else if (_bot.NeedTwapMode == BotMode.Pause)
            {
                AcceptDialogUi acceptDialogUi = new AcceptDialogUi("Продолжить работу бота? Шаги будут пересчитаны.");
                acceptDialogUi.ShowDialog();

                if (!acceptDialogUi.UserAcceptAction)
                {
                    return;
                }

                _bot.NeedTwapMode = BotMode.Start;
                return;
            }

            if (_bot.BotTab.IsReadyToTrade == false)
            {
                _bot.SendNewLogMessage("Бумага недоступна для торговли. Торговля невозможна", Logging.LogMessageType.Error);
                return;
            }
            else if (_bot.StartTime == DateTime.MinValue)
            {
                _bot.SendNewLogMessage("Время старта не указано. Торговля невозможна", Logging.LogMessageType.Error);
                return;
            }
            else if (_bot.StartTime > _bot.EndTime)
            {
                _bot.SendNewLogMessage("Время старта больше времени окончания. Торговля невозможна", Logging.LogMessageType.Error);
                return;
            }
            else if (_bot.VolumeType == QuantityVolume.Quantity && _bot.NeedQuantity <= 0)
            {
                _bot.SendNewLogMessage("В поле Кол-во (лот 1000) указан некорректный объем. Торговля невозможна", Logging.LogMessageType.Error);
                return;
            }
            else if (_bot.VolumeType == QuantityVolume.Volume && _bot.NeedVolume <= 0)
            {
                _bot.SendNewLogMessage("В поле Объем указан некорректный объем. Торговля невозможна", Logging.LogMessageType.Error);
                return;
            }
            else if (_bot.IntervalParameters == IntervalType.QuantityIterations && _bot.QuantityIterations <= 0)
            {
                _bot.SendNewLogMessage("Кол-во итераций указано неверно. Торговля невозможна", Logging.LogMessageType.Error);
                return;
            }
            else if (_bot.IntervalParameters == IntervalType.IterationTime && _bot.IterationTime <= 0)
            {
                _bot.SendNewLogMessage("Время итераций указано неверно. Торговля невозможна", Logging.LogMessageType.Error);
                return;
            }
            else if (_bot.SlipageValue == 0 && _bot.IsMarketOrder == false)
            {
                _bot.SendNewLogMessage("Ограничение цены равно 0. Заполните данный параметр или включите галочку Рыночная", Logging.LogMessageType.Error);
                return;
            }

            if (!int.TryParse(VolumeRangeTextBox.Text, out var volumeRange) || volumeRange < 0)
            {
                _bot.SendNewLogMessage("Диапазон объема указан неверно. Торговля невозможна", Logging.LogMessageType.Error);
                return;
            }

            if (int.TryParse(HourStartTextBox.Text, out int hourStart))
            {
                if (hourStart >= 0 && hourStart <= 23)
                {
                    // игнор
                }
                else
                {
                    _bot.SendNewLogMessage("Часы старта указаны некорректно. Торговля невозможна", Logging.LogMessageType.Error);
                    return;
                }
            }

            if (int.TryParse(MinuteStartTextBox.Text, out int minuteStart))
            {
                if (minuteStart >= 0 && minuteStart <= 59)
                {
                    // игнор
                }
                else
                {
                    _bot.SendNewLogMessage("Минуты старта указаны некорректно. Торговля невозможна", Logging.LogMessageType.Error);
                    return;
                }
            }

            if (int.TryParse(SecondStartTextBox.Text, out int secondStart))
            {
                if (secondStart >= 0 && secondStart <= 59)
                {
                    // игнор
                }
                else
                {
                    _bot.SendNewLogMessage("Секунды старта указаны некорректно. Торговля невозможна", Logging.LogMessageType.Error);
                    return;
                }
            }

            if (int.TryParse(HourEndTextBox.Text, out int hourEnd))
            {
                if (hourStart >= 0 && hourStart <= 23)
                {
                    // игнор
                }
                else
                {
                    _bot.SendNewLogMessage("Часы окончания указаны некорректно. Торговля невозможна", Logging.LogMessageType.Error);
                    return;
                }
            }

            if (int.TryParse(MinuteEndTextBox.Text, out int minuteEnd))
            {
                if (minuteEnd >= 0 && minuteEnd <= 59)
                {
                    // игнор
                }
                else
                {
                    _bot.SendNewLogMessage("Минуты окончания указаны некорректно. Торговля невозможна", Logging.LogMessageType.Error);
                    return;
                }
            }

            if (int.TryParse(SecondEndTextBox.Text, out int secondEnd))
            {
                if (secondEnd >= 0 && secondEnd <= 59)
                {
                    // игнор
                }
                else
                {
                    _bot.SendNewLogMessage("Секунды окончания указаны некорректно. Торговля невозможна", Logging.LogMessageType.Error);
                    return;
                }
            }

            decimal resultSlippage;
            if (!TryParseDecimal(SlipageTextBox, out resultSlippage) && _bot.IsMarketOrder)
            {
                _bot.SendNewLogMessage("Параметр ограничение цены указан некорректно. Торговля невозможна", Logging.LogMessageType.Error);
                return;
            }

            if (resultSlippage == 0 && !_bot.IsMarketOrder)
            {
                _bot.SendNewLogMessage("Параметр ограничение цены указан некорректно. Торговля невозможна", Logging.LogMessageType.Error);
                return;
            }

            if (_bot.RestrictionPrice != 0 && _bot.OrderDirection == OrderDirection.Buy && _bot.RestrictionPrice < _bot.BotTab.PriceBestAsk)
            {
                _bot.SendNewLogMessage("Текущая цена находится за ограничением. Торговля невозможна", Logging.LogMessageType.Error);
                return;
            }

            AcceptDialogUi ui = new AcceptDialogUi("Начать торговлю с текущими параметрами? Прошлые настройки будут стерты");
            ui.ShowDialog();

            if (ui.UserAcceptAction == false)
            {
                return;
            }

            if (_bot.CurrentPosition != null)
            {
                _bot.BotTab.CloseAtFake(_bot.CurrentPosition, _bot.CurrentPosition.OpenVolume, _bot.BotTab.PriceBestAsk, _bot.TimeServerCurrent);
                _bot.CurrentPosition = null;
            }

            _bot.NeedTwapMode = BotMode.Start;

            CurrentModeTextBox.Text = _bot.NeedTwapMode.ToString();

            EnableParameters();
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_bot.NeedTwapMode == BotMode.Off)
            {
                return;
            }

            AcceptDialogUi acceptDialogUi = new AcceptDialogUi("Полностью остановить текущую торговлю? Все активные ордера будут сняты.");
            acceptDialogUi.ShowDialog();

            if (!acceptDialogUi.UserAcceptAction)
            {
                return;
            }

            if (_bot.TwapCancelStatistics == null)
            {
                _bot.TwapCancelStatistics = new List<TwapStatistic>();
            }

            _bot.NeedTwapMode = BotMode.Off;
            _bot.IsCancelTwapOrder = true;

            CurrentModeTextBox.Text = _bot.NeedTwapMode.ToString();
        }

        private void ButtonPause_Click(object sender, RoutedEventArgs e)
        {
            if (_bot.NeedTwapMode == BotMode.Start)
            {
                AcceptDialogUi acceptDialogUi = new AcceptDialogUi("Поставить текущую работу бота на паузу? Возобновить работу можно нажав на кнопку Старт. Бот пересчитает оставшиеся шаги с учетом уже набранного объема.");
                acceptDialogUi.ShowDialog();

                if (!acceptDialogUi.UserAcceptAction)
                {
                    return;
                }

                _bot.NeedTwapMode = BotMode.Pause;

                _bot.NeedRecalculateArbitrationStep = true;
            }
            else
            {
                return;
            }

            CurrentModeTextBox.Text = _bot.NeedTwapMode.ToString();

            EnableParameters();
        }

        private void ButtonOrderTable_Click(object sender, RoutedEventArgs e)
        {
            if (_tableOrders == null)
            {
                _tableOrders = new TwapBotTableOrders(_bot);
                _tableOrders.Closed += _tableOrders_Closed;
                _tableOrders.Show();
            }
            else
            {
                _tableOrders.Activate();
            }
        }

        private void _tableOrders_Closed(object sender, EventArgs e)
        {
            _tableOrders.Closed -= _tableOrders_Closed;
            _tableOrders = null;
        }

        #region Helpers

        private void EnableParameters()
        {
            bool isEnable = true;
            if (_bot.NeedTwapMode == BotMode.Start)
            {
                isEnable = false;
            }

            if (isEnable)
            {
                if (TypeVolumeComboBox.SelectedIndex == 0)
                {
                    VolumeTextBox.IsEnabled = false;
                    QuantityTextBox.IsEnabled = true;
                }
                else if (TypeVolumeComboBox.SelectedIndex == 1)
                {
                    VolumeTextBox.IsEnabled = true;
                    QuantityTextBox.IsEnabled = false;
                }

                if (IntervalParametersComboBox.SelectedIndex == 0)
                {
                    QuantityIterationsTextBox.IsEnabled = false;
                    IterationTimeTextBox.IsEnabled = true;
                }
                else if (IntervalParametersComboBox.SelectedIndex == 1)
                {
                    QuantityIterationsTextBox.IsEnabled = true;
                    IterationTimeTextBox.IsEnabled = false;
                }

                if (_bot.IsMarketOrder)
                {
                    SlipageTextBox.IsEnabled = false;
                    TypeRestrictionComboBox.IsEnabled = false;
                }
                else
                {
                    SlipageTextBox.IsEnabled = true;
                    TypeRestrictionComboBox.IsEnabled = true;
                }
            }
            else
            {
                QuantityIterationsTextBox.IsEnabled = isEnable;
                IterationTimeTextBox.IsEnabled = isEnable;
                VolumeTextBox.IsEnabled = isEnable;
                QuantityTextBox.IsEnabled = isEnable;
                SlipageTextBox.IsEnabled = isEnable;
                TypeRestrictionComboBox.IsEnabled = isEnable;
            }

            TimeShiftTextBox.IsEnabled = isEnable;
            PriceRestrictionTextBox.IsEnabled = isEnable;
            MarketCheckBox.IsEnabled = isEnable;
            TypeVolumeComboBox.IsEnabled = isEnable;
            VolumeRangeTextBox.IsEnabled = isEnable;
            HourStartTextBox.IsEnabled = isEnable;
            MinuteStartTextBox.IsEnabled = isEnable;
            SecondStartTextBox.IsEnabled = isEnable;
            CurrentTimeCheckBox.IsEnabled = isEnable;
            HourEndTextBox.IsEnabled = isEnable;
            MinuteEndTextBox.IsEnabled = isEnable;
            SecondEndTextBox.IsEnabled = isEnable;
            IntervalParametersComboBox.IsEnabled = isEnable;
        }

        private void CreateBanLogMessage()
        {
            _bot.SendNewLogMessage("Бот в режиме Start, данный параметр сейчас нельзя поменять. Включите паузу, чтобы поменять параметры", Logging.LogMessageType.Error);
        }

        private bool TryParseDecimal(TextBox textBox, out decimal result)
        {
            result = 0;
            string text = textBox.Text.Replace(',', '.');
            if (!decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
            {
                textBox.Foreground = Brushes.Red;
                return false;
            }
            textBox.ClearValue(TextBox.ForegroundProperty);
            return true;
        }


        #endregion


    }
}
