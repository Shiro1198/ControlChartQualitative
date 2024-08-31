using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Wpf.Charts.Base;
using System;
using System.Collections.Generic;
using System.IO;
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
        public  List<M_CTRL_TUBE> mCtrlTube = new List<M_CTRL_TUBE>();
        public List<string> yLabels = new List<string>();
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
            //this.textCTRLPARM_DISP_SU.Text = mCtrlTube.CTRLPARM_DISP_SU.ToString(); // CTRL係数・表示本数
            if (startDate == null) return;
            if (endDate == null) return;
            DateTime sDate = startDate.Value;
            DateTime eDate = endDate.Value;
            this.textJissiDate.Text = $"{sDate.ToString("yyyy年MM月dd日")}　～　{eDate.ToString("yyyy年MM月dd日")}"; // 開始日～終了日
            try
            {
                // カレントディレクトリを取得
                string currentDirectory = Directory.GetCurrentDirectory();
                int tubeNo = 0;
                foreach (M_CTRL_TUBE tube in mCtrlTube)
                {
                    string tubeCode = tube.TUBE_CODE;
                    string filePath = System.IO.Path.Combine(currentDirectory, $"data_{tubeCode}.csv");

                    // ファイルが存在する場合
                    if (File.Exists(filePath))
                    {
                        var filteredData = dataGenerator.FilterDataByDateRange(dataGenerator.ReadCsvData(filePath), startDate.Value, endDate.Value);

                        DisplayCharts(filteredData, tubeNo);
                        tubeNo++;
                    }
                }
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
        private void DisplayCharts(List<DateValue> data, int tubeNo)
        {
            ControlChartCalculator controlChartCalculator = new ControlChartCalculator();

            // チャートに表示するデータを作成
            var qualitativeData = new ChartValues<DateModel>();

            int sequence = 1;
            foreach (var dt in data)
            {
                int index = yLabels.IndexOf(dt.Value);
                qualitativeData.Add(new DateModel { Sequence = sequence, DateTime = dt.Date, Index = index, Value = dt.Value, LotNumber = dt.LotNumber });
                sequence++;
            }

            // データグリッドに全ての値を表示
            string qcLotNo = "";
            foreach (var item in qualitativeData)
            {
                string sTubeNo = (tubeNo + 1).ToString();
                string sSeq = item.Sequence.ToString();
                setTextBlock($"Seq{sTubeNo}_{sSeq}", item.Sequence.ToString());
                setTextBlock($"Date{sTubeNo}_{sSeq}", item.DateTime.ToString("M.d"));
                setTextBlock($"Value{sTubeNo}_{sSeq}", item.Value);
                setTextBlock($"Lot{sTubeNo}_{sSeq}", item.LotNumber);
                if (item.LotNumber != qcLotNo)
                {
                    qcLotNo = item.LotNumber;
                    textQCLOT_NO.Text = qcLotNo;
                }
            }

            // シーケンス番号のリストを作成
            List<string> sequenceLabels = new List<string> { "" };
            sequenceLabels.AddRange(qualitativeData.Select(av => av.Sequence.ToString()));

            dspChart(qualitativeData, sequenceLabels, tubeNo);

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
        private void dspChart(ChartValues<DateModel> averages, List<string> sequenceLabels, int tubeNo)
        {
            ControlChartCalculator controlChartCalculator = new ControlChartCalculator();

            switch (tubeNo)
            {
                case 0:
                    Chart_1.Series.Clear();
                    Chart_1.AxisX.Clear();
                    Chart_1.AxisY.Clear();
                    this.Name_1.Content = mCtrlTube[tubeNo].MNG_NAME;
                    //var gridData1 = qualitativeData;
                    //DataGridView_1.ItemsSource = gridData1;        // データグリッドに全ての値を表示
                    break;
                case 1:
                    Chart_2.Series.Clear();
                    Chart_2.AxisX.Clear();
                    Chart_2.AxisY.Clear();
                    this.Name_2.Content = mCtrlTube[tubeNo].MNG_NAME;
                    //var gridData2 = qualitativeData;
                    //DataGridView_2.ItemsSource = gridData2;        // データグリッドに全ての値を表示
                    break;
                default:
                    return;
            }

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
                    switch (tubeNo)
                    {
                        case 0:
                            controlChartCalculator.AddSegmentToChart(Chart_1, currentSegment, colors[colorIndex % colors.Count], "Xbar");
                            break;
                        case 1:
                            controlChartCalculator.AddSegmentToChart(Chart_2, currentSegment, colors[colorIndex % colors.Count], "Xbar");
                            break;
                        default:
                            break;
                    }
                    colorIndex++;
                    currentSegment = new ChartValues<DateModel>();
                    currentLotNumber = data.LotNumber;
                }
                currentSegment.Add(data);
            }
            // 最後のセグメントを追加
            switch (tubeNo)
            {
                case 0:
                    controlChartCalculator.AddSegmentToChart(Chart_1, currentSegment, colors[colorIndex % colors.Count], "Xbar");
                    break;
                case 1:
                    controlChartCalculator.AddSegmentToChart(Chart_2, currentSegment, colors[colorIndex % colors.Count], "Xbar");
                    break;
                default:
                    break;
            }
            // X軸の設定
            var axisX = new Axis
            {
                Title = "",
                Labels = sequenceLabels,
                Separator = new LiveCharts.Wpf.Separator { Step = 1 },
                MinValue = 1,
                MaxValue = sequenceLabels.Count - 1
            };
            switch (tubeNo)
            {
                case 0:
                    Chart_1.AxisX.Add(axisX);
                    break;
                case 1:
                    Chart_2.AxisX.Add(axisX);
                    break;
                default:
                    break;
            }

            // Y軸の設定
            var axisY = new Axis
            {
                Title = "",
                Labels = yLabels,
                MinValue = 0,                   // "Normal" のインデックス
                MaxValue = yLabels.Count - 1,   // "4+" のインデックス
            };
            switch (tubeNo)
            {
                case 0:
                    Chart_1.AxisY.Add(axisY);
                    break;
                case 1:
                    Chart_2.AxisY.Add(axisY);
                    break;
                default:
                    break;
            }

            // 2SDおよび3SDのラインをチャートに追加
            //controlChartCalculator.AddConstantLine(XBarChart, UCL, "UCL", Brushes.Orange, null);                                    // 実線で表示
            //controlChartCalculator.AddConstantLine(XBarChart, UCL2Sigma, "2σ", Brushes.Orange, new DoubleCollection { 1, 2 });     // 点線で表示
            //controlChartCalculator.AddConstantLine(XBarChart, overallAverage, "平均", Brushes.Blue, new DoubleCollection { 2, 2 }); // 破線で表示
            //controlChartCalculator.AddConstantLine(XBarChart, LCL2Sigma, "-2σ", Brushes.Orange, new DoubleCollection { 1, 2 });
            //controlChartCalculator.AddConstantLine(XBarChart, LCL, "LCL", Brushes.Orange, null);

            //textCV_S.Text = $"{cv:F2} %";
            //textSD_S.Text = $"{standardDeviation:F3}";

            //// UCLを表示
            //lblUCLValue.Content = $"UCL: {strUCL}";
            //textUCL_Xbar.Text = strUCL;
            //textUCL_Xbar_M.Text = "";
            //// 2SDを表示
            //lbl2SDValue.Content = $"+2σ: {strUCL2Sigma}";
            //text2Sigma_Xber.Text = strUCL2Sigma;
            //text2Sigma_Xber_M.Text = "";
            //// 平均を表示
            //lblAverageValue.Content = $"CL: {strAverage}";
            //textXbar.Text = strAverage;
            //textXbar_M.Text = "";
            //// -2SDを表示
            //lblMinus2SDValue.Content = $"-2σ: {strLCL2Sigma}";
            //textMinus2Sigma_Xbar.Text = strLCL2Sigma;
            //textMinus2Sigma_Xbar_M.Text = "";
            //// LCLを表示
            //lblLCLValue.Content = $"LCL: {strLCL}";
            //textLCL_Xbar.Text = strLCL;
            //textLCL_Xbar_M.Text = "";
            // チャートをリフレッシュ
            switch (tubeNo)
            {
                case 0:
                    Chart_1.Update(true, true);
                    break;
                case 1:
                    Chart_2.Update(true, true);
                    break;
                default:
                    break;
            }
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
