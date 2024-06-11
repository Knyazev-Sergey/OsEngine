/*
 * Your rights to use code governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using OsEngine.Entity;
using OsEngine.Language;
using OsEngine.OsTrader.Panels;

namespace OsEngine.Robots.DeribitPI
{
    public partial class DeribitPIUi
    {
        private DeribitPI _strategy;

        public DeribitPIUi(DeribitPI strategy)
        {
            InitializeComponent();
            OsEngine.Layout.StickyBorders.Listen(this);
            _strategy = strategy;

            ComboBoxRegime.Items.Add("Off");
            ComboBoxRegime.Items.Add("Начать набор конструкции");
            ComboBoxRegime.Items.Add("Разобрать конструкцию");
            ComboBoxRegime.Items.Add("Остановить торговлю фьючерсом");
            ComboBoxRegime.SelectedItem = _strategy.Regime;

            StartThread();

            /* TextBoxVolumeOne.Text = _strategy.Volume.ToString();

             ComboBoxDirection.Items.Add(Side.Buy);
             ComboBoxDirection.Items.Add(Side.Sell);
             ComboBoxDirection.SelectedItem = _strategy.Direction;*/

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

                //Dispatcher.Invoke(() => LabelTextInfo.Text = _strategy.viewModel);
                Dispatcher.Invoke(new Action(UpdateText));               
            }
        }

        private void UpdateText()
        {
            LabelTextInfo.Text = _strategy.ViewModel;
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
    }
}
