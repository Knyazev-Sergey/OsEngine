/*
 * Your rights to use code governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using OsEngine.Language;

namespace OsEngine.Robots.DeribitPI
{
    public partial class DeribitPIUi
    {
        private DeribitPI _strategy;
        public bool _visibleParameters;

        public DeribitPIUi(DeribitPI strategy)
        {
            InitializeComponent();
            OsEngine.Layout.StickyBorders.Listen(this);
            _strategy = strategy;

            ComboBoxRegime.Items.Add(new { Value = NameRegime.Off, Description = "Выключен" });
            ComboBoxRegime.Items.Add(new { Value = NameRegime.AssemblyConstruction, Description = "Набор конструкции" });
            ComboBoxRegime.Items.Add(new { Value = NameRegime.DisassemblyConstruction, Description = "Разбор конструкции" });
            ComboBoxRegime.Items.Add(new { Value = NameRegime.TradeFutures, Description = "Торговля фьючерсами" });
            ComboBoxRegime.Items.Add(new { Value = NameRegime.StopTradeFutures, Description = "Остановить торговлю фьючерсом" });
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
            for (int i = 0; i < _strategy.LogList.Count; i++)
            {
                ListBoxLog.Items.Add(_strategy.LogList[i]);
                if (ListBoxLog.Items.Count > 500)
                {
                    ListBoxLog.Items.RemoveAt(0);
                }
            } 

            StartThread();

            this.Activate();
            this.Focus();
        }

        private void StartThread()
        {
            Thread worker = new Thread(StartText) { IsBackground = true };
            worker.Start();
        }

        private void StartText() 
        {           
            while (true)
            {
                Thread.Sleep(1000);

                Dispatcher.Invoke(new Action(UpdateWpf));
                Dispatcher.Invoke(new Action(VisibleParameters));
            }
        }

        private void VisibleParameters()
        {
            if (_strategy.Regime != NameRegime.Off)
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
          
            while (!_strategy.ListLog.IsEmpty)
            {
                _strategy.ListLog.TryDequeue(out string message);

                if (message != null)
                {                    
                    ListBoxLog.Items.Add(message);

                    while (ListBoxLog.Items.Count > 500)
                    {
                        ListBoxLog.Items.RemoveAt(0);
                    }
                }
            }
           
            
            

           /* ListBoxLog.Items.Clear();
            for (int i = 0; i < _strategy.LogList.Count; i++)
            {
                ListBoxLog.Items.Add(_strategy.LogList[i]);
            }*/
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                /*if (TextBoxVolumeOne.Text.ToDecimal() <= 0)
                {
                    throw new Exception("");
                }*/
            }
            catch (Exception)
            {
                MessageBox.Show(OsLocalization.Trader.Label13);
                return;
            }

           /* _strategy.Volume = TextBoxVolumeOne.Text.ToDecimal();
            Enum.TryParse(ComboBoxRegime.Text, true, out _strategy.Regime);
            Enum.TryParse(ComboBoxDirection.Text, true, out _strategy.Direction);*/

            Close();
        }

        public enum NameRegime
        {
            Off,
            AssemblyConstruction,
            DisassemblyConstruction,
            TradeFutures,
            StopTradeFutures
        }
                
        private void LabelPercentDeposit_TextChanged(object sender, TextChangedEventArgs e)
        {
            _strategy.PercentOfDeposit = TextChanger(sender);
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
            _strategy.OneIncreaseY = TextChanger(sender);
            _strategy.SaveParameters();
        }

        private void TwoIncreaseX_TextChanged(object sender, TextChangedEventArgs e)
        {
            _strategy.TwoIncreaseX = TextChanger(sender);
            _strategy.SaveParameters();
        }

        private void TwoIncreaseY_TextChanged(object sender, TextChangedEventArgs e)
        {
            _strategy.TwoIncreaseY = TextChanger(sender);
            _strategy.SaveParameters();
        }

        private void ThreeIncreaseX_TextChanged(object sender, TextChangedEventArgs e)
        {
            _strategy.ThreeIncreaseX = TextChanger(sender);
            _strategy.SaveParameters();
        }

        private void ThreeIncreaseY_TextChanged(object sender, TextChangedEventArgs e)
        {
            _strategy.ThreeIncreaseY = TextChanger(sender);
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

        private void ComboBoxRegime_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _strategy.Regime = (NameRegime)ComboBoxRegime.SelectedValue;
            _strategy.SaveParameters();
        }
    }
}
