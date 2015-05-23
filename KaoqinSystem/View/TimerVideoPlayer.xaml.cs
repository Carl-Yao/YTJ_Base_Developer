using ConsoleApplication1;
using MusicPlayer.Common;
using SwipCardSystem.Controller;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;
using TimerTask;

namespace SwipCardSystem.View
{
    /// <summary>
    /// GuangBoSetting.xaml 的交互逻辑   ------  定时播放的托管函数传播放文件地址，两遍，随机顺序,多选，保存计划,所有音响
    /// </summary>
    public partial class TimerVideoPlayer : Window
    {
        private ObservableCollection<PlanItem> _planList;
        private ObservableCollection<Thread> _planThreadList;
        private SoundConfigManager _soundConfigManage;
        private PlanConfigManager _planConfigManager;
        private Thread _stopMusicThread;
        private int _cycleIndex = 0;
        private bool _isEndByTime = false;
        private List<string> _planMusicList = new List<string>();
        private int _planMusicPlayingIndex = 0;
        private static TimerVideoPlayer _instance;  
        private static readonly object ObjLok = new object();

        public static TimerVideoPlayer CreateSingleton()  
        {  
           lock (ObjLok)  
           {
               return _instance ?? (_instance = new TimerVideoPlayer());  
           }  
        }

        private TimerVideoPlayer()
        {
            InitializeComponent();

            if (DSAManager.CreateSingleton().IsDisable)
            {
                FourStep.Visibility = Visibility.Collapsed;
                FiveStepLabel.Content = "第四步：确认：";
            }

            SomeDayValue.DisplayDate = DateTime.Now.Date;
            
            //所有音响
            _soundConfigManage = SoundConfigManager.CreateSingleton();
            VideoValue.Items.Add("所有");
            foreach (string item in _soundConfigManage.arrListSoundNames)
            {
                VideoValue.Items.Add(item);
            }
            VideoValue.SelectedIndex = 0;

            //所有计划
            _planConfigManager = PlanConfigManager.CreateSingleton();
            _planList = _planConfigManager._planList;
            PlanList.DataContext = _planList;
            
            //所有计划线程
            _planThreadList = new ObservableCollection<Thread>();
            foreach (PlanItem item in _planList)
            {
                Thread planThread = CreatePlanTimerThread(item);
                _planThreadList.Add(planThread);
            }

            ReadMusicFiles();
        }       

       
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private System.Uri strCurrentMusic = null;//当前正在播放的歌曲
        private ArrayList arrListmusicFullPaths = null;//文件全路径
        private ArrayList arrListmusicFilesNames = null;//文件名
        private XmlDocument xmlDocMusicList = new XmlDocument();//音乐列表配置文件
        private string musicFileListConfig = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName.Replace(System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName, "") + "MusicFilesConfigList.xml";
        public MusicPlayerBase musicPlayBase = new MusicPlayerBase();


        #region 加载
        private void MyMusicPlay_Loaded(object sender, RoutedEventArgs e)
        {
            Transparent(this.Width, this.Height);
        }

        void Transparent(double Width, double Height)
        {
            try
            {
                IntPtr mainWindowPtr = new WindowInteropHelper(this).Handle;
                HwndSource mainWindowSrc = HwndSource.FromHwnd(mainWindowPtr);
                mainWindowSrc.CompositionTarget.BackgroundColor = System.Windows.Media.Color.FromArgb(0, 0, 0, 0);
                System.Drawing.Graphics desktop = System.Drawing.Graphics.FromHwnd(mainWindowPtr);
                float DesktopDpiX = desktop.DpiX;
                float DesktopDpiY = desktop.DpiY;
                NonClientRegionAPI.MARGINS margins = new NonClientRegionAPI.MARGINS();
                margins.cxLeftWidth = Convert.ToInt32(Width * (DesktopDpiX / 96));
                margins.cxRightWidth = Convert.ToInt32(Width * (DesktopDpiX / 96));
                margins.cyTopHeight = Convert.ToInt32(Height * (DesktopDpiX / 96));
                margins.cyBottomHeight = Convert.ToInt32(Height * (DesktopDpiX / 96));
                int hr = NonClientRegionAPI.DwmExtendFrameIntoClientArea(mainWindowSrc.Handle, ref margins);
            }
            catch (DllNotFoundException)
            {
                System.Windows.Application.Current.MainWindow.Background = System.Windows.Media.Brushes.White;
            }
        }


        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Transparent(e.NewSize.Width, e.NewSize.Height);
        }



