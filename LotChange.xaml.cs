﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data;
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
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace ControlChart
{
    /// <summary>
    /// LotChange.xaml の相互作用ロジック
    /// </summary>
    public partial class LotChange : Window
    {
        public string ownerK_CODE;

        // Oracleデータベースへの接続文字列

        private string connectStrOra = ConfigurationManager.AppSettings["OraConnectString"];
        // ロット情報のグリッド表示用
        public ObservableCollection<dCtrlLot> gridCtrlLot { get; set; }
        private ObservableCollection<CtrlList> cmbCtrl = new ObservableCollection<CtrlList>();


        public LotChange()
        {
            InitializeComponent();

            this.Loaded += MainWindow_Loaded;
            //dataGridLot.PreviewKeyDown += DataGridLot_PreviewKeyDown;

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


            // データグリッドの初期化（登録データの取得）
            gridCtrlLot = new ObservableCollection<dCtrlLot>();
            gridCtrlLot.Clear();
            dataGridLot.ItemsSource = gridCtrlLot;

        }
        private void set_gridCtrlTube()
        {
            gridCtrlLot.Clear();
            try
            {
                OracleDatabase db = new OracleDatabase(ConfigurationManager.AppSettings["OraConnectString"]);

                string ctrlCode = CmbCtrlList.SelectedItem is CtrlList selectedItem ? selectedItem.CtrlCode : "";
                if (string.IsNullOrEmpty(ctrlCode))
                {
                    MessageBox.Show("コントロールコードを選択してください。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                DataTable dt = db.ExecuteQuery("select * from D_CTRL_QCLOT_INFO where K_CODE = :K_CODE and TUBE_CODE = :TUBE_CODE order by S_DATE", new Dictionary<string, object>
                {
                    { "K_CODE", ownerK_CODE },
                    { "TUBE_CODE", ctrlCode }
                });

                foreach (DataRow row in dt.Rows)
                {
                    gridCtrlLot.Add(new dCtrlLot
                    {
                        S_DATE = row["S_DATE"].ToString(),
                        QCLOT_NO = row["QCLOT_NO"].ToString(),
                        MNGVAL = row["MNGVAL"].ToString(),
                        DAYOVER_CV = row["DAYOVER_CV"].ToString(),
                        DAYIN_CV = row["DAYIN_CV"].ToString(),
                        SPEC_MEMO = row["SPEC_MEMO"].ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"データ取得中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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

                    string ctrlCode = CmbCtrlList.SelectedItem is CtrlList selectedItem ? selectedItem.CtrlCode : "";
                    if (string.IsNullOrEmpty(ctrlCode))
                    {
                        MessageBox.Show("コントロールコードを選択してください。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    string sql = $"select * from D_CTRL_QCLOT_INFO where K_CODE='{TxtK_CODE.Text}' and TUBE_CODE='{ctrlCode}'"
                        + $" and QCLOT_NO='{selectedRow.QCLOT_NO}' and S_DATE='{selectedRow.S_DATE}'";
                    DataTable result = oracleDb.ExecuteQuery(sql);
                    if (result.Rows.Count <= 0)
                    {
                        MessageBox.Show("更新対象レコードがありません。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    // 更新処理
                    sql = $"update D_CTRL_QCLOT_INFO set"
                        + $" MNGVAL='{selectedRow.MNGVAL}', DAYOVER_CV='{selectedRow.DAYOVER_CV}'"
                        + $", DAYIN_CV='{selectedRow.DAYIN_CV}', SPEC_MEMO='{selectedRow.SPEC_MEMO}'"
                        + $" where K_CODE='{TxtK_CODE.Text}' and TUBE_CODE='{ctrlCode}'"
                        + $" and QCLOT_NO='{selectedRow.QCLOT_NO}' and S_DATE='{selectedRow.S_DATE}'";
                    oracleDb.ExecuteNonQuery(sql);
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

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            // 選択された行を取得
            if (dataGridLot.SelectedItem is dCtrlLot selectedRow)
            {
                MessageBox.Show($"選択された行: QCLOT_NO = {selectedRow.QCLOT_NO}, S_DATE = {selectedRow.S_DATE}");
                try
                {
                    OracleDatabase oracleDb = new OracleDatabase(connectStrOra);

                    string ctrlCode = CmbCtrlList.SelectedItem is CtrlList selectedItem ? selectedItem.CtrlCode : "";
                    if (string.IsNullOrEmpty(ctrlCode))
                    {
                        MessageBox.Show("コントロールコードを選択してください。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    string sql = $"select * from D_CTRL_QCLOT_INFO where K_CODE='{TxtK_CODE.Text}' and TUBE_CODE='{ctrlCode}'"
                        + $" and QCLOT_NO='{selectedRow.QCLOT_NO}' and S_DATE='{selectedRow.S_DATE}'";
                    DataTable result = oracleDb.ExecuteQuery(sql);
                    if (result.Rows.Count <= 0)
                    {
                        MessageBox.Show("削除対象レコードがありません。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    // 削除処理
                    sql = $"delete from D_CTRL_QCLOT_INFO where K_CODE='{TxtK_CODE.Text}' and TUBE_CODE='{ctrlCode}'"
                        + $" and QCLOT_NO='{selectedRow.QCLOT_NO}' and S_DATE='{selectedRow.S_DATE}'";
                    oracleDb.ExecuteNonQuery(sql);
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

                string ctrlCode = CmbCtrlList.SelectedItem is CtrlList selectedItem ? selectedItem.CtrlCode : "";
                if (string.IsNullOrEmpty(ctrlCode))
                {
                    MessageBox.Show("コントロールコードを選択してください。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                string sql = $"select * from D_CTRL_QCLOT_INFO"
                    + $" where K_CODE='{TxtK_CODE.Text}' and TUBE_CODE='{ctrlCode}' and S_DATE='{dateStart}'";
                DataTable result = oracleDb.ExecuteQuery(sql);
                if (result.Rows.Count > 0)
                {
                    MessageBox.Show("登録済みです。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                // 新規登録処理
                sql = $"insert into D_CTRL_QCLOT_INFO (QCLOT_NO, TUBE_CODE, K_CODE, MNGVAL, DAYOVER_CV, DAYIN_CV, S_DATE, SPEC_MEMO)"
                    + $" values ('{TxtQCLOT_NO.Text}','{ctrlCode}','{TxtK_CODE.Text}','{TxtMNGVAL.Text}'"
                    + $",'{TxtDAYOVER_CV.Text}','{TxtDAYIN_CV.Text}','{dateStart}','{TxtSPEC_MEMO.Text}')";
                oracleDb.ExecuteNonQuery(sql);

                StartDatePicker.SelectedDate = DateTime.Now;
                TxtQCLOT_NO.Text = "";
                TxtMNGVAL.Text = "";
                TxtDAYOVER_CV.Text = "";
                TxtDAYIN_CV.Text = "";
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
            private string _sDate;
            private string _qclotNo;
            private string _mngVal;
            private string _dayoverCv;
            private string _dayinCv;
            private string _specMemo;

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
            public string MNGVAL
            {
                get { return _mngVal; }
                set
                {
                    if (_mngVal != value)
                    {
                        _mngVal = value;
                        OnPropertyChanged(nameof(MNGVAL));
                    }
                }
            }
            public string DAYOVER_CV
            {
                get { return _dayoverCv; }
                set
                {
                    if (_dayoverCv != value)
                    {
                        _dayoverCv = value;
                        OnPropertyChanged(nameof(DAYOVER_CV));
                    }
                }
            }
            public string DAYIN_CV
            {
                get { return _dayinCv; }
                set
                {
                    if (_dayinCv != value)
                    {
                        _dayinCv = value;
                        OnPropertyChanged(nameof(DAYIN_CV));
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

        private void CmbTubeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            set_gridCtrlTube();
        }
    }
}
