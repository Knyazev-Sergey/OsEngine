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
        //public bool CheckBoxTest;

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

                Dispatcher.Invoke(new Action(UpdateText));               
            }
        }

        private void UpdateText()
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
            TextBox textBox = sender as TextBox;
            if (textBox != null && !string.IsNullOrEmpty(textBox.Text))
            {
                _strategy.PercentOfDeposit = int.Parse(textBox.Text);
            }
            else if (textBox.Text == "0")
            {
                _strategy.PercentOfDeposit = 0;
            }
            else
            {
                _strategy.PercentOfDeposit = 0;
            }
        }
    }
}