        #endregion 加载

        #region 读取音乐列表文件
        public void ReadMusicFiles()
        {
            try
            {
                arrListmusicFilesNames = new ArrayList();
                arrListmusicFullPaths = new ArrayList();
                xmlDocMusicList.Load(musicFileListConfig);
                XmlNodeList xmlNodeMusiclist = xmlDocMusicList.SelectNodes("/Files/song");
                foreach (XmlNode oNode in xmlNodeMusiclist)
                {
                    arrListmusicFullPaths.Add(oNode.Attributes["filePath"].Value);
                    arrListmusicFilesNames.Add(oNode.Attributes["filePath"].Value.Split('\\')[oNode.Attributes["filePath"].Value.Split('\\').Length - 1].Split('.')[0]);
                }
                listMusicList.ItemsSource = arrListmusicFilesNames;
                listMusicList.Items.Refresh();
            }
            catch (Exception ex) {
                Log.LogInstance.Write(ex.Message, MessageType.Error);
                //System.Windows.Forms.MessageBox.Show(ex.Message);
            }
        }
        #endregion 读取音乐列表文件


        /// <summary>
        /// 播放音乐
        /// </summary>
        /// <param name="mediaPath"></param>
        private void playMedia(string currentMusicPath)
        {
            try
            {
                if (File.Exists(currentMusicPath))
                {
                    Uri uri = new Uri(currentMusicPath);
                    strCurrentMusic = uri;
                    MediaPlayer.Source = uri;
                    MediaPlayer.Play();
                    btnPlay.Content = "暂停";
                    lblMusicCurrent.Content = "正在播放：" + currentMusicPath.Split('\\')[currentMusicPath.Split('\\').Length - 1].Split('.')[0];//(arrListmusicFilesNames[listMusicList.SelectedIndex].ToString().Length > 10 ? (arrListmusicFilesNames[listMusicList.SelectedIndex].ToString().Substring(0, 10) + "...") : arrListmusicFilesNames[listMusicList.SelectedIndex].ToString());
                    LaunchTimer();
                    MediaPlayer.MediaEnded -= MediaPlayer_MediaEnded;
                    MediaPlayer.MediaEnded += MediaPlayer_MediaEnded;

                    Mp3Info mp3info = new Mp3Info();
                    mp3info = this.getMp3Info(this.getLast128(currentMusicPath));
                    string authorImgPath = "../Resource/Artist/DefaultAuthor.png"; ;
                    if (!string.IsNullOrEmpty(mp3info.Artist))
                    {
                        authorImgPath = "../Resource/Artist/" + mp3info.Artist + ".png";
                        if (!File.Exists(authorImgPath))
                        {
                            authorImgPath = "../Resource/Artist/DefaultAuthor.png";
                        }
                    }
                    imgMusicAuthor.Source = new BitmapImage(new Uri(authorImgPath, UriKind.RelativeOrAbsolute));
                }
                else
                {
                    DialogResult mbr = System.Windows.Forms.MessageBox.Show("文件不存在！是否删除？", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button3);
                    if (mbr.ToString() == "OK")
                    {
                        arrListmusicFullPaths.RemoveAt(listMusicList.SelectedIndex);
                        RefreshXmlMusicList(arrListmusicFullPaths);
                    }
                }
            }
            catch (Exception ex) {
                System.Windows.Forms.MessageBox.Show(ex.Message); }
        }

