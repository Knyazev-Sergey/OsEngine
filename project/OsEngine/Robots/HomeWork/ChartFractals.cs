﻿using System.Collections.Generic;
using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.OsTrader.Panels.Attributes;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System;
using System.Windows.Forms.DataVisualization.Charting;
using System.Drawing;
using System.Threading;
using OsEngine.Charts.CandleChart.Indicators;

namespace OsEngine.Robots.HomeWork
{
    [Bot("ChartFractals")]
    public class ChartFractals : BotPanel
    {
        private BotTabSimple _tab;
        private StrategyParameterString _regime;

        private WindowsFormsHost _host;

        private Chart _chart;
        private Candle[] _candleArray;

        private Fractal _fractal;

        public ChartFractals(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            _regime = CreateParameter("Regime", "Off", new string[] { "Off", "On" });

            _fractal = new Fractal(name + "Fractal", false);

            this.ParamGuiSettings.Title = "TableBot Parameters";
            this.ParamGuiSettings.Height = 300;
            this.ParamGuiSettings.Width = 600;
            CustomTabToParametersUi customTab = ParamGuiSettings.CreateCustomTab("Table Parameters");

            CreateChart();

            customTab.AddChildren(_host);

            StartThread();
        }

        private void StartThread()
        {
            Thread worker = new Thread(StartPaintChart) { IsBackground = true };
            worker.Start();
        }

        private void StartPaintChart()
        {
            long countCandles = 0;

            while (true)
            {
                Thread.Sleep(1000);

                if (_tab.Securiti != null)
                {
                    if (_tab.CandlesFinishedOnly != null)
                    {
                        if (countCandles < _tab.CandlesFinishedOnly.Count)
                        {
                            countCandles = _tab.CandlesFinishedOnly.Count;

                            LoadCandleOnChart();

                        }
                    }
                }
            }
        }

        private void CreateChart()
        {
            if (MainWindow.GetDispatcher.CheckAccess() == false)
            {
                MainWindow.GetDispatcher.Invoke(new Action(CreateChart));
                return;
            }

            _host = new WindowsFormsHost();
            _chart = new Chart();
            _host.Child = _chart;
            _host.Child.Show();

            _chart.Series.Clear();
            _chart.ChartAreas.Clear();

            ChartArea candleArea = new ChartArea("ChartAreaCandle"); // создаём область на графике
            candleArea.CursorX.IsUserSelectionEnabled = true;
            candleArea.CursorX.IsUserEnabled = true;                // разрешаем пользователю изменять рамки представления
            candleArea.CursorY.AxisType = AxisType.Secondary;
            candleArea.Position.Height = 70;
            candleArea.Position.X = 0;
            candleArea.Position.Width = 100;
            candleArea.Position.Y = 0;

            _chart.ChartAreas.Add(candleArea); // добавляем область на чарт

            // общее
            for (int i = 0; i < _chart.ChartAreas.Count; i++)
            {
                _chart.ChartAreas[i].CursorX.LineColor = Color.Red;
                _chart.ChartAreas[i].CursorX.LineWidth = 2;
            }

            // подписываемся на события изменения масштабов
            _chart.AxisScrollBarClicked += chart_AxisScrollBarClicked; // событие передвижения курсора
            _chart.AxisViewChanged += chart_AxisViewChanged; // событие изменения масштаба
            _chart.CursorPositionChanged += chart_CursorPositionChanged;// событие выделения диаграммы
        }

        private void LoadCandleOnChart() // формирует серии данных
        {
            _candleArray = _tab.CandlesFinishedOnly.ToArray();

            _fractal.Process(_tab.CandlesFinishedOnly);

            Series candleSeries = new Series("SeriesCandle");
            candleSeries.ChartType = SeriesChartType.Candlestick;// назначаем этой коллекции тип "Свечи"
            candleSeries.YAxisType = AxisType.Secondary;// назначаем ей правую линейку по шкале Y (просто для красоты)
            candleSeries.ChartArea = "ChartAreaCandle";// помещаем нашу коллекцию на ранее созданную область
            candleSeries.ShadowOffset = 2; // наводим тень
            candleSeries.YValuesPerPoint = 4; // насильно устанавливаем число У точек для серии

            Series fractalUpSeries = new Series("SeriesFractalUp");
            fractalUpSeries.ChartType = SeriesChartType.Point;
            fractalUpSeries.YAxisType = AxisType.Secondary;
            fractalUpSeries.ChartArea = "ChartAreaCandle";

            Series fractalDownSeries = new Series("SeriesFractalDown");
            fractalDownSeries.ChartType = SeriesChartType.Point;
            fractalDownSeries.YAxisType = AxisType.Secondary;
            fractalDownSeries.ChartArea = "ChartAreaCandle";

            for (int i = 0; i < _candleArray.Length; i++)
            {
                // забиваем новую свечку
                candleSeries.Points.AddXY(i, _candleArray[i].Low, _candleArray[i].High, _candleArray[i].Open,
                    _candleArray[i].Close);

                // подписываем время
                candleSeries.Points[candleSeries.Points.Count - 1].AxisLabel =
                    _candleArray[i].TimeStart.ToString();

                // разукрышиваем в привычные цвета
                if (_candleArray[i].Close > _candleArray[i].Open)
                {
                    candleSeries.Points[candleSeries.Points.Count - 1].Color = Color.Green;
                }
                else
                {
                    candleSeries.Points[candleSeries.Points.Count - 1].Color = Color.Red;
                }

                // заносим точку фрактала

                if (_fractal.ValuesUp[i] != 0)
                {
                    fractalUpSeries.Points.AddXY(i, _fractal.ValuesUp[i]);
                    fractalUpSeries.Points[fractalUpSeries.Points.Count - 1].Color = _fractal.ColorUp;
                }

                if (_fractal.ValuesDown[i] != 0)
                {
                    fractalDownSeries.Points.AddXY(i, _fractal.ValuesDown[i]);
                    fractalDownSeries.Points[fractalDownSeries.Points.Count - 1].Color = _fractal.ColorDown;
                }

            }
            SetSeries(candleSeries, fractalUpSeries, fractalDownSeries);
        }

