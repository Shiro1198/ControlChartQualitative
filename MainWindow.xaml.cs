using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Configuration;
using System.Data;
using System.Reflection.Emit;
using System.Windows.Markup;
using static System.Runtime.CompilerServices.RuntimeHelpers;
using Microsoft.SqlServer.Server;
using static ControlChart.ClassDataTable;
using System.Runtime.InteropServices.ComTypes;

namespace ControlChart
{
    public partial class MainWindow : Window
    {
        // Oracleデータベースへの接続文字列
        private string connectStrOra = ConfigurationManager.AppSettings["OraConnectString"];

        // ItemSource（項目コード,コントロール）のリスト
        private ObservableCollection<ItemList> cmbItems = new ObservableCollection<ItemList>();
        private ObservableCollection<CtrlList> cmbCtrl = new ObservableCollection<CtrlList>();

        // チャートマスタのレコード
        private M_CTRL_CHART mCtrlChart = new M_CTRL_CHART();
        private M_CTRL_TUBE mCtrlTube = new M_CTRL_TUBE();

        public string SelectOption { get; set; } = "All";  // ラジオボタンの選択オプション

        private Tools dataGenerator = new Tools();  // データ生成クラス

        public MainWindow()
        {
            InitializeComponent();

            SelectOption = "All";   // ラジオボタンの初期値を設定

            DataContext = this;
            // Loadedイベントにハンドラを追加
            this.Loaded += MainWindow_Loaded;

            // LiveChartsのマッピング設定
            var mapper = Mappers.Xy<DateModel>()
                .X(model => model.Sequence)
                .Y(model => model.Value);

            Charting.For<DateModel>(mapper);
        }

        /// <summary>
        /// フォームロード時の処理
        /// </summary>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // DatePickerの初期化
            InitializeDatePicker();
            // コンボボックス（項目コード）の初期化
            LoadComboBoxItems();
        }

