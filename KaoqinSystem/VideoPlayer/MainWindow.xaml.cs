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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Collections.ObjectModel;
using System.Collections;
using System.Windows.Forms;
using System.Windows.Media.Effects;
using System.Xml;
using System.Windows.Threading;
using MusicPlayer.Common;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Drawing;
//using MusicPlayer.Properties;


namespace MusicPlayer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.Uri strCurrentMusic = null;//当前正在播放的歌曲
        private ArrayList arrListmusicFullPaths = null;//文件全路径
        private ArrayList arrListmusicFilesNames = null;//文件名
        private XmlDocument xmlDocMusicList = new XmlDocument();//音乐列表配置文件
        private string musicFileListConfig = "../Config/MusicFilesConfigList.xml";
        public MusicPlayerBase musicPlayBase = new MusicPlayerBase();

        public MainWindow()
        {
            InitializeComponent();
        }

        #region 加载
        private void MyMusicPlay_Loaded(object sender, RoutedEventArgs e)
        {
            Transparent(this.Width, this.Height);
            ReadMusicFiles();
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
        private void ReadMusicFiles()
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
            catch (Exception ex) { System.Windows.Forms.MessageBox.Show(ex.Message); }
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
                    lblMusicCurrent.Content = "正在播放：" + (arrListmusicFilesNames[listMusicList.SelectedIndex].ToString().Length > 10 ? (arrListmusicFilesNames[listMusicList.SelectedIndex].ToString().Substring(0, 10) + "...") : arrListmusicFilesNames[listMusicList.SelectedIndex].ToString());
                    LaunchTimer();
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
            catch (Exception ex) { System.Windows.Forms.MessageBox.Show(ex.Message); }
        }

        void MediaPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (listMusicList.SelectedIndex < listMusicList.Items.Count)
            {
                listMusicList.SelectedIndex = listMusicList.SelectedIndex + 1;
                playMedia(arrListmusicFullPaths[listMusicList.SelectedIndex].ToString());
            }
            else
            {
                MediaPlayer.Stop();
            }
        }


        /// <summary>
        /// 列表双击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listMusicList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

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
            playOrPause();
        }

        private void playOrPause()
        {
            if (btnPlay.Content.ToString() == "播放")
            {
                MediaPlayer.Play();
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
            MyMusicPlay.Left = 0.0;
            MyMusicPlay.Top = 0.0;
            MyMusicPlay.Width = System.Windows.SystemParameters.PrimaryScreenWidth;
            MyMusicPlay.Height = System.Windows.SystemParameters.PrimaryScreenHeight;

        }

        private void mediaElement_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            playOrPause();
        }


        /*****************************************获取歌曲内部信息*begin****************************************************/

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


        /*****************************************获取歌曲内部信息*end****************************************************/
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

    }
}

