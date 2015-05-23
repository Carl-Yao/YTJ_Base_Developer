using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SwipCardSystem.View
{
    /// <summary>
    /// MaintainPage.xaml 的交互逻辑
    /// </summary>
    public partial class MaintainPage : Window
    {
        public MaintainPage()
        {
            //progress.progressText.Text = "正在进行" + _maintainWorkType + "操作......";
            InitializeComponent();
        }

        private string _maintainWorkType = string.Empty;

        public string MaintainWorkType
        {
            get
            {
                return _maintainWorkType;
            }
            set
            {
                _maintainWorkType = value;
            }
        }
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            Close();
            //Hide();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            button1.Visibility = Visibility.Collapsed;
            textBlock1.Text = _maintainWorkType;
        }
        public void SetComputerState()
        {
            textBlock1.Text = _maintainWorkType;
            button1.Visibility = Visibility.Visible;
            progress.Visibility = Visibility.Collapsed;
        }
    }
}
