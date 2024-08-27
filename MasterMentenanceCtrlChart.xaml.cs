using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
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

namespace ControlChart
{
    /// <summary>
    /// MasterMentenanceCtrlChart.xaml の相互作用ロジック
    /// </summary>
    public partial class MasterMentenanceCtrlChart : Window
    {
        // Oracleデータベースへの接続文字列
        private string connectStrOra = ConfigurationManager.AppSettings["OraConnectString"];

        public MasterMentenanceCtrlChart()
        {
            InitializeComponent();

            LoadData();
        }

        /// <summary>
        /// M_CTRL_TUBE_MST -> DataGrid にデータをロードする
        /// </summary>
        private void LoadData()
        {
            try
            {
                TxtK_CODE.Text = "";
                TxtK_NAME.Text = "";
                TxtK_RYAK.Text = "";
                TxtASSAY_STYLE.Text = "";
                TxtASSAY_UNIT_NAME.Text = "";
                TxtVALID_COL.Text = "";
                RdoOther.IsChecked = true;
                RdoRounding.IsChecked = true;
                RdoRandom.IsChecked = true;
                TxtMEMO_INF.Text = "";
                optionUpdate.IsChecked = true;

                using (OracleConnection connection = new OracleConnection(connectStrOra))
                {
                    connection.Open();
                    string query = "SELECT K_CODE, K_NAME, K_RYAK"
                        + ", TO_CHAR(CREATE_DATE, 'YYYY-MM-DD HH24:MI') AS CREATE_DATE, TO_CHAR(UPDATE_DATE,'YYYY-MM-DD HH24:MI') AS UPDATE_DATE"
                        + ", MEMO_INF"
                        + " FROM M_CTRL_CHART ORDER BY K_CODE";

                    OracleCommand command = new OracleCommand(query, connection);
                    OracleDataAdapter dataAdapter = new OracleDataAdapter(command);

                    DataTable dataTable = new DataTable();
                    dataAdapter.Fill(dataTable);

                    dataGrid.ItemsSource = dataTable.DefaultView;
                    this.InvalidateVisual();    // ウィンドウの再描画
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dataGrid.SelectedItem is DataRowView row)
            {
                try
                {
                    using (OracleConnection connection = new OracleConnection(connectStrOra))
                    {
                        connection.Open();
                        string query = "SELECT * FROM M_CTRL_CHART"
                            + $" WHERE K_CODE = '{row["K_CODE"].ToString()}'";

                        OracleCommand command = new OracleCommand(query, connection);
                        OracleDataAdapter dataAdapter = new OracleDataAdapter(command);

                        DataTable ctrlDataTable = new DataTable();
                        dataAdapter.Fill(ctrlDataTable);
                        if (ctrlDataTable.Rows.Count > 0)
                        {
                            DataRow dataRow = ctrlDataTable.Rows[0];
                            TxtK_CODE.Text = dataRow["K_CODE"].ToString();
                            // 丸目条件のラジオボタン設定
                            string kind = dataRow["K_KUBUN"].ToString();
                            if (kind == "0")
                            {
                                RdoOther.IsChecked = true;      // その他
                            }
                            else if (kind == "1")
                            {
                                RdoBlood.IsChecked = true;      // 血液
                            }
                            else if (kind == "2")
                            {
                                RdoBiochemistry.IsChecked = true;   // 生化学
                            }
                            else if (kind == "3")
                            {
                                RdoUrine.IsChecked = true;      // 尿
                            }
                            TxtK_NAME.Text = dataRow["K_NAME"].ToString();
                            TxtK_RYAK.Text = dataRow["K_RYAK"].ToString();
                            TxtASSAY_STYLE.Text = dataRow["ASSAY_STYLE"].ToString();
                            TxtASSAY_UNIT_NAME.Text = dataRow["ASSAY_UNIT_NAME"].ToString();
                            TxtVALID_COL.Text = dataRow["VALID_COL"].ToString();
                            TxtMEMO_INF.Text = dataRow["MEMO_INF"].ToString();

                            // 丸目条件のラジオボタン設定
                            string roundingCondition = dataRow["MARU_COND"].ToString();
                            if (roundingCondition == "0")
                            {
                                RdoRounding.IsChecked = true;   // 四捨五入
                            }
                            else if (roundingCondition == "1")
                            {
                                RdoRoundDown.IsChecked = true;  // 切り捨て
                            }
                            else if (roundingCondition == "2")
                            {
                                RdoRoundUp.IsChecked = true;    // 切り上げ
                            }

                            // その他のラジオボタン設定
                            string selectionCondition = dataRow["GETSEL_KBN"].ToString();
                            if (selectionCondition == "0")
                            {
                                RdoTop.IsChecked = true;    // TOPからN本
                            }
                            else if (selectionCondition == "1")
                            {
                                RdoRandom.IsChecked = true; // ランダムにN本
                            }
                        }

                    }

                    optionUpdate.IsChecked = true;      // 更新モードに変更
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// ボタン（登録）がクリックされたときの処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void registerButton_Click(object sender, RoutedEventArgs e)
        {
            if (optionUpdate.IsChecked == true)         // 更新モードの場合
            {
                dbUpdate();
            }
            else if (optionAddid.IsChecked == true)     // 新規登録モードの場合
            {
                dbInsert();
            }
            else if (optionDelete.IsChecked == true)    // 削除モードの場合
            {
                dbDelete();
            }
        }

        /// <summary>
        /// DB更新処理
        /// </summary>
        private void dbUpdate()
        {
            try
            {
                OracleDatabase oracleDb = new OracleDatabase(connectStrOra);

                string sql = $"select * from M_CTRL_CHART where K_CODE='{TxtK_CODE.Text}'";
                DataTable result = oracleDb.ExecuteQuery(sql);

                if (result.Rows.Count > 0)
                {
                    // 更新処理
                    MessageBoxResult resultMsg = MessageBox.Show("更新してもよろしいですか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (resultMsg == MessageBoxResult.Yes)
                    {
                        sql = $"update M_CTRL_CHART set "
                            + $"K_NAME='{TxtK_NAME.Text}'"
                            + $",K_RYAK='{TxtK_RYAK.Text}'"
                            + $",ASSAY_STYLE='{TxtASSAY_STYLE.Text}'"
                            + $",ASSAY_UNIT_NAME='{TxtASSAY_UNIT_NAME.Text}'"
                            + $",VALID_COL='{TxtVALID_COL.Text}'"
                            + $",MARU_COND='{getMARU_COND()}'"
                            + $",GETSEL_KBN='{getGETSEL_KBN()}'"
                            + $",MEMO_INF='{TxtMEMO_INF.Text}'"
                            + $",UPDATE_DATE=sysdate"
                            + $",K_KUBUN={getK_KUBUN()}"
                            + $" where K_CODE={TxtK_CODE.Text}";
                        oracleDb.ExecuteNonQuery(sql);

                        MessageBox.Show("更新しました。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);

                        // M_CTRL_TUBE_MST -> DataGrid にデータをロードする
                        LoadData();
                    }
                }
                else
                {
                    MessageBox.Show("更新対象はありません。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"データの更新中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// DB新規登録処理
        /// </summary>
        private void dbInsert()
        {
            try
            {
                OracleDatabase oracleDb = new OracleDatabase(connectStrOra);

                string sql = $"select * from M_CTRL_CHART where K_CODE='{TxtK_CODE.Text}'";
                DataTable result = oracleDb.ExecuteQuery(sql);
                if (result.Rows.Count > 0)
                {
                    MessageBox.Show("登録済みのチューブコードです。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                // 新規登録処理
                MessageBoxResult resultMsg = MessageBox.Show("新規登録してもよろしいですか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (resultMsg == MessageBoxResult.Yes)
                {
                    sql = $"insert into M_CTRL_CHART (K_CODE, K_NAME, K_RYAK, ASSAY_STYLE, ASSAY_UNIT_NAME, VALID_COL, MARU_COND, GETSEL_KBN, CREATE_DATE, K_KUBUN)"
                        + $" values ('{TxtK_CODE.Text}','{TxtK_NAME.Text}','{TxtK_RYAK.Text}','{TxtASSAY_STYLE.Text}','{TxtASSAY_UNIT_NAME.Text}'"
                        + $",{TxtVALID_COL.Text},{getMARU_COND()},{getGETSEL_KBN()}, sysdate, {getK_KUBUN()})";
                    oracleDb.ExecuteNonQuery(sql);

                    MessageBox.Show("追加しました。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);

                    // M_CTRL_TUBE_MST -> DataGrid にデータをロードする
                    LoadData();
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"レコード挿入中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 丸目条件のラジオボタンの設定値を取得する
        /// </summary>
        /// <returns></returns>
        private string getK_KUBUN()
        {
            string kubun = RdoBlood.IsChecked == true ? "1" :
                RdoBiochemistry.IsChecked == true ? "2" :
                RdoUrine.IsChecked == true ? "3" : "0";
            return kubun;
        }

        /// <summary>
        /// 丸目条件のラジオボタンの設定値を取得する
        /// </summary>
        /// <returns></returns>
        private string getMARU_COND()
        {
            string maruCond = RdoRounding.IsChecked == true ? "0" :
                RdoRoundDown.IsChecked == true ? "1" :
                RdoRoundUp.IsChecked == true ? "2" : "0";
            return maruCond;
        }

        /// <summary>
        /// 取込選択区分のラジオボタンの設定値を取得する
        /// </summary>
        /// <returns></returns>
        private string getGETSEL_KBN()
        {
            string getSel = RdoTop.IsChecked == true ? "0" :
                RdoRandom.IsChecked == true ? "1" : "1";
            return getSel;
        }
        /// <summary>
        /// DB削除処理
        /// </summary>
        private void dbDelete()
        {
            try
            {
                // 既存データの場合
                OracleDatabase oracleDb = new OracleDatabase(connectStrOra);

                string sql = $"select * from M_CTRL_CHART where K_CODE='{TxtK_CODE.Text}'";
                DataTable result = oracleDb.ExecuteQuery(sql);

                if (result.Rows.Count > 0)
                {
                    MessageBoxResult resultMsg = MessageBox.Show("削除してもよろしいですか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (resultMsg == MessageBoxResult.Yes)
                    {
                        sql = $"delete from M_CTRL_CHART where K_CODE='{TxtK_CODE.Text}'";
                        oracleDb.ExecuteNonQuery(sql);

                        MessageBox.Show("削除しました。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);

                        // M_CTRL_TUBE_MST -> DataGrid にデータをロードする
                        LoadData();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"データの削除中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// ボタン（コントロール登録）がクリックされたときの処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void controlButton_Click(object sender, RoutedEventArgs e)
        {
            MasterMentenanceCtrlChart_Sub maintenanceMaster = new MasterMentenanceCtrlChart_Sub
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            maintenanceMaster.ownerK_CODE = TxtK_CODE.Text;

            maintenanceMaster.ShowDialog();

        }
    }
}
