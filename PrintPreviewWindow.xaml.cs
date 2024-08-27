using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Wpf.Charts.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Xps;
using FontFamily = System.Windows.Media.FontFamily;

namespace ControlChart
{
    public partial class PrintPreviewWindow : Window
    {
        public M_CTRL_CHART mCtrlChart { get; set; }        // マスタ（コントロールチャート）
        public M_CTRL_TUBE mCtrlTube { get; set; }          // マスタ（コントロールチューブ）
        public DateTime? startDate { get; set; }            // 開始日
        public DateTime? endDate { get; set; }              // 終了日

        private Tools dataGenerator = new Tools();          // データ生成クラス

        public PrintPreviewWindow()
        {
            InitializeComponent();

            // メインウィンドウのデータコンテキストを取得して設定する
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                DataContext = mainWindow.DataContext;
            }
            // Loaded イベントを追加
            this.Loaded += PrintPreviewWindow_Loaded;
        }

        /// <summary>
        /// 画面表示時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PrintPreviewWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.textK_CODE.Text = mCtrlChart.K_CODE;                   // 項目コード
            this.textK_NAME.Text = mCtrlChart.K_NAME;                   // 項目名称
            this.textASSAY_STYLE.Text = mCtrlChart.ASSAY_STYLE;         // 検査方法
            this.textASSAY_UNIT_NAME.Text = mCtrlChart.ASSAY_UNIT_NAME; // 検査単位名称
            this.textMNG_NAME.Text = mCtrlTube.MNG_NAME;                // 管理試験名称
            this.textCTRLPARM_DISP_SU.Text = mCtrlTube.CTRLPARM_DISP_SU.ToString(); // CTRL係数・表示本数
            if (startDate == null) return;
            if (endDate == null) return;
            DateTime sDate = startDate.Value;
            DateTime eDate = endDate.Value;
            this.textJissiDate.Text = $"{sDate.ToString("yyyy年MM月dd日")}　～　{eDate.ToString("yyyy年MM月dd日")}"; // 開始日～終了日
            try
            {
                var filteredData = dataGenerator.FilterDataByDateRange(dataGenerator.ReadCsvData("data.csv"), startDate.Value, endDate.Value);
                DisplayCharts(filteredData);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating chart: {ex.Message}");
            }

        }
        /// <summary>
        /// コントロールチャートを更新する
        /// </summary>
        /// <param name="data"></param>
        private void DisplayCharts(List<DateValue> data)
        {
            ControlChartCalculator controlChartCalculator = new ControlChartCalculator();

            // 日付ごとにグループ化し、ロット番号も取得
            var groupedData = data.GroupBy(d => new { d.Date, d.LotNumber })
                                  .Select(g => new { g.Key.Date, g.Key.LotNumber, Values = g.Select(d => d.Value).ToList() }).ToList();
            if (groupedData.Count <= 1)
            {
                MessageBox.Show("対象データが２日以上ありません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            int significantDigits = int.Parse(mCtrlChart.VALID_COL.Substring(0, 1));    // 有効桁数
            int decimalPlaces = int.Parse(mCtrlChart.VALID_COL.Substring(1, 1));        // 小数点以下桁数
            int roundingMode = mCtrlChart.MARU_COND;                                    // 丸め条件

            // チャートに表示するデータを作成
            var averages = new ChartValues<DateModel>();
            var ranges = new ChartValues<DateModel>();
            var rangeStandard = new ChartValues<DateModel>();

            int sequence = 1;
            var befAvg = 0.0;
            foreach (var group in groupedData)
            {
                // 平均値
                string strAve = controlChartCalculator.Marume(group.Values.Average(), significantDigits, decimalPlaces + 1, roundingMode);
                var avg = Math.Round(double.Parse(strAve), decimalPlaces + 1);
                // 最大値を取得
                string strMax = controlChartCalculator.Marume(group.Values.Max(), significantDigits, decimalPlaces, roundingMode);
                var max = Math.Round(double.Parse(strMax), decimalPlaces);
                // 最小値を取得
                string strMin = controlChartCalculator.Marume(group.Values.Min(), significantDigits, decimalPlaces, roundingMode);
                var min = Math.Round(double.Parse(strMin), decimalPlaces);
                // レンジを取得
                var range = Math.Round(max - min, decimalPlaces);
                // 日差
                var rs = sequence == 1 ? 0 : Math.Round(Math.Abs(avg - befAvg), decimalPlaces + 1);
                // データ数を取得
                var count = group.Values.Count();                               // データ数を取得

                averages.Add(new DateModel { Sequence = sequence, DateTime = group.Date, Value = avg, Max = max, Min = min, Count = count, LotNumber = group.LotNumber });
                rangeStandard.Add(new DateModel { Sequence = sequence, DateTime = group.Date, Value = rs, Count = count, LotNumber = group.LotNumber });
                ranges.Add(new DateModel { Sequence = sequence, DateTime = group.Date, Value = range, Count = count, LotNumber = group.LotNumber });
                befAvg = avg;
                sequence++;
            }

            var gridData = averages.Zip(ranges, (avg, range) => new
            {
                avg.Sequence,
                avg.DateTime,
                Average = controlChartCalculator.ConvertDoubleToString(avg.Value, decimalPlaces),
                Range = controlChartCalculator.ConvertDoubleToString(range.Value, decimalPlaces),
                Max = controlChartCalculator.ConvertDoubleToString(avg.Max, decimalPlaces),
                Min = controlChartCalculator.ConvertDoubleToString(avg.Min, decimalPlaces),
                avg.Count,
                avg.LotNumber
            }).Zip(rangeStandard, (temp, rs) => new
            {
                temp.Sequence,
                temp.DateTime,
                temp.Average,
                temp.Range,
                temp.Max,
                temp.Min,
                temp.Count,
                temp.LotNumber,
                RangeStandard = controlChartCalculator.ConvertDoubleToString(rs.Value, decimalPlaces + 1)
            });
            // データグリッドに全ての値を表示
            string qcLotNo = "";
            foreach (var item in gridData)
            {
                setTextBlock("Seq" + item.Sequence.ToString(), item.Sequence.ToString());
                setTextBlock("Date" + item.Sequence.ToString(), item.DateTime.ToString("M.d"));
                setTextBlock("Xbar" + item.Sequence.ToString(), item.Average);
                setTextBlock("Xmax" + item.Sequence.ToString(), item.Max);
                setTextBlock("Xmin" + item.Sequence.ToString(), item.Min);
                setTextBlock("Rs" + item.Sequence.ToString(), item.RangeStandard);
                setTextBlock("R" + item.Sequence.ToString(), item.Range);
                setTextBlock("Lot" + item.Sequence.ToString(), item.LotNumber);
                if (item.LotNumber != qcLotNo)
                {
                    qcLotNo = item.LotNumber;
                    textQCLOT_NO.Text = qcLotNo;
                }
            }
            // シーケンス番号のリストを作成
            List<string> sequenceLabels = new List<string> { "" };
            sequenceLabels.AddRange(averages.Select(av => av.Sequence.ToString()));
            int intN = mCtrlTube.CTRLPARM_DISP_SU;      // サンプル数

            dspChart_XBar(averages, rangeStandard, sequenceLabels, intN);
            dspChart_Rs(averages, rangeStandard, sequenceLabels, intN);
            dspChart_R(ranges, sequenceLabels, intN);

            // チャートをリフレッシュ
            XBarChart.Update(true, true);
            RsChart.Update(true, true);
            RChart.Update(true, true);
        }
        private void setTextBlock(string strName, string strText)
        {
            var textblock = FindName(strName) as TextBlock;
            if (textblock != null)
                textblock.Text = strText;
        }
        /// <summary>
        /// X Barチャートを表示する
        /// </summary>
        /// <param name="averages"></param>
        /// <param name="sequenceLabels"></param>
        private void dspChart_XBar(ChartValues<DateModel> averages, ChartValues<DateModel> rangeStandard, List<string> sequenceLabels, int N)
        {
            int significantDigits = int.Parse(mCtrlChart.VALID_COL.Substring(0, 1));    // 有効桁数
            int decimalPlaces = int.Parse(mCtrlChart.VALID_COL.Substring(1, 1));        // 小数点以下桁数
            int roundingMode = mCtrlChart.MARU_COND;                                    // 丸め条件

            ControlChartCalculator controlChartCalculator = new ControlChartCalculator();

            // 平均
            string strAverage = controlChartCalculator.Marume(averages.Average(a => a.Value), significantDigits, decimalPlaces + 1, roundingMode);
            var overallAverage = double.Parse(strAverage);

            // UCLとLCL
            var (ucl, lcl) = controlChartCalculator.Calculate_XbarChart(overallAverage, averages, rangeStandard, N);
            string strUCL = controlChartCalculator.Marume(ucl, significantDigits, decimalPlaces + 1, roundingMode);
            var UCL = double.Parse(strUCL);
            var strLCL = controlChartCalculator.Marume(lcl, significantDigits, decimalPlaces + 1, roundingMode);
            var LCL = double.Parse(strLCL);

            var (ucl2sigma, lcl2sigma) = controlChartCalculator.Calculate2Sigma_XbarChart(overallAverage, averages, rangeStandard, N);
            string strUCL2Sigma = controlChartCalculator.Marume(ucl2sigma, significantDigits, decimalPlaces + 1, roundingMode);
            var UCL2Sigma = double.Parse(strUCL2Sigma);
            var strLCL2Sigma = controlChartCalculator.Marume(lcl2sigma, significantDigits, decimalPlaces + 1, roundingMode);
            var LCL2Sigma = double.Parse(strLCL2Sigma);

            // 標準偏差,2SD,3SDの計算
            var standardDeviation = Math.Sqrt(averages.Average(a => Math.Pow(a.Value - overallAverage, 2)));
            var cv = standardDeviation / overallAverage;
            var plus2SD = overallAverage + (2 * (cv * overallAverage));
            var minus2SD = overallAverage - (2 * (cv * overallAverage));

            XBarChart.Series.Clear();
            XBarChart.AxisX.Clear();
            XBarChart.AxisY.Clear();

            // LotNumberの変化を検出し、変化点でセグメントを分割する
            var currentLotNumber = averages.First().LotNumber;
            var currentSegment = new ChartValues<DateModel>();
            var colors = new List<Brush> { Brushes.Blue, Brushes.Red, Brushes.Green, Brushes.Orange, Brushes.Purple };
            int colorIndex = 0;
            foreach (var data in averages)
            {
                if (data.LotNumber != currentLotNumber)
                {
                    // 新しいセグメントを追加
                    controlChartCalculator.AddSegmentToChart(XBarChart, currentSegment, colors[colorIndex % colors.Count], "Xbar");
                    colorIndex++;
                    currentSegment = new ChartValues<DateModel>();
                    currentLotNumber = data.LotNumber;
                }
                currentSegment.Add(data);
            }
            // 最後のセグメントを追加
            controlChartCalculator.AddSegmentToChart(XBarChart, currentSegment, colors[colorIndex % colors.Count], "Xbar");

            // X軸の設定
            var axisX = new Axis
            {
                Title = "",
                Labels = sequenceLabels,
                Separator = new LiveCharts.Wpf.Separator { Step = 1 },
                MinValue = 1,
                MaxValue = sequenceLabels.Count - 1
            };
            XBarChart.AxisX.Add(axisX);

            // Y軸の設定
            var axisY = new Axis
            {
                Title = "",
                MinValue = LCL,
                MaxValue = UCL,
            };
            XBarChart.AxisY.Add(axisY);

            // 2SDおよび3SDのラインをチャートに追加
            controlChartCalculator.AddConstantLine(XBarChart, UCL, "UCL", Brushes.Orange, null);                                    // 実線で表示
            controlChartCalculator.AddConstantLine(XBarChart, UCL2Sigma, "2σ", Brushes.Orange, new DoubleCollection { 1, 2 });     // 点線で表示
            controlChartCalculator.AddConstantLine(XBarChart, overallAverage, "平均", Brushes.Blue, new DoubleCollection { 2, 2 }); // 破線で表示
            controlChartCalculator.AddConstantLine(XBarChart, LCL2Sigma, "-2σ", Brushes.Orange, new DoubleCollection { 1, 2 });
            controlChartCalculator.AddConstantLine(XBarChart, LCL, "LCL", Brushes.Orange, null);

            textCV_S.Text = $"{cv:F2} %";
            textSD_S.Text = $"{standardDeviation:F3}";

            // UCLを表示
            lblUCLValue.Content = $"UCL: {strUCL}";
            textUCL_Xbar.Text = strUCL;
            textUCL_Xbar_M.Text = "";
            // 2SDを表示
            lbl2SDValue.Content = $"+2σ: {strUCL2Sigma}";
            text2Sigma_Xber.Text = strUCL2Sigma;
            text2Sigma_Xber_M.Text = "";
            // 平均を表示
            lblAverageValue.Content = $"CL: {strAverage}";
            textXbar.Text = strAverage;
            textXbar_M.Text = "";
            // -2SDを表示
            lblMinus2SDValue.Content = $"-2σ: {strLCL2Sigma}";
            textMinus2Sigma_Xbar.Text = strLCL2Sigma;
            textMinus2Sigma_Xbar_M.Text = "";
            // LCLを表示
            lblLCLValue.Content = $"LCL: {strLCL}";
            textLCL_Xbar.Text = strLCL;
            textLCL_Xbar_M.Text = "";
        }

        /// <summary>
        /// Rsチャートを表示する
        /// </summary>
        /// <param name="rangeStandard"></param>
        /// <param name="sequenceLabels"></param>
        private void dspChart_Rs(ChartValues<DateModel> averages, ChartValues<DateModel> rangeStandard, List<string> sequenceLabels, int N)
        {
            int significantDigits = int.Parse(mCtrlChart.VALID_COL.Substring(0, 1));    // 有効桁数
            int decimalPlaces = int.Parse(mCtrlChart.VALID_COL.Substring(1, 1));        // 小数点以下桁数
            int roundingMode = mCtrlChart.MARU_COND;                                    // 丸め条件

            ControlChartCalculator controlChartCalculator = new ControlChartCalculator();

            // 平均
            var rs = rangeStandard.Where(a => a.Sequence != 1).Average(a => a.Value);
            string strRs = controlChartCalculator.Marume(rs, significantDigits, decimalPlaces + 1, roundingMode);
            var overallAverageRs = double.Parse(strRs);

            // UCL
            var ucl = controlChartCalculator.Calculate_RsChart(overallAverageRs, N);
            var rsUCL = Math.Round(ucl, decimalPlaces + 1);
            var ucl2 = controlChartCalculator.Calculate2Siggma_RsChart(overallAverageRs, N);
            var rsUCL2Sigma = Math.Round(ucl2, decimalPlaces + 1);

            // LCL
            var rsLCL = 0.0;

            // 標準偏差の計算（標本標準偏差）
            var variance = rangeStandard.Where(a => a.Sequence != 1).Average(a => Math.Pow(a.Value - overallAverageRs, 2));
            var standardDeviation = Math.Sqrt(variance);
            // 変動係数の計算
            var cv = standardDeviation / overallAverageRs;

            RsChart.Series.Clear();
            RsChart.AxisX.Clear();
            RsChart.AxisY.Clear();

            // X軸の設定
            var rsAxisX = new Axis
            {
                Title = "",
                Labels = sequenceLabels,
                Separator = new LiveCharts.Wpf.Separator { Step = 1 },
                MinValue = 1,
                MaxValue = sequenceLabels.Count - 1
            };
            RsChart.AxisX.Add(rsAxisX);

            // Y軸の設定
            var rsAxisY = new Axis
            {
                Title = "",
                MinValue = 0,
                MaxValue = rsUCL,
            };
            RsChart.AxisY.Add(rsAxisY);


            // LotNumberの変化を検出し、変化点でセグメントを分割する
            var currentLotNumber = rangeStandard.First().LotNumber;
            var currentSegment = new ChartValues<DateModel>();
            var colors = new List<Brush> { Brushes.Blue, Brushes.Red, Brushes.Green, Brushes.Orange, Brushes.Purple };
            int colorIndex = 0;

            foreach (var data in rangeStandard)
            {
                if (data.LotNumber != currentLotNumber)
                {
                    // 新しいセグメントを追加
                    controlChartCalculator.AddSegmentToChart(RsChart, currentSegment, colors[colorIndex % colors.Count], "Rs");
                    colorIndex++;
                    currentSegment = new ChartValues<DateModel>();
                    currentLotNumber = data.LotNumber;
                }
                if (data.Sequence == 1)
                    data.Value = double.NaN;
                currentSegment.Add(data);
            }
            // 最後のセグメントを追加
            controlChartCalculator.AddSegmentToChart(RsChart, currentSegment, colors[colorIndex % colors.Count], "Rs");

            // 平均（実線）、UCL（破線）のラインをチャートに追加
            controlChartCalculator.AddConstantLine(RsChart, overallAverageRs, "CL", Brushes.Blue, new DoubleCollection { 2, 2 });
            controlChartCalculator.AddConstantLine(RsChart, rsUCL, "UCL", Brushes.Orange, null);
            controlChartCalculator.AddConstantLine(RsChart, rsUCL2Sigma, "2σ", Brushes.Orange, new DoubleCollection { 1, 2 });
            controlChartCalculator.AddConstantLine(RsChart, rsLCL, "LCL", Brushes.Orange, null);

            // 管理値Rsをラベルに表示
            var overallAverage = averages.Where(a => a.Sequence != 1).Average(a => a.Value);

            // 日差変動
            textCV_N.Text = $"{cv:F2} %";
            textSD_N.Text = $"{standardDeviation:F3}";


            // 平均を表示
            lblRsAverageValue.Content = $"CL: {strRs}";
            textRs.Text = strRs;
            textRs_M.Text = "";
            // UCLを表示
            lblRsUCL.Content = $"UCL: {controlChartCalculator.ConvertDoubleToString(rsUCL, decimalPlaces + 1)}";
            textUCL_Rs.Text = controlChartCalculator.ConvertDoubleToString(rsUCL, decimalPlaces + 1);
            textUCL_Rs_M.Text = "";
            // 2SDを表示
            lblRs2Sigma.Content = $"2σ: {controlChartCalculator.ConvertDoubleToString(rsUCL2Sigma, decimalPlaces + 1)}";
            text2Sigma_Rs.Text = controlChartCalculator.ConvertDoubleToString(rsUCL2Sigma, decimalPlaces + 1);
            text2Sigma_Rs_M.Text = "";
            // LCLを表示
            string sLCL = controlChartCalculator.ConvertDoubleToString(rsLCL, decimalPlaces + 1);
            lblRsLCL.Content = $"LCL: {sLCL}";
        }

        /// <summary>
        /// Rチャートを表示する
        /// </summary>
        /// <param name="ranges"></param>
        /// <param name="sequenceLabels"></param>
        private void dspChart_R(ChartValues<DateModel> ranges, List<string> sequenceLabels, int N)
        {
            int significantDigits = int.Parse(mCtrlChart.VALID_COL.Substring(0, 1));    // 有効桁数
            int decimalPlaces = int.Parse(mCtrlChart.VALID_COL.Substring(1, 1));        // 小数点以下桁数
            int roundingMode = mCtrlChart.MARU_COND;                                    // 丸め条件

            ControlChartCalculator controlChartCalculator = new ControlChartCalculator();

            // RチャートのY軸の最小値と最大値を設定
            var r = ranges.Average(a => a.Value);
            string strR = controlChartCalculator.Marume(r, significantDigits, decimalPlaces + 1, roundingMode);
            var overallAverageR = double.Parse(strR);
            var (ucl, lcl) = controlChartCalculator.Calculate_RChart(overallAverageR, N);
            string strUCL = controlChartCalculator.Marume(ucl, significantDigits, decimalPlaces + 1, roundingMode);
            var UCL = double.Parse(strUCL);

            // 標準偏差と変動係数の計算
            var standardDeviation = Math.Sqrt(ranges.Average(a => Math.Pow(a.Value - overallAverageR, 2)));
            var cv = Math.Round(standardDeviation / overallAverageR, 1);

            RChart.Series.Clear();
            RChart.AxisX.Clear();
            RChart.AxisY.Clear();

            // LotNumberの変化を検出し、変化点でセグメントを分割する
            var currentLotNumber = ranges.First().LotNumber;
            var currentSegment = new ChartValues<DateModel>();
            var colors = new List<Brush> { Brushes.Blue, Brushes.Red, Brushes.Green, Brushes.Orange, Brushes.Purple };
            int colorIndex = 0;
            foreach (var data in ranges)
            {
                if (data.LotNumber != currentLotNumber)
                {
                    // 新しいセグメントを追加
                    controlChartCalculator.AddSegmentToChart(RChart, currentSegment, colors[colorIndex % colors.Count], "R");
                    colorIndex++;
                    currentSegment = new ChartValues<DateModel>();
                    currentLotNumber = data.LotNumber;
                }
                currentSegment.Add(data);
            }
            // 最後のセグメントを追加
            controlChartCalculator.AddSegmentToChart(RChart, currentSegment, colors[colorIndex % colors.Count], "R");

            // X軸の設定
            var rangeAxisX = new Axis
            {
                Title = "",
                Labels = sequenceLabels,
                Separator = new LiveCharts.Wpf.Separator { Step = 1 },
                MinValue = 1,
                MaxValue = ranges.Count
            };
            RChart.AxisX.Add(rangeAxisX);

            // Y軸の設定
            var rAxisY = new Axis
            {
                Title = "",
                MinValue = 0.0,
                MaxValue = UCL,
            };
            RChart.AxisY.Add(rAxisY);

            // 平均（破線）、UCL（実線）,LCL（実線）のラインをチャートに追加
            controlChartCalculator.AddConstantLine(RChart, overallAverageR, "平均", Brushes.Blue, new DoubleCollection { 2, 2 });
            controlChartCalculator.AddConstantLine(RChart, UCL, "UCL", Brushes.Orange, null);
            controlChartCalculator.AddConstantLine(RChart, 0.0, "", Brushes.Orange, null);

            // 日内変動
            textSD.Text = $"{standardDeviation:F3}";
            textCV.Text = $"{cv:F2} %";

            // 平均を表示
            lblRAverageValue.Content = $"CL: {strR}";
            textR.Text = strR;
            textR_M.Text = "";
            // UCLを表示
            lblRUCL.Content = $"UCL: {strUCL}";
            textUCL_R.Text = strUCL;
            textUCL_R_M.Text = "";
            // LCLを表示
            lblRLCL.Content = $"{controlChartCalculator.ConvertDoubleToString(0.0, decimalPlaces + 1)}";
        }


        public void msg(string st)
        {
            MessageBox.Show(st);
        }

        /// <summary>
        /// ボタン（印刷）クリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog printDialog = new PrintDialog();

            if (printDialog.ShowDialog() == true)
            {
                // 印刷ボタンを非表示にする
                Button printButton = sender as Button;
                printButton.Visibility = Visibility.Hidden;
                PrintList.Visibility = Visibility.Hidden;
                ExitButton.Visibility = Visibility.Hidden;

                // 印刷チケットを取得して印刷領域を設定
                PrintTicket printTicket = printDialog.PrintTicket;
                PrintCapabilities printCapabilities = printDialog.PrintQueue.GetPrintCapabilities(printTicket);

                // A4サイズの寸法（210mm x 297mm）をインチに変換
                double a4Width = 8.27 * 96;     // 210mm / 25.4mm per inch * 96 DPI
                double a4Height = 11.69 * 96;   // 297mm / 25.4mm per inch * 96 DPI

                // 印刷可能領域を取得
                double printableWidth = printCapabilities.PageImageableArea.ExtentWidth;
                double printableHeight = printCapabilities.PageImageableArea.ExtentHeight;

                // 余白を設定（例えば、20mmの余白を設定）
                double margin = 10 / 25.4 * 96; // 20mm / 25.4mm per inch * 96 DPI

                // 余白を考慮した印刷領域のサイズを計算
                double contentWidth = Math.Min(a4Width, printableWidth) - margin * 2;
                double contentHeight = Math.Min(a4Height, printableHeight) - margin * 2;

                // 印刷用のビジュアル要素を取得
                Transform originalTransform = PrintArea.LayoutTransform;
                PrintArea.LayoutTransform = null; // レイアウトを印刷用にリセット

                // コンテンツのサイズを設定
                Size contentSize = new Size(contentWidth, contentHeight);
                PrintArea.Measure(contentSize);
                PrintArea.Arrange(new Rect(new Point(0, 0), contentSize));

                // レイアウトの更新を強制
                PrintArea.UpdateLayout();

                // デバッグ出力でサイズを確認
                Console.WriteLine($"Printable Width: {printableWidth}, Printable Height: {printableHeight}");
                Console.WriteLine($"PrintArea Desired Size: {PrintArea.DesiredSize}");

                // ビジュアルの実際のサイズをPDFのページサイズに合わせ、余白を追加
                DrawingVisual visual = new DrawingVisual();
                using (DrawingContext context = visual.RenderOpen())
                {
                    // 余白を適用して描画
                    VisualBrush brush = new VisualBrush(PrintArea);
                    context.DrawRectangle(brush, null, new Rect(new Point(margin, margin), contentSize));
                }

                printDialog.PrintVisual(visual, "Print Preview");

                MessageBox.Show("印刷が完了しました。");

                // 元のレイアウトに戻す
                PrintArea.LayoutTransform = originalTransform;
                // 印刷ボタンを再表示する
                printButton.Visibility = Visibility.Visible;
                PrintList.Visibility = Visibility.Visible;
                ExitButton.Visibility = Visibility.Visible;
            }
        }

        private void PrinterList_Click(object sender, RoutedEventArgs e)
        {
            // 既定のPRNのデータ表示
            System.Drawing.Printing.PrintDocument pd = null;
            try
            {
                pd = new System.Drawing.Printing.PrintDocument();
            }
            catch (Exception ex)
            {
                msg("規定のプリンタ設定がされていません.");
                msg(ex.ToString());
            }
            //L1.Items.Clear();
            //L1.Items.Add("既定のPRN.... " + pd.PrinterSettings.PrinterName);

            //// 登録されているPRN一覧
            //L1.Items.Add("登録されているPRNは下記の通りです....");
            //foreach (string s in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
            //{
            //    L1.Items.Add(s);

            //}
        }

        /// <summary>
        /// ボタン（終了）クリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
