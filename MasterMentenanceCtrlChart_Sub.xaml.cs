using Oracle.ManagedDataAccess.Client;
using System;
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

namespace ControlChart
{
    /// <summary>
    /// MasterMentenanceCtrlChart_Sub.xaml の相互作用ロジック
    /// </summary>
    public partial class MasterMentenanceCtrlChart_Sub : Window
    {
        public string ownerK_CODE;

        // Oracleデータベースへの接続文字列
        private string connectStrOra = ConfigurationManager.AppSettings["OraConnectString"];

        public ObservableCollection<mCtrlTube> gridCtrlTube { get; set; }

        public ObservableCollection<mCtrlTube> gridAddRecord { get; set; }

        public MasterMentenanceCtrlChart_Sub()
        {
            InitializeComponent();

            MyViewModel viewModel = new MyViewModel();
            DataContext = viewModel;

            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            TxtK_CODE.Text = ownerK_CODE;

            // データグリッドの初期化（登録データの取得）

            gridCtrlTube = new ObservableCollection<mCtrlTube>();
            set_gridCtrlTube();
            dataGridCtrlTube.ItemsSource = gridCtrlTube;

            // データグリッドの初期化（追加データの取得）

            gridAddRecord = new ObservableCollection<mCtrlTube>();
            gridAddRecord.Add(new mCtrlTube
            {
                TUBE_CODE = "",
                MNG_NAME = "",
                MNG_RYAK = "",
                CUTOFF_H = 99999,
                CUTOFF_L = 0,
                CTRLPARM_DISP_SU = 2
            });
            addRecordDataGrid.ItemsSource = gridAddRecord;
        }

