using System;
using System.Collections.Generic;
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
    /// MasterMentenanceMenu.xaml の相互作用ロジック
    /// </summary>
    public partial class MasterMentenanceMenu : Window
    {
        public MasterMentenanceMenu()
        {
            InitializeComponent();
        }
        private void btnControlChart_Click(object sender, RoutedEventArgs e)
        {
            MasterMentenanceCtrlChart masterMente = new MasterMentenanceCtrlChart()
            {
                Owner = this
            };
            masterMente.Show();
        }

        private void btnControlTube_Click(object sender, RoutedEventArgs e)
        {
            // コントロールチューブボタンがクリックされたときの処理
            MessageBox.Show("コントロールチューブボタンがクリックされました。");
        }

        private void btnTube_Click(object sender, RoutedEventArgs e)
        {
            // チューブボタンがクリックされたときの処理
            MasterMentenanceTube masterMentenanceTube = new MasterMentenanceTube()
            {
                Owner = this
            };
            masterMentenanceTube.Show();
        }

        private void btnManagementFactors_Click(object sender, RoutedEventArgs e)
        {
            // 管理係数ボタンがクリックされたときの処理
            MessageBox.Show("管理係数ボタンがクリックされました。（作成中です）");
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            // 終了ボタンがクリックされたときの処理
            this.Close();
        }
    }
}