        /// <summary>
        /// DatePickerの初期化
        /// </summary>
        private void InitializeDatePicker()
        {
            // DatePickerのイベントハンドラを一時的に解除
            StartDatePicker.SelectedDateChanged -= DatePicker_SelectedDateChanged;
            EndDatePicker.SelectedDateChanged -= DatePicker_SelectedDateChanged;

            // DatePickerの初期値を設定
            StartDatePicker.SelectedDate = DateTime.Now.AddMonths(-1);
            EndDatePicker.SelectedDate = DateTime.Now;

            // DatePickerのイベントハンドラを再び追加
            StartDatePicker.SelectedDateChanged += DatePicker_SelectedDateChanged;
            EndDatePicker.SelectedDateChanged += DatePicker_SelectedDateChanged;
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton)
            {
                if (SelectOption != radioButton.Tag.ToString())
                {
                    SelectOption = radioButton.Tag.ToString();
                    // 他の処理をここに追加できます
                    OnRadioButtonChecked(SelectOption);
                }
            }
        }
        private void OnRadioButtonChecked(string selectedOption)
        {
            // 選択が変わったときに実行される処理
            //MessageBox.Show($"選択されたオプション: {selectedOption}");

            LoadComboBoxItems();
        }
        /// <summary>
        /// コンボボックス（項目コード）の初期化
        /// </summary>
        private void LoadComboBoxItems()
        {
            try
            {
                cmbItems.Clear();
                cmbCtrl.Clear();
                OracleDatabase oracleDb = new OracleDatabase(connectStrOra);

                string sql = "select K_CODE, K_RYAK from M_CTRL_CHART";
                switch(SelectOption)
                {
                    case "Blood":
                        sql += " where K_KUBUN = 1";
                        break;
                    case "Biochemistry":
                        sql += " where K_KUBUN = 2";
                        break;
                    case "Urine":
                        sql += " where K_KUBUN = 3";
                        break;
                    case "Other":
                        sql += " where K_KUBUN = 0";
                        break;
                    default:
                        break;
                }
                sql += " order by K_CODE";
                DataTable result = oracleDb.ExecuteQuery(sql);

                if (result != null)
                {
                    var items = from DataRow row in result.Rows select new ItemList
                    { 
                        ItemCode = row["K_CODE"].ToString(), ItemName = row["K_RYAK"].ToString()
                    };
                    foreach (var item in items)
                    {
                        cmbItems.Add(item);
                    }
                }
                this.CmbItemList.ItemsSource = cmbItems;
                this.CmbCtrlList.ItemsSource = cmbCtrl;

                PrintButton.IsEnabled = false;              // 印刷ボタンを無効にする
                LotChangeButton.IsEnabled = false;          // ロット変更ボタンを無効にする
            }
            catch (Exception ex)
            {
                // エラーハンドリング：適切なログ記録やメッセージ表示を行う
                MessageBox.Show($"データの読み込み中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// コンボボックス（項目コード）の選択変更時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CmbItemList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbItemList.SelectedItem is ItemList selectedItem)
            {
                // 選択されたアイテムの処理を行います
                try
                {
                    cmbCtrl.Clear();
                    OracleDatabase oracleDb = new OracleDatabase(connectStrOra);

                    string sql = "select tb.TUBE_CODE, tb.MNG_RYAK, tb.CTRLPARM_DISP_SU"
                        + ",ct.K_NAME, ct.ASSAY_STYLE, ct.ASSAY_UNIT_NAME"
                        + " from M_CTRL_TUBE tb"
                        + " left join M_CTRL_CHART ct on tb.K_CODE = ct.K_CODE"
                        + $" where tb.K_CODE = '{selectedItem.ItemCode}' order by tb.TUBE_CODE";
                    DataTable result = oracleDb.ExecuteQuery(sql);
                    if (result != null)
                    {
                        this.TextBoxK_NAME.Text = result.Rows[0]["K_NAME"].ToString();                      // 項目名を表示
                        this.TextBoxASSAY_UNIT_NAME.Text = result.Rows[0]["ASSAY_UNIT_NAME"].ToString();    // 単位を表示
                        this.TextBoxASSAY_STYLE.Text = result.Rows[0]["ASSAY_STYLE"].ToString();            // 測定方法を表示
                        this.TextBoxCTRLPARM_DISP_SU.Text = result.Rows[0]["CTRLPARM_DISP_SU"].ToString();  // 表示桁数を表示

                        var items = from DataRow row in result.Rows
                            select new CtrlList
                            {
                                CtrlCode = row["TUBE_CODE"].ToString(), CtrlName = row["MNG_RYAK"].ToString()
                            };
                        foreach (var item in items)
                        {
                            cmbCtrl.Add(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // エラーハンドリング：適切なログ記録やメッセージ表示を行う
                    MessageBox.Show($"データの読み込み中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }

            }
        }


        /// <summary>
        /// コントロールチャートを更新する
        /// </summary>
        /// <param name="data"></param>
        private bool DisplayCharts(List<DateValue> data)
        {
            ControlChartCalculator controlChartCalculator = new ControlChartCalculator();

            // 日付ごとにグループ化し、ロット番号も取得
            var groupedData = data.GroupBy(d => new { d.Date, d.LotNumber })
                                  .Select(g => new { g.Key.Date, g.Key.LotNumber, Values = g.Select(d => d.Value).ToList() }).ToList();
            if (groupedData.Count <= 1)
            {
                MessageBox.Show("対象データが２日以上ありません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
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
                var count = group.Values.Count();

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
                Average = controlChartCalculator.ConvertDoubleToString(avg.Value, decimalPlaces + 1),
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
            DataGridView.ItemsSource = gridData;        // データグリッドに全ての値を表示

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

            return true;
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

            lblXBarCv.Content = $"CV: {(cv * 100):F2} %";
            lblXBarStandardDeviation.Content = $"SD: {standardDeviation:F3}";
            lblUCLValue.Content = $"UCL: {strUCL}";
            lbl2SDValue.Content = $"+2σ: {strUCL2Sigma}";
            lblAverageValue.Content = $"CL: {strAverage}";
            lblMinus2SDValue.Content = $"-2σ: {strLCL2Sigma}";
            lblLCLValue.Content = $"LCL: {strLCL}";
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
            var standardDeviation = Math.Round(Math.Sqrt(variance), 3);
            // 変動係数の計算
            var cv = Math.Round(standardDeviation / overallAverageRs, 2);

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
            controlChartCalculator.AddSegmentToChart(RsChart, currentSegment, colors[colorIndex % colors.Count],"Rs");

            // 平均（実線）、UCL（破線）のラインをチャートに追加
            controlChartCalculator.AddConstantLine(RsChart, overallAverageRs, "CL", Brushes.Blue, new DoubleCollection { 2, 2 });
            controlChartCalculator.AddConstantLine(RsChart, rsUCL, "UCL", Brushes.Orange, null);
            controlChartCalculator.AddConstantLine(RsChart, rsUCL2Sigma, "2σ", Brushes.Orange, new DoubleCollection { 1, 2 });
            controlChartCalculator.AddConstantLine(RsChart, rsLCL, "", Brushes.Orange, null);

            // 管理値Rsをラベルに表示
            var overallAverage = averages.Where(a => a.Sequence != 1).Average(a => a.Value);

            // 平均、UCLをラベルに表示
            lblRsCv.Content = $"CV: {cv:F2} %";
            lblRsStandardDeviation.Content = $"SD: {standardDeviation:F3}";

            lblRsAverageValue.Content = $"CL: {strRs}";
            lblRsUCL.Content = $"UCL: {controlChartCalculator.ConvertDoubleToString(rsUCL, decimalPlaces + 1)}";
            lblRs2Sigma.Content = $"2σ: {controlChartCalculator.ConvertDoubleToString(rsUCL2Sigma, decimalPlaces + 1)}";
            lblRsLCL.Content = $"{controlChartCalculator.ConvertDoubleToString(rsLCL, decimalPlaces + 1)}";
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
            var cv = standardDeviation / overallAverageR;

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
                MinValue = 0,
                MaxValue = UCL,
            };
            RChart.AxisY.Add(rAxisY);

            // 平均（破線）、UCL（実線）,LCL（実線）のラインをチャートに追加
            controlChartCalculator.AddConstantLine(RChart, overallAverageR, "平均", Brushes.Blue, new DoubleCollection { 2, 2 });
            controlChartCalculator.AddConstantLine(RChart, UCL, "UCL", Brushes.Orange, null);
            controlChartCalculator.AddConstantLine(RChart, 0.0, "LCL", Brushes.Orange, null);

            // 平均をラベルに表示
            lblRCv.Content = $"CV: {cv:F2} %";
            lblRStandardDeviation.Content = $"SD: {standardDeviation:F3}";

            lblRAverageValue.Content = $"CL: {strR}";
            lblRUCL.Content = $"UCL: {controlChartCalculator.ConvertDoubleToString(UCL, decimalPlaces + 1)}";
            lblRLCL.Content = $"{controlChartCalculator.ConvertDoubleToString(0, decimalPlaces + 1)}";
        }

        /// <summary>
        /// ボタン（チャート表示）クリック時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonChartDisplay_Click(object sender, RoutedEventArgs e)
        {
            // コントロールマスタの取得
            if (GetMasterData() == false)
                return;
            // 有効データの選択処理
            if (ctrlDataSelect() == false)
                return;
            // CSVファイルを作成する
            if (createCsvFile() == false)
                return;
            // チャートを更新する
            if (UpdateChart() == false)
                return;
            
            PrintButton.IsEnabled = true;               // 印刷ボタンを有効にする
            LotChangeButton.IsEnabled = true;           // ロット変更ボタンを無効にする
            DataMentenanceButton.IsEnabled = true;      // データメンテナンスボタンを無効にする
        }

        /// <summary>
        /// コントロール、チューブ・マスタの取得
        /// </summary>
        private bool GetMasterData()
        {
            try
            {
                // 項目コード、コントロールコードを取得
                string itemCode = CmbItemList.SelectedItem is ItemList selectedItem ? selectedItem.ItemCode : "";
                string ctrlCode = CmbCtrlList.SelectedItem is CtrlList selectedCtrl ? selectedCtrl.CtrlCode : "";

                // マスタデータの取得
                OracleDatabase oracleDb = new OracleDatabase(connectStrOra);

                // コントロールチャート -> mCtrlChart 
                mCtrlChart = new M_CTRL_CHART();
                string sql = $"select * from M_CTRL_CHART where K_CODE = '{itemCode}'";
                DataTable result = oracleDb.ExecuteQuery(sql);
                if (result != null && result.Rows.Count > 0)
                {
                    DataRow row = result.Rows[0];
                    Type type = typeof(M_CTRL_CHART);

                    foreach (DataColumn column in result.Columns)
                    {
                        var property = type.GetProperty(column.ColumnName);
                        if (property != null && row[column] != DBNull.Value)
                        {
                            var value = Convert.ChangeType(row[column], property.PropertyType);
                            property.SetValue(mCtrlChart, value);
                        }
                    }
                }
                else
                {
                    return false;
                }
                // チューブ -> mCtrlTube
                mCtrlTube = new M_CTRL_TUBE();
                sql = $"select * from M_CTRL_TUBE where TUBE_CODE = '{ctrlCode}' and K_CODE = '{itemCode}'";
                DataTable resultTube = oracleDb.ExecuteQuery(sql);
                if (resultTube != null && resultTube.Rows.Count > 0)
                {
                    DataRow row = resultTube.Rows[0];
                    Type type = typeof(M_CTRL_TUBE);

                    foreach (DataColumn column in resultTube.Columns)
                    {
                        var property = type.GetProperty(column.ColumnName);
                        if (property != null && row[column] != DBNull.Value)
                        {
                            var value = Convert.ChangeType(row[column], property.PropertyType);
                            property.SetValue(mCtrlTube, value);
                        }
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting master data: {ex.Message}");
                return false;
            }
            return true;
        }
        /// <summary>
        /// CSVファイルを作成する
        /// </summary>
        private bool createCsvFile()
        {
            try
            {
                OracleDatabase oracleDb = new OracleDatabase(connectStrOra);

                string filePath = "data.csv";
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
                string itemCode = CmbItemList.SelectedItem is ItemList selectedItem ? selectedItem.ItemCode : "";
                string ctrlCode = CmbCtrlList.SelectedItem is CtrlList selectedCtrl ? selectedCtrl.CtrlCode : "";
                DateTime? startDate = StartDatePicker.SelectedDate.Value.Date;
                DateTime? endDate = EndDatePicker.SelectedDate.Value.Date;

                // コントロールQCロットデータの取得（D_CTRL_QCLOT_INFO -> ctrlLots）
                List< D_CTRL_QCLOT_INFO> ctrlLots = new List<D_CTRL_QCLOT_INFO>();
                string sqlLot = "select * from D_CTRL_QCLOT_INFO"
                    + $" where K_CODE = '{itemCode}' and TUBE_CODE = '{ctrlCode}'"
                    + " order by S_DATE desc";
                DataTable resultLot = oracleDb.ExecuteQuery(sqlLot);
                if (resultLot != null && resultLot.Rows.Count > 0)
                {
                    foreach (DataRow row in resultLot.Rows)
                    {
                        D_CTRL_QCLOT_INFO ctrlLot = new D_CTRL_QCLOT_INFO();  // 新しいインスタンスを作成
                        Type type = typeof(D_CTRL_QCLOT_INFO);

                        foreach (DataColumn column in resultLot.Columns)
                        {
                            var property = type.GetProperty(column.ColumnName);
                            if (property != null && row[column] != DBNull.Value)
                            {
                                var value = Convert.ChangeType(row[column], property.PropertyType);
                                property.SetValue(ctrlLot, value);
                            }
                        }
                        ctrlLots.Add(ctrlLot);
                    }
                }
                // CSVファイルのヘッダー行を作成
                var sb = new StringBuilder();
                sb.AppendLine("Date,Value,LotNo");

                // 有効データの取得
                string dateFrom = startDate.Value.ToString("yyyyMMdd");
                string dateTo = endDate.Value.ToString("yyyyMMdd");
                string sql = "select * from D_CTRL_DATA_RESRV"
                    + $" where KENSA_DATE between '{dateFrom}' and '{dateTo}'"
                    + $" and K_CODE = '{itemCode}' and TUBE_CODE = '{ctrlCode}'"
                    + " and DOSE_NO > 0"
                    + " order by KENSA_DATE, DOSE_NO";
                DataTable result = oracleDb.ExecuteQuery(sql);
                if (result == null && result.Rows.Count <= 0 )
                {
                    MessageBox.Show("表示対象データがありません", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }
                foreach (DataRow row in result.Rows)
                {
                    string kensaDate = row["KENSA_DATE"].ToString();
                    if (kensaDate.Length == 8)
                    {
                        // LOT番号を取得
                        string lotNo = "";
                        foreach (D_CTRL_QCLOT_INFO ctrlLot in ctrlLots)
                        {
                            double lotDate = double.TryParse(ctrlLot.S_DATE, out double dblDate) ? dblDate : 0;
                            double knsDate = double.TryParse(kensaDate, out double dblKensaDate) ? dblKensaDate : 0;
                            if (knsDate >= lotDate)
                            {
                                lotNo = ctrlLot.QCLOT_NO;
                                break;
                            }
                        }
                        // ファイル出力データ作成
                        string strDate = kensaDate.Substring(0, 4) + "/" + kensaDate.Substring(4, 2) + "/" + kensaDate.Substring(6, 2);
                        if (DateTime.TryParse(strDate, out DateTime dt))
                        {
                            strDate = dt.ToString("yyyy/MM/dd");
                            string strValue = row["DOSE"].ToString();
                            string strLotNo = lotNo;
                            sb.AppendLine($"{strDate},{strValue},{strLotNo}");
                        }
                    }
                }
                File.WriteAllText(filePath, sb.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating CSV file: {ex.Message}");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 有効データの選択処理
        /// </summary>
        private bool ctrlDataSelect()
        {
            try
            {
                // 検査日、項目コード、コントロールコード、開始日、終了日を取得
                string itemCode = CmbItemList.SelectedItem is ItemList selectedItem ? selectedItem.ItemCode : "";
                string ctrlCode = CmbCtrlList.SelectedItem is CtrlList selectedCtrl ? selectedCtrl.CtrlCode : "";
                DateTime? startDate = StartDatePicker.SelectedDate.Value.Date;
                DateTime? endDate = EndDatePicker.SelectedDate.Value.Date;

                // Oracleから、項目、チューブで、DOSE_NOがNULLまたは0のデータを取得（有効データの選択）
                OracleDatabase oracleDb = new OracleDatabase(connectStrOra);
                string dateFrom = startDate.Value.ToString("yyyyMMdd");
                string dateTo = endDate.Value.ToString("yyyyMMdd");

                string sql = "select distinct KENSA_DATE, TUBE_CODE, K_CODE, DOSE_NO"
                    + " from D_CTRL_DATA_RESRV"
                    + $" where K_CODE = '{itemCode}' and TUBE_CODE = '{ctrlCode}' and DOSE_NO is null"
                    + $" and KENSA_DATE between '{dateFrom}' and '{dateTo}'"
                    + " order by KENSA_DATE";
                DataTable result = oracleDb.ExecuteQuery(sql);
                if (result == null)
                {
                    return false;
                }
                foreach (DataRow row in result.Rows)
                {
                    // 有効データ選択処理
                    List<D_CTRL_DATA_RESRV> dataList = new List<D_CTRL_DATA_RESRV>();

                    string sql_1 = "select * from D_CTRL_DATA_RESRV"
                        + $" where K_CODE = '{row["K_CODE"].ToString()}'"
                        + $" and TUBE_CODE = '{row["TUBE_CODE"].ToString()}'"
                        + $" and KENSA_DATE = '{row["KENSA_DATE"].ToString()}'";
                    DataTable result_1 = oracleDb.ExecuteQuery(sql_1);
                    if (result_1 != null && result_1.Rows.Count > 0)
                    {
                        // カットオフ（上限）
                        double cutoffH = double.TryParse(mCtrlTube.CUTOFF_H, out double dblH) ? dblH : 999999999;
                        // カットオフ（下限）
                        double cutoffL = double.TryParse(mCtrlTube.CUTOFF_L, out double dblL) ? dblL : 0;

                        foreach (DataRow row_1 in result_1.Rows)
                        {
                            if (double.TryParse(row_1["DOSE"].ToString(), out double dblDose))
                            {
                                // カットオフのチェック
                                if (cutoffL <= dblDose && dblDose <= cutoffH)
                                {
                                    D_CTRL_DATA_RESRV data = new D_CTRL_DATA_RESRV
                                    {
                                        KENSA_DATE = row_1["KENSA_DATE"].ToString(),
                                        TUBE_CODE = row_1["TUBE_CODE"].ToString(),
                                        K_CODE = row_1["K_CODE"].ToString(),
                                        SUB_NO = int.Parse(row_1["SUB_NO"].ToString()),
                                        DOSE_NO = int.TryParse(row_1["DOSE_NO"].ToString(), out int iTmp) ? int.Parse(row_1["DOSE_NO"].ToString()) : -1,
                                        DOSE = row_1["DOSE"].ToString(),
                                        DOSE_OLD = row_1["DOSE_OLD"].ToString(),
                                        IMP_DATE = DateTime.Parse(row_1["IMP_DATE"].ToString())
                                    };
                                    dataList.Add(data);
                                }
                            }
                        }
                        // N本選択処理
                        if (dataList.Count > 0)
                        {
                            Random random = new Random();       // 乱数を生成するためのRandomオブジェクトを作成
                            int N = mCtrlTube.CTRLPARM_DISP_SU; // Nの値を設定
                            if (mCtrlChart.GETSEL_KBN == 1) // 0:トップからN本、1:ランダムにN本
                            {
                                // ランダムにN本選択
                                dataList = dataList.OrderBy(i => random.Next()).Take(N).ToList();
                            }
                            else
                            {
                                // トップからN本選択
                                dataList = dataList.Take(N).ToList();
                            }
                        }
                        if (dataList.Count > 0)
                        {
                            // DB更新
                            int intSelNo = 1;
                            foreach (D_CTRL_DATA_RESRV data in dataList)
                            {
                                string sql_2 = "";
                                if (intSelNo == 1)
                                {
                                    sql_2 = "update D_CTRL_DATA_RESRV"
                                        + $" set DOSE_NO = 0"
                                        + $" where KENSA_DATE = '{data.KENSA_DATE}'"
                                        + $" and TUBE_CODE = '{data.TUBE_CODE}'"
                                        + $" and K_CODE = '{data.K_CODE}'";
                                    oracleDb.ExecuteNonQuery(sql_2);
                                }
                                sql_2 = "update D_CTRL_DATA_RESRV"
                                    + $" set DOSE_NO = {intSelNo}"
                                    + $" where KENSA_DATE = '{data.KENSA_DATE}'"
                                    + $" and TUBE_CODE = '{data.TUBE_CODE}'"
                                    + $" and K_CODE = '{data.K_CODE}'"
                                    + $" and SUB_NO = {data.SUB_NO}";
                                oracleDb.ExecuteNonQuery(sql_2);
                                intSelNo++;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating CSV file: {ex.Message}");
                return false;
            }
            return true;
        }
        /// <summary>
        /// プレビュー画面を表示する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            PrintPreviewWindow ppw = new PrintPreviewWindow
            {
                Owner = this,
                //WindowStartupLocation = WindowStartupLocation.CenterOwner,
                mCtrlChart = this.mCtrlChart,
                mCtrlTube = this.mCtrlTube,
                startDate = (DateTime)StartDatePicker.SelectedDate.Value.Date,
                endDate = (DateTime)EndDatePicker.SelectedDate.Value.Date

            };
            ppw.labelDate.Content = DateTime.Now.ToString("yyyy 年 MM 月 dd 日");

            ppw.ShowDialog();
        }

        /// <summary>
        /// CSVファイルを作成する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonCreateData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string filePath = "data.csv";
                var startDate = new DateTime(2024, 3, 1);
                var endDate = new DateTime(2024, 5, 31);

                dataGenerator.CreateData(filePath, startDate, endDate, "Lot");

                MessageBox.Show($"CSV file created at {filePath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating CSV file: {ex.Message}");
            }
        }

        /// <summary>
        /// CSVファイルを読み込む
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonReadCsv_Click(object sender, RoutedEventArgs e)
        {
            UpdateChart();
        }

        /// <summary>
        /// DatePickerの日付変更時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            //UpdateChart();
        }

        /// <summary>
        /// コントロールチャートを更新する
        /// </summary>
        private bool UpdateChart()
        {
            DateTime? startDate = StartDatePicker.SelectedDate.Value.Date;
            DateTime? endDate = EndDatePicker.SelectedDate.Value.Date;

            if (startDate.HasValue && endDate.HasValue)
            {
                try
                {
                    var filteredData = dataGenerator.FilterDataByDateRange(dataGenerator.ReadCsvData("data.csv"), startDate.Value, endDate.Value);
                    if (DisplayCharts(filteredData) == false)
                        return false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating chart: {ex.Message}");
                    return false;
                }
            }
            else
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// ボタン（マスタメンテナンス） 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MasterButton_Click(object sender, RoutedEventArgs e)
        {
            MasterMentenanceMenu maintenanceMaster = new MasterMentenanceMenu
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            maintenanceMaster.ShowDialog();
        }

        /// <summary>
        /// ボタン（ロット変更）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LotChangeButton_Click(object sender, RoutedEventArgs e)
        {
            LotChange lotChange = new LotChange
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            lotChange.ownerK_CODE = CmbItemList.SelectedItem is ItemList selectedItem ? selectedItem.ItemCode : "";
            lotChange.ownerTUBE_CODE = CmbCtrlList.SelectedItem is CtrlList selectedCtrl ? selectedCtrl.CtrlCode : "";

            lotChange.ShowDialog();
        }

        /// <summary>
        /// ボタン（データメンテナンス）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataMentenanceButton_Click(object sender, RoutedEventArgs e)
        {
            DataMentenance dataMente = new DataMentenance
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            dataMente.ownerK_CODE = CmbItemList.SelectedItem is ItemList selectedItem ? selectedItem.ItemCode : "";
            dataMente.ownerTUBE_CODE = CmbCtrlList.SelectedItem is CtrlList selectedCtrl ? selectedCtrl.CtrlCode : "";

            dataMente.ShowDialog();

        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class DateModel
    {
        public int Sequence { get; set; }
        public DateTime DateTime { get; set; }
        public double Value { get; set; }
        public double Max { get; set; }
        public double Min { get; set; }
        public int Count { get; set; }
        public string LotNumber { get; set; }
    }

    public class DateValue
    {
        public DateTime Date { get; set; }
        public double Value { get; set; }
        public string LotNumber { get; set; }
    }

    /// <summary>
    /// コンボボックス（項目コード）のリスト
    /// </summary>
    public class ItemList : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }

        protected bool SetProperty<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null)
        {
            if (!Equals(field, newValue))
            {
                field = newValue;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                return true;
            }

            return false;
        }

        private System.Collections.IEnumerable dataContext;

        public System.Collections.IEnumerable DataContext { get => dataContext; set => SetProperty(ref dataContext, value); }
    }

    /// <summary>
    /// コンボボックス（コントロール）のリスト
    /// </summary>
    public class CtrlList : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public string CtrlCode { get; set; }
        public string CtrlName { get; set; }
    }

}