        private void set_gridCtrlTube()
        {
            gridCtrlTube.Clear();
            try
            {
                OracleDatabase db = new OracleDatabase(ConfigurationManager.AppSettings["OraConnectString"]);
                DataTable dt = db.ExecuteQuery("SELECT * FROM M_CTRL_TUBE WHERE K_CODE = :K_CODE", new Dictionary<string, object>
                {
                    { "K_CODE", ownerK_CODE }
                });

                foreach (DataRow row in dt.Rows)
                {
                    gridCtrlTube.Add(new mCtrlTube
                    {
                        TUBE_CODE = row["TUBE_CODE"].ToString(),
                        MNG_NAME = row["MNG_NAME"].ToString(),
                        MNG_RYAK = row["MNG_RYAK"].ToString(),
                        CUTOFF_H = Convert.ToDouble(row["CUTOFF_H"]),
                        CUTOFF_L = Convert.ToDouble(row["CUTOFF_L"]),
                        CTRLPARM_DISP_SU = Convert.ToInt32(row["CTRLPARM_DISP_SU"])
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"データ取得中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is string selectedTubeCode)
            {
                // コンボボックスのDataContextを取得
                if (comboBox.DataContext is mCtrlTube selectedRow)
                {
                    // 適宜、隣のカラムの値を更新（例：MNG_NAME）
                    selectedRow.MNG_NAME = "";
                    selectedRow.MNG_RYAK = "";
                }
            }
        }
        public class MyViewModel
        {
            public ObservableCollection<string> TubeCodes { get; set; } = new ObservableCollection<string>
            {
                "CTRL",
                "Code2",
                "Code3"
            };
            public MyViewModel()
            {
                TubeCodes.Clear();
                OracleDatabase db = new OracleDatabase(ConfigurationManager.AppSettings["OraConnectString"]);
                string sql = "select TUBE_CODE from M_CTRL_TUBE_MST order by TUBE_CODE";
                DataTable dt = db.ExecuteQuery(sql);
                TubeCodes.Add("");
                foreach (DataRow row in dt.Rows)
                {
                    TubeCodes.Add(row["TUBE_CODE"].ToString());
                }
            }
        }

        /// <summary>
        /// ボタン（更新）クリック時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void registerButton_Click(object sender, RoutedEventArgs e)
        {
            // 選択された行を取得
            if (dataGridCtrlTube.SelectedItem is mCtrlTube selectedRow)
            {
                //MessageBox.Show($"選択された行: TUBE_CODE = {selectedRow.TUBE_CODE}, MNG_NAME = {selectedRow.MNG_NAME}");
                try
                {
                    OracleDatabase oracleDb = new OracleDatabase(connectStrOra);

                    string sql = $"select * from M_CTRL_TUBE where K_CODE='{TxtK_CODE.Text}' and TUBE_CODE='{selectedRow.TUBE_CODE}'";
                    DataTable result = oracleDb.ExecuteQuery(sql);
                    if (result.Rows.Count <= 0)
                    {
                        MessageBox.Show("更新対象レコードがありません。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    // 新規登録処理
                    sql = $"update M_CTRL_TUBE set MNG_NAME='{selectedRow.MNG_NAME}', MNG_RYAK='{selectedRow.MNG_RYAK}'"
                        + $", CUTOFF_H='{selectedRow.CUTOFF_H}', CUTOFF_L='{selectedRow.CUTOFF_L}', CTRLPARM_DISP_SU={selectedRow.CTRLPARM_DISP_SU}"
                        + $" where TUBE_CODE='{selectedRow.TUBE_CODE}' and K_CODE='{TxtK_CODE.Text}'";
                    oracleDb.ExecuteNonQuery(sql);
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
        /// ボタン（削除）クリック時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void deleteButton_Click(object sender, RoutedEventArgs e)
        {
            // 選択された行を取得
            if (dataGridCtrlTube.SelectedItem is mCtrlTube selectedRow)
            {
                MessageBox.Show($"選択された行: TUBE_CODE = {selectedRow.TUBE_CODE}, MNG_NAME = {selectedRow.MNG_NAME}");
                try
                {
                    OracleDatabase oracleDb = new OracleDatabase(connectStrOra);

                    string sql = $"select * from M_CTRL_TUBE where K_CODE='{TxtK_CODE.Text}' and TUBE_CODE='{selectedRow.TUBE_CODE}'";
                    DataTable result = oracleDb.ExecuteQuery(sql);
                    if (result.Rows.Count <= 0)
                    {
                        MessageBox.Show("削除対象レコードがありません。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    // 新規登録処理
                    sql = $"delete from M_CTRL_TUBE where TUBE_CODE='{selectedRow.TUBE_CODE}' and K_CODE='{TxtK_CODE.Text}'";
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

        /// <summary>
        /// ボタン（追加）クリック時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void addButton_Click(object sender, RoutedEventArgs e)
        {
            var rec = gridAddRecord[0];

            try
            {
                OracleDatabase oracleDb = new OracleDatabase(connectStrOra);

                string sql = $"select * from M_CTRL_TUBE where K_CODE='{TxtK_CODE.Text}' and TUBE_CODE='{rec.TUBE_CODE}'";
                DataTable result = oracleDb.ExecuteQuery(sql);
                if (result.Rows.Count > 0)
                {
                    MessageBox.Show("登録済みのチューブコードです。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                    rec.TUBE_CODE = "";
                    rec.MNG_NAME = "";
                    rec.MNG_RYAK = "";
                    rec.CUTOFF_H = 99999;
                    rec.CUTOFF_L = 0;
                    rec.CTRLPARM_DISP_SU = 2;
                    return;
                }
                // 新規登録処理
                sql = $"insert into M_CTRL_TUBE (TUBE_CODE, K_CODE, MNG_NAME, MNG_RYAK, CUTOFF_H, CUTOFF_L, CTRLPARM_DISP_SU)"
                    + $" values ('{rec.TUBE_CODE}','{TxtK_CODE.Text}','{rec.MNG_NAME}','{rec.MNG_RYAK}',{rec.CUTOFF_H},{rec.CUTOFF_L},{rec.CTRLPARM_DISP_SU})";
                oracleDb.ExecuteNonQuery(sql);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"レコード挿入中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            gridCtrlTube.Add(new mCtrlTube
            {
                TUBE_CODE = rec.TUBE_CODE,
                MNG_NAME = rec.MNG_NAME,
                MNG_RYAK = rec.MNG_RYAK,
                CUTOFF_H = rec.CUTOFF_H,
                CUTOFF_L = rec.CUTOFF_L,
                CTRLPARM_DISP_SU = rec.CTRLPARM_DISP_SU
            });
            rec.TUBE_CODE = "";
            rec.MNG_NAME = "";
            rec.MNG_RYAK = "";
            rec.CUTOFF_H = 99999;
            rec.CUTOFF_L = 0;
            rec.CTRLPARM_DISP_SU = 2;
        }

        /// <summary>
        /// ボタン（終了）クリック時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class mCtrlTube : INotifyPropertyChanged
    {
        private string _tubeCode;
        private string _mngName;
        private string _mngRyak;
        private double _cutoffH;
        private double _cutoffL;
        private int _ctrlParmDispSu;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string TUBE_CODE
        {
            get { return _tubeCode; }
            set
            {
                if (_tubeCode != value)
                {
                    _tubeCode = value;
                    OnPropertyChanged(nameof(TUBE_CODE));
                }
            }
        }

        public string MNG_NAME
        {
            get { return _mngName; }
            set
            {
                if (_mngName != value)
                {
                    _mngName = value;
                    OnPropertyChanged(nameof(MNG_NAME));
                }
            }
        }

        public string MNG_RYAK
        {
            get { return _mngRyak; }
            set
            {
                if (_mngRyak != value)
                {
                    _mngRyak = value;
                    OnPropertyChanged(nameof(MNG_RYAK));
                }
            }
        }

        public double CUTOFF_H
        {
            get { return _cutoffH; }
            set
            {
                if (_cutoffH != value)
                {
                    _cutoffH = value;
                    OnPropertyChanged(nameof(CUTOFF_H));
                }
            }
        }

        public double CUTOFF_L
        {
            get { return _cutoffL; }
            set
            {
                if (_cutoffL != value)
                {
                    _cutoffL = value;
                    OnPropertyChanged(nameof(CUTOFF_L));
                }
            }
        }

        public int CTRLPARM_DISP_SU
        {
            get { return _ctrlParmDispSu; }
            set
            {
                if (_ctrlParmDispSu != value)
                {
                    _ctrlParmDispSu = value;
                    OnPropertyChanged(nameof(CTRLPARM_DISP_SU));
                }
            }
        }
    }

}
