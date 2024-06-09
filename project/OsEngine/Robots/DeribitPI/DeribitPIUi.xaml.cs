/*
 * Your rights to use code governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Windows;
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
            OsEngine.Layout.StartupLocation.Start_MouseInCentre(this);
            _strategy = strategy;

            ComboBoxRegime.Items.Add(BotTradeRegime.Off);
            ComboBoxRegime.Items.Add(BotTradeRegime.On);
            ComboBoxRegime.SelectedItem = _strategy.Regime;

           /* TextBoxVolumeOne.Text = _strategy.Volume.ToString();

            ComboBoxDirection.Items.Add(Side.Buy);
            ComboBoxDirection.Items.Add(Side.Sell);
            ComboBoxDirection.SelectedItem = _strategy.Direction;*/

            this.Activate();
            this.Focus();
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
