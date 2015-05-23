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
using System.Windows.Shapes;
using System.Windows.Navigation;
using System.Data.SqlClient;
using System.Data;
using SwipCardSystem.Controller;


namespace SwipCardSystem.View
{
    public delegate void CloseEventHandler();
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {

        private AdminWindow _adminWindow;
        public LoginWindow()
        {
            InitializeComponent();
        }

        public event CloseEventHandler closeEvent;

        public void _init()
        {
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
        public void ClearPassword()
        {
            m_pbox_password.Password = "";
        }
        //private bool isAdminUser = false;
        //public bool IsAdminUser
        //{
        //    set 
        //    {
        //        isAdminUser = value;
        //    }
        //}
        //public Visibility UserTypeVisible
        //{
        //    set
        //    {
        //        UserType.Visibility = value;
        //    }
        //}
        private void login_Click(object sender, RoutedEventArgs e)
        {
            string password = m_pbox_password.Password;
            if (string.IsNullOrEmpty(password))
            {
                VoiceManager.Speak("请输入密码！");
                return;
            }
            if (closeEvent != null)
            {
                closeEvent();
            }

            //MySqlManager mySqlManager = MySqlManager.CreateSingleton();
            //if (!mySqlManager.IsInitilize)
            //{
            //    mySqlManager.Initilize();
            //}
            //if (isAdminUser)
            //{
            if (string.Equals(m_pbox_password.Password, ConfigManager.CreateSingleton().ConfigInfo.AdminAccount.Password))
            {
                if (_adminWindow == null)
                {
                    _adminWindow = new AdminWindow();
                }
                _adminWindow.Show();
                _adminWindow.WindowState = WindowState.Maximized;
                if (_adminWindow.IsVisible == true)
                {
                    _adminWindow.Activate();
                }
            }
            else
            {
                VoiceManager.Speak("密码错误！");
                return;
            }
            //}
            //else
            //{
            //    if (string.Equals(m_txt_user.Text, ConfigManager.CreateSingleton().ConfigInfo.StandardAccount.Name) && string.Equals(m_txt_user.Text, ConfigManager.CreateSingleton().ConfigInfo.StandardAccount.Password))
            //    {
            //        KaoqinMainWindow mainWindow = new KaoqinMainWindow();
            //        mainWindow.Show();
            //    }
            //    else
            //    {
            //        Log.LogInstance.Write("账号密码错误！");
            //        return;
            //    }
            //}
			
            //Close();
            this.Hide();
        }

        private void tips_close_MouseEnter(object sender, MouseEventArgs e)
        {
            Label lb_1 = (Label)sender;
            try
            {
                ImageBrush ib1 = new ImageBrush(new BitmapImage(new Uri(BaseUriHelper.GetBaseUri(this), "/SwipCardSystem;component/Images/cancel_1.png")));

                ib1.Stretch = Stretch.Fill;

                lb_1.Background = ib1;
            }
            catch (Exception ef)
            {
                Log.LogInstance.Write("出现错误！tips_close_MouseEnter -->" + ef.Message, MessageType.Error);
                //MessageBox.Show("出现错误！tips_close_MouseEnter -->" + ef.Message);
            }

        }

        private void tips_close_MouseLeave(object sender, MouseEventArgs e)
        {
            Label lb_1 = (Label)sender;
            try
            {
                ImageBrush ib1 = new ImageBrush(new BitmapImage(new Uri(BaseUriHelper.GetBaseUri(this), "/SwipCardSystem;component/Images/cancel.png")));

                ib1.Stretch = Stretch.Fill;

                lb_1.Background = ib1;
            }
            catch (Exception ef)
            {
                Log.LogInstance.Write("出现错误！tips_close_MouseLeave -->" + ef.Message, MessageType.Error);
                //MessageBox.Show("出现错误！tips_close_MouseLeave -->" + ef.Message);
            }
        }

        private void btn_close_Click(object sender, RoutedEventArgs e)
        {
            //this.Close();
            Hide();
        }

        private void btn_login_MouseEnter(object sender, MouseEventArgs e)
        {
            //Button btn_login = (Button)sender;
            //Label lb1 = (Label)btn_login.Template.FindName("tips_for_login", btn_login);
            //lb1.Foreground = new SolidColorBrush(Colors.GreenYellow);

        }

        private void btn_login_MouseLeave(object sender, MouseEventArgs e)
        {
            //Button btn_login = (Button)sender;
            //Label lb1 = (Label)btn_login.Template.FindName("tips_for_login", btn_login);
            //lb1.Foreground = new SolidColorBrush(Colors.White);

        }

        //private void m_radio1_Checked(object sender, RoutedEventArgs e)
        //{
        //    isAdminUser = false;
        //}

        //private void m_radio2_Checked(object sender, RoutedEventArgs e)
        //{
        //    isAdminUser = true;
        //}
    }
}
