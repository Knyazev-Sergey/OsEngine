using OsEngine.Entity;
using OsEngine.Robots.AlexBots;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Threading;
using OsEngine.Layout;
using OsEngine.Charts.CandleChart;

namespace OsEngine.Robots.AlexBots
{
    /// <summary>
    /// Interaction logic for CandleChartUiPair.xaml
    /// </summary>
    public partial class CandleChartUiPair : Window
    {
        public CandleChartUiPair(string nameUniq, StartProgram startProgramm, PairArbitrageAssistant assistant)
        {
            InitializeComponent();
            OsEngine.Layout.StickyBorders.Listen(this);
            GlobalGUILayout.Listen(this, "Chart " + nameUniq);

            _chart = new ChartCandleMaster(nameUniq, startProgramm);
            _chart.StartPaint(GridChart, ChartHostPanel, RectChart);

            _assistant = assistant;

            Title = "Pair Assistant: " + assistant.NameStrategyUniq;

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

        public void ClearChart()
        {
            _chart.Clear();
        }

        private bool _isDeleted = false;

        PairArbitrageAssistant _assistant;

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
                    _assistant.SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
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