        void MediaPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            MediaPlayer.MediaEnded -= MediaPlayer_MediaEnded;
            if (_isEndByTime)
            {

            }
            //定时器触发的音乐播放
            else if (_cycleIndex > 0)
            {
                if (_planMusicPlayingIndex < _planMusicList.Count-1)
                {
                    _planMusicPlayingIndex = _planMusicPlayingIndex + 1;
                    playMedia(_planMusicList[_planMusicPlayingIndex].ToString());
                }
                else
                {
                    _cycleIndex -= 1;
                    if (_cycleIndex > 0)
                    {
                        _planMusicPlayingIndex = 0;
                        playMedia(_planMusicList[_planMusicPlayingIndex].ToString());
                    }
                    else
                    {
                        _planMusicPlayingIndex = 0;
                        DSAManager.CreateSingleton().YinXiangClose();
                        MediaPlayer.Stop();
                    }
                }
            }
            else
            {
                if (listMusicList.SelectedIndex < listMusicList.Items.Count - 1)
                {
                    listMusicList.SelectedIndex = listMusicList.SelectedIndex + 1;
                    playMedia(arrListmusicFullPaths[listMusicList.SelectedIndex].ToString());
                }
                else
                {
                    MediaPlayer.Stop();
                }
            }
        }


        /// <summary>
        /// 列表双击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listMusicList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (listMusicList.SelectedIndex > 0)
            playMedia(arrListmusicFullPaths[listMusicList.SelectedIndex].ToString());
        }

        #region 添加歌曲目录
        private void AddMusicFolders_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FolderBrowserDialog fbd = new FolderBrowserDialog();
                DialogResult ret = fbd.ShowDialog();
                string strMusicFolderPath = fbd.SelectedPath + "\\";
                if (strMusicFolderPath != "")
                {
                    string[] AllFileFullNames = Directory.GetFiles(strMusicFolderPath, "*.*", SearchOption.AllDirectories);
                    for (int i = 0; i < AllFileFullNames.Length; i++)
                    {
                        if (!arrListmusicFullPaths.Contains(AllFileFullNames[i]))
                        {
                            FileInfo musicFile = new FileInfo(AllFileFullNames[i]);
                            if ((musicFile.Extension == ".mp3" || musicFile.Extension == ".wma" || musicFile.Extension == ".avi" || musicFile.Extension == ".mp4" || musicFile.Extension == ".rmvb") && musicFile.Length / 1024 / 1024 > 0.1)
                            {
                                XmlElement node = xmlDocMusicList.CreateElement("song");
                                node.SetAttribute("filePath", AllFileFullNames[i]);
                                xmlDocMusicList.DocumentElement.AppendChild(node);
                            }
                        }
                    }
                    xmlDocMusicList.Save(musicFileListConfig);
                    ReadMusicFiles();
                }
                fbd.Dispose();
            }
            catch (Exception ex) { System.Windows.Forms.MessageBox.Show(ex.Message); }
        }
        #endregion 添加歌曲目录

        #region 删除列表文件
        private void DelMusic_Click(object sender, RoutedEventArgs e)
        {
            if (listMusicList.SelectedIndex >= 0)
            {
                DialogResult dialogResult = System.Windows.Forms.MessageBox.Show("亲，确定要删除吗？", "提示", MessageBoxButtons.YesNo);
                if (dialogResult.ToString() == "Yes")
                {
                    if (MediaPlayer.Source != null)
                    {
                        if (arrListmusicFullPaths[listMusicList.SelectedIndex].ToString() == MediaPlayer.Source.LocalPath)
                        {
                            MediaPlayer.Stop();
                        }
                    }
                    arrListmusicFullPaths.RemoveAt(listMusicList.SelectedIndex);
                    RefreshXmlMusicList(arrListmusicFullPaths);
                }
            }
        }
        #endregion 删除列表文件

        #region 清空列表
        private void ClearMusic_Click(object sender, RoutedEventArgs e)
        {

            DialogResult dialogReturn = System.Windows.Forms.MessageBox.Show("亲，真的要清空列表吗？", "提示", MessageBoxButtons.YesNo);
            if (dialogReturn.ToString() == "Yes")
            {
                arrListmusicFullPaths.Clear();
                RefreshXmlMusicList(arrListmusicFullPaths);
            }
            MediaPlayer.Stop();
        }
        #endregion 清空列表

        #region 刷新当前列表
        private void RefreshXmlMusicList(ArrayList arr)
        {
            XmlNodeList xnl = xmlDocMusicList.SelectNodes("/Files");
            foreach (XmlNode xn in xnl) { xn.RemoveAll(); }
            for (int i = 0; i < arr.Count; i++)
            {
                if (!string.IsNullOrEmpty(arr[i].ToString()))
                {
                    XmlElement node = xmlDocMusicList.CreateElement("song");
                    node.SetAttribute("filePath", arr[i].ToString());
                    xmlDocMusicList.DocumentElement.AppendChild(node);
                }
            }
            xmlDocMusicList.Save(musicFileListConfig);
            ReadMusicFiles();
        }
        #endregion 刷新当前列表

        private void TimerTick(object sender, EventArgs e)
        {
            if (MediaPlayer.NaturalDuration.HasTimeSpan)
            {
                progressMusic.Maximum = MediaPlayer.NaturalDuration.TimeSpan.TotalMinutes;
                progressMusic.Value = MediaPlayer.Position.TotalMinutes;// MediaPlayer.Clock.CurrentProgress.Value;
                lblProgressValue.Content = MediaPlayer.Position.TotalMinutes.ToString("0.00") + "/" + MediaPlayer.NaturalDuration.TimeSpan.TotalMinutes.ToString("0.00");
            }
        }

        //启动一个Timer,它的做用就是每一秒更新一下时间
        private void LaunchTimer()
        {
            // 判断是否在设计模式，是则返回，注意：如果不加这句话，正在设计界面的时候可能会报错哦！
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 1);   //间隔1秒
            timer.Tick += new EventHandler(TimerTick);
            timer.Start();
        }

        private void MyMusicPlay_Closed(object sender, EventArgs e)
        {

        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (listMusicList.SelectedIndex == -1 && listMusicList.Items.Count > 0)
            {
                listMusicList.SelectedIndex = 0;
            }
            playOrPause();
        }

        private void playOrPause()
        {
            if (btnPlay.Content.ToString() == "播放")
            {
                if (MediaPlayer.HasAudio)
                {
                    MediaPlayer.Play();
                }
                else
                {
                    
                    playMedia(arrListmusicFullPaths[listMusicList.SelectedIndex].ToString());
                }
                btnPlay.Content = "暂停";
                btnPlay.ToolTip = "单击暂停";                
                lblMusicCurrent.Content = "正在播放：" + (arrListmusicFilesNames[listMusicList.SelectedIndex].ToString().Length > 10 ? (arrListmusicFilesNames[listMusicList.SelectedIndex].ToString().Substring(0, 10) + "...") : arrListmusicFilesNames[listMusicList.SelectedIndex].ToString());
            }
            else
            {
                MediaPlayer.Pause();
                btnPlay.Content = "播放";
                btnPlay.ToolTip = "单击播放";
            }
        }


        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (listMusicList.SelectedIndex < listMusicList.Items.Count)
            {
                listMusicList.SelectedIndex = listMusicList.SelectedIndex + 1;
                playMedia(arrListmusicFullPaths[listMusicList.SelectedIndex].ToString());
            }
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            if (listMusicList.SelectedIndex > 0)
            {
                listMusicList.SelectedIndex = listMusicList.SelectedIndex - 1;
                playMedia(arrListmusicFullPaths[listMusicList.SelectedIndex].ToString());
            }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            MediaPlayer.Stop();
            btnPlay.Content = "播放";
            MediaPlayer.ToolTip = "单击播放";
            lblMusicCurrent.Content = "暂无播放歌曲";
        }

        private void ProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //progressVol.Minimum = 0;
            //progressVol.Maximum = 100;
            //lblVolValue.Content = progressVol.Value.ToString();
        }

        private void progressVol_PreviewDragEnter(object sender, System.Windows.DragEventArgs e)
        {
            //progressVol.Value = progressVol.get;
        }

        private void contCtrlMP_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //MyMusicPlay.Left = 0.0;
            //MyMusicPlay.Top = 0.0;
            //MyMusicPlay.Width = System.Windows.SystemParameters.PrimaryScreenWidth;
            //MyMusicPlay.Height = System.Windows.SystemParameters.PrimaryScreenHeight;

        }

        private void mediaElement_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            playOrPause();
        }


        #region /*****************************************获取歌曲内部信息*begin****************************************************/

        public struct Mp3Info
        {

            public string identify;//TAG，三个字节

            public string Title;//歌曲名,30个字节

            public string Artist;//歌手名,30个字节

            public string Album;//所属唱片,30个字节

            public string Year;//年,4个字符

            public string Comment;//注释,28个字节



            public char reserved1;//保留位，一个字节

            public char reserved2;//保留位，一个字节

            public char reserved3;//保留位，一个字节

        }



        //所以，我们只要把MP3文件的最后128个字节分段读出来并保存到该结构里就可以了。函数定义如下：

        /// <summary>

        /// 获取MP3文件最后128个字节

        /// </summary>

        /// <param name="FileName">文件名</param>

        /// <returns>返回字节数组</returns>

        public byte[] getLast128(string FileName)
        {

            FileStream fs = new FileStream(FileName, FileMode.Open, FileAccess.Read);

            Stream stream = fs;



            stream.Seek(-128, SeekOrigin.End);



            const int seekPos = 128;

            int rl = 0;

            byte[] Info = new byte[seekPos];

            rl = stream.Read(Info, 0, seekPos);



            fs.Close();

            stream.Close();



            return Info;

        }



        //再对上面返回的字节数组分段取出，并保存到Mp3Info结构中返回。

        /// <summary>

        /// 获取MP3歌曲的相关信息

        /// </summary>

        /// <param name = "Info">从MP3文件中截取的二进制信息</param>

        /// <returns>返回一个Mp3Info结构</returns>

        public Mp3Info getMp3Info(byte[] Info)
        {

            Mp3Info mp3Info = new Mp3Info();



            string str = null;

            int i;

            int position = 0;//循环的起始值

            int currentIndex = 0;//Info的当前索引值

            //获取TAG标识

            for (i = currentIndex; i < currentIndex + 3; i++)
            {

                str = str + (char)Info[i];



                position++;

            }

            currentIndex = position;

            mp3Info.identify = str;



            //获取歌名

            str = null;

            byte[] bytTitle = new byte[30];//将歌名部分读到一个单独的数组中

            int j = 0;

            for (i = currentIndex; i < currentIndex + 30; i++)
            {

                bytTitle[j] = Info[i];

                position++;

                j++;

            }

            currentIndex = position;

            mp3Info.Title = this.byteToString(bytTitle);



            //获取歌手名

            str = null;

            j = 0;

            byte[] bytArtist = new byte[30];//将歌手名部分读到一个单独的数组中

            for (i = currentIndex; i < currentIndex + 30; i++)
            {

                bytArtist[j] = Info[i];

                position++;

                j++;

            }

            currentIndex = position;

            mp3Info.Artist = this.byteToString(bytArtist);



            //获取唱片名

            str = null;

            j = 0;

            byte[] bytAlbum = new byte[30];//将唱片名部分读到一个单独的数组中

            for (i = currentIndex; i < currentIndex + 30; i++)
            {

                bytAlbum[j] = Info[i];

                position++;

                j++;

            }

            currentIndex = position;

            mp3Info.Album = this.byteToString(bytAlbum);



            //获取年

            str = null;

            j = 0;

            byte[] bytYear = new byte[4];//将年部分读到一个单独的数组中

            for (i = currentIndex; i < currentIndex + 4; i++)
            {

                bytYear[j] = Info[i];

                position++;

                j++;

            }

            currentIndex = position;

            mp3Info.Year = this.byteToString(bytYear);



            //获取注释

            str = null;

            j = 0;

            byte[] bytComment = new byte[28];//将注释部分读到一个单独的数组中

            for (i = currentIndex; i < currentIndex + 25; i++)
            {

                bytComment[j] = Info[i];

                position++;

                j++;

            }

            currentIndex = position;

            mp3Info.Comment = this.byteToString(bytComment);



            //以下获取保留位

            mp3Info.reserved1 = (char)Info[++position];

            mp3Info.reserved2 = (char)Info[++position];

            mp3Info.reserved3 = (char)Info[++position];



            return mp3Info;

        }

        //上面程序用到下面的方法：

        /// <summary>

        /// 将字节数组转换成字符串

        /// </summary>

        /// <param name = "b">字节数组</param>

        /// <returns>返回转换后的字符串</returns>

        public string byteToString(byte[] b)
        {

            Encoding enc = Encoding.GetEncoding("GB2312");

            string str = enc.GetString(b);

            str = str.Substring(0, str.IndexOf('\0') >= 0 ? str.IndexOf('\0') : str.Length);//去掉无用字符



            return str;

        }


        #endregion/*****************************************获取歌曲内部信息*end****************************************************/
        class NonClientRegionAPI
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct MARGINS
            {
                public int cxLeftWidth;
                public int cxRightWidth;
                public int cyTopHeight;
                public int cyBottomHeight;
            };
            [DllImport("DwmApi.dll")]
            public static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS pMarInset);
        }

        private void btnQuickLast_Click(object sender, RoutedEventArgs e)
        {
            MediaPlayer.Position = MediaPlayer.Position - TimeSpan.FromSeconds(10);
            if (MediaPlayer.Position.TotalMinutes <= 0)
            {
                if (listMusicList.SelectedIndex > 0)
                {
                    listMusicList.SelectedIndex = listMusicList.SelectedIndex + 1;
                    listMusicList.SelectedIndex = listMusicList.SelectedIndex - 1;
                    playMedia(arrListmusicFullPaths[listMusicList.SelectedIndex].ToString());
                }
            }
        }

        private void btnQuickNext_Click(object sender, RoutedEventArgs e)
        {
            MediaPlayer.Position = MediaPlayer.Position + TimeSpan.FromSeconds(10);
            if (MediaPlayer.Position.TotalMinutes >= MediaPlayer.NaturalDuration.TimeSpan.TotalMinutes)
            {
                listMusicList.SelectedIndex = listMusicList.SelectedIndex + 1;

                if (listMusicList.SelectedIndex < listMusicList.Items.Count)
                {
                    listMusicList.SelectedIndex = listMusicList.SelectedIndex + 1;
                    playMedia(arrListmusicFullPaths[listMusicList.SelectedIndex].ToString());
                }
            }
        }
        /// <summary>
        /// 把前几步的设置保存到planitem中
        /// </summary>
        /// <param name="planItem"></param>
        /// <returns></returns>
        private bool GetPlanDataValue(out PlanItem planItem)
        {
            if (listMusicList.SelectedIndex > -1)
            {
                string fileCount = "0";
                string date = "每天:";
                string beginTime = "10:0:0";
                string endTime = "10:1:0";
                string order = "顺序播放";
                string sound = "所有";
                string filePaths = string.Empty;
                fileCount = listMusicList.SelectedItems.Count.ToString();
                if (MeiYue.IsChecked == true)
                {
                    date = "每月:" + ((TextBlock)(((ItemsControl)MeiYueValue.SelectedItem).Items[0])).Text;
                }
                else if (MeiZhou.IsChecked == true)
                {
                    date = "每周:" + ((TextBlock)(((ItemsControl)MeiZhouValue.SelectedItem).Items[0])).Text;
                }
                else if (MeiTian.IsChecked == true)
                {
                    date = "每天";
                }
                else
                {
                    date = SomeDayValue.DisplayDate.ToString();
                }
                beginTime = ((TextBlock)(((ItemsControl)BeginHour.SelectedItem).Items[0])).Text + ":" + ((TextBlock)(((ItemsControl)BeginMinute.SelectedItem).Items[0])).Text + ":" + ((TextBlock)(((ItemsControl)BeginSecond.SelectedItem).Items[0])).Text;
                if (SetAroundCountBool.IsChecked == true)
                {
                    endTime = ((TextBlock)(((ItemsControl)AroundCountValue.SelectedItem).Items[0])).Text;
                }
                else
                {
                    endTime = ((TextBlock)(((ItemsControl)EndHour.SelectedItem).Items[0])).Text + ":" + ((TextBlock)(((ItemsControl)EndMinute.SelectedItem).Items[0])).Text + ":" + ((TextBlock)(((ItemsControl)EndSecond.SelectedItem).Items[0])).Text;
                }
                if (OrderBool.IsChecked == true)
                {
                    order = "顺序播放";
                }
                else
                {
                    order = "随机播放";
                }
                sound = VideoValue.Text;
                //to do 
                
                foreach (string item in listMusicList.SelectedItems)
                {
                    foreach ( string path in arrListmusicFullPaths)
                        if (path.IndexOf(item) > -1)
                            filePaths += path + "%&%";
                }
                planItem = new PlanItem() { FileCount = fileCount, Date = date, BeginTime = beginTime, EndTime = endTime, Order = order, Sound = sound, MusicPaths = filePaths };
                return true;
            }
            else
            {
                System.Windows.MessageBox.Show("请在音乐文件列表中选择一条要定时播放的音乐！");
                planItem = null;
                return false;
            }
        }
        private Thread CreatePlanTimerThread(PlanItem item)
        {
            TimerTask.TimerInfo timerInfo = new TimerTask.TimerInfo();
            if ("每天".Equals(item.Date))
            {
                timerInfo.TimerType = "EveryDay";
            }
            else if (item.Date.IndexOf("每月") > -1)
            {
                timerInfo.TimerType = "DayOfMonth";
                timerInfo.DateValue = int.Parse(item.Date.Split(':')[1]);
            }
            else if (item.Date.IndexOf("每周") > -1)
            {
                timerInfo.TimerType = "DayOfWeek";
                timerInfo.DateValue = int.Parse(item.Date.Split(':')[1]);
            }
            else
            {
                timerInfo.TimerType = "LoopDays";
                timerInfo.DateValue = (SomeDayValue.DisplayDate - DateTime.UtcNow.Date).Days;
            }
            string[] str = item.BeginTime.Split(':');
            timerInfo.Hour = int.Parse(str[0]);
            timerInfo.Minute = int.Parse(str[1]);
            timerInfo.Second = int.Parse(str[2]);
            //第二种调用方法
            ParmTimerTaskDelegate ptrd = new ParmTimerTaskDelegate(PlayMusicTimerTask);
            object[] p = new object[] { item.FileCount, item.EndTime, item.Order, item.Sound, item.MusicPaths };
            //创建定时任务线程
            Thread ThreadTimerTaskService = TimerTaskService.CreateTimerTaskService(timerInfo, ptrd, p);
            ThreadTimerTaskService.Start();
            return ThreadTimerTaskService;
        }

        private void AddPlanDataClick(object sender, RoutedEventArgs e)
        {
            PlanItem item;
            if (GetPlanDataValue(out item))
            {
                _planList.Add(item);
                _planConfigManager.RefreshXmlList(_planList);
                _planThreadList.Add(CreatePlanTimerThread(item));
            }
            //ObservableObj.Add(new PlanItem() { FileCount = fileCount, Date = date, BeginTime = beginTime, EndTime = endTime, Order = order, Sound = sound });
        }

        private void ChangePlanDataClick(object sender, RoutedEventArgs e)
        {
            if (PlanList.SelectedIndex > -1)
            {
                PlanItem item;
                if (GetPlanDataValue(out item))
                {
                    _planList[PlanList.SelectedIndex] = item;
                    _planConfigManager.RefreshXmlList(_planList);
                    CreatePlanTimerThread(item);
                }
               // new PlanItem() { FileCount = fileCount, Date = date, BeginTime = beginTime, EndTime = endTime, Order = order, Sound = sound };
            }
            else
            {
                System.Windows.MessageBox.Show("请在定时播放音乐计划列表中选择一条要更新的计划！");
            }
        }

        /// <summary>
        /// 列表中删除计划
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (PlanList.SelectedIndex > -1)
            {
                _planList.RemoveAt(PlanList.SelectedIndex);
                _planConfigManager.RefreshXmlList(_planList);
            }
        }

        /// <summary>
        /// 清空
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            _planList.Clear();
            _planConfigManager.RefreshXmlList(_planList);
        }

        /// <summary>
        /// 顺序播放按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_4(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// 随机播放按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_5(object sender, RoutedEventArgs e)
        {

        }

        private void PlayMusicTimerTask(object[] parm)//string FileCount, string EndTime,string Order,string Sound,string MusicPaths)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (parm != null && parm.Length > 0)
                {
                    _planMusicList.Clear();
                    string[] stringSeparators = new string[] { "%&%" };

                    _planMusicList = ((string)parm[4]).Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries).ToList<string>();

                    VoiceManager.SetDefaultDevice(0);
                    if ("所有".Equals((string)parm[3]))
                    {
                        DSAManager.CreateSingleton().SetHZ(86.5);
                    }
                    else
                    {
                        for (int i = 0; i < _soundConfigManage.arrListSoundNames.Count; i++)
                        {
                            if (((string)parm[3]).Equals(_soundConfigManage.arrListSoundNames[i]))
                            {
                                DSAManager.CreateSingleton().SetHZ(Double.Parse(_soundConfigManage.arrListSoundFrequency[i].ToString()));
                                break;
                            }
                        }
                    }
                    if ("顺序播放".Equals((string)parm[2]))
                    {
                        if (((string)parm[1]).IndexOf(':') > -1)
                        {
                            //定时结束
                            foreach (string item in _planMusicList)
                            {
                                playMedia(_planMusicList[0]);
                            }
                            if (_stopMusicThread != null && _stopMusicThread.IsAlive)
                            {
                                _stopMusicThread.Abort();
                            }
                            //第二种调用方法
                            ParmTimerTaskDelegate ptrd = new ParmTimerTaskDelegate(StopMusicTimerTask);
                            object[] myparam =  new object[] {""};
                             TimerTask.TimerInfo timerInfo = new TimerTask.TimerInfo();
                             string[] str = ((string)parm[1]).Split(':');
                             timerInfo.Hour = int.Parse(str[0]);
                             timerInfo.Minute = int.Parse(str[1]);
                             timerInfo.Second = int.Parse(str[2]);                           
                             timerInfo.TimerType = "DesDate";
                             timerInfo.Year = DateTime.Now.Year;
                             timerInfo.Month = DateTime.Now.Month;
                             timerInfo.Day = DateTime.Now.Day;
                             _isEndByTime = true;
                            _stopMusicThread = TimerTaskService.CreateTimerTaskService(timerInfo, ptrd, myparam);
                            _stopMusicThread.Start();
                        }
                        else
                        {
                            //几遍
                            _cycleIndex = int.Parse((string)parm[1]);
                            //while (i > 0)
                            {
                                foreach (string item in _planMusicList)
                                {
                                    playMedia(_planMusicList[0]);
                                }                                
                            }
                        }
                    }
                    else
                    {
                        //随机
                        if (((string)parm[1]).IndexOf(':') > -1)
                        {
                            //定时结束
                            foreach (string item in _planMusicList)
                            {
                                playMedia(_planMusicList[0]);
                            }  
                            if (_stopMusicThread.IsAlive)
                            {
                                _stopMusicThread.Abort();
                            }
                            //第二种调用方法
                            ParmTimerTaskDelegate ptrd = new ParmTimerTaskDelegate(StopMusicTimerTask);
                            object[] myparam = new object[] { "" };
                            TimerTask.TimerInfo timerInfo = new TimerTask.TimerInfo();
                            string[] str = ((string)parm[1]).Split(':');
                            timerInfo.Hour = int.Parse(str[0]);
                            timerInfo.Minute = int.Parse(str[1]);
                            timerInfo.Second = int.Parse(str[2]);
                            timerInfo.TimerType = "DesDate";
                            timerInfo.Year = DateTime.Now.Year;
                            timerInfo.Month = DateTime.Now.Month;
                            timerInfo.Day = DateTime.Now.Day;
                            _stopMusicThread = TimerTaskService.CreateTimerTaskService(timerInfo, ptrd, myparam);
                            _stopMusicThread.Start();
                        }
                        else
                        {
                            //几遍
                            _cycleIndex = int.Parse((string)parm[1]);
                            //while (i > 0)
                            {
                                foreach (string item in _planMusicList)
                                {
                                    playMedia(_planMusicList[0]);
                                }  
                            }
                        }
                    }
                }
            })      
        ,null);
        }


        private void StopMusicTimerTask(object[] parm)
        {
            _cycleIndex = 0;
            DSAManager.CreateSingleton().YinXiangClose();
            this.Dispatcher.BeginInvoke(new Action(() => { MediaPlayer.Stop(); }));
            _isEndByTime = false;
        }

        /// <summary>
        /// 返回
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }
    }
    public class  PlanItem
    {
        public string FileCount{get;set;}
        public string Date{get;set;}
        public string BeginTime{get;set;}
        public string EndTime{get;set;}
        public string Order{get;set;}
        public string Sound{get;set;}
        public string MusicPaths { get; set; }
    }
}
