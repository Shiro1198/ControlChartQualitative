using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using LiveCharts;
using System.Windows;
using LiveCharts.Wpf;
using System.Windows.Media;

namespace ControlChart
{
    public class Tools
    {
        public List<DateValue> FilterDataByDateRange(List<DateValue> data, DateTime startDate, DateTime endDate)
        {
            return data.Where(d => d.Date >= startDate && d.Date <= endDate).ToList();
        }

        /// <summary>
        /// CSVファイルを読み込む
        /// </summary>
        /// <param="filePath"></param>
        /// <returns></returns>
        public List<DateValue> ReadCsvData(string filePath)
        {
            var data = new List<DateValue>();
            try
            {
                var lines = File.ReadAllLines(filePath);
                foreach (var line in lines.Skip(1))
                {
                    var parts = line.Split(',');
                    string strDate = parts[0];
                    if (strDate.Length == 8)
                    {
                        strDate = strDate.Substring(0, 4) + "/" + strDate.Substring(4, 2) + "/" + strDate.Substring(6, 2);
                    }
                    if (DateTime.TryParse(strDate, out DateTime date))
                    {
                        var value = double.Parse(parts[1]);
                        var lotNumber = parts[2];
                        data.Add(new DateValue { Date = date, Value = value, LotNumber = lotNumber });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading CSV file: {ex.Message}");
            }
            return data;
        }

        public void CreateData(string filePath, DateTime startDate, DateTime endDate, string initialLotNo)
        {
            var random = new Random();
            var sb = new StringBuilder();
            sb.AppendLine("Date,Value,LotNo");

            // 初期LotNoを設定
            string currentLotNo = initialLotNo;
            int lotNoCounter = 1;

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                // 10日ごとにLotNoを更新
                if ((date - startDate).Days % 10 == 0)
                {
                    currentLotNo = $"{initialLotNo}-{lotNoCounter}";
                    lotNoCounter++;
                }

                for (int i = 0; i < 2; i++)
                {
                    var value = random.Next(40, 61);
                    sb.AppendLine($"{date:yyyy-MM-dd},{value},{currentLotNo}");
                }
            }

            File.WriteAllText(filePath, sb.ToString());
            Console.WriteLine($"CSV file created at {filePath}");
        }
    }

    /// <summary>
    /// 精度管理チャートの計算を行うクラス
    /// </summary>
    public class ControlChartCalculator
    {
        public int N { get; set; }
        public double A2 { get; set; }
        public double D3 { get; set; }
        public double D4 { get; set; }

        /// <summary>
        /// 精度管理用係数のデータを取得する
        /// </summary>
        /// <returns></returns>
        public static List<ControlChartCalculator> GetCoefficient()
        {
            return new List<ControlChartCalculator>
            {
                new ControlChartCalculator { N = 2, A2 = 1.880, D3 = 0, D4 = 3.267 },
                new ControlChartCalculator { N = 3, A2 = 1.023, D3 = 0, D4 = 2.574 },
                new ControlChartCalculator { N = 4, A2 = 0.729, D3 = 0, D4 = 2.282 },
                new ControlChartCalculator { N = 5, A2 = 0.577, D3 = 0, D4 = 2.114 },
                new ControlChartCalculator { N = 6, A2 = 0.483, D3 = 0, D4 = 2.004 },
                new ControlChartCalculator { N = 7, A2 = 0.419, D3 = 0.076, D4 = 1.924 },
                new ControlChartCalculator { N = 8, A2 = 0.373, D3 = 0.136, D4 = 1.864 },
                new ControlChartCalculator { N = 9, A2 = 0.337, D3 = 0.184, D4 = 1.816 },
                new ControlChartCalculator { N = 10, A2 = 0.308, D3 = 0.223, D4 = 1.777 }
            };
        }

        /// <summary>
        /// XバーチャートのUCLとLCLを計算する
        /// </summary>
        /// <param name="averages"></param>
        /// <param name="ranges"></param>
        /// <returns></returns>        
        public (double UCL, double LCL) Calculate_XbarChart(double overallAverage, ChartValues<DateModel> averages, ChartValues<DateModel> rangeStandard, int N)
        {
            // 精度管理用係数のデータを取得する
            var data = GetCoefficient();
            var entry = data.Find(e => e.N == N);

            // 範囲の平均（分母を -1 する）
            double sumOfRangesStandard = rangeStandard.Sum(a => a.Value);
            double meanOfRangesStandard = sumOfRangesStandard / (rangeStandard.Count() - 1);

            // UCLとLCLを計算
            double UCL = overallAverage + (2.66 * meanOfRangesStandard);
            double LCL = overallAverage - (2.66 * meanOfRangesStandard);

            return (UCL, LCL);
        }

        /// <summary>
        /// XバーチャートのUCLとLCLを計算する
        /// </summary>
        /// <param name="averages"></param>
        /// <param name="ranges"></param>
        /// <returns></returns>        
        public (double UCL, double LCL) Calculate2Sigma_XbarChart(double overallAverage, ChartValues<DateModel> averages, ChartValues<DateModel> rangeStandard, int N)
        {
            // 精度管理用係数のデータを取得する
            var data = GetCoefficient();
            var entry = data.Find(e => e.N == N);

            // 範囲の平均（分母を -1 する）
            double sumOfRangesStandard = rangeStandard.Sum(a => a.Value);
            double meanOfRangesStandard = sumOfRangesStandard / (rangeStandard.Count() - 1);

            // UCLとLCLを計算
            double UCL = overallAverage + (1.77 * meanOfRangesStandard);
            double LCL = overallAverage - (1.77 * meanOfRangesStandard);

            return (UCL, LCL);
        }


