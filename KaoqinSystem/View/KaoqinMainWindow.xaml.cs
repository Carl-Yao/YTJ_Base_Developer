using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.IO;
using System.Drawing.Imaging;
using AForge.Video.DirectShow;
using System.Speech.Synthesis;
using SwipCardSystem.Controller;
using SwipCardSystem.Medol;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Threading;
using M1Card.Common;
using System.Timers;
using System.Windows.Media;
using System.Threading.Tasks;
using DotNetSpeech;
using System.Windows.Media.Animation;
using ConsoleApplication1;

namespace SwipCardSystem.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class KaoqinMainWindow : Window
    {
        private NetworkManager _networkManager = null;

        SpVoice _voice = new SpVoice();

        private LoginWindow _loginWindow = null;

        private MySqlManager _mySqlManager = null;

        private WebServiceManager _webserviceManager = null;//new WebServiceManager();

        private TemperatureManager _temperatureManager = null;

        private ConfigManager _configManager = null;

        private DSAManager _dSAManager = null;

        private SoundConfigManager _soundConfigMageger = null;

        private SpeechSynthesizer speecher = new SpeechSynthesizer();
        
        private System.Timers.Timer _AutoHideTimer = null;

        private MaintainPage _main = null;
        //private bool _isInternetConnect = false;

        private List<int> _icdevs = new List<int>();

        private Task _delayUploadTask = null;
        private CancellationTokenSource _ct = null;//new CancellationTokenSource();
        private Object _object = new Object();

        KaoqinInfo _kaoqinInfo = null;

        private bool _isUploaded = true;

        private bool _isTemperatureEnable = true;
        // private System.Timers.Timer _timer = null;
        //用于判断前后两次是否为一张卡，设为1可能不安全
        private ulong _serialNum = 1;
        private int _iSameCount = 0;
        public KaoqinMainWindow()
        {
            InitializeComponent();
            _AutoHideTimer = new System.Timers.Timer();
            _AutoHideTimer.Interval = 50;
            _AutoHideTimer.Elapsed += new ElapsedEventHandler(AutoHideTimer_Tick);
            _AutoHideTimer.Start();

            _configManager = ConfigManager.CreateSingleton();
        }

        #region 内存回收
        [DllImport("kernel32.dll", EntryPoint = "SetProcessWorkingSetSize")]
        public static extern int SetProcessWorkingSetSize(IntPtr process, int minSize, int maxSize);
        /// <summary>
        /// 释放内存
        /// </summary>
        public void ClearMemory()//Handler(object sender, EventArgs e)
        {
            if (null != _kaoqinInfo && null != _kaoqinInfo.PicBitMap)
            {
                _kaoqinInfo.PicBitMap.Dispose();
                _kaoqinInfo.PicBitMap = null;
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                SetProcessWorkingSetSize(System.Diagnostics.Process.GetCurrentProcess().Handle, -1, -1);
            }
        }
        #endregion

        private void KaoqinUI_Loaded(object sender, RoutedEventArgs e)
        {
            //VoiceManager.textToFile("，，，，，，，，，，，，" + "小一班" + "的," + "小哈林" + "小朋友，你的家人来接你啦！"
//+ "小一班" + "的," + "小哈林" + "小朋友，你的家人来接你啦！");
            _main = new MaintainPage();
            _main.MaintainWorkType = "正在进行初始化操作...";
            _main.Show();
            Task.Factory.StartNew(() =>
                {
                    _configManager = ConfigManager.CreateSingleton();
                    _networkManager = NetworkManager.CreateSingleton();
                    _mySqlManager = MySqlManager.CreateSingleton();
                    Log.LogInstance.Write("ggg_mySqlManager" + DateTime.Now.ToShortTimeString(), MessageType.Error);
                    _webserviceManager = WebServiceManager.CreateSingleton();
                    Log.LogInstance.Write("ggg_webserviceManager" + DateTime.Now.ToShortTimeString(), MessageType.Error);
                    //网络连接上后马上上传本地考勤记录
                    _networkManager.OnMyValueChanged += new NetworkManager.MyValueChanged(UploadRecordDataHandler);
                    if (!_networkManager.IsInternetConnecting)
                    {
                        VoiceManager.Speak("未连网，请先联网！");
                        Log.LogInstance.Write("未连网，请先联网！", MessageType.Warning);
                        _mySqlManager.CreateDataTable(false);
                        //Close();
                    }
                    else
                    {
                        if (_webserviceManager.UpdateApplication())
                        {
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                            Process[] ps = Process.GetProcessesByName("SCKeeper");
                            foreach (Process pro in ps)
                            {
                                pro.Kill();
                            }
                            Process process = new Process();

                            process.StartInfo.FileName = System.Windows.Forms.Application.StartupPath + "\\Update.exe";

                            process.Start();
                            Close();
                            System.Windows.Application.Current.Shutdown();
                            }));
                        }

                        //启动程序后先查看是否要上传本地考勤记录
                        UploadRecordData();

                        _webserviceManager.DownloadData();
                    }
                    System.Windows.Threading.DispatcherTimer timer1 = new System.Windows.Threading.DispatcherTimer();
                    //每十分钟查看是否要上传本地考勤记录
                    timer1.Tick += new EventHandler(UploadRecordDataHandler);
                    
                    //先同步数据，这功能应该放在显示这个界面之前
                    //WebServiceManager webservice = new WebServiceManager();

                    //没10分钟更新一次公告                
                    timer1.Tick += new EventHandler(GongGaoUpdateHandler);
                    timer1.Interval = new TimeSpan(0, 10, 0);
                    timer1.IsEnabled = true;

                    System.Windows.Threading.DispatcherTimer timer2 = new System.Windows.Threading.DispatcherTimer();
                    timer2.Tick += new EventHandler(DownloadDataHandler);
                    timer2.Interval = new TimeSpan(1, 0, 0);
                    timer2.IsEnabled = true;

                    


                    _soundConfigMageger = SoundConfigManager.CreateSingleton();

                    List<string> list = new List<string>();
                    if (MySqlManager.CreateSingleton().GetAllClass(ref list))
                    {
                        if (list.Count != _soundConfigMageger.arrListSoundNames.Count || list.Where(p => !_soundConfigMageger.arrListSoundNames.Contains(p)).ToList().Count() > 0)
                        {
                            foreach (string item in list)
                            {
                                _soundConfigMageger.arrListSoundNames.Add(item);
                                _soundConfigMageger.arrListSoundFrequency.Add("0");
                            }
                            _soundConfigMageger.RefreshXmlList(_soundConfigMageger.arrListSoundNames, _soundConfigMageger.arrListSoundFrequency);
                        }
                    }
                    Dispatcher.BeginInvoke(new Action(() =>
                        {
                            if (!string.IsNullOrEmpty(_configManager.ConfigInfo.Notice))
                            {
                                string[] strs = _configManager.ConfigInfo.Notice.Split('@');
                                NoticeTitel.Text = strs[0];
                                NoticeContent.Text = strs[1];
                                NoticeTime.Text = strs[2].Substring(0, 10);
                            }

                            if (!string.IsNullOrEmpty(_configManager.ConfigInfo.ZiXun))
                            {
                                zixun.Text = _configManager.ConfigInfo.ZiXun;
                            }

                            if (File.Exists(System.Environment.CurrentDirectory +@"/logo.png"))
                            {
                                try
                                {
                                    LogoImage.Source = new BitmapImage(new Uri(System.Environment.CurrentDirectory + @"/logo.png", UriKind.RelativeOrAbsolute));
                                }
                                catch
                                {
                                    //File.Delete(System.Environment.CurrentDirectory + @"/logo.png");
                                }
                                
                            }
                            this.UpdateLayout();

                            zongrenshu.Text = _configManager.ConfigInfo.StudentSumNumber;
                            yidaorenshu.Text = MySqlManager.CreateSingleton().SumStudent(1);
                            KaoqinTitle.FontFamily = new System.Windows.Media.FontFamily("方正琥珀简体");
                            KaoqinTitle.Text = _configManager.ConfigInfo.InstitutionName + "欢迎您！";

                            if (zixun.ActualHeight > ScrollZiXun.ActualHeight)
                            {
                                ThicknessAnimation da = new ThicknessAnimation();
                                da.From = new Thickness(5, 0, 5, 0);    //起始值
                                da.To = new Thickness(5, 40 - zixun.ActualHeight, 5, 0);      //结束值
                                //只需改这个值
                                da.SpeedRatio = 4;
                                da.Duration = TimeSpan.FromSeconds(zixun.ActualHeight / da.SpeedRatio);
                                da.RepeatBehavior = RepeatBehavior.Forever;//动画持续时间
                                zixun.BeginAnimation(TextBlock.MarginProperty, da);//开始动画
                            }

                            if (NoticeContent.ActualHeight + NoticeTime.ActualHeight + NoticeTitel.ActualHeight + 20 > NoticeScroll.ActualHeight)
                            {
                                ThicknessAnimation da1 = new ThicknessAnimation();
                                da1.From = new Thickness(5, 0, 5, 0);    //起始值
                                da1.To = new Thickness(5, 20 - NoticeContent.ActualHeight, 5, 0);      //结束值
                                //只需改这个值
                                da1.SpeedRatio = 4;
                                da1.Duration = TimeSpan.FromSeconds(NoticeContent.ActualHeight / da1.SpeedRatio);
                                da1.RepeatBehavior = RepeatBehavior.Forever;//动画持续时间
                                Notice.BeginAnimation(TextBlock.MarginProperty, da1);//开始动画
                            }

                            Log.LogInstance.Write("ggg_InitDevices" + DateTime.Now.ToShortTimeString(), MessageType.Error);
                            InitDevices();
                            Log.LogInstance.Write("ggg_InitDevices" + DateTime.Now.ToShortTimeString(), MessageType.Error);
                            //to do 初始化完全后再显示主页面
                            _main.Close();
                            //创建plan计划
                            TimerVideoPlayer player = TimerVideoPlayer.CreateSingleton();
                            player.Show();
                            player.Hide();
                        }));
                });
        }

        //private const int INTERNET_CONNECTION_MODEM = 1;
        //private const int INTERNET_CONNECTION_LAN = 2;
        //[DllImport("winInet.dll")]
        //private static extern bool InternetGetConnectedState(
        //ref   int dwFlag,
        //int dwReserved
        //);        
        #region 隐藏Admin按钮
        void AutoHideTimer_Tick(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                POINT p;
                if (!GetCursorPos(out p))
                {
                    return;
                }

                if (p.x <= SystemInformation.WorkingArea.Width && p.x > (SystemInformation.WorkingArea.Width - 60)
                    && p.y >= 0 && p.y <= (this.Top + 60))
                {
                    AdminButton.Visibility = Visibility.Visible;
                }
                else
                {
                    AdminButton.Visibility = Visibility.Hidden;
                }
            }));            
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
            public POINT(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetCursorPos(out POINT pt);

        private ImageCodecInfo GetCodecInfo(string mimeType)
        {
            ImageCodecInfo[] CodecInfo = ImageCodecInfo.GetImageEncoders();

            foreach (ImageCodecInfo ici in CodecInfo)
            {
                if (ici.MimeType == mimeType)
                    return ici;
            }
            return null;
        }

        #endregion

        private void button_Capture_Click(object sender, RoutedEventArgs e)
        {
            UpdateKaoqinRecord();
        }

        private void UploadRecordDataHandler(object sender, EventArgs e)
        {
            if (_networkManager.IsInternetConnecting)
            {
                //_webserviceManager.SetServiceClient();
                UploadRecordData();
            }            
        }

        private void DownloadDataHandler(object sender, EventArgs e)
        {
            if (_networkManager.IsInternetConnecting)
            {
                //每天12点查看是否要大更新，或每日清空到校人数
                if (DateTime.Now.Hour == 23)
                {
                    _webserviceManager.DownloadData();
                }
            } 
        }

        private void UploadRecordData()
        {
            Task.Factory.StartNew(() =>
            {
                bool bRet = false;
                try
                {
                    //上传
                    List<KaoqinInfo> kaoqinInfos = new List<KaoqinInfo>();
                    _mySqlManager.GetAllKaoqinRecord(ref kaoqinInfos);
                    if (_webserviceManager.UploadDataAllInTime(kaoqinInfos))//(_webserviceManager.UploadDataAll(kaoqinInfos) && _webserviceManager.UploadPicAll(kaoqinInfos))
                    {
                        //VoiceManager.Speak("上传数据成功！");
                        bRet = true;
                    }
                    else
                    {
                        //VoiceManager.Speak("上传数据失败！");
                        bRet = false;
                    }
                    if (bRet)
                    {
                        //补充删除，防止文件遗留过多
                        //清空parentpicture文件夹下的照片文件
                        DirectoryInfo directory = new DirectoryInfo(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName.Replace(System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName, "") + Constants.CAPTUREPICTURE_FOLDERNAME);
                        FileInfo[] files = directory.GetFiles();
                        foreach (FileInfo file in files)
                        {
                            File.Delete(file.DirectoryName + @"\" + file.Name);
                        }

                        _mySqlManager.ClearDataTable(true);
                        //--可能创建数据库时与其他线程冲突
                        _mySqlManager.CreateDataTable(true);
                    }
                }
                catch
                {
                    bRet = false;
                }
                if (bRet)
                {
                    Log.LogInstance.Write("上传考勤记录成功！", MessageType.Success);
                }
                else
                {
                    Log.LogInstance.Write("上传考勤记录失败！", MessageType.Error);
                }

            });
        }

        public void InitDevices()
        {
            try
            {
                //成功返回正数，错误返回负值
                for (int i = 0; i < 5; i++)
                {
                    int iRes = URF.rf_init(i, 115200);
                    if (iRes > 0)
                    {
                        //for (int j = 0; j < i; j++)
                        //{
                        //URF.rf_exit(iRes);
                          //  int icdev = URF.rf_init(iRes, 115200);
                        _icdevs.Add(iRes);
                        //}
                    }
                    else
                    {
                        //iRes = URF.rf_exit(iRes);
                        foreach(int item in _icdevs)
                        {
                            iRes = URF.rf_exit(item);
                            //_icdevs.Remove(item);
                        }
                        _icdevs.Clear();
                        for (int j = 0; j < i; j++)
                        {
                            iRes = URF.rf_init(j, 115200);
                            _icdevs.Add(iRes);
                        }
                        break;
                    }
                }
                    
                if (_icdevs.Count < 1)
                {
                    Log.LogInstance.Write("初始化IC卡读写器失败，请检查连接是否正确。返回码：", MessageType.Error);
                    //VoiceManager.Speak("初始化IC卡读写器失败，请检查连接是否正确。");
                    System.Windows.MessageBox.Show("初始化IC卡读写器失败，请检查连接是否正确。");
                    //this.Close();
                }
                else
                {
                    DirectoryInfo directory = new DirectoryInfo(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName.Replace(System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName, "") + Constants.CAPTUREPICTURE_FOLDERNAME);
                    if (!Directory.Exists(directory.Name))
                    {
                        Directory.CreateDirectory(directory.Name);
                    }
                    //System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
                    //5秒后开始运行，接着每隔1秒的调用Tick方法
                    //System.Threading.Timer tmr = new System.Threading.Timer(TimeEventForICDevice,"", 0, 10);
                    System.Timers.Timer tmr1 = new System.Timers.Timer();
                    tmr1.Interval = 200;
                    tmr1.Elapsed += new ElapsedEventHandler(TimeEventForICDevice);
                    tmr1.Start();
                }
                //Byte IDLE = 0;
                //unsafe
                //{
                //    short tagtype;
                //    iRes = URF.rf_request(_icdev, IDLE, &tagtype);
                //}
                
                //初始化音频设备
                _voice.Voice = _voice.GetVoices(string.Empty, string.Empty).Item(0);
                
                //初始化分布式广播
                _dSAManager = DSAManager.CreateSingleton();

                //初始化红外线体温计端口
                _temperatureManager = new TemperatureManager();
                if (!_temperatureManager.Initilize(UpdateTemerature))
                {
                    Log.LogInstance.Write("初始化温度计设备失败！",MessageType.Error);
                    //VoiceManager.Speak("初始化温度计设备失败！");
                    //System.Windows.MessageBox.Show("初始化温度计设备失败！");
                    _isTemperatureEnable = false;
                    //this.Close();
                }

                // 设定初始视频设备
                FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

                if (videoDevices.Count > 0)
                {   // 默认设备
                    sourcePlayer.VideoSource = new VideoCaptureDevice(videoDevices[videoDevices.Count - 1].MonikerString);
                    sourcePlayer.Start();
                }
                else
                {
                    Log.LogInstance.Write("初始化视频设备失败！", MessageType.Error);
                    //VoiceManager.Speak("初始化视频设备失败！");
                    System.Windows.MessageBox.Show("初始化视频设备失败！");
                }               

                //设置ui显示逻辑
                KaoqinUI.Visibility = Visibility.Visible;
                Progress.Visibility = Visibility.Collapsed;
            }
            catch (Exception e)
            {
                Log.LogInstance.Write("初始化设备失败：" + e.Message, MessageType.Error);
                //VoiceManager.Speak("初始化设备失败：" + e.Message);
                System.Windows.MessageBox.Show("初始化设备失败：" + e.Message);                
            }

            
            
        }
        static int i = 0;
        private object _lock = new object();
        private void TimeEventForICDevice(object sender, ElapsedEventArgs e)
        {
            try
            {
                lock (_lock)
                {
                    //byte[] version = new byte[18];
                    //8:53，错误返回 -1//成功返回27394048或1或30670848
                    //iRes = URF.rf_get_status(_icdev, version);
                    int iRes = 0;
                    foreach (int item in _icdevs)
                    {
                        ulong serialNum = 0;
                        //3931429079,3931340327,3131650823----返回85655552重复,233504768,78249984第一次，242024448初始化时/halt后11/无卡返回1,或者0/错误返回负值-1
                        iRes = URF.rf_card(item, 0, ref serialNum);
                        //i++;
                        //if(i%10 == 0)
                        //{
                        //    serialNum = 3321793859;
                        //}else if (i%10 == 5)
                        //{
                        //    serialNum = 3321807907;
                        //}
                        //    else
                        //    {
                        //        serialNum =0;
                        //    }
                        if (serialNum > 0)
                        {
                            Console.WriteLine("1");

                            if (_serialNum == serialNum)
                            {
                                _iSameCount++;
                                if (_iSameCount > 3)
                                {
                                    URF.rf_beep(item, 20);
                                    URF.rf_beep(item, 20);
                                    //iRes = URF.rf_halt(version);
                                    //成功87818240 /错误返回65535--成功0
                                    URF.rf_halt(item);
                                    if (_iSameCount == 4)
                                    {
                                        VoiceManager.Speak("刷卡过于频繁！");
                                    }
                                    continue;
                                }
                            }
                            else
                            {
                                _iSameCount = 0;
                                _serialNum = serialNum;
                            }
                            //错误返回65535//成功87818240--陈功0
                            Int16 i16Res = 0;
                            i16Res = URF.rf_beep(item, 20);
                            //iRes = URF.rf_halt(version);
                            //成功87818240 /错误返回65535--成功0
                            i16Res = URF.rf_halt(item);
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                //搜索数据库
                                UpdateKaoqinRecord();
                            }));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogInstance.Write("TimeEventForICDevice函数异常：" + ex.Message, MessageType.Error);
                //VoiceManager.Speak("TimeEventForICDevice函数异常：" + ex.Message);
                //System.Windows.MessageBox.Show("TimeEventForICDevice函数异常：" + ex.Message);                
            }
        }

        /// <summary>
        /// 更新公告和资讯
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void GongGaoUpdateHandler(object source, EventArgs e)
        {
            try
            {
                if (NetworkManager.CreateSingleton().IsInternetConnecting && _webserviceManager.DownloadNotice() && _webserviceManager.DownloadZiXun())
                {
                    _configManager.SetConfigInfo();
                    if (!string.IsNullOrEmpty(_configManager.ConfigInfo.Notice))
                    {
                        string[] strs = _configManager.ConfigInfo.Notice.Split('@');
                        NoticeTitel.Text = strs[0];
                        NoticeContent.Text = strs[1];
                        NoticeTime.Text = strs[2].Substring(0, 10);
                    }
                    if (!string.IsNullOrEmpty(_configManager.ConfigInfo.ZiXun))
                    {
                        zixun.Text = _configManager.ConfigInfo.ZiXun;
                    }
                }
            }
            catch
            {
                Log.LogInstance.Write("TimeEventForGongGaoUpdate", MessageType.Error);
            }
        }

        private void UpdateKaoqinRecord()
        {
            try
            {
                ClearMemory();
                if (!_isUploaded)
                {
                    if (_delayUploadTask != null && _delayUploadTask.Status == TaskStatus.Running && _ct != null)
                    {
                        _ct.Cancel();
                    }
                    _isUploaded = true;
                    UploadOrSaveRecord();
                }
                DateTime now = DateTime.Now;
                string recordId = now.ToString("yyyyMMddhhmmssfff");
                string studentName = string.Empty;
                string studentNO = string.Empty;
                string studentId = string.Empty;
                string studentGroup = string.Empty;
                string cardId = string.Empty;
                string[] parentNames = new string[4] { string.Empty, string.Empty,string.Empty, string.Empty };
                string[] parentRelationships = new string[4] { string.Empty, string.Empty, string.Empty, string.Empty };
                string[] picturePaths = new string[4] { string.Empty, string.Empty, string.Empty, string.Empty };
                string isGoSchoolStudentNum = string.Empty;
                string classId = string.Empty;
                string userId = string.Empty;
                //test
                //_serialNum = 1058842273;
                string cardNo = _serialNum.ToString();
                //Console.WriteLine(cardNo);
                //return;
                _mySqlManager = MySqlManager.CreateSingleton();
                if (!_mySqlManager.GetOtherInfoByCardNo(cardNo, ref userId, ref studentName, ref cardId, ref studentNO, ref studentId, ref  studentGroup, ref parentNames, ref parentRelationships, ref picturePaths, ref isGoSchoolStudentNum, ref classId))
                {
                    return;
                }

                if (string.IsNullOrEmpty(studentNO))
                {
                    jiaoshixinxi.Visibility = Visibility.Visible;
                    xueshengxinxi.Visibility = Visibility.Collapsed;
                    baba.Visibility = Visibility.Collapsed;
                    mama.Visibility = Visibility.Collapsed;
                    jiaoshixingming.Text = studentName;
                    jiaoshishuakashijian.Text = now.ToString("HH:mm:ss");
                    if (_mySqlManager == null)
                    {
                        //耗时,应该初始化时调用
                        _mySqlManager = MySqlManager.CreateSingleton();
                    }

                    string pictureBase64 = string.Empty;
                    System.Drawing.Bitmap picBitMap = new System.Drawing.Bitmap(400, 300);
                    // 判断视频设备是否开启
                    if (sourcePlayer.IsRunning)
                    {
                        Log.LogInstance.Write("Before", MessageType.Unknown);
                        System.Drawing.Bitmap bitmap = sourcePlayer.GetCurrentVideoFrame();
                        kaoqinjietu.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                    bitmap.GetHbitmap(),
                                    IntPtr.Zero,
                                    Int32Rect.Empty,
                                    BitmapSizeOptions.FromEmptyOptions());
                        Log.LogInstance.Write("1", MessageType.Unknown);
                        EncoderParameter p = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 30);
                        Log.LogInstance.Write("2", MessageType.Unknown);
                        EncoderParameters ps = new EncoderParameters();
                        Log.LogInstance.Write("3", MessageType.Unknown);
                        System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(picBitMap);
                        Log.LogInstance.Write("4", MessageType.Unknown);
                        g.DrawImage(bitmap, 0, 0, picBitMap.Width, picBitMap.Height);
                        bitmap.Dispose();
                        bitmap = null;
                        g.Dispose();
                        Log.LogInstance.Write("5", MessageType.Unknown);
                        ps.Param[0] = p;
                        Log.LogInstance.Write("6", MessageType.Unknown);
                        ImageToBase64.ImgToBase64String(picBitMap, ref pictureBase64);
                        Log.LogInstance.Write("After", MessageType.Unknown);
                    }
                    _kaoqinInfo = new KaoqinInfo();
                    _kaoqinInfo.EqupId = "0";
                    _kaoqinInfo.RecordId = recordId;
                    _kaoqinInfo.ICCardId = cardId;
                    _kaoqinInfo.ICCardNo = cardNo;
                    _kaoqinInfo.StudentID = studentId;
                    _kaoqinInfo.ClassId = classId;
                    _kaoqinInfo.RecordTime = now.ToString("yyyy-MM-dd HH:mm:ss");
                    _kaoqinInfo.PicturePath = string.Empty;
                    _kaoqinInfo.PictureBase64 = pictureBase64;
                    _kaoqinInfo.PicBitMap = picBitMap;
                    _isUploaded = true;
                    UploadOrSaveRecord();
                }
                else if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(studentName))
                {

                    string strName = studentName;
                    //for (int j = studentName.Length; j > -1;j-- )
                    //{
                    //    strName = strName.Insert(j, ",");
                    //}

                    //for (int i = 0; i < _soundConfigMageger.arrListSoundNames.Count; i++)
                    //{
                    //    if (studentGroup.Equals(_soundConfigMageger.arrListSoundNames[i]))
                    //    {
                    //        _dSAManager.SetHZ(Double.Parse(_soundConfigMageger.arrListSoundFrequency[i].ToString()));
                    //        break;
                    //    }
                    //}
                    //var thread = new Thread(() =>
                    //{
                    VoiceManager.Speak("，，，，，，，，，，，，" + studentGroup.Replace("（", "").Replace("）", "") + "的," + strName + "小朋友，你的家人来接你啦！"
                        + "," + studentGroup.Replace("（", "").Replace("）", "") + "的," + strName + "小朋友，你的家人来接你啦！", 0, CallBack);

                        //    this.Dispatcher.BeginInvoke(new Action(()=>DSAManager.CreateSingleton().YinXiangClose()));
                        //}) { IsBackground = true };
                    //thread.Start();

                    return;
                }
                else
                {
                    jiaoshixinxi.Visibility = Visibility.Collapsed;
                    xueshengxinxi.Visibility = Visibility.Visible;
                    baba.Visibility = Visibility.Visible;
                    mama.Visibility = Visibility.Visible;
                    //xueshengxinxi.Visibility = Visibility.Visible;
                    tiwen.Text = "";
                    xueshengxingming.Text = studentName;
                    shuakashijian.Text = now.ToString("HH:mm:ss"); ;
                    banji.Text = studentGroup;
                    xuehao.Text = studentNO;

                    yidaorenshu.Text = isGoSchoolStudentNum;

                    //baba.Visibility = Visibility.Visible;
                    baba.guanxi.Text = parentRelationships[0];
                    baba.xingming.Text = parentNames[0];
                    //父母截图
                    if (File.Exists(picturePaths[0]))
                    {
                        //BinaryReader binReader = new BinaryReader(File.Open(picturePaths[0], FileMode.Open));
                        //FileInfo fileInfo = new FileInfo(picturePaths[0]);
                        //byte[] bytes = binReader.ReadBytes((int)fileInfo.Length);
                        //binReader.Close();

                        // Init bitmap
                        BitmapImage bitmap = new BitmapImage();//new Uri(picturePaths[0]));
                        //bitmap.CacheOption = BitmapCacheOption.OnLoad; 
                        bitmap.BeginInit();
                        bitmap.StreamSource = new MemoryStream(File.ReadAllBytes(picturePaths[0]));
                        bitmap.EndInit();
                        baba.zhaopian.Source = bitmap;
                    }
                    else
                    {
                        //用默认照片
                        baba.zhaopian.Source = new BitmapImage(new Uri(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName.Replace(System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName, "") + "nopicture.jpg"));
                    }
                    //mama.Visibility = Visibility.Visible;
                    mama.guanxi.Text = parentRelationships[1];
                    mama.xingming.Text = parentNames[1];
                    if (File.Exists(picturePaths[1]))
                    {
                        //BinaryReader binReader = new BinaryReader(File.Open(picturePaths[1], FileMode.Open));
                        //FileInfo fileInfo = new FileInfo(picturePaths[1]);
                        //byte[] bytes = binReader.ReadBytes((int)fileInfo.Length);
                        //binReader.Close();

                        // Init bitmap
                        BitmapImage bitmap = new BitmapImage();//new Uri(picturePaths[1]));
                        bitmap.BeginInit();
                        bitmap.StreamSource = new MemoryStream(File.ReadAllBytes(picturePaths[1]));
                        bitmap.EndInit();
                        //bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        mama.zhaopian.Source = bitmap;

                    }
                    else
                    {
                        //用默认照片
                        mama.zhaopian.Source = new BitmapImage(new Uri(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName.Replace(System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName, "") + "nopicture.jpg"));
                    }
                    if (_mySqlManager == null)
                    {
                        //耗时,应该初始化时调用
                        _mySqlManager = MySqlManager.CreateSingleton();
                    }

                    string pictureBase64 = string.Empty;
                    System.Drawing.Bitmap picBitMap = new System.Drawing.Bitmap(400, 300);
                    // 判断视频设备是否开启
                    if (sourcePlayer.IsRunning)
                    {
                        Log.LogInstance.Write("Before", MessageType.Unknown);
                        System.Drawing.Bitmap bitmap = sourcePlayer.GetCurrentVideoFrame();
                        kaoqinjietu.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                    bitmap.GetHbitmap(),
                                    IntPtr.Zero,
                                    Int32Rect.Empty,
                                    BitmapSizeOptions.FromEmptyOptions());
                        EncoderParameter p = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 30);
                        EncoderParameters ps = new EncoderParameters();
                        System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(picBitMap);
                        g.DrawImage(bitmap, 0, 0, picBitMap.Width, picBitMap.Height);
                        bitmap.Dispose();
                        bitmap = null;
                        g.Dispose();

                        ps.Param[0] = p;
                        ImageToBase64.ImgToBase64String(picBitMap, ref pictureBase64);
                    }
                    //lock (_object)
                    //{
                    //    Task task = new Task(() =>
                    //    {
                    _kaoqinInfo = new KaoqinInfo();

                    _kaoqinInfo.EqupId = "0";
                    _kaoqinInfo.RecordId = recordId;
                    _kaoqinInfo.ICCardId = cardId;
                    _kaoqinInfo.ICCardNo = cardNo;
                    _kaoqinInfo.StudentID = studentId;
                    _kaoqinInfo.ClassId = classId;
                    _kaoqinInfo.RecordTime = now.ToString("yyyy-MM-dd HH:mm:ss");
                    _kaoqinInfo.PicturePath = string.Empty;
                    _kaoqinInfo.PictureBase64 = pictureBase64;
                    _kaoqinInfo.PicBitMap = picBitMap;
                    _isUploaded = false;
                    if (!_isTemperatureEnable)
                    {
                        _isUploaded = true;
                        UploadOrSaveRecord();
                    }
                    else
                    {
                        _ct = new CancellationTokenSource();
                        _delayUploadTask = new Task(() => WorkForUpload(_ct));
                        _delayUploadTask.Start();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogInstance.Write("更新考勤记录失败！UdateKaoqinRecord：" + ex.Message, MessageType.Error);
                //System.Windows.MessageBox.Show("更新考勤记录失败！UdateKaoqinRecord：" + ex.Message);
            }
        }

        /// <summary>
        /// 语音播放回调
        /// </summary>
        /// <param name="b"></param>
        /// <param name="InputWordPosition"></param>
        /// <param name="InputWordLength"></param>
        private void CallBack(bool b, int InputWordPosition, int InputWordLength)
        {
            if (b)
            {
                _dSAManager.YinXiangClose();
            }
        }

        private void WorkForUpload(CancellationTokenSource ct)
        {
            for (int i = 0; i < 100; i++)
            {
                if (!ct.IsCancellationRequested)
                {
                    Thread.Sleep(100);
                }
                else
                {
                    return;
                }
            }
            if (ct.IsCancellationRequested)
            {
                return;
            }
            _isUploaded = true;
            UploadOrSaveRecord();
        }
        
        private void UploadOrSaveRecord(bool isTeacher = false)
        {
            try
            {
                //或者在量完温度后执行
                //_mySqlManager.SaveKaoqinRecord(_kaoqinInfo);
                //或者保存记录后就不上传，找一时间统一上传；或者不保存记录直接上传
                //每次判断网洛会影响ui
                if (_networkManager.IsInternetConnecting)
                {
                    if (_mySqlManager.IsTeacherRecord(_kaoqinInfo.ICCardNo))
                    {
                        _webserviceManager.UploadDataOne(_kaoqinInfo, false, true);
                    }
                    else
                    {
                        _webserviceManager.UploadDataOne(_kaoqinInfo, false);
                    }
                }
                else
                {
                    _webserviceManager.SaveKaoqinRecordAndPicture(_kaoqinInfo);
                }
            }
            catch (Exception e)
            {
                Log.LogInstance.Write("UploadOrSaveRecord:" + e.Message, MessageType.Error);
            }
        }

        //先量体温再刷卡
        private void UpdateTemerature()
        {
            try
            {
                lock (_object)
                {
                    Task task = new Task(() =>
                        {                            
                            //在刷卡时new，关于刷卡测体温顺序需求还需等待确认
                            //_kaoqinInfo = new KaoqinInfo();
                            if (_kaoqinInfo != null)
                            {
                                _kaoqinInfo.TemplateVal = _temperatureManager.TemperatureValue;
                            }
                            else
                            {
                                return;
                            }
                            //this.speecher.SpeakAsync(textToSpeak);                           

                            double teperature = double.Parse(_temperatureManager.TemperatureValue);
                            string textToSpeak = string.Empty;
                            if (teperature < 20 || teperature > 50)
                            {
                                textToSpeak = "体温异常，请重新测量";
                                return;
                            }
                            else
                            {
                                textToSpeak = _temperatureManager.TemperatureValue + "度";
                            }

                            if (_delayUploadTask != null && _delayUploadTask.Status == TaskStatus.Running && _ct != null)
                            {
                                _ct.Cancel();
                            }
                            //可能测多次
                            //if (!_isUploaded)
                            //{
                            _isUploaded = true;

                            UploadOrSaveRecord();
                            //}

                            this.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                //VoiceManager.Beep();
                                VoiceManager.Speak(textToSpeak);
                                tiwen.Text = "";
                                if (teperature < 37.3)
                                {
                                    tiwen.Foreground = Brushes.Green;
                                }
                                else if (teperature < 37.9)
                                {
                                    tiwen.Foreground = Brushes.Orange;
                                }
                                else
                                {
                                    tiwen.Foreground = Brushes.Red;
                                }
                                tiwen.FontSize = 28;

                                tiwen.Text = _temperatureManager.TemperatureValue + "℃";
                            }));                                                   
                        });
                    task.Start();
                }
            }
            catch (Exception ex)
            {
                Log.LogInstance.Write("UpdateTemerature:" + ex.Message,MessageType.Error);
                //System.Windows.MessageBox.Show("UpdateTemerature:" + ex.Message);

            }
        }

        public void ReleaseDevices()
        {
            if (sourcePlayer.IsRunning)
            {   // 停止视频
                sourcePlayer.SignalToStop();
                sourcePlayer.WaitForStop();
            }
            try
            {
                int i=-1;
                foreach (int item in _icdevs)
                {
                    if (item > 0)
                    {
                        //成功0 /二次退出会异常
                        URF.rf_exit(item);
                    }
                }
            }
            catch
            {

            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (_loginWindow == null) //_loginWindow.IsVisible == false)
            {
                _loginWindow = new LoginWindow();
                //_loginWindow.IsAdminUser = true;
                //_loginWindow.UserTypeVisible = Visibility.Collapsed;
                //_loginWindow.closeEvent += new CloseEventHandler(this.Close);
                _loginWindow.ClearPassword();
                _loginWindow.Show();
            }
            else
            {
                _loginWindow.ClearPassword();
                _loginWindow.Show();
                _loginWindow.WindowState = WindowState.Maximized;
                if (_loginWindow.IsVisible == true)
                {
                    _loginWindow.Activate();
                }

            }           

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ReleaseDevices();
        }

        private void guangao_Click(object sender, RoutedEventArgs e)
        {
            KaoqinUI.Visibility = Visibility.Hidden;
            GuanggaoUI.Visibility = Visibility.Visible;
        }

        private void GuanggaoUI_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            KaoqinUI.Visibility = Visibility.Visible;
            GuanggaoUI.Visibility = Visibility.Collapsed;
        }

        private void ProgressUserControl_Loaded(object sender, RoutedEventArgs e)
        {

        }

    }
}
