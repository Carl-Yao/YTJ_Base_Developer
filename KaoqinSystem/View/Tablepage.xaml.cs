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
    /// Tablepage.xaml 的交互逻辑
    /// </summary>
    public partial class Tablepage : Page
    {
        public Tablepage()
        {
            InitializeComponent();
        }

        public string TableName
        {
            set 
            {
                tableName.Text = value;
            }
            get
            {
                return tableName.Text;
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}
