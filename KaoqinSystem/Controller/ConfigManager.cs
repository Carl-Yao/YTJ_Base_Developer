using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SwipCardSystem.Medol;
using System.Xml;
using System.Windows;

namespace SwipCardSystem.Controller
{
    public class ConfigManager
    {
        private static ConfigManager _configManager = null;

        private static object _lock = new object();

        public bool _isInitilize = false;

        private string _configFilePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName.Replace(System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName, "") + @"config.xml";

        private string configFilePath = null;

        private ConfigInfo _configInfo = null;
        public bool IsInitilize
        {
            get
            {
                return _isInitilize;
            }
        }
        public ConfigInfo ConfigInfo
        {
            set
            {
                _configInfo = value;
            }
            get
            {
                return _configInfo;
            }
        }

        ConfigManager()
        {
            Initilize();
        }

        public void Initilize()
        {
            if (!_isInitilize)
            {
                if (!GetConfigInfo())
                {
                    Log.LogInstance.Write("Get config info error",MessageType.Error);
                    //MessageBox.Show("Get config info error");
                }
                _isInitilize = true;
            }
        }

        public static ConfigManager CreateSingleton()
        {
            lock (_lock)
            {
                if (_configManager == null)
                {
                    _configManager = new ConfigManager();
                }
            }
            return _configManager;
        }

        public bool GetConfigInfo()
        {
            _configInfo = new ConfigInfo();
            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(_configFilePath);
                if (xmlDocument == null)
                {
                    return false;
                }
                XmlElement xmlElement = xmlDocument.DocumentElement;
                if (xmlElement == null)
                {
                    return false;
                }

                _configInfo.InstitutionID = xmlElement.SelectSingleNode("InstitutionID").InnerText;
                _configInfo.Today = xmlElement.SelectSingleNode("Today").InnerText;
                _configInfo.InstitutionName = xmlElement.SelectSingleNode("InstitutionName").InnerText;
                _configInfo.StudentSumNumber = xmlElement.SelectSingleNode("StudentSumNumber").InnerText;
                _configInfo.ServiceUrl = xmlElement.SelectSingleNode("ServiceUrl").InnerText;
                _configInfo.AdminAccount = new Account();
                _configInfo.AdminAccount.Name = xmlElement.SelectSingleNode("AdminAccount").SelectSingleNode("Name").InnerText;
                _configInfo.AdminAccount.Password = xmlElement.SelectSingleNode("AdminAccount").SelectSingleNode("Password").InnerText;
                _configInfo.StandardAccount = new Account();
                _configInfo.StandardAccount.Name = xmlElement.SelectSingleNode("StandardAccount").SelectSingleNode("Name").InnerText;
                _configInfo.StandardAccount.Password = xmlElement.SelectSingleNode("StandardAccount").SelectSingleNode("Password").InnerText;
                _configInfo.BeginTime = new MyTime();
                _configInfo.BeginTime.Hour = int.Parse(xmlElement.SelectSingleNode("BeginTime").SelectSingleNode("Hour").InnerText);
                _configInfo.BeginTime.Minute = int.Parse(xmlElement.SelectSingleNode("BeginTime").SelectSingleNode("Minute").InnerText);
                _configInfo.EndTime = new MyTime();
                _configInfo.EndTime.Hour = int.Parse(xmlElement.SelectSingleNode("EndTime").SelectSingleNode("Hour").InnerText);
                _configInfo.EndTime.Minute = int.Parse(xmlElement.SelectSingleNode("EndTime").SelectSingleNode("Minute").InnerText);

                _configInfo.ClearRecordFrequencyByDay = Int32.Parse(xmlElement.SelectSingleNode("ClearRecordFrequencyByDay").InnerText);

                _configInfo.IsFirstUpdate = bool.Parse(xmlElement.SelectSingleNode("IsFirstUpdate").InnerText);

                _configInfo.WaitTimeForToPosterBySecond = int.Parse(xmlElement.SelectSingleNode("WaitTimeForToPosterBySecond").InnerText);

                if (string.IsNullOrEmpty(xmlElement.SelectSingleNode("LastClearRecordDay").InnerText))
                {
                    _configInfo.LastClearRecordDay = DateTime.Now.DayOfYear;
                    xmlElement.SelectSingleNode("LastClearRecordDay").InnerText = _configInfo.LastClearRecordDay.ToString();
                    xmlDocument.Save(_configFilePath);
                }
                else
                {
                    _configInfo.LastClearRecordDay = int.Parse(xmlElement.SelectSingleNode("LastClearRecordDay").InnerText);
                }
                _configInfo.DateBaseName = xmlElement.SelectSingleNode("DateBaseName").InnerText;

                _configInfo.KaoqinTableName = xmlElement.SelectSingleNode("KaoqinTableName").InnerText;

                _configInfo.CardTableName = xmlElement.SelectSingleNode("CardTableName").InnerText;

                _configInfo.FamilyTableName = xmlElement.SelectSingleNode("FamilyTableName").InnerText;

                _configInfo.InstitutionTableName = xmlElement.SelectSingleNode("InstitutionTableName").InnerText;

                _configInfo.StudentTableName = xmlElement.SelectSingleNode("StudentTableName").InnerText;

                _configInfo.CardTableUpdateTime = xmlElement.SelectSingleNode("CardTableUpdateTime").InnerText;

                _configInfo.FamilyTableUpdateTime = xmlElement.SelectSingleNode("FamilyTableUpdateTime").InnerText;

                _configInfo.InstitutionTableUpdateTime = xmlElement.SelectSingleNode("InstitutionTableUpdateTime").InnerText;

                _configInfo.StudentTableUpdateTime = xmlElement.SelectSingleNode("StudentTableUpdateTime").InnerText;

                _configInfo.ConnectionString = xmlElement.SelectSingleNode("ConnectionString").InnerText;

                _configInfo.Notice = xmlElement.SelectSingleNode("Notice").InnerText;

                _configInfo.ZiXun = xmlElement.SelectSingleNode("ZiXun").InnerText;
            }
            catch
            {
                return false;
            }
            return true;
        }

