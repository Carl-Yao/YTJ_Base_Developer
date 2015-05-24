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
using SwipCardSystem.Controller;
using System.Diagnostics;
using SwipCardSystem.Medol;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SwipCardSystem.View
{
    /// <summary>
    /// AdminMainPage.xaml 的交互逻辑
    /// </summary>
    public partial class AdminMainPage : Page
    {
        ConfigManager _configManager = null;
        MySqlManager _mySqlManager = null;
        WebServiceManager _webServiceManager = null;
        TimerVideoPlayer _timerVideoPlayer = null;
        public AdminMainPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshConfigInfo();
            _mySqlManager = MySqlManager.CreateSingleton();
            _webServiceManager = WebServiceManager.CreateSingleton();
        }

        private void Close()
        {
            ((NavigationWindow)(this.Parent)).Close();
        }
        private void Hide()
        {
            ((NavigationWindow)(this.Parent)).Hide();
        }
        private bool RefreshConfigInfo()
        {
            bool bRes = false;
            try
            {
                if (_configManager == null)
                {
                    _configManager = ConfigManager.CreateSingleton();
                }
                //textBox1.Text = _configManager.ConfigInfo.BeginTime.Hour.ToString();
                //textBox2.Text = _configManager.ConfigInfo.BeginTime.Minute.ToString();
                //textBox1_Copy.Text = _configManager.ConfigInfo.EndTime.Hour.ToString();
                //textBox2_Copy.Text = _configManager.ConfigInfo.EndTime.Minute.ToString();
                //textBox1_Copy1.Text = _configManager.ConfigInfo.ClearRecordFrequencyByDay.ToString();
                textBox1_Copy2.Text = _configManager.ConfigInfo.InstitutionID;
                textBox1_Copy.Text = _configManager.ConfigInfo.ServiceUrl;
                //textBox1_Copy3.Text = _configManager.ConfigInfo.ServiceUrl;
                //textBox1_Copy4.Text = _configManager.ConfigInfo.AdminAccount.Name;
                //textBox1_Copy5.Text = _configManager.ConfigInfo.AdminAccount.Password;
                //textBox1_Copy6.Text = _configManager.ConfigInfo.StandardAccount.Name;
                //textBox1_Copy7.Text = _configManager.ConfigInfo.StandardAccount.Password;
                bRes = true;
            }
            catch
            {

            }
            return bRes;
        }

        private bool SaveConfigInfo()
        {
            bool bRes = false;
            try
            {
                if (_configManager == null)
                {
                    _configManager = ConfigManager.CreateSingleton();
                }
                //_configManager.ConfigInfo.BeginTime.Hour = Int32.Parse(textBox1.Text);
                //_configManager.ConfigInfo.BeginTime.Minute=  Int32.Parse(textBox2.Text);
                //_configManager.ConfigInfo.EndTime.Hour = Int32.Parse(textBox1_Copy.Text);
                //_configManager.ConfigInfo.EndTime.Minute = Int32.Parse(textBox2_Copy.Text);
               
                //_configManager.ConfigInfo.ClearRecordFrequencyByDay = Int32.Parse(textBox1_Copy1.Text);
                _configManager.ConfigInfo.InstitutionID = textBox1_Copy2.Text;
                _configManager.ConfigInfo.ServiceUrl = textBox1_Copy.Text;
                //_configManager.ConfigInfo.AdminAccount.Name = textBox1_Copy4.Text;
                //_configManager.ConfigInfo.AdminAccount.Password = textBox1_Copy5.Text;
                //_configManager.ConfigInfo.StandardAccount.Name = textBox1_Copy6.Text;
                //_configManager.ConfigInfo.StandardAccount.Password = textBox1_Copy7.Text;
                _configManager.SetConfigInfo();
                bRes = true;
            }
            catch
            {

            }
            return bRes;
        }

        private bool UploadRecordData()
        {
            bool bRet = false;
            try
            {
                //上传
                List<KaoqinInfo> kaoqinInfos = new List<KaoqinInfo>();
                _mySqlManager.GetAllKaoqinRecord(ref kaoqinInfos);
                if (_webServiceManager.UploadDataAllInTime(kaoqinInfos))//_webServiceManager.UploadDataAll(kaoqinInfos) && _webServiceManager.UploadPicAll(kaoqinInfos))
                {
                    //VoiceManager.Speak("上传数据成功！");
                    bRet = true;
                }
                else
                {
                    //VoiceManager.Speak("上传数据失败！");
                    bRet = false;
                }
            }
            catch
            {
                bRet = false;
            }
            return bRet;
        }

        private void button5_Click_2(object sender, RoutedEventArgs e)
        {
            SaveConfigInfo();
            MessageBox.Show("保存成功！");
        }

        private void button5_Click_1(object sender, RoutedEventArgs e)
        {
            SaveConfigInfo();
            _webServiceManager.SetServiceClient();
            MessageBox.Show("保存成功！");
        }

        //private void button5_Click(object sender, RoutedEventArgs e)
        //{
        //    MaintainPage page = new MaintainPage();
        //    page.MaintainWorkType = "关闭软件";            
        //    NavigationService.Navigate(page);
        //    Close();
        //}
        private void button2_Click(object sender, RoutedEventArgs e)
        {
            //LoginWindow loginWindow = new LoginWindow();
            //loginWindow.IsAdminUser = false;
            //loginWindow.UserTypeVisible = Visibility.Collapsed;
            //loginWindow.closeEvent += new CloseEventHandler(this.Close);
            //loginWindow.ShowDialog();
            //KaoqinMainWindow mainWindow = new KaoqinMainWindow();
            //mainWindow.Show();
            Hide();
        }

        private void button4_Click_1(object sender, RoutedEventArgs e)
        {
            Process myProcess = new Process();
            myProcess.StartInfo.FileName = "cmd.exe";
            myProcess.StartInfo.UseShellExecute = false;
            myProcess.StartInfo.RedirectStandardInput = true;
            myProcess.StartInfo.RedirectStandardOutput = true;
            myProcess.StartInfo.RedirectStandardError = true;
            myProcess.StartInfo.CreateNoWindow = true; 
            myProcess.Start();
            myProcess.StandardInput.WriteLine("shutdown -s -t 0");
            //Close();
        }

        private void button3_Click_1(object sender, RoutedEventArgs e)
        {
            //真关闭了
            Process[] ps = Process.GetProcessesByName("SCKeeper");
            foreach (Process pro in ps)
            {
                pro.Kill();
            }
            TimerVideoPlayer.CreateSingleton().Close();
            foreach (Window item in Application.Current.Windows)
            {
                item.Close();
            }

            Application.Current.Shutdown();
            Environment.Exit(0);// 

            //Close();
        }

        private void button17_Click(object sender, RoutedEventArgs e)
        {
            if (!NetworkManager.CreateSingleton().IsInternetConnecting)
            {
                VoiceManager.Speak("请先连接网络！");
                return;
            }

            MaintainPage maintainPage = new MaintainPage();
            maintainPage.MaintainWorkType = "正在进行上传考勤数据操作...";
            maintainPage.Show();
            Task.Factory.StartNew(() =>
            {

                if (UploadRecordData())
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        VoiceManager.Speak("上传考勤数据成功！");
                        maintainPage.MaintainWorkType = "上传考勤数据成功！";
                        maintainPage.SetComputerState();
                    }));
                    //Log.LogInstance.Write("下载数据成功！", MessageType.Success);

                }
                else
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        VoiceManager.Speak("上传考勤数据失败！");
                        //Log.LogInstance.Write("下载数据失败！", MessageType.Error);
                        maintainPage.MaintainWorkType = "上传考勤数据失败！";
                        maintainPage.SetComputerState();
                    }));
                }
            });            
        }        

        private void button16_Click(object sender, RoutedEventArgs e)
        {
            if (!NetworkManager.CreateSingleton().IsInternetConnecting)
            {
                MessageBox.Show("请先连接网络！");
                return;
            }

            _configManager.ConfigInfo.IsFirstUpdate = true;
            _configManager.SetConfigInfo();
            MaintainPage maintainPage = new MaintainPage();
            maintainPage.MaintainWorkType = "正在进行同步数据操作...";
            maintainPage.Show();
            Task.Factory.StartNew(() =>
                {
                    
                    if (_webServiceManager.DownloadData())
                    {
                        Dispatcher.BeginInvoke(new Action(()=>
                            {
                                VoiceManager.Speak("同步数据成功！");
                                maintainPage.MaintainWorkType = "同步数据成功！";
                                maintainPage.SetComputerState();
                            }));
                        //Log.LogInstance.Write("下载数据成功！", MessageType.Success);
                        
                    }
                    else
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                           {
                               VoiceManager.Speak("同步数据失败！");
                               //Log.LogInstance.Write("下载数据失败！", MessageType.Error);
                               maintainPage.MaintainWorkType = "同步数据失败！";
                               maintainPage.SetComputerState();
                           }));
                    }
                });
            
        }

        private void button16_Copy_Click(object sender, RoutedEventArgs e)
        {
            if (_timerVideoPlayer == null)
            {
                _timerVideoPlayer = TimerVideoPlayer.CreateSingleton();
                //gb.ShowDialog();
                _timerVideoPlayer.Show();
            }
            else
            {
                _timerVideoPlayer.Show();
                _timerVideoPlayer.WindowState = WindowState.Maximized;
                if (_timerVideoPlayer.IsVisible == true)
                {
                    _timerVideoPlayer.Activate();
                }
            }
        }

        private void button17_Copy_Click(object sender, RoutedEventArgs e)
        {
            GuangBoSystem gb = new GuangBoSystem();
            gb.ShowDialog();
        }

        private void minButton_Click(object sender, RoutedEventArgs e)
        {
            //this.w = WindowState.Minimized;
            foreach (Window item in Application.Current.Windows)
            {
                item.WindowState = WindowState.Minimized;
            }
        }
    }
}
