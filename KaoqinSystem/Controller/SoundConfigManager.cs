using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml;

namespace SwipCardSystem.Controller
{
    public class SoundConfigManager
    {
        private static SoundConfigManager _soundConfigManager = null;
        private static object _object = new object();
        private XmlDocument xmlDocList = new XmlDocument();//音乐列表配置文件
        private string fileListConfig = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName.Replace(System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName, "") + @"SoundConfigList.xml";
        public ArrayList arrListSoundNames
        {
            set;
            get;
        }
        public ArrayList arrListSoundFrequency
        {
            set;
            get;
        }
        public static SoundConfigManager CreateSingleton()
        {
            lock (_object)
            {
                if (_soundConfigManager == null)
                {
                    _soundConfigManager = new SoundConfigManager();
                }
            }
            return _soundConfigManager;
        }
        SoundConfigManager()
        {
            Initilize();
        }

        public void Initilize()
        {
            if (!ReadFiles())
            {
                Log.LogInstance.Write("Get config info error", MessageType.Error);
                //MessageBox.Show("Get frequency config info error");
            }            
        }

        public bool ReadFiles()
        {
            bool bRes = false;
            try
            {
                arrListSoundNames = new ArrayList();
                arrListSoundFrequency = new ArrayList();

                xmlDocList.Load(fileListConfig);
                XmlNodeList xmlNodelist = xmlDocList.SelectNodes("/Sounds/sound");
                foreach (XmlNode oNode in xmlNodelist)
                {
                    arrListSoundNames.Add(oNode.Attributes["soundName"].Value);
                    arrListSoundFrequency.Add(oNode.Attributes["soundFrequency"].Value);
                }
                //listMusicList.ItemsSource = arrListmusicFilesNames;
                //listMusicList.Items.Refresh();
                bRes = true;
            }
            catch (Exception ex) 
            {
                Log.LogInstance.Write(ex.Message, MessageType.Error);
                //System.Windows.Forms.MessageBox.Show(ex.Message); 
            }
            return bRes;
        }

        public void RefreshXmlList(ArrayList arr, ArrayList arr1)
        {
            //无分布式音响，所以屏蔽
            return;
            XmlNodeList xnl = xmlDocList.SelectNodes("/Sounds");
            foreach (XmlNode xn in xnl) 
            {
                xn.RemoveAll(); 
            }
            for (int i = 0; i < arr.Count; i++)
            {
                if (!string.IsNullOrEmpty(arr[i].ToString()))
                {
                    XmlElement node = xmlDocList.CreateElement("sound");
                    node.SetAttribute("soundName", arr[i].ToString());
                    node.SetAttribute("soundFrequency", arr1[i].ToString());
                    xmlDocList.DocumentElement.AppendChild(node);
                }
            }
            xmlDocList.Save(fileListConfig);

            ReadFiles();
        }

        public bool AddSound(string name, string frequency)
        {
            bool bRes = false;
            try
            {
                XmlElement node = xmlDocList.CreateElement("sound");
                node.SetAttribute("soundName", name);
                node.SetAttribute("soundFrequency", frequency);
                xmlDocList.DocumentElement.AppendChild(node);
                bRes = true;
                xmlDocList.Save(fileListConfig);
                ReadFiles();
            }                
            catch (Exception ex) 
            {
                Log.LogInstance.Write(ex.Message, MessageType.Error);
                //System.Windows.Forms.MessageBox.Show(ex.Message);
            }
            
            return bRes;
        }
        
    }
}
