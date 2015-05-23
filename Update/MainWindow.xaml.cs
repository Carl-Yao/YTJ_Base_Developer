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
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using System.Xml;
using System.Diagnostics;

namespace Update
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //sleep->upzip->copy to local->delete update folder->update version.xml->launch keeper
            string strPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName.Replace(System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName, "");
            
            Task.Factory.StartNew(() =>
                {
                    if (Directory.Exists(strPath))
                    {
                    try
                    {
                        Thread.Sleep(5000);
                        string updatePath = strPath + "Update";
                        DirectoryInfo directoryInfo = new DirectoryInfo(updatePath);
                        FileInfo[] files = directoryInfo.GetFiles();
                        string version = string.Empty;
                        foreach (FileInfo file in files)
                        {
                            if (file.Name.IndexOf("zip") > -1)
                            {
                                string error = null;
                                UnZipFile(file.DirectoryName + @"\" + file.Name, null, out error);
                                version = file.Name.ToLower().Replace("update", "").Replace(".zip", "");
                            }
                            else
                            {
                                continue;
                            }
                            DirectoryInfo directoryInfoUpdate = new DirectoryInfo(file.DirectoryName + @"\" + file.Name.Replace(".zip", ""));
                            FileInfo[] fileinfos = directoryInfoUpdate.GetFiles();
                            
                            foreach (FileInfo item in fileinfos)
                            {
                                try
                                {
                                    File.Copy(item.FullName, strPath + item.Name, true);
                                }
                                catch
                                {
                                }
                            }
                            string versionFilePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName.Replace(System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName, "") + "Version.xml";

                            XmlDocument xmlDocument = new XmlDocument();
                            xmlDocument.Load(versionFilePath);
                            if (xmlDocument == null)
                            {
                                break;
                            }
                            XmlElement xmlElement = xmlDocument.DocumentElement;
                            if (xmlElement == null)
                            {
                                break;
                            }

                            xmlElement.SelectSingleNode("Version").InnerText=version;
                            xmlDocument.Save(versionFilePath);

                            DeleteFolder(updatePath);
                        }
                        
                    }
                    catch (Exception ex)
                    {
                        //Log.LogInstance.Write(ex.Message, MessageType.Error);
                        //MessageBox.Show(ex.Message);
                    }
                }
                    Process process = new Process();

                    process.StartInfo.FileName = strPath + "\\SCKeeper.exe";

                    process.Start();
                    Dispatcher.BeginInvoke(new Action(() =>
                        {
                            Close();
                        }));
                });

        }
        private void DeleteFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                return;
            }
            DirectoryInfo folderInfo = new DirectoryInfo(folderPath);
            FileInfo[] fileinfos1 = folderInfo.GetFiles();

            foreach (FileInfo item in fileinfos1)
            {
                File.Delete(item.FullName);

            }
            DirectoryInfo[] directoryInfo = folderInfo.GetDirectories();
            foreach (DirectoryInfo item in directoryInfo)
            {
                DeleteFolder(item.FullName);
            }
            Directory.Delete(folderInfo.FullName);
        }
       #region 解压  
   /// <summary>  
   /// 功能：解压zip格式的文件。  
   /// </summary>  
   /// <param name="zipFilePath">压缩文件路径</param>  
   /// <param name="unZipDir">解压文件存放路径,为空时默认与压缩文件同一级目录下，跟压缩文件同名的文件夹</param>  
   /// <param name="err">出错信息</param>  
   /// <returns>解压是否成功</returns>  
   public static bool UnZipFile(string zipFilePath, string unZipDir, out string err)  
   {  
       err = "";  
       if (zipFilePath == string.Empty)  
       {  
           err = "压缩文件不能为空！";  
           return false;  
       }  
       if (!File.Exists(zipFilePath))  
       {  
           err = "压缩文件不存在！";  
           return false;  
       }  
       //解压文件夹为空时默认与压缩文件同一级目录下，跟压缩文件同名的文件夹  
       if (string.IsNullOrEmpty(unZipDir))  
           unZipDir = zipFilePath.Replace(Path.GetFileName(zipFilePath), Path.GetFileNameWithoutExtension(zipFilePath));  
        if (!unZipDir.EndsWith("//"))  
            unZipDir += "//";  
        if (!Directory.Exists(unZipDir))  
            Directory.CreateDirectory(unZipDir);  
  
        try  
        {  
            using (ZipInputStream s = new ZipInputStream(File.OpenRead(zipFilePath)))  
            {  
  
                ZipEntry theEntry;  
                while ((theEntry = s.GetNextEntry()) != null)  
                {  
                    string directoryName = Path.GetDirectoryName(theEntry.Name);  
                    string fileName = Path.GetFileName(theEntry.Name);  
                    if (directoryName.Length > 0)  
                    {  
                        Directory.CreateDirectory(unZipDir + directoryName);  
                    }  
                    if (!directoryName.EndsWith("//"))  
                        directoryName += "//";  
                    if (fileName != String.Empty)  
                    {  
                        using (FileStream streamWriter = File.Create(unZipDir + theEntry.Name))  
                        {  
  
                            int size = 2048;  
                            byte[] data = new byte[2048];  
                            while (true)  
                            {  
                                size = s.Read(data, 0, data.Length);  
                                if (size > 0)  
                                {  
                                    streamWriter.Write(data, 0, size);  
                                }  
                                else  
                                {  
                                    break;  
                                }  
                            }  
                        }  
                    }  
                }//while  
            }  
        }  
        catch (Exception ex)  
        {  
            err = ex.Message;  
            return false;  
        }  
        return true;  
    }//解压结束  
    #endregion  

    }
}