        public bool SetConfigInfo()
        {
            if (_configInfo == null)
            {
                _configInfo = new ConfigInfo();
            }
            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(_configFilePath);
                if (xmlDocument == null)
                {
                    return false;
                }
                XmlElement xmlElement = xmlDocument.DocumentElement;
                if (xmlElement == null)
                {
                    return false;
                }

                xmlElement.SelectSingleNode("InstitutionID").InnerText = _configInfo.InstitutionID;
                xmlElement.SelectSingleNode("Today").InnerText = _configInfo.Today;
                xmlElement.SelectSingleNode("InstitutionName").InnerText = _configInfo.InstitutionName;
                xmlElement.SelectSingleNode("StudentSumNumber").InnerText = _configInfo.StudentSumNumber;
                xmlElement.SelectSingleNode("ServiceUrl").InnerText = _configInfo.ServiceUrl;
                //_configInfo.AdminAccount = new Account();
                xmlElement.SelectSingleNode("AdminAccount").SelectSingleNode("Name").InnerText = _configInfo.AdminAccount.Name ;
                xmlElement.SelectSingleNode("AdminAccount").SelectSingleNode("Password").InnerText = _configInfo.AdminAccount.Password;
                //_configInfo.StandardAccount = new Account();
                xmlElement.SelectSingleNode("StandardAccount").SelectSingleNode("Name").InnerText = _configInfo.StandardAccount.Name;
                xmlElement.SelectSingleNode("StandardAccount").SelectSingleNode("Password").InnerText = _configInfo.StandardAccount.Password;
                //_configInfo.BeginTime = new MyTime();
                xmlElement.SelectSingleNode("BeginTime").SelectSingleNode("Hour").InnerText = _configInfo.BeginTime.Hour.ToString();
                xmlElement.SelectSingleNode("BeginTime").SelectSingleNode("Minute").InnerText = _configInfo.BeginTime.Minute.ToString();
                //_configInfo.EndTime = new MyTime();
                xmlElement.SelectSingleNode("EndTime").SelectSingleNode("Hour").InnerText = _configInfo.EndTime.Hour.ToString();
                xmlElement.SelectSingleNode("EndTime").SelectSingleNode("Minute").InnerText = _configInfo.EndTime.Minute.ToString();

                xmlElement.SelectSingleNode("ClearRecordFrequencyByDay").InnerText = _configInfo.ClearRecordFrequencyByDay.ToString();

                xmlElement.SelectSingleNode("IsFirstUpdate").InnerText = _configInfo.IsFirstUpdate.ToString();

                xmlElement.SelectSingleNode("WaitTimeForToPosterBySecond").InnerText = _configInfo.WaitTimeForToPosterBySecond.ToString();


                xmlElement.SelectSingleNode("LastClearRecordDay").InnerText = _configInfo.LastClearRecordDay.ToString();
                
                xmlElement.SelectSingleNode("DateBaseName").InnerText = _configInfo.DateBaseName;

                xmlElement.SelectSingleNode("KaoqinTableName").InnerText = _configInfo.KaoqinTableName;

                xmlElement.SelectSingleNode("CardTableName").InnerText = _configInfo.CardTableName;

                xmlElement.SelectSingleNode("FamilyTableName").InnerText = _configInfo.FamilyTableName;

                xmlElement.SelectSingleNode("InstitutionTableName").InnerText = _configInfo.InstitutionTableName;

                xmlElement.SelectSingleNode("StudentTableName").InnerText = _configInfo.StudentTableName;

                xmlElement.SelectSingleNode("CardTableUpdateTime").InnerText = _configInfo.CardTableUpdateTime;

                xmlElement.SelectSingleNode("FamilyTableUpdateTime").InnerText = _configInfo.FamilyTableUpdateTime;

                xmlElement.SelectSingleNode("StudentTableUpdateTime").InnerText = _configInfo.StudentTableUpdateTime;

                xmlElement.SelectSingleNode("ConnectionString").InnerText = _configInfo.ConnectionString;

                xmlElement.SelectSingleNode("Notice").InnerText = _configInfo.Notice;

                xmlElement.SelectSingleNode("ZiXun").InnerText = _configInfo.ZiXun;
                
                xmlDocument.Save(_configFilePath);
            }
            catch
            {
                return false;
            }
            return true;
        }
    }
}
