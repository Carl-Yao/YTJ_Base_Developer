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

namespace SwipCardSystem.View.UserController
{
    /// <summary>
    /// UserControlSound.xaml 的交互逻辑
    /// </summary>
    public partial class UserControlSound : UserControl
    {
        public UserControlSound()
        {
            InitializeComponent();
            for (double start = 76.0; start <= 78.0; start+=0.1)
                Sound.Items.Add(start.ToString());
            Sound.SelectedIndex = 0;
        }
    }
}
