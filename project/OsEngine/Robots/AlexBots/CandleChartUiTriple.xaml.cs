using OsEngine.Charts.CandleChart;
using OsEngine.Entity;
using OsEngine.Layout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OsEngine.Robots.AlexBots
{
    /// <summary>
    /// Interaction logic for CandleChartUiTriple.xaml
    /// </summary>
    public partial class CandleChartUiTriple : Window
    {
        public CandleChartUiTriple(string nameUniq, StartProgram startProgramm, TripleArbitrageAssistant assistant)
        {
            InitializeComponent();
            OsEngine.Layout.StickyBorders.Listen(this);
            GlobalGUILayout.Listen(this, "Chart " + nameUniq);

            _chart = new ChartCandleMaster(nameUniq, startProgramm);
            _chart.StartPaint(GridChart, ChartHostPanel, RectChart);

            _assistant = assistant;

            Title = "Triple Assistant: " + assistant.NameStrategyUniq;

            this.Closed += CandleChartUi_Closed;

            Thread worker = new Thread(PainterThreadArea);
            worker.Start();
        }

        private void CandleChartUi_Closed(object sender, EventArgs e)
        {
            try
            {
                _chart.StopPaint();
                _isDeleted = true;
            }
            catch
            {
                // ignore
            }
        }

        private bool _isDeleted = false;

        TripleArbitrageAssistant _assistant;

        public ChartCandleMaster _chart;

        // прорисовка

        private void PainterThreadArea()
        {
            while (true)
            {
                Thread.Sleep(5000);

                if (_isDeleted == true)
                {
                    return;
                }
                try
                {
                    TryRePaintChartTriple();
                }
                catch (Exception ex)
                {
                    _assistant.Tab1.SetNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
                }
            }
        }

        private void TryRePaintChartTriple()
        {
            List<Candle> candles = _assistant.PriceModule.GetSpreadCandles();

            if (candles == null ||
                candles.Count == 0)
            {
                return;
            }

            _chart.SetCandles(candles);
        }
    }
}
