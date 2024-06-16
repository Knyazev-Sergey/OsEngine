/*
 * Your rights to use code governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using OsEngine.Entity;
using OsEngine.Language;
using OsEngine.OsTrader.Panels;
using System.ComponentModel;

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

            _strategy.CheckTestServer = CheckTestServer();

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
                LabelPauseBuyOption.IsReadOnly = true;
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
                LabelPauseBuyOption.IsReadOnly = false;
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
            TextLastPrice.Text = _strategy.LastPrice.ToString();
            TextCurrStrike.Text = _strategy.CurrentStrike;
            TextPriceOption.Text = _strategy.PriceOption.ToString();
            TextSizeOption.Text = _strategy.SizeOption.ToString();

            TextDeposit.Text = _strategy.Deposit.ToString();
            
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

        public bool CheckTestServer()
        {
            bool checkbox = false;

            if (checkBoxTest.IsChecked == true)
            {
                checkbox = true;
            }
           
            return checkbox;
        }

        private void checkBoxTest_Checked(object sender, RoutedEventArgs e)
        {
            if (_strategy == null)
            {
                return;
            }
            _strategy.CheckTestServer = true;
        }
        private void checkBoxTest_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_strategy == null)
            {
                return;
            }
            _strategy.CheckTestServer = false;
        }

        private void LabelPercentDeposit_TextChanged(object sender, TextChangedEventArgs e)
        {
            _strategy.PercentOfDeposit = TextChanger(sender);            
        }

        private void CountIteration_TextChanged(object sender, TextChangedEventArgs e)
        {
            _strategy.CountIteration = TextChanger(sender);
        }

        private void TimeToCloseOption_TextChanged(object sender, TextChangedEventArgs e)
        {
            _strategy.TimeToCloseOption = TextChanger(sender);            
        }

        private void LabelTimeFuturesLimit_TextChanged(object sender, TextChangedEventArgs e)
        {
            _strategy.TimeFuturesLimit = TextChanger(sender);
        }

        private void checkBoxMarketOrder_Checked(object sender, RoutedEventArgs e)
        {
            if (_strategy == null)
            {
                return;
            }
            _strategy.CheckBoxMarketOrder = true;
        }
        private void checkBoxMarketOrder_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_strategy == null)
            {
                return;
            }
            _strategy.CheckBoxMarketOrder = false;
        }

        private void LabelTimeOptionLimit_TextChanged(object sender, TextChangedEventArgs e)
        {
            _strategy.TimeOptionLimit = TextChanger(sender);
        }

        private void LabelPauseBuyOption_TextChanged(object sender, TextChangedEventArgs e)
        {
            _strategy.PauseBuyOption = TextChanger(sender);
        }

        private void CountWorkParts_TextChanged(object sender, TextChangedEventArgs e)
        {
            _strategy.CountWorkParts = TextChanger(sender);            
        }

        private void RatioWorkParts_TextChanged(object sender, TextChangedEventArgs e)
        {
            _strategy.RatioWorkParts = TextChanger(sender);
        }

        private void OneIncreaseX_TextChanged(object sender, TextChangedEventArgs e)
        {
            _strategy.OneIncreaseX = TextChanger(sender);
        }

        private void OneIncreaseY_TextChanged(object sender, TextChangedEventArgs e)
        {
            _strategy.OneIncreaseY = TextChanger(sender);
        }

        private void TwoIncreaseX_TextChanged(object sender, TextChangedEventArgs e)
        {
            _strategy.TwoIncreaseX = TextChanger(sender);
        }

        private void TwoIncreaseY_TextChanged(object sender, TextChangedEventArgs e)
        {
            _strategy.TwoIncreaseY = TextChanger(sender);
        }

        private void ThreeIncreaseX_TextChanged(object sender, TextChangedEventArgs e)
        {
            _strategy.ThreeIncreaseX = TextChanger(sender);
        }

        private void ThreeIncreaseY_TextChanged(object sender, TextChangedEventArgs e)
        {
            _strategy.ThreeIncreaseY = TextChanger(sender);
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
        }
    }
}
