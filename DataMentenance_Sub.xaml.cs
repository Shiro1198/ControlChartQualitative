using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CsvHelper;
using static ControlChart.DataMentenance_Sub;

namespace ControlChart
{
    /// <summary>
    /// DataMentenance_Sub.xaml の相互作用ロジック
    /// </summary>
    public partial class DataMentenance_Sub : Window
    {
        string CounterName;
        string CsvFileName;

        public ObservableCollection<DataMentenanceClass> DataItems { get; set; }

        public List<(string CntDate, string Value, string KentaiNo)> extractedData = new List<(string CntDate, string Value, string KentaiNo)>();

        /// <summary>
        /// メイン
        /// </summary>
        /// <param name="counterName"></param>
        /// <param name="csvFileName"></param>
        public DataMentenance_Sub(string counterName, string csvFileName)
        {
            InitializeComponent();

            CounterName = counterName;      // 測定器名
            CsvFileName = csvFileName;      // CSVファイル名
            DataItems = new ObservableCollection<DataMentenanceClass>();
            DataGridCtrlData.ItemsSource = DataItems;

            switch (CounterName)
            {
                case "Labospect":
                    this.Title = "Labospect";
                    SearchColumn.Text = "検体番号";
                    SearchText.Text = "QAP1";
                    RdoTop.IsChecked = true;
                    ReadCsvFile_Labospect(CsvFileName);
                    break;
                case "XR-1000":
                    this.Title = "XR-1000";
                    SearchColumn.Text = "検体番号";
                    SearchText.Text = "01";
                    RdoEnd.IsChecked = true;
                    ReadCsvFile_XR1000(CsvFileName);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// CSV対象カラム一覧を取得（Labospect）
        /// </summary>
        /// <param name="csvFileName"></param>
        private void ReadCsvFile_Labospect(string csvFileName)
        {
            try
            {
                using (var reader = new StreamReader(csvFileName))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    // CSVファイルを読み込む
                    var records = csv.GetRecords<dynamic>().ToList();

                    // レコードが存在するかチェック
                    if (records.Count > 1)
                    {
                        var secondRow = records[0];     // ヘッダ行
                        // 2行目の各列の値をリストボックスに追加
                        foreach (var field in secondRow)
                        {
                            if (field.Key != null && field.Value != null)
                            {
                                if (field.Value == "V_Unit")
                                {
                                    ItemsListBox.Items.Add(field.Key);
                                }
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("CSVファイルに2行目がありません。");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"エラーが発生しました: {ex.Message}");
            }

        }

        /// <summary>
        /// CSV対象カラム一覧を取得（XR1000）
        /// </summary>
        /// <param name="csvFileName"></param>
        private void ReadCsvFile_XR1000(string csvFileName)
        {
            try
            {
                var lines = File.ReadAllLines(CsvFileName, Encoding.GetEncoding("Shift_JIS"));
                if (lines.Length == 0)
                {
                    MessageBox.Show("CSVファイルが空です。");
                    return;
                }
                // ヘッダーの解析
                var headers_1 = lines[1].Split(',');

                // Na列のインデックスを取得
                string selectedItem = "WBC(10^2/uL)";
                int naIndex = Array.IndexOf(headers_1, selectedItem);
                if (naIndex == -1)
                {
                    MessageBox.Show($"[{selectedItem}]列が見つかりません。");
                    return;
                }
                for (int i = naIndex; i < headers_1.Length; i++)
                {
                    if (headers_1[i].Trim() != "")
                        ItemsListBox.Items.Add(headers_1[i]);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"エラーが発生しました: {ex.Message}");
            }

        }

        /// <summary>
        /// ボタン（選択）クリックイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = ItemsListBox.SelectedItem as string;
            if (selectedItem != null)
            {
                if (MessageBox.Show("表示されているデータを取込ますか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    if (Owner is DataMentenance ownewWindow)
                    {
                        ownewWindow.ReceiveSelectedItem(selectedItem, DataItems);
                        this.Close();
                    }
                }
            }
            else
            {
                MessageBox.Show("アイテムが選択されていません。");
            }
        }

        /// <summary>
        /// CSV対象アイテムリストボックス選択イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ItemsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 選択されたアイテムを取得
            if (ItemsListBox.SelectedItem != null)
            {
                DataItems.Clear();

                string selectedItem = ItemsListBox.SelectedItem.ToString();
                //MessageBox.Show($"Selected item: {selectedItem}");

                switch (CounterName)
                {
                    case "Labospect":
                        ItemsListBox_SelectionChanged_Labospect(selectedItem);
                        break;
                    case "XR-1000":
                        ItemsListBox_SelectionChanged_XR1000(selectedItem);
                        break;
                    default:
                        break;
                }


            }
        }

        /// <summary>
        /// 抽出アイテムリストボックス選択イベント（Labospect）
        /// </summary>
        /// <param name="selectedItem"></param>
        private void ItemsListBox_SelectionChanged_Labospect(string selectedItem)
        {
            var lines = File.ReadAllLines(CsvFileName);
            if (lines.Length == 0)
            {
                MessageBox.Show("CSVファイルが空です。");
                return;
            }

            // ヘッダーの解析
            var headers_0 = lines[0].Split(',');

            // 選択した項目のインデックスを取得
            int naIndex = Array.IndexOf(headers_0, selectedItem);
            if (naIndex == -1)
            {
                MessageBox.Show($"[{selectedItem}]列が見つかりません。");
                return;
            }

            // 左側の列とA_Date列のインデックスを取得
            var headers_1 = lines[1].Split(',');

            int valueIndex = naIndex - 1;
            int aDateIndex = Array.IndexOf(headers_1, "A_Date");
            int sIdIndex = Array.IndexOf(headers_1, "S_ID");

            if (valueIndex < 0 || aDateIndex == -1 || sIdIndex == -1)
            {
                MessageBox.Show("必要な列が見つかりません。");
                return;
            }

            // データの抽出

            for (int i = 1; i < lines.Length; i++)
            {
                var columns = lines[i].Split(',');

                // Value列を文字列として取得
                string value = columns[valueIndex];
                string aDate = columns[aDateIndex];
                string sId = columns[sIdIndex];

                extractedData.Add((aDate, value, sId));
            }

            // 抽出したデータを表示
            foreach (var data in extractedData)
            {
                //Console.WriteLine($"Value: {data.Value}, A_Date: {data.A_Date}");
                if (DateTime.TryParse(data.CntDate, out DateTime aDate))
                {
                    if (data.Value.Trim() != "")
                    {
                        DataMentenanceClass dateItem = new DataMentenanceClass
                        {
                            KENSA_DATE = aDate,
                            DOSE = data.Value,
                            KentaiNo = data.KentaiNo
                        };
                        DataItems.Add(dateItem);
                    }
                }
            }
        }

        /// <summary>
        /// 抽出アイテムリストボックス選択イベント（XR1000）
        /// </summary>
        /// <param name="selectedItem"></param>
        private void ItemsListBox_SelectionChanged_XR1000(string selectedItem)
        {
            var lines = File.ReadAllLines(CsvFileName, Encoding.GetEncoding("Shift_JIS"));
            if (lines.Length == 0)
            {
                MessageBox.Show("CSVファイルが空です。");
                return;
            }

            // ヘッダーの解析
            var headers_1 = lines[1].Split(',');

            // 日付列のインデックスを取得
            int aDateIndex = Array.IndexOf(headers_1, "日付");
            if (aDateIndex < 0 || aDateIndex == -1)
            {
                MessageBox.Show("[日付]列が見つかりません。");
                return;
            }
            // 選択した項目のインデックスを取得
            int naIndex = Array.IndexOf(headers_1, selectedItem);
            if (naIndex == -1)
            {
                MessageBox.Show($"[{selectedItem}]列が見つかりません。");
                return;
            }
            // 検体番号列のインデックスを取得
            int kensaiNoIndex = Array.IndexOf(headers_1, "検体番号");
            if (kensaiNoIndex == -1)
            {
                MessageBox.Show($"[検体番号]列が見つかりません。");
                return;
            }

            // データの抽出
            extractedData.Clear();
            for (int i = 2; i < lines.Length; i++)
            {
                var columns = lines[i].Split(',');

                // Value列を文字列として取得
                string aDate = columns[aDateIndex];
                string value = columns[naIndex];
                string kentaiNo = columns[kensaiNoIndex];

                extractedData.Add((aDate, value, kentaiNo));
            }

            // 抽出したデータを表示
            foreach (var data in extractedData)
            {
                //Console.WriteLine($"Value: {data.Value}, A_Date: {data.A_Date}");
                if (DateTime.TryParse(data.CntDate, out DateTime aDate))
                {
                    if (data.Value.Trim() != "")
                    {
                        DataMentenanceClass dateItem = new DataMentenanceClass
                        {
                            KENSA_DATE = aDate,
                            DOSE = data.Value,
                            KentaiNo = data.KentaiNo
                        };
                        DataItems.Add(dateItem);
                    }
                }
            }

        }

        /// <summary>
        /// ボタン（検索）クリックイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            DataItems.Clear();
            // 抽出したデータを表示
            foreach (var data in extractedData)
            {
                if (DateTime.TryParse(data.CntDate, out DateTime aDate))    // 日付は変換できるか
                {
                    if (data.Value.Trim() != "")                            // 値が空でないか
                    {
                        // 検索文字列が含まれているか
                        bool flagAdd = false;
                        if (RdoTop.IsChecked == true)                       // 先頭文字列チェック
                        {
                            if (data.KentaiNo.StartsWith(SearchText.Text))
                            {
                                flagAdd = true;
                            }
                        }
                        else if (RdoEnd.IsChecked == true)                  // 末尾文字列チェック
                        {
                            if (data.KentaiNo.EndsWith(SearchText.Text))
                            {
                                flagAdd = true;
                            }
                        }
                        if (flagAdd == true)
                        {
                            DataMentenanceClass dateItem = new DataMentenanceClass
                            {
                                KENSA_DATE = aDate,
                                DOSE = data.Value,
                                KentaiNo = data.KentaiNo
                            };
                            DataItems.Add(dateItem);
                        }

                    }
                }
            }
        }
    }
}
