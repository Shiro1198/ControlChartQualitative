using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection.Emit;
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
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace ControlChart
{
    /// <summary>
    /// LotChange.xaml の相互作用ロジック
    /// </summary>
    public partial class LotChange : Window
    {
        public string ownerK_CODE;
        public string ownerLoginID;

        // Oracleデータベースへの接続文字列

        private string connectStrOra = ConfigurationManager.AppSettings["OraConnectString"];
        // ロット情報のグリッド表示用
        public ObservableCollection<dCtrlLot> gridCtrlLot { get; set; }
        private ObservableCollection<ItemList> cmbItems = new ObservableCollection<ItemList>();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public LotChange()
        {
            InitializeComponent();

            this.Loaded += MainWindow_Loaded;
            //dataGridLot.PreviewKeyDown += DataGridLot_PreviewKeyDown;

        }

        /// <summary>
        /// コンボボックス（項目コード）の初期化
        /// </summary>
        private void LoadComboBoxItems()
        {
            //ComboBox[] comboBoxes =
            //{
            //        this.cmbLv1Code_High, this.cmbLv2Code_1High, this.cmbLv2Code_2High, this.cmbLv2Code_3High, this.cmbLv2Code_4High, this.cmbLv2Code_5High
            //        , this.cmbLv2Code_6High, this.cmbLv2Code_7High, this.cmbLv2Code_8High, this.cmbLv2Code_9High,
            //        this.cmbLv1Code_Low, this.cmbLv2Code_1Low, this.cmbLv2Code_2Low, this.cmbLv2Code_3Low, this.cmbLv2Code_4Low, this.cmbLv2Code_5Low
            //        , this.cmbLv2Code_6Low, this.cmbLv2Code_7Low, this.cmbLv2Code_8Low, this.cmbLv2Code_9Low
            //};

            //try
            //{
            //    cmbItems.Clear();

            //    OracleDatabase oracleDb = new OracleDatabase(connectStrOra);

            //    string sql = "select a.K_CODE, b.RYAKU"
            //        + " from M_US2200_KOUMOKU a"
            //        + " left join M_KOUMOKU b on a.K_CODE = b.K_CODE"
            //        + " order by a.K_ORDER";
            //    DataTable result = oracleDb.ExecuteQuery(sql);

            //    if (result != null)
            //    {
            //        var items = from DataRow row in result.Rows select new ItemList
            //        {
            //            ItemCode = row["K_CODE"].ToString(), ItemName = row["RYAKU"].ToString() 
            //        };
            //        foreach (var item in items)
            //        {
            //            cmbItems.Add(item);
            //        }
            //        // ItemCodeとItemNameがスペースのみのレコードを先頭に追加
            //        cmbItems.Insert(0, new ItemList
            //        {
            //            ItemCode = "",
            //            ItemName = ""
            //        });
            //    }
            //    foreach (var comboBox in comboBoxes)
            //    {
            //        comboBox.ItemsSource = cmbItems;
            //    }
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show($"データの読み込み中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            //}
        }

        /// <summary>
        /// コンボボックス（判定値）の初期化
        /// </summary>
        private void LoadComboBoxItemsVal()
        {
            ComboBox[] comboBoxes =
            {
                this.cmbVal_H0, this.cmbVal_H1, this.cmbVal_H2, this.cmbVal_H3, this.cmbVal_H4, this.cmbVal_H5, this.cmbVal_H6, this.cmbVal_H7,
                this.cmbVal_L0, this.cmbVal_L1, this.cmbVal_L2, this.cmbVal_L3, this.cmbVal_L4, this.cmbVal_L5, this.cmbVal_L6, this.cmbVal_L7,
                this.Val_H0, this.Val_H1, this.Val_H2, this.Val_H3, this.Val_H4, this.Val_H5, this.Val_H6, this.Val_H7,
                this.Val_L0, this.Val_L1, this.Val_L2, this.Val_L3, this.Val_L4, this.Val_L5, this.Val_L6, this.Val_L7
            };

            try
            {
                string appVal = ConfigurationManager.AppSettings["SelectVal"];
                List<string> listVals = new List<string>(appVal.Split(','));

                foreach (var comboBox in comboBoxes)
                {
                    foreach (var item in listVals)
                    {
                        comboBox.Items.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"データの読み込み中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DataGridLot_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // 現在のセル編集をコミット
            if (dataGridLot.CommitEdit(DataGridEditingUnit.Cell, true) && dataGridLot.CommitEdit(DataGridEditingUnit.Row, true))
            {
                // フォーカスを移動して編集を終了
                dataGridLot.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }

            // イベントを処理済みとしてマーク
            e.Handled = true;
        }

        /// <summary>
        /// フォームロード
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.TxtK_CODE.Text = ownerK_CODE;

            // データグリッドの初期化（登録データの取得）
            gridCtrlLot = new ObservableCollection<dCtrlLot>();

            // コントロール名の表示
            using (OracleConnection connection = new OracleConnection(connectStrOra))
            {
                connection.Open();
                string query = "SELECT * FROM M_CTRL_TUBE"
                + $" WHERE K_CODE = '{this.TxtK_CODE.Text}' ORDER BY TUBE_CODE";
                OracleCommand command = new OracleCommand(query, connection);
                OracleDataAdapter dataAdapter = new OracleDataAdapter(command);
                DataTable tubeDataTable = new DataTable();
                dataAdapter.Fill(tubeDataTable);
                if (tubeDataTable.Rows.Count > 0)
                {
                    int iCnt = 0;
                    foreach (DataRow tubeRow in tubeDataTable.Rows)
                    {
                        switch (iCnt)
                        {
                            case 0:
                                Application.Current.Dispatcher.Invoke(() => {
                                    this.txtCtrlHigh.Text = tubeRow["TUBE_CODE"].ToString();
                                    this.CtrlHigh.Text = tubeRow["TUBE_CODE"].ToString(); ;
                                });
                                break;
                            case 1:
                                Application.Current.Dispatcher.Invoke(() => {
                                    this.txtCtrlLow.Text = tubeRow["TUBE_CODE"].ToString();
                                    this.CtrlLow.Text = tubeRow["TUBE_CODE"].ToString();
                                });
                                break;
                        }
                        iCnt++;
                    }
                }
            }
            // ロットチェン情報表示
            set_gridCtrlTube();
            LoadComboBoxItemsVal();
        }

        /// <summary>
        /// ロットチェン情報表示
        /// </summary>
        private void set_gridCtrlTube()
        {
            this.gridCtrlLot.Clear();

            try
            {
                OracleDatabase db = new OracleDatabase(ConfigurationManager.AppSettings["OraConnectString"]);

                DataTable dt = db.ExecuteQuery("select distinct QCLOT_NO, K_CODE, S_DATE, SPEC_MEMO"
                    + " from D_CTRL_QCLOT_INFO"
                    + " where K_CODE = :K_CODE order by S_DATE", new Dictionary<string, object>
                {
                    { "K_CODE", ownerK_CODE }
                });

                foreach (DataRow row in dt.Rows)
                {
                    this.gridCtrlLot.Add(new dCtrlLot
                    {
                        QCLOT_NO = row["QCLOT_NO"].ToString(),
                        //TUBE_CODE = row["TUBE_CODE"].ToString(),
                        K_CODE = row["K_CODE"].ToString(),
                        S_DATE = row["S_DATE"].ToString(),
                        SPEC_MEMO = row["SPEC_MEMO"].ToString()
                    });
                }
                this.dataGridLot.ItemsSource = this.gridCtrlLot;
                // レベル＆チェックのクリア
                clrLevelAndCheck();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"データ取得中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// ボタン（更新）クリック時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            // データグリッドの編集を終了
            dataGridLot.CommitEdit(DataGridEditingUnit.Cell, true);
            dataGridLot.CommitEdit(DataGridEditingUnit.Row, true);

            // データグリッドのアイテムを更新
            dataGridLot.Items.Refresh();

            // 選択されたアイテムがdCtrlLot型にキャストできるかを確認
            // 選択された行を取得
            if (dataGridLot.SelectedItem is dCtrlLot selectedRow)
            {
                //MessageBox.Show($"選択された行: TUBE_CODE = {selectedRow.TUBE_CODE}, MNG_NAME = {selectedRow.MNG_NAME}");
                try
                {
                    OracleDatabase oracleDb = new OracleDatabase(connectStrOra);

                    string strKoumokuCode = TxtK_CODE.Text;
                    string strQcLotNo = selectedRow.QCLOT_NO;
                    string strStartDate = selectedRow.S_DATE;

                    // 更新処理
                    string sql = $"update D_CTRL_QCLOT_INFO set"
                        + $" SPEC_MEMO='{selectedRow.SPEC_MEMO}'"
                        + $" where K_CODE='{strKoumokuCode}' and QCLOT_NO='{strQcLotNo}' and S_DATE='{strStartDate}'";
                    int ret = oracleDb.ExecuteNonQuery(sql);

                    // コントロールQCロット定性項目設定テーブルの更新or登録
                    upseartTeiseiTable("update", strQcLotNo, strStartDate);

                    MessageBox.Show("更新しました", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"レコード挿入中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                set_gridCtrlTube();
            }
            else
            {
                MessageBox.Show("行が選択されていません。");
            }

        }

        /// <summary>
        /// コントロールQCロット定性項目設定テーブルの更新or登録
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="strQcLotNo"></param>
        /// <param name="strStartDate"></param>
        private void upseartTeiseiTable(string mode, string strQcLotNo, string strStartDate)
        {
            List<string> tubeCodes = new List<string>();
            string sql = "";

            if (mode == "update")
            {
                tubeCodes.Add(this.txtCtrlHigh.Text);
                tubeCodes.Add(this.txtCtrlLow.Text);
            }
            else
            {
                tubeCodes.Add(this.CtrlHigh.Text);
                tubeCodes.Add(this.CtrlLow.Text);
            }
            using (OracleConnection connection = new OracleConnection(connectStrOra))
            {
                connection.Open();
                // トランザクションの開始
                using (OracleTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        using (OracleCommand command = new OracleCommand("", connection))
                        {
                            // トランザクションに関連付ける
                            command.Transaction = transaction;
                            foreach (string tubeCode in tubeCodes)
                            {
                                if (tubeCode.Equals(""))
                                    continue;

                                var comboBoxes = new[] { new ComboBox() };
                                var checkBoxes = new[] { new CheckBox() };

                                if (mode == "update")
                                {
                                    if (tubeCode == this.txtCtrlHigh.Text)
                                    {
                                        comboBoxes = new[] { cmbVal_H0, cmbVal_H1, cmbVal_H2, cmbVal_H3, cmbVal_H4, cmbVal_H5, cmbVal_H6, cmbVal_H7 };
                                        checkBoxes = new[] { chkChk_H0, chkChk_H1, chkChk_H2, chkChk_H3, chkChk_H4, chkChk_H5, chkChk_H6, chkChk_H7 };
                                    }
                                    else if (tubeCode == this.txtCtrlLow.Text)
                                    {
                                        comboBoxes = new[] { cmbVal_L0, cmbVal_L1, cmbVal_L2, cmbVal_L3, cmbVal_L4, cmbVal_L5, cmbVal_L6, cmbVal_L7 };
                                        checkBoxes = new[] { chkChk_L0, chkChk_L1, chkChk_L2, chkChk_L3, chkChk_L4, chkChk_L5, chkChk_L6, chkChk_L7 };
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                                else
                                {
                                    if (tubeCode == this.CtrlHigh.Text)
                                    {
                                        comboBoxes = new[] { Val_H0, Val_H1, Val_H2, Val_H3, Val_H4, Val_H5, Val_H6, Val_H7 };
                                        checkBoxes = new[] { Chk_H0, Chk_H1, Chk_H2, Chk_H3, Chk_H4, Chk_H5, Chk_H6, Chk_H7 };
                                    }
                                    else if (tubeCode == this.CtrlLow.Text)
                                    {
                                        comboBoxes = new[] { Val_L0, Val_L1, Val_L2, Val_L3, Val_L4, Val_L5, Val_L6, Val_L7 };
                                        checkBoxes = new[] { Chk_L0, Chk_L1, Chk_L2, Chk_L3, Chk_L4, Chk_L5, Chk_L6, Chk_L7 };
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                                string[] lvValues = new string[comboBoxes.Length];

                                for (int i = 0; i < comboBoxes.Length; i++)
                                {
                                    string lv = comboBoxes[i].Text.Trim();
                                    if (!string.IsNullOrEmpty(lv))
                                    {
                                        lv += ":" + (checkBoxes[i].IsChecked == true ? "1" : "0");
                                    }
                                    lvValues[i] = lv;
                                }
                                // パラメータのクリア
                                command.Parameters.Clear();
                                sql = $"select * from D_CTRL_QCLOT_TEISEI"
                                    + $" where K_CODE='{TxtK_CODE.Text}' and QCLOT_NO='{strQcLotNo}' and S_DATE='{strStartDate}'"
                                    + $" and TUBE_CODE = '{tubeCode}'";
                                command.CommandText = sql;
                                OracleDataAdapter dataAdapter = new OracleDataAdapter(command);
                                DataTable teiseiTable = new DataTable();
                                dataAdapter.Fill(teiseiTable);
                                if (teiseiTable.Rows.Count <= 0)
                                {
                                    command.Parameters.Clear();
                                    sql = "INSERT INTO D_CTRL_QCLOT_TEISEI"
                                        + "(QCLOT_NO, TUBE_CODE, K_CODE, S_DATE, LV_0, LV_1, LV_2, LV_3, LV_4, LV_5, LV_6, LV_7"
                                        + ", CREATE_DATE, CREATE_U_ID) "
                                        + $"VALUES ('{strQcLotNo}','{tubeCode}','{this.TxtK_CODE.Text}','{strStartDate}'"
                                        + $",'{lvValues[0]}','{lvValues[1]}','{lvValues[2]}','{lvValues[3]}','{lvValues[4]}','{lvValues[5]}','{lvValues[6]}','{lvValues[7]}'"
                                        + $", TO_DATE('{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}', 'YYYY/MM/DD HH24:MI:SS'),'{ownerLoginID}')";
                                    command.CommandText = sql;
                                    int retIns = command.ExecuteNonQuery();
                                }
                                else
                                {
                                    command.Parameters.Clear();
                                    sql = "UPDATE D_CTRL_QCLOT_TEISEI"
                                        + $" SET LV_0='{lvValues[0]}', LV_1='{lvValues[1]}', LV_2='{lvValues[2]}', LV_3='{lvValues[3]}'"
                                        + $", LV_4='{lvValues[4]}', LV_5='{lvValues[5]}', LV_6='{lvValues[6]}', LV_7='{lvValues[7]}'"
                                        + $", UPDATE_DATE=TO_DATE('{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}', 'YYYY/MM/DD HH24:MI:SS')"
                                        + $", UPDATE_U_ID='{ownerLoginID}'"
                                        + $" WHERE K_CODE='{this.TxtK_CODE.Text}' AND QCLOT_NO='{strQcLotNo}' AND S_DATE='{strStartDate}'"
                                        + $" AND TUBE_CODE='{tubeCode}'";
                                    command.CommandText = sql;
                                    int retIns = command.ExecuteNonQuery();
                                }
                            }
                        }
                        // コミット (すべて成功した場合)
                        transaction.Commit();
                        Console.WriteLine("全てのレコードが正常に更新されました。");
                    }
                    catch (Exception ex)
                    {
                        // 例外が発生した場合、ロールバック
                        transaction.Rollback();
                        Console.WriteLine($"エラーが発生したためロールバックされました: {ex.Message}");
                    }
                }
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
            if (dataGridLot.SelectedItem is dCtrlLot selectedRow)
            {
                try
                {
                    var res = MessageBox.Show($"削除しますか？: QCLOT_NO = {selectedRow.QCLOT_NO}, S_DATE = {selectedRow.S_DATE}","確認",MessageBoxButton.YesNoCancel,MessageBoxImage.Question);
                    if (res != MessageBoxResult.Yes)
                    {
                        return;
                    }
                    OracleDatabase oracleDb = new OracleDatabase(connectStrOra);

                    string sql = $"select * from D_CTRL_QCLOT_INFO where K_CODE='{TxtK_CODE.Text}'"
                        + $" and QCLOT_NO='{selectedRow.QCLOT_NO}' and S_DATE='{selectedRow.S_DATE}'";
                    DataTable result = oracleDb.ExecuteQuery(sql);
                    if (result.Rows.Count <= 0)
                    {
                        MessageBox.Show("削除対象レコードがありません。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    // 削除処理
                    sql = $"delete from D_CTRL_QCLOT_TEISEI"
                        + $" where K_CODE='{TxtK_CODE.Text}' and QCLOT_NO='{selectedRow.QCLOT_NO}' and S_DATE='{selectedRow.S_DATE}'";
                    int ret = oracleDb.ExecuteNonQuery(sql);

                    sql = $"delete from D_CTRL_QCLOT_INFO where K_CODE='{TxtK_CODE.Text}'"
                        + $" and QCLOT_NO='{selectedRow.QCLOT_NO}' and S_DATE='{selectedRow.S_DATE}'";
                    ret = oracleDb.ExecuteNonQuery(sql);

                    MessageBox.Show($"削除しました", "", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"レコード削除中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                set_gridCtrlTube();
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
                DateTime? startDate = StartDatePicker.SelectedDate;
                string dateStart = startDate.Value.ToString("yyyyMMdd");

                OracleDatabase oracleDb = new OracleDatabase(connectStrOra);

                // 新規登録処理

                string sql = $"select * from D_CTRL_QCLOT_INFO"
                    + $" where K_CODE='{TxtK_CODE.Text}' and S_DATE='{dateStart}'";
                DataTable resultInfo = oracleDb.ExecuteQuery(sql);
                if (resultInfo.Rows.Count > 0)
                {
                    MessageBox.Show("D_CTRL_QCLOT_INFO テーブルに登録済みです。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                sql = $"insert into D_CTRL_QCLOT_INFO (QCLOT_NO, TUBE_CODE, K_CODE, S_DATE, SPEC_MEMO)"
                    + $" values ('{TxtQCLOT_NO.Text}','*','{TxtK_CODE.Text}','{dateStart}','{TxtSPEC_MEMO.Text}')";
                oracleDb.ExecuteNonQuery(sql);

                // コントロールQCロット定性項目設定テーブルの更新or登録
                upseartTeiseiTable("insert", TxtQCLOT_NO.Text, dateStart);

                StartDatePicker.SelectedDate = DateTime.Now;
                TxtQCLOT_NO.Text = "";
                TxtSPEC_MEMO.Text = "";

                set_gridCtrlTube();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"レコード挿入中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        public class dCtrlLot : INotifyPropertyChanged
        {
            private string _qclotNo;
            private string _sDate;
            private string _specMemo;

            public string TUBE_CODE { get; set; }
            public string K_CODE { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            public string S_DATE
            {
                get { return _sDate; }
                set
                {
                    if (_sDate != value)
                    {
                        _sDate = value;
                        OnPropertyChanged(nameof(S_DATE));
                    }
                }
            }
            public string QCLOT_NO
            {
                get { return _qclotNo; }
                set
                {
                    if (_qclotNo != value)
                    {
                        _qclotNo = value;
                        OnPropertyChanged(nameof(QCLOT_NO));
                    }
                }
            }
            public string SPEC_MEMO
            {
                get { return _specMemo; }
                set
                {
                    if (_specMemo != value)
                    {
                        _specMemo = value;
                        OnPropertyChanged(nameof(SPEC_MEMO));
                    }
                }
            }
        }

        /// <summary>
        /// グリッドの行選択時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CmbTubeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            set_gridCtrlTube();
        }

        /// <summary>
        /// グリッドの行選択時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridLot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dataGridLot.SelectedItem is dCtrlLot row)
            {
                try
                {
                    using (OracleConnection connection = new OracleConnection(connectStrOra))
                    {
                        connection.Open();

                        string query = "SELECT * FROM D_CTRL_QCLOT_TEISEI"
                            + $" WHERE QCLOT_NO = '{row.QCLOT_NO}'"
                            + $" AND K_CODE = '{row.K_CODE}'"
                            + $" AND S_DATE = '{row.S_DATE}'";
                        OracleCommand command = new OracleCommand(query, connection);
                        OracleDataAdapter dataAdapter = new OracleDataAdapter(command);
                        DataTable teiseiTable = new DataTable();
                        dataAdapter.Fill(teiseiTable);
                        if (teiseiTable.Rows.Count > 0)
                        {
                            foreach (DataRow ctrlRow in teiseiTable.Rows)
                            {
                                ComboBox[] comboBoxesVal = new[] { cmbVal_H0 };
                                CheckBox[] checkBoxesChk = new[] { chkChk_H0 };
                                var lvKeys = new[] { "LV_0", "LV_1", "LV_2", "LV_3", "LV_4", "LV_5", "LV_6", "LV_7" };

                                bool isSet = false;
                                string tubeCode = ctrlRow["TUBE_CODE"].ToString();

                                if (txtCtrlHigh.Text == tubeCode)
                                {
                                    comboBoxesVal = new[] { cmbVal_H0, cmbVal_H1, cmbVal_H2, cmbVal_H3, cmbVal_H4, cmbVal_H5, cmbVal_H6, cmbVal_H7 };
                                    checkBoxesChk = new[] { chkChk_H0, chkChk_H1, chkChk_H2, chkChk_H3, chkChk_H4, chkChk_H5, chkChk_H6, chkChk_H7 };
                                    isSet = true;
                                }
                                else if (this.txtCtrlLow.Text == tubeCode)
                                {
                                    comboBoxesVal = new[] { cmbVal_L0, cmbVal_L1, cmbVal_L2, cmbVal_L3, cmbVal_L4, cmbVal_L5, cmbVal_L6, cmbVal_L7 };
                                    checkBoxesChk = new[] { chkChk_L0, chkChk_L1, chkChk_L2, chkChk_L3, chkChk_L4, chkChk_L5, chkChk_L6, chkChk_L7 };
                                    isSet = true;
                                }
                                if (isSet)
                                {
                                    for (int i = 0; i < lvKeys.Length; i++)
                                    {
                                        string[] values = ctrlRow[lvKeys[i]].ToString().Split(':');
                                        if (values.Length > 1)
                                        {
                                            if (values[0] == "-")
                                                comboBoxesVal[i].SelectedItem = " -";
                                            else if (values[0] == "+")
                                                comboBoxesVal[i].SelectedItem = " +";
                                            else
                                                comboBoxesVal[i].SelectedItem = values[0];
                                            checkBoxesChk[i].IsChecked = values[1] == "1";
                                        }
                                        else
                                        {
                                            comboBoxesVal[i].SelectedItem = "";
                                            checkBoxesChk[i].IsChecked = false;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            // レベル＆チェックのクリア
                            clrLevelAndCheck();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// レベル＆チェックのクリア
        /// </summary>
        private void clrLevelAndCheck()
        {
            var comboBoxesVal = new[] { cmbVal_H0, cmbVal_H1, cmbVal_H2, cmbVal_H3, cmbVal_H4, cmbVal_H5, cmbVal_H6, cmbVal_H7
                                ,cmbVal_L0, cmbVal_L1, cmbVal_L2, cmbVal_L3, cmbVal_L4, cmbVal_L5, cmbVal_L6, cmbVal_L7
                                ,Val_H0, Val_H1, Val_H2, Val_H3, Val_H4, Val_H5, Val_H6, Val_H7
                                ,Val_L0, Val_L1, Val_L2, Val_L3, Val_L4, Val_L5, Val_L6, Val_L7 };
            var checkBoxesChk = new[] { chkChk_H0, chkChk_H1, chkChk_H2, chkChk_H3, chkChk_H4, chkChk_H5, chkChk_H6, chkChk_H7
                                ,chkChk_L0, chkChk_L1, chkChk_L2, chkChk_L3, chkChk_L4, chkChk_L5, chkChk_L6, chkChk_L7
                                ,Chk_H0, Chk_H1, Chk_H2, Chk_H3, Chk_H4, Chk_H5, Chk_H6, Chk_H7
                                ,Chk_L0, Chk_L1, Chk_L2, Chk_L3, Chk_L4, Chk_L5, Chk_L6, Chk_L7 };
            for (int i = 0; i < comboBoxesVal.Length; i++)
            {
                comboBoxesVal[i].SelectedItem = "";
                checkBoxesChk[i].IsChecked = false;
            }
        }
    }
}
