using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Wpf.Charts.Base;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
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
        private List<string> yLabels = new List<string>();
        private List<string> uclLine = new List<string>();

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
            yLabels = new List<string>();
            uclLine = new List<string>();

            // データの最後のレコードからY軸のラベルを取得
            int pnt = data.Count - 1;

            var dataLevels = new[]
            {
                    data[pnt].Lv0, data[pnt].Lv1, data[pnt].Lv2, data[pnt].Lv3,
                    data[pnt].Lv4, data[pnt].Lv5, data[pnt].Lv6, data[pnt].Lv7
            };
            foreach (var level in dataLevels)
            {
                string[] splitLevel = level.Split(':');
                if (splitLevel.Length > 1)
                {
                    if (splitLevel[0] == "-")
                        yLabels.Add(" -");
                    else if (splitLevel[0] == "+")
                        yLabels.Add(" +");
                    else
                        yLabels.Add(splitLevel[0]);
                    uclLine.Add(splitLevel[1]);
                }
            }
            if (yLabels.Count <= 0)
            {
                // AppSettingsを取得　<add key="Y-Axis1005" value="4+,3+,2+,1+,normal"/>
                string appKey = "Y-Axis" + mCtrlChart.K_CODE;
                string appVal = ConfigurationManager.AppSettings[appKey];
                yLabels = new List<string>(appVal.Split(','));
            }

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

            // チャート初期化
            initChart(tubeNo);

            // LotNumberの変化を検出し、変化点でセグメントを分割する
            var currentLotNumber = averages.First().LotNumber;
            var currentSegment = new ChartValues<DateModel>();
            var colors = new List<System.Windows.Media.Brush> { System.Windows.Media.Brushes.Blue
                , System.Windows.Media.Brushes.Green, System.Windows.Media.Brushes.Orange, System.Windows.Media.Brushes.Purple };
            int colorIndex = 0;
            foreach (var data in averages)
            {
                if (data.LotNumber != currentLotNumber)
                {
                    // 新しいセグメントを追加
                    AddSegmentToChart(controlChartCalculator, tubeNo, currentSegment, colors[colorIndex % colors.Count]);
                    colorIndex++;
                    currentSegment = new ChartValues<DateModel>();
                    currentLotNumber = data.LotNumber;
                }
                currentSegment.Add(data);
            }
            // 最後のセグメントを追加
            AddSegmentToChart(controlChartCalculator, tubeNo, currentSegment, colors[colorIndex % colors.Count]);

            // X軸の設定
            SetXAxis(tubeNo, sequenceLabels);

            // Y軸の設定
            SetYAxis(tubeNo);

            // チャートに上限ラインを表示
            double uclIndex = 0;
            foreach (var line in uclLine)
            {
                if (double.TryParse(line, out double dblLine))
                {
                    if (dblLine > 0)
                        if (tubeNo == 0)
                            controlChartCalculator.AddConstantLine(Chart_1, uclIndex, "UCL", System.Windows.Media.Brushes.Red, new DoubleCollection { 1, 2 });
                        else if (tubeNo == 1)
                            controlChartCalculator.AddConstantLine(Chart_2, uclIndex, "UCL", System.Windows.Media.Brushes.Red, new DoubleCollection { 1, 2 });
                }
                uclIndex++;
            }

            // チャートをリフレッシュ
            chartRefresh(tubeNo);

        }

        /// <summary>
        /// チャート初期化
        /// </summary>
        /// <param name="tubeNo"></param>
        /// <param name="qualitativeData"></param>
        private void initChart(int tubeNo)
        {
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
        }

        // セグメントをチャートに追加するヘルパーメソッド
        private void AddSegmentToChart(ControlChartCalculator controlChartCalculator, int tubeNo, ChartValues<DateModel> segment
            , System.Windows.Media.Brush color)
        {
            switch (tubeNo)
            {
                case 0:
                    controlChartCalculator.AddSegmentToChart(Chart_1, segment, color, "high");
                    break;
                case 1:
                    controlChartCalculator.AddSegmentToChart(Chart_2, segment, color, "low");
                    break;
                default:
                    break;
            }
        }

        // X軸の設定メソッド
        private void SetXAxis(int tubeNo, List<string> sequenceLabels)
        {
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
        }

        // Y軸の設定メソッド
        private void SetYAxis(int tubeNo)
        {
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
        }

        /// <summary>
        /// チャートをリフレッシュ
        /// </summary>
        /// <param name="tubeNo"></param>
        private void chartRefresh(int tubeNo)
        {
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
                // 印刷ボタンと他のコントロールを非表示にする
                Button printButton = sender as Button;
                printButton.Visibility = Visibility.Hidden;
                PrintList.Visibility = Visibility.Hidden;
                ExitButton.Visibility = Visibility.Hidden;

                // 印刷チケットを取得して印刷領域を設定
                PrintTicket printTicket = printDialog.PrintTicket;
                PrintCapabilities printCapabilities = printDialog.PrintQueue.GetPrintCapabilities(printTicket);

                // 印刷可能領域のサイズを取得
                double printableWidth = printCapabilities.PageImageableArea.ExtentWidth;
                double printableHeight = printCapabilities.PageImageableArea.ExtentHeight;
                double originX = printCapabilities.PageImageableArea.OriginWidth;
                double originY = printCapabilities.PageImageableArea.OriginHeight;

                // 印刷用のビジュアル要素を取得
                Transform originalTransform = PrintArea.LayoutTransform;
                PrintArea.LayoutTransform = null; // レイアウトを印刷用にリセット

                // コンテンツのサイズを設定
                PrintArea.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
                PrintArea.Arrange(new Rect(new System.Windows.Point(0, 0), new System.Windows.Size(PrintArea.ActualWidth, PrintArea.ActualHeight)));

                // 印刷するコンテンツのサイズを取得
                double contentWidth = PrintArea.ActualWidth;
                double contentHeight = PrintArea.ActualHeight;

                // スケール係数を計算（幅と高さのいずれか小さい方を使用）
                double scale = Math.Min(printableWidth / contentWidth, printableHeight / contentHeight);

                // コンテンツを印刷領域にフィットさせるためにスケーリング
                ScaleTransform scaleTransform = new ScaleTransform(scale, scale);
                PrintArea.LayoutTransform = scaleTransform;

                // レイアウトの更新を強制
                PrintArea.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
                PrintArea.Arrange(new Rect(new System.Windows.Point(0, 0), new System.Windows.Size(contentWidth * scale, contentHeight * scale)));

                // デバッグ出力でサイズを確認
                Console.WriteLine($"Printable Width: {printableWidth}, Printable Height: {printableHeight}");
                Console.WriteLine($"Scaled PrintArea Size: {PrintArea.ActualWidth * scale} x {PrintArea.ActualHeight * scale}");

                // 印刷用のビジュアルを作成
                DrawingVisual visual = new DrawingVisual();
                using (DrawingContext context = visual.RenderOpen())
                {
                    // スケーリングされたコンテンツを描画
                    VisualBrush brush = new VisualBrush(PrintArea);
                    //context.DrawRectangle(brush, null, new Rect(new Point(0, 0), new Size(printableWidth, printableHeight)));
                    context.DrawRectangle(brush, null, new Rect(new System.Windows.Point(originX, originY), new System.Windows.Size(printableWidth, printableHeight)));
                }

                // 印刷を実行
                printDialog.PrintVisual(visual, "Print Preview");

                // 元のレイアウトに戻す
                PrintArea.LayoutTransform = originalTransform;

                // 印刷ボタンと他のコントロールを再表示
                printButton.Visibility = Visibility.Visible;
                PrintList.Visibility = Visibility.Visible;
                ExitButton.Visibility = Visibility.Visible;

                MessageBox.Show("印刷が完了しました。");
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