        /// <summary>
        /// RチャートのUCLRとLCLRを計算する
        /// </summary>
        /// <param name="R"></param>
        /// <param name="N"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public (double UCL, double LCL) Calculate_RChart(double R, int N)
        {
            // 精度管理用係数のデータを取得する
            var data = GetCoefficient();
            var entry = data.Find(e => e.N == N);

            if (entry == null)
            {
                throw new ArgumentException($"No data found for n = {N}");
            }

            var UCL = entry.D4 * R;
            var LCL = (N <= 6) ? 0 : entry.D3 * R; // LCLRはnが6以下の時は考慮しない

            return (UCL, LCL);
        }

        /// <summary>
        /// RsチャートのUCLRとLCLRを計算する
        /// </summary>
        /// <param name="Rs"></param>
        /// <param name="N"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public double Calculate_RsChart(double Rs, int N)
        {
            return 3.27 * Rs;
        }
        public double Calculate2Siggma_RsChart(double Rs, int N)
        {
            return 2.51 * Rs;
        }

        /// <summary>
        /// 丸め処理
        /// </summary>
        /// <param name="value"></param>
        /// <param name="significantDigits">有効桁数</param>
        /// <param name="decimalPlaces">小数点以下の桁数</param>
        /// <param name="roundingMode">丸め条件（0：四捨五入、1：切り捨て、2：切り上げ）</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public string Marume(double value, int significantDigits, int decimalPlaces, int roundingMode)
        {
            if (significantDigits < 1 || decimalPlaces < 0 || roundingMode < 0 || roundingMode > 2)
            {
                throw new ArgumentException("Invalid parameters");
            }

            double powerOfTen = Math.Pow(10, decimalPlaces);
            double roundedValue;

            // 丸め条件に基づいて値を丸める
            switch (roundingMode)
            {
                case 0:
                    roundedValue = Math.Round(value, decimalPlaces, MidpointRounding.AwayFromZero); // 四捨五入
                    break;
                case 1:
                    roundedValue = Math.Floor(value * powerOfTen) / powerOfTen; // 切り捨て
                    break;
                case 2:
                    roundedValue = Math.Ceiling(value * powerOfTen) / powerOfTen; // 切り上げ
                    break;
                default:
                    throw new ArgumentException("Invalid rounding mode");
            }

            // 有効桁数を考慮して文字列に変換
            string format = $"{{0:G{significantDigits}}}";
            string result = string.Format(format, roundedValue);

            // 小数点以下の桁数を考慮して再フォーマット
            int pointIndex = result.IndexOf('.');
            if (pointIndex >= 0)
            {
                int currentDecimals = result.Length - pointIndex - 1;
                if (currentDecimals < decimalPlaces)
                {
                    result = result.PadRight(pointIndex + 1 + decimalPlaces, '0');
                }
            }
            else
            {
                if (decimalPlaces > 0)
                {
                    result += "." + new string('0', decimalPlaces);
                }
            }

            return result;
        }
        /// <summary>
        /// 指定された小数点以下の桁数で double 型の数値を文字列に変換する
        /// </summary>
        /// <param name="value">変換する double 型の数値</param>
        /// <param name="decimalPlaces">小数点以下の桁数</param>
        /// <returns>指定された小数点以下の桁数でフォーマットされた文字列</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public string ConvertDoubleToString(double value, int decimalPlaces)
        {
            if (decimalPlaces < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(decimalPlaces), "小数点以下の桁数は0以上でなければなりません");
            }

            string format = "F" + decimalPlaces;
            return value.ToString(format);
        }
        /// <summary>
        /// チャートにセグメントを追加する
        /// </summary>
        /// <param name="cChart"></param>
        /// <param name="segment"></param>
        /// <param name="color"></param>
        /// <param name="title"></param>
        public void AddSegmentToChart(CartesianChart cChart, ChartValues<DateModel> segment, Brush color, string title)
        {
            try
            {
                if (segment.Count > 0)
                {
                    var lineSeries = new LineSeries
                    {
                        Title = title,
                        Values = segment,
                        PointGeometry = DefaultGeometries.Circle,
                        PointGeometrySize = 10,
                        Stroke = color,
                        Fill = Brushes.Transparent,
                        LineSmoothness = 0,
                        DataLabels = true
                    };
                    cChart.Series.Add(lineSeries);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error AddSegmentToChart(): {ex.Message}");
            }
        }

        /// <summary>
        /// 指定した値に水平線を追加する
        /// </summary>
        /// <param name="chart">チャート</param>
        /// <param name="value">値</param>
        /// <param name="title">タイトル</param>
        /// <param name="color">色</param>
        /// <param name="dashStyle">線の種類</param>
        public void AddConstantLine(CartesianChart chart, double value, string title, Brush color, DoubleCollection dashStyle)
        {
            try
            {
                var line = new LineSeries
                {
                    Title = title,
                    Values = new ChartValues<DateModel>
                    {
                        new DateModel { Sequence = 1, Value = value },
                        new DateModel { Sequence = chart.AxisX[0].Labels.Count, Value = value }
                    },
                    Stroke = color,
                    Fill = Brushes.Transparent,
                    PointGeometry = null,
                    StrokeDashArray = dashStyle
                };
                chart.Series.Add(line);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding constant line: {ex.Message}");
            }
        }


    }
}
