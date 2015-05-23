using SwipCardSystem.View;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml;

namespace SwipCardSystem.Controller
{
    public class PlanConfigManager
    {
        private static PlanConfigManager _planConfigManager = null;
        private static object _object = new object();
        private XmlDocument xmlDocList = new XmlDocument();
        private string fileListConfig = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName.Replace(System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName, "") + @"PlanConfigList.xml";
        public ObservableCollection<PlanItem> _planList
        {
            set;
            get;
        }
        public static PlanConfigManager CreateSingleton()
        {
            lock (_object)
            {
                if (_planConfigManager == null)
                {
                    _planConfigManager = new PlanConfigManager();
                }
            }
            return _planConfigManager;
        }
        PlanConfigManager()
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
                _planList = new ObservableCollection<PlanItem>();
                xmlDocList.Load(fileListConfig);
                XmlNodeList xmlNodelist = xmlDocList.SelectNodes("/Plans/plan");
                foreach (XmlNode oNode in xmlNodelist)
                {
                    PlanItem item = new PlanItem();
                    item.BeginTime = oNode.Attributes["BeginTime"].Value;
                    item.Date = oNode.Attributes["Date"].Value;
                    item.EndTime = oNode.Attributes["EndTime"].Value;
                    item.FileCount = oNode.Attributes["FileCount"].Value;
                    item.MusicPaths = oNode.Attributes["MusicPaths"].Value;
                    item.Order = oNode.Attributes["Order"].Value;
                    item.Sound = oNode.Attributes["Sound"].Value;

                    _planList.Add(item);
                }
                bRes = true;
            }
            catch (Exception ex) 
            {
                Log.LogInstance.Write(ex.Message, MessageType.Error);
                //System.Windows.Forms.MessageBox.Show(ex.Message); 
            }
            return bRes;
        }

        public void RefreshXmlList(ObservableCollection<PlanItem> list)
        {
            XmlNodeList xnl = xmlDocList.SelectNodes("/Plans");
            foreach (XmlNode xn in xnl) { xn.RemoveAll(); }
            for (int i = 0; i < list.Count; i++)
            {
                if (!string.IsNullOrEmpty(list[i].ToString()))
                {
                    XmlElement node = xmlDocList.CreateElement("plan");

                    node.SetAttribute("BeginTime",list[i].BeginTime);
                    node.SetAttribute("Date",list[i].Date);
                    node.SetAttribute("EndTime",list[i].EndTime);
                    node.SetAttribute("FileCount",list[i].FileCount);
                    node.SetAttribute("MusicPaths",list[i].MusicPaths);
                    node.SetAttribute("Order",list[i].Order);
                    node.SetAttribute("Sound", list[i].Sound);

                    xmlDocList.DocumentElement.AppendChild(node);
                }
            }
            xmlDocList.Save(fileListConfig);

            ReadFiles();
        }        
    }
}
