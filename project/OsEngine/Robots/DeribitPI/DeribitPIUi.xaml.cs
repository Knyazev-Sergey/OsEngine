/*
 * Your rights to use code governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace OsEngine.Robots.DeribitPI
{
    public partial class DeribitPIUi
    {
        private DeribitPI _strategy;
        public bool _visibleParameters;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private Thread _worker;

        public DeribitPIUi(DeribitPI strategy)
        {
            InitializeComponent();
            OsEngine.Layout.StickyBorders.Listen(this);
            _strategy = strategy;

            ComboBoxRegime.Items.Add(new { Value = NameRegime.Off, Description = "Выключен" });
            ComboBoxRegime.Items.Add(new { Value = NameRegime.SettingsTrade, Description = "Настройка торговли" });
            ComboBoxRegime.Items.Add(new { Value = NameRegime.AssemblyConstruction, Description = "Набор конструкции" });
            ComboBoxRegime.Items.Add(new { Value = NameRegime.TradeFutures, Description = "Торговля фьючерсами" });
            ComboBoxRegime.SelectedValue = _strategy.Regime;
            ComboBoxRegime.IsEnabled = false;

            LabelPercentDeposit.Text = _strategy.PercentOfDeposit.ToString();
            CountIteration.Text = _strategy.CountIteration.ToString();
            TimeToCloseOption.Text = _strategy.TimeToCloseOption.ToString();
            LabelTimeFuturesLimit.Text = _strategy.TimeFuturesLimit.ToString();
            checkBoxMarketOrder.IsChecked = _strategy.CheckBoxMarketOrder;
            LabelTimeOptionLimit.Text = _strategy.TimeOptionLimit.ToString();
            TimeLifeAssemblyConstruction.Text = _strategy.TimeLifeAssemblyConstruction.ToString();
            CountWorkParts.Text = _strategy.CountWorkParts.ToString();
            RatioWorkParts.Text = _strategy.RatioWorkParts.ToString();
            OneIncreaseX.Text = _strategy.OneIncreaseX.ToString();
            OneIncreaseY.Text = _strategy.OneIncreaseY.ToString();
            TwoIncreaseX.Text = _strategy.TwoIncreaseX.ToString();
            TwoIncreaseY.Text = _strategy.TwoIncreaseY.ToString();
            ThreeIncreaseX.Text = _strategy.ThreeIncreaseX.ToString();
            ThreeIncreaseY.Text = _strategy.ThreeIncreaseY.ToString();

            ListBoxLog.Items.Clear();
            if (_strategy.LogList.Count > 0)
            {
                for (int i = 0; i < _strategy.LogList.Count; i++)
                {
                    ListBoxLog.Items.Add(_strategy.LogList[i]);
                }
                ListBoxLog.ScrollIntoView(ListBoxLog.Items[ListBoxLog.Items.Count - 1]);
            }

            _strategy.LogMessageEvent += _strategy_LogMessageEvent;

            StartThread();

            this.Activate();
            this.Focus();
            this.Closed += DeribitPIUi_Closed;
        }

        private void DeribitPIUi_Closed(object sender, EventArgs e)
        {
            try
            {
                _cts.Cancel();
                _cts.Dispose();
                ListBoxLog = null;
                _strategy.LogMessageEvent -= _strategy_LogMessageEvent;
                _worker = null;
            }
            catch
            {
            }

        }

        private void _strategy_LogMessageEvent(string arg1, Logging.LogMessageType arg2)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (arg2 == Logging.LogMessageType.User)
                {
                    ListBoxLog.Items.Add(arg1);

                    while (ListBoxLog.Items.Count > _strategy.CountListLog) // кол-во записей в логе
                    {
                        ListBoxLog.Items.RemoveAt(0);
                    }
                    ListBoxLog.ScrollIntoView(ListBoxLog.Items[ListBoxLog.Items.Count - 1]);
                }
            });            
        }

       private void StartThread()
       {
            CancellationToken token = _cts.Token;
            _worker = new Thread(() => StartText(token));
            _worker.Start();            
       }    
            
       private void StartText(CancellationToken token) 
       {            
            while (!token.IsCancellationRequested)
            {
                Thread.Sleep(1000);
                Dispatcher.Invoke(new Action(UpdateWpf));
                Dispatcher.Invoke(new Action(VisibleParameters));
            }

            _strategy = null;
        }

        private void VisibleParameters()
       {
           if (_strategy.Regime != NameRegime.SettingsTrade)
           {
               LabelPercentDeposit.IsReadOnly = true;
               CountIteration.IsReadOnly = true;
               TimeToCloseOption.IsReadOnly = true;
               LabelTimeFuturesLimit.IsReadOnly = true;
               checkBoxMarketOrder.IsHitTestVisible = false;
               checkBoxMarketOrder.Focusable = false;
               LabelTimeOptionLimit.IsReadOnly = true;
               TimeLifeAssemblyConstruction.IsReadOnly = true;
               CountWorkParts.IsReadOnly = true;
               RatioWorkParts.IsReadOnly = true;
               OneIncreaseX.IsReadOnly = true;
               OneIncreaseY.IsReadOnly = true;
               TwoIncreaseX.IsReadOnly = true;
               TwoIncreaseY.IsReadOnly = true;
               ThreeIncreaseX.IsReadOnly = true;
               ThreeIncreaseY.IsReadOnly = true;
           }
           else
           {
               LabelPercentDeposit.IsReadOnly = false;
               CountIteration.IsReadOnly = false;
               TimeToCloseOption.IsReadOnly = false;
               LabelTimeFuturesLimit.IsReadOnly = false;
               checkBoxMarketOrder.IsHitTestVisible = true;
               checkBoxMarketOrder.Focusable = true;
               LabelTimeOptionLimit.IsReadOnly = false;
               TimeLifeAssemblyConstruction.IsReadOnly = false;
               CountWorkParts.IsReadOnly = false;
               RatioWorkParts.IsReadOnly = false;
               OneIncreaseX.IsReadOnly = false;
               OneIncreaseY.IsReadOnly = false;
               TwoIncreaseX.IsReadOnly = false;
               TwoIncreaseY.IsReadOnly = false;
               ThreeIncreaseX.IsReadOnly = false;
               ThreeIncreaseY.IsReadOnly = false;
           }         
           
          /* if(_strategy.Regime == NameRegime.Off &&
                _strategy.OnTradeRegime)
            {
                ComboBoxExpir.IsEnabled = true;
            }
            else
            {
                ComboBoxExpir.IsEnabled = false;
            }*/
        }

        private void UpdateWpf()
        {
            ComboBoxRegime.SelectedValue = _strategy.Regime;
            if (_strategy.OnTradeRegime)
            {
                ComboBoxRegime.IsEnabled = true;
            }

            TextLastPrice.Text = _strategy.UnderlyingPrice.ToString();
            TextCurrStrike.Text = _strategy.CurrentStrike;
            TextPriceOption.Text = _strategy.MarkPriceOption.ToString();
            TextSizeOption.Text = _strategy.SettlementSizeOption.ToString();

            TextDeposit.Text = _strategy.Deposit.ToString();
            TextSizeOptionOnBoard.Text = _strategy.PositionOptionSize.ToString();
            TextSizeFuturesOnBoard.Text = _strategy.PositionFutureSize.ToString();
            TextSizeFutureIntraday.Text = _strategy.PositionFutureIntraday.ToString();

            /*if (ComboBoxExpir.Items.Count == 0)
            {
                if (_strategy.OptionSeries != null &&
                _strategy.OptionSeries.Count > 0)
                {
                    ComboBoxExpir.Items.Clear();
                    for (int i = 0; i < _strategy.OptionSeries.Count; i++)
                    {
                        ComboBoxExpir.Items.Add(_strategy.OptionSeries[i]);
                    }
                }
            }   */              
        }

        public enum NameRegime
        {
            Off,
            AssemblyConstruction,
            DisassemblyConstruction,
            TradeFutures,
            SettingsTrade
        }
                
        private void LabelPercentDeposit_TextChanged(object sender, TextChangedEventArgs e)
        {
            _strategy.PercentOfDeposit = TextChangerToDecimal(sender);
            _strategy.SaveParameters();
        }

        private void CountIteration_TextChanged(object sender, TextChangedEventArgs e)
        {
            _strategy.CountIteration = TextChanger(sender);
            _strategy.SaveParameters();
        }

        private void TimeToCloseOption_TextChanged(object sender, TextChangedEventArgs e)
        {
            _strategy.TimeToCloseOption = TextChanger(sender);
            _strategy.SaveParameters();
        }

        private void LabelTimeFuturesLimit_TextChanged(object sender, TextChangedEventArgs e)
        {
            _strategy.TimeFuturesLimit = TextChanger(sender);
            _strategy.SaveParameters();
        }

        private void checkBoxMarketOrder_Checked(object sender, RoutedEventArgs e)
        {
            if (_strategy == null)
            {
                return;
            }
            _strategy.CheckBoxMarketOrder = true;
            _strategy.SaveParameters();
        }
        private void checkBoxMarketOrder_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_strategy == null)
            {
                return;
            }
            _strategy.CheckBoxMarketOrder = false;
            _strategy.SaveParameters();
        }

        private void LabelTimeOptionLimit_TextChanged(object sender, TextChangedEventArgs e)
        {
            _strategy.TimeOptionLimit = TextChanger(sender);
            _strategy.SaveParameters();
        }

        private void TimeLifeAssemblyConstruction_TextChanged(object sender, TextChangedEventArgs e)
        {
            _strategy.TimeLifeAssemblyConstruction = TextChanger(sender);
            _strategy.SaveParameters();
        }

        private void CountWorkParts_TextChanged(object sender, TextChangedEventArgs e)
        {
            _strategy.CountWorkParts = TextChanger(sender);
            _strategy.SaveParameters();
        }

        private void RatioWorkParts_TextChanged(object sender, TextChangedEventArgs e)
        {
            _strategy.RatioWorkParts = TextChanger(sender);
            _strategy.SaveParameters();
        }

        private void OneIncreaseX_TextChanged(object sender, TextChangedEventArgs e)
        {
            _strategy.OneIncreaseX = TextChanger(sender);
            _strategy.SaveParameters();
        }

        private void OneIncreaseY_TextChanged(object sender, TextChangedEventArgs e)
        {
            _strategy.OneIncreaseY = TextChangerToDecimal(sender);
            _strategy.SaveParameters();
        }

        private void TwoIncreaseX_TextChanged(object sender, TextChangedEventArgs e)
        {
            _strategy.TwoIncreaseX = TextChanger(sender);
            _strategy.SaveParameters();
        }

        private void TwoIncreaseY_TextChanged(object sender, TextChangedEventArgs e)
        {
            _strategy.TwoIncreaseY = TextChangerToDecimal(sender);
            _strategy.SaveParameters();
        }

        private void ThreeIncreaseX_TextChanged(object sender, TextChangedEventArgs e)
        {
            _strategy.ThreeIncreaseX = TextChanger(sender);
            _strategy.SaveParameters();
        }

        private void ThreeIncreaseY_TextChanged(object sender, TextChangedEventArgs e)
        {
            _strategy.ThreeIncreaseY = TextChangerToDecimal(sender);
            _strategy.SaveParameters();
        }

        private int TextChanger(object sender)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null && !string.IsNullOrEmpty(textBox.Text))
            {                
                if (int.TryParse(textBox.Text, out int num))
                {
                    return num;
                }
                else
                {
                    MessageBox.Show("В параметре должно быть числовое значение", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    textBox.Clear();
                    return 0;
                }
            }
            else if (textBox.Text == "0")
            {
                return 0;
            }
            else
            {
                return 0;
            }
        }

        private decimal TextChangerToDecimal(object sender)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null && !string.IsNullOrEmpty(textBox.Text))
            {                
                if (decimal.TryParse(textBox.Text, out decimal num))
                {
                    
                    return num;
                }
                else
                {
                    MessageBox.Show("В параметре должно быть числовое значение. Для дробного числа нужно использовать запятую", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    textBox.Clear();
                    return 0;
                }
            }
            else if (textBox.Text == "0")
            {
                return 0;
            }
            else
            {
                return 0;
            }
        }

        private void ComboBoxRegime_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _strategy.Regime = (NameRegime)ComboBoxRegime.SelectedValue;
            _strategy.SaveParameters();
        }

        private void LogTabSelected(object sender, RoutedEventArgs e)
        {
            if (ListBoxLog.Items.Count > 1)
            {
                ListBoxLog.ScrollIntoView(ListBoxLog.Items[ListBoxLog.Items.Count - 1]);
            }            
        }

        private void ComboBoxExpir_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
