using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// MasterMentenanceTube.xaml の相互作用ロジック
    /// </summary>
    public partial class MasterMentenanceTube : Window
    {
        // Oracleデータベースへの接続文字列
        private string connectStrOra = ConfigurationManager.AppSettings["OraConnectString"];

        public MasterMentenanceTube()
        {
            InitializeComponent();

            // M_CTRL_TUBE_MST -> DataGrid にデータをロードする
            LoadData();
            // ラジオボタンの初期値を設定
            optionAddid.IsChecked = true;
        }

        /// <summary>
        /// M_CTRL_TUBE_MST -> DataGrid にデータをロードする
        /// </summary>
        private void LoadData()
        {
            try
            {
                tubeCodeTextBox.Text = "";
                tubeNameTextBox.Text = "";
                tubeShortNameTextBox.Text = "";

                using (OracleConnection connection = new OracleConnection(connectStrOra))
                {
                    connection.Open();
                    string query = "SELECT TUBE_CODE, TUBE_NAME, TUBE_RYAK"
                        + ", TO_CHAR(CREATE_DATE, 'YYYY-MM-DD') AS CREATE_DATE, TO_CHAR(UPDATE_DATE,'YYYY-MM-DD') AS UPDATE_DATE"
                        + " FROM M_CTRL_TUBE_MST ORDER BY TUBE_CODE";

                    OracleCommand command = new OracleCommand(query, connection);
                    OracleDataAdapter dataAdapter = new OracleDataAdapter(command);

                    DataTable dataTable = new DataTable();
                    dataAdapter.Fill(dataTable);

                    dataGrid.ItemsSource = dataTable.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            // 終了ボタンがクリックされたときの処理
            this.Close();
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

                string sql = $"select * from M_CTRL_TUBE_MST where TUBE_CODE='{tubeCodeTextBox.Text}'";
                DataTable result = oracleDb.ExecuteQuery(sql);

                if (result.Rows.Count > 0)
                {
                    // 更新処理
                    MessageBoxResult resultMsg = MessageBox.Show("更新してもよろしいですか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (resultMsg == MessageBoxResult.Yes)
                    {
                        sql = $"update M_CTRL_TUBE_MST set "
                            + $"TUBE_NAME='{tubeNameTextBox.Text}',"
                            + $"TUBE_RYAK='{tubeShortNameTextBox.Text}',"
                            + $"UPDATE_DATE=sysdate"
                            + $" where TUBE_CODE='{tubeCodeTextBox.Text}'";
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

                string sql = $"select * from M_CTRL_TUBE_MST where TUBE_CODE='{tubeCodeTextBox.Text}'";
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
                    sql = $"insert into M_CTRL_TUBE_MST (TUBE_CODE,TUBE_NAME,TUBE_RYAK,CREATE_DATE)"
                        + $" values ('{tubeCodeTextBox.Text}','{tubeNameTextBox.Text}','{tubeShortNameTextBox.Text}',sysdate)";
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
        /// DB削除処理
        /// </summary>
        private void dbDelete()
        {
            try
            {
                // 既存データの場合
                OracleDatabase oracleDb = new OracleDatabase(connectStrOra);

                string sql = $"select * from M_CTRL_TUBE_MST where TUBE_CODE='{tubeCodeTextBox.Text}'";
                DataTable result = oracleDb.ExecuteQuery(sql);

                if (result.Rows.Count > 0)
                {
                    MessageBoxResult resultMsg = MessageBox.Show("削除してもよろしいですか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (resultMsg == MessageBoxResult.Yes)
                    {
                        sql = $"delete from M_CTRL_TUBE_MST where TUBE_CODE='{tubeCodeTextBox.Text}'";
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


        private void optionUpdate_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void optionAddid_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void optionDelete_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dataGrid.SelectedItem is DataRowView row)
            {
                tubeCodeTextBox.Text = row["TUBE_CODE"].ToString();
                tubeNameTextBox.Text = row["TUBE_NAME"].ToString();
                tubeShortNameTextBox.Text = row["TUBE_RYAK"].ToString();
                optionUpdate.IsChecked = true;      // 更新モードに変更
            }
        }
    }
}
