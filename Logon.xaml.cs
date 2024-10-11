using ControlChart;
using System;
using System.Collections.Generic;
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

namespace ControlChartQualitative
{
    /// <summary>
    /// Logon.xaml の相互作用ロジック
    /// </summary>
    public partial class Logon : Window
    {
        string connectStr = "";
        OracleDatabase oracleDb = null;

        // 認証フラグ
        public bool PractitionerFlag { get; set; } = false;
        // バーコード値
        public string BarcodeNo { get; set; } = string.Empty;
        // 名前
        public string Name { get; set; } = string.Empty;

        public Logon(Window owner)
        {
            InitializeComponent();
            this.Owner = owner;
        }
        // ウィンドウがロードされたときに、ユーザーIDにフォーカスを当てる
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            connectStr = System.Configuration.ConfigurationManager.AppSettings["OraConnectString"];
            oracleDb = new OracleDatabase(connectStr);

            UserIDTextBox.Focus();
        }
        // UserIDTextBoxでEnterが押されたらPasswordBoxにフォーカスを移す
        private void UserIDTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PasswordBox.Focus();
            }
        }

        // PasswordBoxでEnterが押されたらOKボタンにフォーカスを移す
        private void PasswordBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OkButton.Focus();
            }
        }

        // OKボタンがクリックされたときの処理
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            string userID = UserIDTextBox.Text;
            string password = PasswordBox.Password;

            // 簡単なバリデーション
            if (string.IsNullOrEmpty(userID) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("ユーザーIDまたはパスワードを入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                // ここでユーザーIDとパスワードの検証処理を実行します
                //MessageBox.Show($"ユーザーID: {userID}\nパスワード: {password}", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                string sSql = $"select * from M_USER where U_ID = '{userID}'";
                DataTable result = oracleDb.ExecuteQuery(sSql);
                if (result.Rows.Count > 0)
                {
                    DataRow row = result.Rows[0];

                    if (row["PASS"].ToString() == password)
                    {
                        // 認証成功
                        MessageBox.Show("認証成功", "情報", MessageBoxButton.OK, MessageBoxImage.Information);

                        Name = row["NAME"].ToString();
                        BarcodeNo = userID;
                        PractitionerFlag = true;
                        // 認証成功後の処理（例: ウィンドウを閉じる）
                        this.Close();
                        return;
                    }
                }
                // 認証失敗
                MessageBox.Show("ユーザID または、パスワードが違います", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private string[] splitName(string name)
        {
            string[] ary = name.Split(' ');
            if (ary.Length > 1) { return ary; }
            ary = name.Split('　');
            if (ary.Length > 1) { return ary; }
            Array.Resize(ref ary, 2);
            ary[1] = "";
            return ary;
        }

        // キャンセルボタンがクリックされたときの処理
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // アプリケーションを終了する、またはウィンドウを閉じる
            PractitionerFlag = false;
            this.Close();
        }
    }
}