        private void SetSeries(Series candleSeries, Series fractalUpSeries, Series fractalDownSeries) // подгружает серии данных на график
        {
            if (MainWindow.GetDispatcher.CheckAccess() == false)
            {
                // перезаходим в метод потоком формы, чтобы не было исключения
                MainWindow.GetDispatcher.Invoke(new Action<Series, Series, Series>(SetSeries),
                    candleSeries, fractalUpSeries, fractalDownSeries);
                return;
            }

            _chart.Series.Clear(); // убираем с нашего графика все до этого созданные серии с данными

            _chart.Series.Add(candleSeries);
            _chart.Series.Add(fractalUpSeries);
            _chart.Series.Add(fractalDownSeries);

            ChartArea candleArea = _chart.ChartAreas.FindByName("ChartAreaCandle");
            if (candleArea != null && candleArea.AxisX.ScrollBar.IsVisible)
            // если уже выбран какой-то диапазон
            {
                // сдвигаем представление вправо
                candleArea.AxisX.ScaleView.Scroll(_chart.ChartAreas[0].AxisX.Maximum);
            }
            ChartResize();
            _chart.Refresh();
        }

        // события
        private void chart_CursorPositionChanged(object sender, CursorEventArgs e)
        // событие изменение отображения диаграммы
        {
            ChartResize();
        }

        private void chart_AxisViewChanged(object sender, ViewEventArgs e)
        // событие изменение отображения диаграммы 
        {
            ChartResize();
        }

        private void chart_AxisScrollBarClicked(object sender, ScrollBarEventArgs e)
        // событие изменение отображения диаграммы
        {
            ChartResize();
        }

        private void ChartResize() // устанавливает границы представления по оси У
        {
            // вообще-то можно это автоматике доверить, но там вечно косяки какие-то, поэтому лучше самому следить за всеми осями
            try
            {
                if (_candleArray == null)
                {
                    return;
                }
                // свечи
                Series candleSeries = _chart.Series.FindByName("SeriesCandle");
                ChartArea candleArea = _chart.ChartAreas.FindByName("ChartAreaCandle");

                if (candleArea == null ||
                    candleSeries == null)
                {
                    return;
                }

                int startPozition = 0; // первая отображаемая свеча
                int endPozition = candleSeries.Points.Count; // последняя отображаемая свеча

                if (_chart.ChartAreas[0].AxisX.ScrollBar.IsVisible)
                {
                    // если уже выбран какой-то диапазон, назначаем первую и последнюю исходя из этого диапазона

                    startPozition = Convert.ToInt32(candleArea.AxisX.ScaleView.Position);
                    endPozition = Convert.ToInt32(candleArea.AxisX.ScaleView.Position) +
                                  Convert.ToInt32(candleArea.AxisX.ScaleView.Size);
                }

                candleArea.AxisY2.Maximum = GetMaxValueOnChart(_candleArray, startPozition, endPozition);
                candleArea.AxisY2.Minimum = GetMinValueOnChart(_candleArray, startPozition, endPozition);


                _chart.Refresh();
            }
            catch (Exception error)
            {
                MessageBox.Show("Обибка при изменении ширины представления. Ошибка: " + error);
            }
        }

        private double GetMinValueOnChart(Candle[] book, int start, int end)
        // берёт минимальное значение из массива свечек
        {
            double result = double.MaxValue;

            for (int i = start; i < end && i < book.Length; i++)
            {
                if ((double)book[i].Low < result)
                {
                    result = (double)book[i].Low;
                }
            }

            return result;
        }

        private double GetMaxValueOnChart(Candle[] book, int start, int end)
        // берёт максимальное значение из массива свечек
        {
            double result = 0;

            for (int i = start; i < end && i < book.Length; i++)
            {
                if ((double)book[i].High > result)
                {
                    result = (double)book[i].High;
                }
            }

            return result;
        }

        public override string GetNameStrategyType()
        {
            return "ChartFractals";
        }

        public override void ShowIndividualSettingsDialog()
        {

        }
    }
}