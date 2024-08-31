using Microsoft.SqlServer.Server;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static ControlChart.ClassDataTable;
using static ControlChart.LotChange;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace ControlChart
{
    /// <summary>
    /// DataMentenance.xaml の相互作用ロジック
    /// </summary>
    public partial class DataMentenance : Window
    {
        public string ownerK_CODE;
        private ObservableCollection<CtrlList> cmbCtrl = new ObservableCollection<CtrlList>();

        private string connectStrOra = ConfigurationManager.AppSettings["OraConnectString"];
        // ロット情報のグリッド表示用
        public ObservableCollection<dCtrlData> gridCtrlData { get; set; }

        public DataMentenance()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            TxtK_CODE.Text = ownerK_CODE;
            try
            {
                cmbCtrl.Clear();
                OracleDatabase oracleDb = new OracleDatabase(connectStrOra);

                string sql = $"select K_CODE, TUBE_CODE, MNG_RYAK from M_CTRL_TUBE where K_CODE = '{ownerK_CODE}'";
                sql += " order by TUBE_CODE";
                DataTable result = oracleDb.ExecuteQuery(sql);

                if (result != null)
                {
                    var items = from DataRow row in result.Rows
                                select new CtrlList
                                {
                                    CtrlCode = row["TUBE_CODE"].ToString(),
                                    CtrlName = row["MNG_RYAK"].ToString()
                                };
                    foreach (var item in items)
                    {
                        cmbCtrl.Add(item);
                    }
                }
                this.CmbCtrlList.ItemsSource = cmbCtrl;
            }
            catch (Exception ex)
            {
                // エラーハンドリング：適切なログ記録やメッセージ表示を行う
                MessageBox.Show($"データの読み込み中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            gridCtrlData = new ObservableCollection<dCtrlData>();
            DataGridCtrlData.ItemsSource = gridCtrlData;
        }

        /// <summary>
        /// ボタン（検索）クリック時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            set_gridCtrlData();
        }

        private void set_gridCtrlData()
        {
            gridCtrlData.Clear();
            try
            {
                OracleDatabase db = new OracleDatabase(ConfigurationManager.AppSettings["OraConnectString"]);
                string ctrlCode = CmbCtrlList.SelectedItem is CtrlList selectedItem ? selectedItem.CtrlCode : "";
                if (string.IsNullOrEmpty(ctrlCode))
                {
                    MessageBox.Show("コントロールコードを選択してください。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                DateTime? startDate = StartDatePicker.SelectedDate;
                string dateStart = "";
                string dateEnd = "";
                if (startDate.HasValue)
                {
                    dateStart = startDate.Value.ToString("yyyyMMdd");
                }
                else
                {
                    StartDatePicker.SelectedDate = DateTime.Now.AddMonths(-1);
                    startDate = StartDatePicker.SelectedDate;
                    dateStart = startDate.Value.ToString("yyyyMMdd");
                }
                DateTime? endDate = EndDatePicker.SelectedDate;
                if (endDate.HasValue)
                {
                    dateEnd = endDate.Value.ToString("yyyyMMdd");
                }
                else
                {
                    EndDatePicker.SelectedDate = DateTime.Now;
                    endDate = EndDatePicker.SelectedDate;
                    dateEnd = endDate.Value.ToString("yyyyMMdd");
                }

                string sql = "select * from D_CTRL_DATA_RESRV"
                    + $" where K_CODE = '{ownerK_CODE}' and TUBE_CODE = '{ctrlCode}'"
                    + $" and KENSA_DATE between '{dateStart}' and '{dateEnd}'"
                    + " order by KENSA_DATE desc, SUB_NO asc";
                DataTable dt = db.ExecuteQuery(sql);

                foreach (DataRow row in dt.Rows)
                {
                    string kensaDate = row["KENSA_DATE"].ToString();
                    int subNo = int.Parse(row["SUB_NO"].ToString());
                    int? doseNo = int.TryParse(row["DOSE_NO"].ToString(), out int iNo) ? iNo : (int?)null;
                    string dose = row["DOSE"].ToString();
                    DateTime impDate = DateTime.Parse(row["IMP_DATE"].ToString());

                    gridCtrlData.Add(new dCtrlData
                    {
                        KENSA_DATE = kensaDate,
                        SUB_NO = subNo,
                        DOSE_NO = doseNo,
                        DOSE = dose,
                        IMP_DATE = impDate
                    });
                }
                DataGridCtrlData.Items.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"データ取得中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// ボタン（削除）クリック時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            // 選択された行を取得
            var selectedRows = DataGridCtrlData.SelectedItems.Cast<dCtrlData>().ToList();
            if (selectedRows.Count > 0)
            {
                OracleDatabase oracleDb = new OracleDatabase(connectStrOra);
                string sql = "";
                string ctrlCode = CmbCtrlList.SelectedItem is CtrlList selectedItem ? selectedItem.CtrlCode : "";
                if (string.IsNullOrEmpty(ctrlCode))
                {
                    MessageBox.Show("コントロールコードを選択してください。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                foreach (var selectedRow in selectedRows)
                {
                    try
                    {
                        sql = $"select * from D_CTRL_DATA_RESRV where K_CODE='{TxtK_CODE.Text}' and TUBE_CODE='{ctrlCode}'"
                            + $" and KENSA_DATE='{selectedRow.KENSA_DATE}' and SUB_NO={selectedRow.SUB_NO}";
                        DataTable result = oracleDb.ExecuteQuery(sql);
                        if (result.Rows.Count <= 0)
                        {
                            MessageBox.Show("削除対象レコードがありません。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                        // 削除処理
                        sql = $"delete from D_CTRL_DATA_RESRV where K_CODE='{TxtK_CODE.Text}' and TUBE_CODE='{ctrlCode}'"
                            + $" and KENSA_DATE='{selectedRow.KENSA_DATE}' and SUB_NO={selectedRow.SUB_NO}";
                        oracleDb.ExecuteNonQuery(sql);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"レコード削除中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            set_gridCtrlData();

            MessageBox.Show("削除しました", "完了", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        /// <summary>
        /// ボタン（更新）クリック時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            // データグリッドの編集を終了
            DataGridCtrlData.CommitEdit(DataGridEditingUnit.Cell, true);
            DataGridCtrlData.CommitEdit(DataGridEditingUnit.Row, true);

            // データグリッドのアイテムを更新
            DataGridCtrlData.Items.Refresh();

            // 選択されたアイテムがdCtrlLot型にキャストできるかを確認
            // 選択された行を取得
            if (DataGridCtrlData.SelectedItem is dCtrlData selectedRow)
            {
                MessageBox.Show($"選択された行: KENSA_DATE = {selectedRow.KENSA_DATE}, SUB_NO = {selectedRow.SUB_NO}");
                try
                {
                    OracleDatabase oracleDb = new OracleDatabase(connectStrOra);
                    string ctrlCode = CmbCtrlList.SelectedItem is CtrlList selectedItem ? selectedItem.CtrlCode : "";
                    if (string.IsNullOrEmpty(ctrlCode))
                    {
                        MessageBox.Show("コントロールコードを選択してください。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    string sql = $"select * from D_CTRL_DATA_RESRV where K_CODE='{TxtK_CODE.Text}' and TUBE_CODE='{ctrlCode}'"
                        + $" and KENSA_DATE='{selectedRow.KENSA_DATE}' and SUB_NO={selectedRow.SUB_NO}";
                    DataTable result = oracleDb.ExecuteQuery(sql);
                    if (result.Rows.Count <= 0)
                    {
                        MessageBox.Show("更新対象レコードがありません。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    // 更新処理
                    string doseNo = "";
                    if (selectedRow.DOSE_NO == null)
                    {
                        doseNo = "null";
                    }
                    else
                    {
                        doseNo = selectedRow.DOSE_NO.ToString();
                    }
                    sql = $"update D_CTRL_DATA_RESRV set"
                        + $" DOSE_NO={doseNo}"
                        + $", DOSE='{selectedRow.DOSE}'"
                        + $"  where K_CODE='{TxtK_CODE.Text}' and TUBE_CODE='{ctrlCode}'"
                        + $" and KENSA_DATE='{selectedRow.KENSA_DATE}' and SUB_NO={selectedRow.SUB_NO}";
                    oracleDb.ExecuteNonQuery(sql);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"レコード挿入中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                set_gridCtrlData();
                MessageBox.Show("更新しました", "完了", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show("行が選択されていません。");
            }

        }
        /// <summary>
        /// ボタン（追加）クリック時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DateTime? startDate = KensaDatePicker.SelectedDate;
                string kensaStart = startDate.Value.ToString("yyyyMMdd");

                OracleDatabase oracleDb = new OracleDatabase(connectStrOra);
                string ctrlCode = CmbCtrlList.SelectedItem is CtrlList selectedItem ? selectedItem.CtrlCode : "";
                if (string.IsNullOrEmpty(ctrlCode))
                {
                    MessageBox.Show("コントロールコードを選択してください。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                string sql = $"select * from D_CTRL_DATA_RESRV"
                    + $" where K_CODE='{TxtK_CODE.Text}' and TUBE_CODE='{ctrlCode}' and KENSA_DATE='{kensaStart}'"
                    + $" and SUB_NO = {TxtSUB_NO.Text}";
                DataTable result = oracleDb.ExecuteQuery(sql);
                if (result.Rows.Count > 0)
                {
                    MessageBox.Show("登録済みです。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                // 新規登録処理
                sql = $"insert into D_CTRL_DATA_RESRV (KENSA_DATE, TUBE_CODE, K_CODE, SUB_NO, DOSE, IMP_DATE)"
                    + $" values ('{kensaStart}','{ctrlCode}','{TxtK_CODE.Text}',{TxtSUB_NO.Text}"
                    + $",'{TxtDOSE.Text}',sysdate)";
                oracleDb.ExecuteNonQuery(sql);

                KensaDatePicker.SelectedDate = DateTime.Now;
                TxtSUB_NO.Text = "";
                TxtDOSE.Text = "";
                EndDatePicker.SelectedDate = startDate;

                set_gridCtrlData();

                MessageBox.Show("追加しました", "完了", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"レコード挿入中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        /// <summary>
        /// ボタン（CSVファイル取込）クリック時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CsvButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Select a CSV file"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                // CSVファイル読込とデータベースへの登録
                ReadCsvFile(openFileDialog.FileName);

                set_gridCtrlData();
            }
        }

        /// <summary>
        /// CSVファイル読込とデータベースへの登録
        /// </summary>
        /// <param name="filePath"></param>
        private void ReadCsvFile(string filePath)
        {
            try
            {
                // 取込対象の項目を選択

                string counterName = "";
                using (StreamReader reader = new StreamReader(filePath, Encoding.GetEncoding("Shift_JIS")))
                {
                    string line;
                    int iRow = 0;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (iRow == 1)
                        {
                            string[] items = line.Split(',');
                            for (int i = 0; i < items.Length; i++)
                            {
                                if (items[i].Trim() == "Value")
                                {
                                    if (items[i + 1].Trim() == "V_Unit" && items[i + 2].Trim() == "D_Alm" && items[i + 3].Trim() == "Dil" && items[i + 4].Trim() == "Reagent")
                                    {
                                        counterName = "Labospect";
                                        break;
                                    }
                                }
                                if (items[i].Trim() == "測定部名称")
                                {
                                    if (items[i + 1].Trim() == "測定部ID" && items[i + 2].Trim() == "日付")
                                    {
                                        counterName = "XR-1000";
                                        break;
                                    }
                                }
                            }
                        }
                        iRow++;
                    }
                }
                if (counterName != "")
                {
                    DataMentenance_Sub sub = new DataMentenance_Sub(counterName, filePath)
                    { 
                        Owner = this
                    };
                    sub.ShowDialog();
                }
                //MessageBox.Show("取込が完了しました", "完了", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// DataManageMentenance_Subからのデータ受信
        /// </summary>
        /// <param name="selectedItem"></param>
        /// <param name="DataItems"></param>
        public void ReceiveSelectedItem(string selectedItem, ObservableCollection<DataMentenanceClass> DataItems)
        {
            // ここで選択されたアイテムを処理します
            ObservableCollection<DataMentenanceClass> data = DataItems;
            //MessageBox.Show($"Selected item: {selectedItem}");

            var measurementData = new Dictionary<DateTime, List<string>>();
            foreach (DataMentenanceClass item in data)
            {
                if (!measurementData.ContainsKey(item.KENSA_DATE.Date))
                {
                    measurementData[item.KENSA_DATE.Date] = new List<string>();
                }
                measurementData[item.KENSA_DATE.Date].Add(item.DOSE);
            }
            // 最大値と最小値の取得
            if (measurementData.Keys.Any())
            {
                DateTime minDate = measurementData.Keys.Min();
                DateTime maxDate = measurementData.Keys.Max();
                StartDatePicker.SelectedDate = minDate;
                EndDatePicker.SelectedDate = maxDate;
            }
            // measurementDataのデータをデータベースに登録
            CsvDataUpdate(measurementData);
        }

        private void ReadCsvFile1(string filePath)
        {
            try
            {
                // CSVファイル読込みmeasurementDataに日付単位にデータを格納

                using (StreamReader reader = new StreamReader(filePath))
                {
                    var measurementData = new Dictionary<DateTime, List<string>>();
                    string line;

                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] items = line.Split(',');

                        if (items.Length >= 2)
                        {
                            string measurementDate = items[0].Trim();
                            string measurementValue = items[1].Trim();
                            if (DateTime.TryParse(measurementDate, out DateTime dateValue))
                            {
                                if (!measurementData.ContainsKey(dateValue))
                                {
                                    measurementData[dateValue] = new List<string>();
                                }
                                measurementData[dateValue].Add(measurementValue);
                            }
                        }
                        else
                        {
                            MessageBox.Show("Invalid record: " + line, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    // 最大値と最小値の取得
                    if (measurementData.Keys.Any())
                    {
                        DateTime minDate = measurementData.Keys.Min();
                        DateTime maxDate = measurementData.Keys.Max();
                        StartDatePicker.SelectedDate = minDate;
                        EndDatePicker.SelectedDate = maxDate;
                    }
                    // measurementDataのデータをデータベースに登録
                    CsvDataUpdate(measurementData);
                }
                MessageBox.Show("取込が完了しました", "完了", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// measurementDataのデータをデータベースに登録
        /// </summary>
        /// <param name="measurementData"></param>
        private void CsvDataUpdate(Dictionary<DateTime, List<string>> measurementData)
        {
            OracleDatabase oracleDb = new OracleDatabase(connectStrOra);
            string ctrlCode = CmbCtrlList.SelectedItem is CtrlList selectedItem ? selectedItem.CtrlCode : "";
            if (string.IsNullOrEmpty(ctrlCode))
            {
                MessageBox.Show("コントロールコードを選択してください。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            foreach (var date in measurementData.Keys)
            {
                string dateKey = date.ToString("yyyyMMdd");

                string sql = $"select * from D_CTRL_DATA_RESRV"
                    + $" where K_CODE='{TxtK_CODE.Text}' and TUBE_CODE='{ctrlCode}' and KENSA_DATE='{dateKey}'";
                DataTable result = oracleDb.ExecuteQuery(sql);
                if (result.Rows.Count > 0)
                {
                    MessageBox.Show($"[{dateKey}]は登録済みです。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                    continue;
                }
                int iSubNo = 1;
                foreach (var value in measurementData[date])
                {
                    string sqlInsert = $"insert into D_CTRL_DATA_RESRV (KENSA_DATE, TUBE_CODE, K_CODE, SUB_NO, DOSE, IMP_DATE)"
                        + $" values ('{dateKey}','{ctrlCode}','{TxtK_CODE.Text}',{iSubNo}"
                        + $",'{value}',sysdate)";
                    int iRet = oracleDb.ExecuteNonQuery(sqlInsert);
                    iSubNo++;
                }
            }
        }
        /// <summary>
        /// ボタン（終了）クリック時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public class dCtrlData : INotifyPropertyChanged
        {
            private string _kensaDate;
            private int _subNo;
            private int? _doseNo;
            private string _dose;
            private DateTime _impDate;

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            public string KENSA_DATE
            {
                get { return _kensaDate; }
                set
                {
                    if (_kensaDate != value)
                    {
                        _kensaDate = value;
                        OnPropertyChanged(nameof(KENSA_DATE));
                    }
                }
            }
            public int SUB_NO
            {
                get { return _subNo; }
                set
                {
                    if (_subNo != value)
                    {
                        _subNo = value;
                        OnPropertyChanged(nameof(SUB_NO));
                    }
                }
            }
            public int? DOSE_NO
            {
                get { return _doseNo; }
                set
                {
                    if (_doseNo != value)
                    {
                        _doseNo = value;
                        OnPropertyChanged(nameof(DOSE_NO));
                    }
                }
            }
            public string DOSE
            {
                get { return _dose; }
                set
                {
                    if (_dose != value)
                    {
                        _dose = value;
                        OnPropertyChanged(nameof(DOSE));
                    }
                }
            }
            public DateTime IMP_DATE
            {
                get { return _impDate; }
                set
                {
                    if (_impDate != value)
                    {
                        _impDate = value;
                        OnPropertyChanged(nameof(IMP_DATE));
                    }
                }
            }
        }

        private void CmbTubeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OracleDatabase oracleDb = new OracleDatabase(connectStrOra);
            string ctrlCode = CmbCtrlList.SelectedItem is CtrlList selectedItem ? selectedItem.CtrlCode : "";
            if (string.IsNullOrEmpty(ctrlCode))
            {
                MessageBox.Show("コントロールコードを選択してください。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            gridCtrlData.Clear();
            string sql = $"select * from ( select * from D_CTRL_DATA_RESRV where K_CODE = '{ownerK_CODE}' and TUBE_CODE = '{ctrlCode}'"
                + $" order by KENSA_DATE desc ) where rownum <= 300";
            DataTable result = oracleDb.ExecuteQuery(sql);
            if (result.Rows.Count > 0)
            {

                // DatePickerの初期値を設定
                //StartDatePicker.SelectedDate = DateTime.Now.AddMonths(-1);
                //EndDatePicker.SelectedDate = DateTime.Now;

                string dtTmp = result.AsEnumerable().Select(r => r.Field<string>("KENSA_DATE")).Max();
                if (DateTime.TryParseExact(dtTmp, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateValue))
                {
                    EndDatePicker.SelectedDate = dateValue;
                }
                dtTmp = result.AsEnumerable().Select(r => r.Field<string>("KENSA_DATE")).Min();
                if (DateTime.TryParseExact(dtTmp, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateValueEnd))
                {
                    StartDatePicker.SelectedDate = dateValueEnd;
                }

                foreach (DataRow row in result.Rows)
                {
                    string kensaDate = row["KENSA_DATE"].ToString();
                    int subNo = int.Parse(row["SUB_NO"].ToString());
                    int? doseNo = int.TryParse(row["DOSE_NO"].ToString(), out int iNo) ? iNo : (int?)null;
                    string dose = row["DOSE"].ToString();
                    DateTime impDate = DateTime.Parse(row["IMP_DATE"].ToString());

                    gridCtrlData.Add(new dCtrlData
                    {
                        KENSA_DATE = kensaDate,
                        SUB_NO = subNo,
                        DOSE_NO = doseNo,
                        DOSE = dose,
                        IMP_DATE = impDate
                    });
                }
            }

        }
    }
}
