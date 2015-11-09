using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SwipCardSystem.ServiceReference1;
using Newtonsoft.Json.Linq;
using System.Windows.Forms;
using SwipCardSystem.Medol;
using System.IO;
using System.Threading.Tasks;
using System.Web.Services.Description;
using System.Xml.Serialization;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Reflection;
using System.Net;
using System.Xml;
using System.Diagnostics;
using System.Windows.Threading;
using System.Drawing;

namespace SwipCardSystem.Controller
{
    public class WebServiceManager
    {
        private ConfigManager _configManager = null;
        private Type _terminalServiceClientType = null;
        private object _terminalServiceClientInstance = null;
        //private TerminalServiceClient _terminalServiceClient = null;
        private MySqlManager _mySqlManager = null;
        private static WebServiceManager _webServiceManager = null;
        private static NetworkManager _networkManager = null;
        private WebServiceManager()
        {
            _configManager = ConfigManager.CreateSingleton();
            _mySqlManager = MySqlManager.CreateSingleton();
            _networkManager = NetworkManager.CreateSingleton();
            //_terminalServiceClient = new TerminalServiceClient();
            //_terminalServiceClient.Open();

            SetServiceClient();
        }

        public static WebServiceManager CreateSingleton()
        {
            if (_webServiceManager == null)
            {
                _webServiceManager = new WebServiceManager();
            }
            return _webServiceManager;
        }

        public void SetServiceClient()
        {
            if (_networkManager.IsInternetConnecting)
            {
                CreateWebServiceDll(_configManager.ConfigInfo.ServiceUrl);
            }
            Assembly asm = Assembly.LoadFrom(Constants.WebServiceDllName);
            _terminalServiceClientType = asm.GetType(Constants.WebServiceClassName);
            _terminalServiceClientInstance = Activator.CreateInstance(_terminalServiceClientType);
        }

        private void CreateWebServiceDll(string url)
        {

            try
            {
                // 1. 使用 WebClient 下载 WSDL 信息。
                WebClient web = new WebClient();
                Stream stream = web.OpenRead(url);//"http://119.252.247.222:8080/ismsapp/webservice/terminalservice.asmx?WSDL");

                // 2. 创建和格式化 WSDL 文档。
                ServiceDescription description = ServiceDescription.Read(stream);

                // 3. 创建客户端代理代理类。
                ServiceDescriptionImporter importer = new ServiceDescriptionImporter();

                importer.ProtocolName = "Soap"; // 指定访问协议。
                importer.Style = ServiceDescriptionImportStyle.Client; // 生成客户端代理。
                importer.CodeGenerationOptions = CodeGenerationOptions.GenerateProperties | CodeGenerationOptions.GenerateNewAsync;

                importer.AddServiceDescription(description, null, null); // 添加 WSDL 文档。

                // 4. 使用 CodeDom 编译客户端代理类。
                CodeNamespace nmspace = new CodeNamespace(); // 为代理类添加命名空间，缺省为全局空间。
                CodeCompileUnit unit = new CodeCompileUnit();
                unit.Namespaces.Add(nmspace);

                ServiceDescriptionImportWarnings warning = importer.Import(nmspace, unit);
                CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");

                CompilerParameters parameter = new CompilerParameters();
                parameter.GenerateExecutable = false;
                parameter.OutputAssembly = Constants.WebServiceDllName; // 可以指定你所需的任何文件名。
                parameter.ReferencedAssemblies.Add("System.dll");
                parameter.ReferencedAssemblies.Add("System.XML.dll");
                parameter.ReferencedAssemblies.Add("System.Web.Services.dll");
                parameter.ReferencedAssemblies.Add("System.Data.dll");

                CompilerResults result = provider.CompileAssemblyFromDom(parameter, unit);
                if (result.Errors.HasErrors)
                {
                    // 显示编译错误信息
                    Log.LogInstance.Write(result.Errors.ToString(), MessageType.Error);
                }
            }
            catch(Exception e)
            {
                Log.LogInstance.Write(e.Message, MessageType.Error);
            }
        }

        //如错误应退出应用
        public bool DownloadData()
        {
            if (_configManager.ConfigInfo == null)
            {
                //Log.LogInstance.Write("初始化config文件失败！");
                Log.LogInstance.Write("初始化config文件失败！", MessageType.Error);
                //MessageBox.Show("初始化config文件失败！");
                return false;
            }
            try
            {
                //_terminalServiceClient = new TerminalServiceClient();
                //_terminalServiceClient.Open();

                bool isFirstDownload = _configManager.ConfigInfo.IsFirstUpdate;

                //download datatalbe with create or no                
                if (isFirstDownload)
                {
                    _configManager.ConfigInfo.LastClearRecordDay = DateTime.Now.DayOfYear;
                    _mySqlManager.ClearDataTable(false);
                    _mySqlManager.CreateDataTable(false);
                    DownloadGroupInfo();
                }
                else
                {
                    //30天一清理，方便一些删除一些没用的老数据，节省空间，或是开学;ClearRecordFrequencyByDay代表清理数据库等的频率而非清理考勤记录的频率
                    if (_configManager.ConfigInfo.LastClearRecordDay + _configManager.ConfigInfo.ClearRecordFrequencyByDay < DateTime.Now.DayOfYear)
                    {
                        isFirstDownload = true;
                        _configManager.ConfigInfo.LastClearRecordDay = DateTime.Now.DayOfYear;
                        _mySqlManager.ClearDataTable(false);
                        //--可能创建数据库时与其他线程冲突
                        _mySqlManager.CreateDataTable(false);
                        DownloadGroupInfo();
                    }
                    else
                    {
                        _mySqlManager.CreateDataTable(false);
                        ClearStudentGoSchoolNumIfIsNewDay();
                    }
                }
                DownloadNotice();
                DownloadZiXun();
                DownloadTitleAndLogo();
                DownloadStudentInfo(isFirstDownload);
                DownloadTeacherInfo(isFirstDownload);
                //to do download student picture      
                DownloadParentInfo(isFirstDownload);

                DownloadCardInfo(isFirstDownload);
                DownloadCardTeacherInfo(isFirstDownload);
                _configManager.ConfigInfo.IsFirstUpdate = false;
                _configManager.SetConfigInfo();

                //_mySqlManager.GetKaoqinDataSet();
            }
            catch
            {
                //Log.LogInstance.Write("同步数据失败！");
                Log.LogInstance.Write("同步数据失败！", MessageType.Error);
                //MessageBox.Show("同步数据失败！");
                return false;
            }
            return true;
        }

        private void ClearStudentGoSchoolNumIfIsNewDay()
        {
            if (!DateTime.Now.Day.ToString().Equals(_configManager.ConfigInfo.Today))
            {
                _mySqlManager.ClearStudentGoSchoolNum();
                _configManager.ConfigInfo.Today = DateTime.Now.Day.ToString();
                _configManager.SetConfigInfo();
            }
        }
        private string _recordId = string.Empty;

        private int i = 0;
        //看上传的时间是否太长，如果太长改为批量上传，考勤时间到后变为单个上传(经测试不耗时，如网速太慢等问题以后再考虑--可异步或者先保存本地）
        public bool UploadDataOne(KaoqinInfo kaoqinRecord, bool isFromMySQL, bool isTeacherRecord = false)
        {
            string ret = string.Empty;
            bool bRet = true;
            try
            {
                //Task.Factory.StartNew(() =>
                //    {
                        try
                        {
                            if (kaoqinRecord != null)
                            {
                                if (_recordId.Equals(kaoqinRecord.RecordId))
                                {
                                    kaoqinRecord.PictureBase64 = " ";
                                }
                                else
                                {
                                    _recordId = kaoqinRecord.RecordId;
                                }
                                Console.Out.WriteLine("end");
                                MethodInfo method = null;
                                 string[] parameter = null;
                                if (isTeacherRecord)
                                {
                                    method = _terminalServiceClientType.GetMethod("checkononeteacher");
                                    parameter = new string[]{ "{\"group_id\":\""
                                    + _configManager.ConfigInfo.InstitutionID + "\",\"record_id\":\""
                                    + kaoqinRecord.RecordId + "\",\"equp_id\":\""
                                    + kaoqinRecord.EqupId + "\",\"iccard_id\":\""
                                    + kaoqinRecord.ICCardId + "\",\"iccard_no\":\""
                                    + kaoqinRecord.ICCardNo + "\",\"user_id\":\""
                                    + kaoqinRecord.StudentID + "\", \"template_val\":\""
                                    + (kaoqinRecord.TemplateVal == null ? "0" : kaoqinRecord.TemplateVal) + "\",\"record_time\":\""
                                    + kaoqinRecord.RecordTime  + "\",\"picture\":\""
                                    + kaoqinRecord.PictureBase64 + "\"}" };
                                }
                                else
                                {
                                    method = _terminalServiceClientType.GetMethod("checkonone");
                                    parameter = new string[]{ "{\"group_id\":\""
                                    + _configManager.ConfigInfo.InstitutionID + "\",\"class_id\":\""
                                    + kaoqinRecord.ClassId + "\",\"record_id\":\""
                                    + kaoqinRecord.RecordId + "\",\"equp_id\":\""
                                    + kaoqinRecord.EqupId + "\",\"iccard_id\":\""
                                    + kaoqinRecord.ICCardId + "\",\"iccard_no\":\""
                                    + kaoqinRecord.ICCardNo + "\",\"student_id\":\""
                                    + kaoqinRecord.StudentID + "\", \"template_val\":\""
                                    + (kaoqinRecord.TemplateVal == null ? "0" : kaoqinRecord.TemplateVal) + "\",\"record_time\":\""
                                    + kaoqinRecord.RecordTime + "\",\"picture\":\""
                                    + kaoqinRecord.PictureBase64 + "\"}" };
                                }
                               
                                ret = (string)method.Invoke(_terminalServiceClientInstance, parameter);
                                if (ret.IndexOf("fail")>-1)
                                {
                                    if (isFromMySQL)
                                    {
                                        //上传本地数据库考勤记录失败，则返回false，以便于不删除本地数据
                                        bRet = false;
                                        Log.LogInstance.Write("UploadDataOne:Fail---isFromMySQL---" + kaoqinRecord.StudentID, MessageType.Error);
                                    }
                                    else
                                    {
                                        SaveKaoqinRecordAndPicture(kaoqinRecord);
                                        Log.LogInstance.Write("UploadDataOne:Fail---" + kaoqinRecord.StudentID, MessageType.Error);
                                    }                                   
                                }
                                else
                                {
                                    if (isFromMySQL)
                                    {
                                        //补充删除，防止文件遗留过多
                                        //清空parentpicture文件夹下的照片文件
                                        DirectoryInfo directory = new DirectoryInfo(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName.Replace(System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName, "") + Constants.CAPTUREPICTURE_FOLDERNAME);
                                        _mySqlManager.DeleteOneRecordData(kaoqinRecord.RecordId);
                                        File.Delete(kaoqinRecord.PicturePath);
                                        
                                        //_mySqlManager.ClearDataTable(true);
                                        //--可能创建数据库时与其他线程冲突
                                        //_mySqlManager.CreateDataTable(true);

                                        Log.LogInstance.Write("UploadDataOne:Success---isFromSQL---" + kaoqinRecord.StudentID, MessageType.Success);
                                    }
                                    else
                                    {
                                        Log.LogInstance.Write("UploadDataOne:Success---" + kaoqinRecord.StudentID, MessageType.Success);
                                    }
                                    
                                }
                                Console.Out.WriteLine((i++).ToString() + "---" + kaoqinRecord.TemplateVal + "---" + ret);
                            }
                        }
                        catch (Exception e)
                        {
                            SaveKaoqinRecordAndPicture(kaoqinRecord);
                            Log.LogInstance.Write("UploadDataOne" + e.Message, MessageType.Error);
                            return false;
                        }
                    //});
            }
            catch
            {
                return false;
            }
            //上传记录ID为kaoqinRecord的数据
            //上传后把标识设为Y
            return bRet;
        }
        public void SaveKaoqinRecordAndPicture(KaoqinInfo kaoqinInfo)
        {
            //先创建目录在保存,最好当前目录
            kaoqinInfo.PicturePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName.Replace(System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName, "") + Constants.CAPTUREPICTURE_FOLDERNAME + "\\" + kaoqinInfo.RecordId + ".jpg";
            kaoqinInfo.PicBitMap.Save(kaoqinInfo.PicturePath, System.Drawing.Imaging.ImageFormat.Jpeg);
            kaoqinInfo.PicBitMap.Dispose();
            kaoqinInfo.PicturePath = kaoqinInfo.PicturePath.Replace("\\", "\\\\");

            _mySqlManager.SaveKaoqinRecord(kaoqinInfo);
        }

        //到考勤时间后，上传未上传数据
        public bool UploadDataAllInTime(List<KaoqinInfo> kaoqinInfos)
        {
            //查看数据库到9点前的所有记录，看标识为N的上传
            string ret = string.Empty;
            bool bRet = true;
            try
            {
                if (kaoqinInfos.Count > 0)
                {
                    string strkaoqins = string.Empty;
                    foreach (var kaoqinInfo in kaoqinInfos)
                    {
                        if (!File.Exists(kaoqinInfo.PicturePath))
                        {
                            continue;
                        }
                        System.Drawing.Bitmap newPic = new System.Drawing.Bitmap(kaoqinInfo.PicturePath);
                        string pictureBase64 = string.Empty;
                        ImageToBase64.ImgToBase64String(newPic, ref pictureBase64);
                        newPic.Dispose();
                        kaoqinInfo.PictureBase64 = pictureBase64;
                        if (!UploadDataOne(kaoqinInfo, true, _mySqlManager.IsTeacherRecord(kaoqinInfo.ICCardNo)))
                        {
                            bRet = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogInstance.Write("UploadDataAll-->" + ex.Message, MessageType.Error);
                //MessageBox.Show("UploadDataAll-->" + ex.Message);
                return false;
            }

            //checkonpic
            return bRet;
        }

        //后台上传考勤数据
        public bool UploadDataAll(List<KaoqinInfo> kaoqinInfos)
        {
            //查看数据库到9点前的所有记录，看标识为N的上传
            string ret = string.Empty;
            try
            {
                if (kaoqinInfos.Count > 0)
                {
                    string strkaoqins = string.Empty;
                    foreach (var kaoqinInfo in kaoqinInfos)
                    {
                        if (_mySqlManager.IsTeacherRecord(kaoqinInfo.ICCardNo))
                        {
                            continue;
                        }
                        strkaoqins += "{\"group_id\":\""
                        + _configManager.ConfigInfo.InstitutionID + "\",\"record_id\":\""
                        + kaoqinInfo.RecordId + "\",\"equp_id\":\""
                        + kaoqinInfo.EqupId + "\",\"iccard_id\":\""
                        + kaoqinInfo.ICCardId + "\",\"iccard_no\":\""
                        + kaoqinInfo.ICCardNo + "\",\"student_id\":\""
                        + kaoqinInfo.StudentID + "\", \"template_val\":\""
                        + kaoqinInfo.TemplateVal + "\",\"record_time\":\""
                        + kaoqinInfo.RecordTime + "\"}" + ",";
                    }
                    if (strkaoqins.Length > 1)
                        strkaoqins = strkaoqins.Remove(strkaoqins.Length - 1);
                    MethodInfo method = _terminalServiceClientType.GetMethod("checkonbatch");
                    string[] parameter = { "{\"rowAllNum\":\"" + kaoqinInfos.Count.ToString() + "\",\"record\":[" + strkaoqins + "]}" };
                    ret = (string)method.Invoke(_terminalServiceClientInstance, parameter);
                    //ret = _terminalServiceClient.checkonbatch("{\"rowAllNum\":\"" + kaoqinInfos.Count.ToString() + "\",\"record\":[" + strkaoqins + "]}");
                    foreach (var kaoqinInfo in kaoqinInfos)
                    {
                        if (!_mySqlManager.IsTeacherRecord(kaoqinInfo.ICCardNo))
                        {
                            continue;
                        }
                        strkaoqins += "{\"group_id\":\""
                        + _configManager.ConfigInfo.InstitutionID + "\",\"record_id\":\""
                        + kaoqinInfo.RecordId + "\",\"equp_id\":\""
                        + kaoqinInfo.EqupId + "\",\"iccard_id\":\""
                        + kaoqinInfo.ICCardId + "\",\"iccard_no\":\""
                        + kaoqinInfo.ICCardNo + "\",\"user_id\":\""
                        + kaoqinInfo.StudentID + "\", \"template_val\":\""
                        + kaoqinInfo.TemplateVal + "\",\"record_time\":\""
                        + kaoqinInfo.RecordTime + "\"}" + ",";
                    }
                    if (strkaoqins.Length > 1)
                        strkaoqins = strkaoqins.Remove(strkaoqins.Length - 1);
                    MethodInfo method1 = _terminalServiceClientType.GetMethod("checkonbatchteacher");
                    string[] parameter1 = { "{\"rowAllNum\":\"" + kaoqinInfos.Count.ToString() + "\",\"record\":[" + strkaoqins + "]}" };
                    ret = (string)method1.Invoke(_terminalServiceClientInstance, parameter1);

                }
            }
            catch (Exception ex)
            {
                Log.LogInstance.Write("UploadDataAll-->" + ex.Message, MessageType.Error);
                //MessageBox.Show("UploadDataAll-->" + ex.Message);
                return false;
            }

            //checkonpic
            return true;
        }

        public bool UploadPicAll(List<KaoqinInfo> kaoqinInfos)
        {
            //查看数据库到9点前的所有记录，看标识为N的上传
            string ret = string.Empty;
            try
            {
                if (kaoqinInfos.Count > 0)
                {
                    foreach (var kaoqinInfo in kaoqinInfos)
                    {
                        //kaoqinInfo.PicturePath = @"F:\系统\SwipCardSystem\SwipCardSystem\bin\Debug\20140720120339039.jpg";
                        if (!File.Exists(kaoqinInfo.PicturePath))
                        {
                            continue;
                        }
                        System.Drawing.Bitmap newPic = new System.Drawing.Bitmap(kaoqinInfo.PicturePath);
                        string pictureBase64 = string.Empty;
                        ImageToBase64.ImgToBase64String(newPic, ref pictureBase64);

                        if (_mySqlManager.IsTeacherRecord(kaoqinInfo.ICCardNo))
                        {
                            MethodInfo method = _terminalServiceClientType.GetMethod("checkonpicteacher");
                            string[] parameter = { "{\"group_id\":\""
                        + _configManager.ConfigInfo.InstitutionID + "\",\"record_id\":\""
                        + kaoqinInfo.RecordId + "\",\"user_id\":\""
                        + kaoqinInfo.StudentID + "\",\"picture\":\""
                        + pictureBase64 + "\",\"record_time\":\""
                        + kaoqinInfo.RecordTime + "\"}" };
                            ret = (string)method.Invoke(_terminalServiceClientInstance, parameter);
                        }
                        else
                        {
                            MethodInfo method = _terminalServiceClientType.GetMethod("checkonpic");
                            string[] parameter = { "{\"group_id\":\""
                        + _configManager.ConfigInfo.InstitutionID + "\",\"record_id\":\""
                        + kaoqinInfo.RecordId + "\",\"student_id\":\""
                        + kaoqinInfo.StudentID + "\",\"picture\":\""
                        + pictureBase64 + "\",\"record_time\":\""
                        + kaoqinInfo.RecordTime + "\"}" };
                            ret = (string)method.Invoke(_terminalServiceClientInstance, parameter);
                        }
                        //ret = _terminalServiceClient.checkonpic("{\"group_id\":\""
                        //+ _configManager.ConfigInfo.InstitutionID + "\",\"record_id\":\""
                        //+ kaoqinInfo.RecordId + "\",\"student_id\":\""
                        //+ kaoqinInfo.StudentID + "\",\"picture\":\""
                        //+ pictureBase64 + "\",\"record_time\":\""
                        //+ kaoqinInfo.RecordTime + "\"}");
                        var mJObj = JObject.Parse(ret);
                        newPic.Dispose();
                        if (((string)mJObj["returnMessage"]).Equals("success"))
                        {
                            File.Delete(kaoqinInfo.PicturePath);
                        }
                    }
                }
            }
            catch
            {
                return false;
            }

            //checkonpic
            return true;
        }

        public bool DownloadNotice()
        {
            try
            {
                string ret = string.Empty;
                MethodInfo method = _terminalServiceClientType.GetMethod("newnoticeinfo");
                string[] parameter = { "{\"condition\":{\"group_id\":\"" + _configManager.ConfigInfo.InstitutionID + "\"}}" };
                ret = (string)method.Invoke(_terminalServiceClientInstance, parameter);
                //ret = _terminalServiceClient.newnoticeinfo("{\"condition\":{\"group_id\":\"" + _configManager.ConfigInfo.InstitutionID + "\"}}");
                var mJObj = JObject.Parse(ret);
                if (((string)mJObj["returnMessage"]).Equals("success"))
                {
                    string rt = (string)mJObj["receiveTime"];
                    DateTime dt = Convert.ToDateTime(rt);

                    var mName = (JObject)mJObj["result"];

                    if (_configManager.ConfigInfo != null)
                    {
                        string str = string.Empty;
                        if (mName["TITLE"] != null && mName["NOTICE_INFO"] != null && mName["ADD_DATE"] != null)
                        {
                            str = (mName["TITLE"]).ToString() + "@" + (mName["NOTICE_INFO"]).ToString() + "@" + (mName["ADD_DATE"]).ToString();
                        }
                        
                        if (!str.Equals(_configManager.ConfigInfo.Notice))
                        {
                            _configManager.ConfigInfo.Notice = str;
                            return true;
                        }
                    }
                }
                else
                {
                    //Log.LogInstance.Write("获取公告失败！");
                    Log.LogInstance.Write("获取公告失败！", MessageType.Error);
                    //MessageBox.Show("获取公告失败！");
                }
            }
            catch (Exception ex)
            {
                //Log.LogInstance.Write("获取公告发生异常！DownloadGroupInfo:" + ex.Message);
                Log.LogInstance.Write("获取公告发生异常！DownloadGroupInfo:" + ex.Message, MessageType.Error);
                //MessageBox.Show("获取公告发生异常！DownloadGroupInfo:" + ex.Message);
            }
            return false;
        }

        public bool DownloadZiXun()
        {
            try
            {
                string ret = string.Empty;
                MethodInfo method = _terminalServiceClientType.GetMethod("newdynamicinfo");
                string[] parameter = { "{\"condition\":{\"group_id\":\"" + _configManager.ConfigInfo.InstitutionID + "\"}}" };
                ret = (string)method.Invoke(_terminalServiceClientInstance, parameter);
                //ret = _terminalServiceClient.newnoticeinfo("{\"condition\":{\"group_id\":\"" + _configManager.ConfigInfo.InstitutionID + "\"}}");
                var mJObj = JObject.Parse(ret);
                if (((string)mJObj["returnMessage"]).Equals("success"))
                {
                    string rt = (string)mJObj["receiveTime"];
                    DateTime dt = Convert.ToDateTime(rt);

                    var result = (JArray)mJObj["result"];

                    if (_configManager.ConfigInfo != null)
                    {
                        string str = string.Empty;

                        for (int i = 0; i < Math.Min(result.Count,3); i++)
                        {
                            if (result[i]["title"] != null && result[i]["info"] != null)
                            {
                                str += result[i]["title"].ToString() + "\n" + result[i]["info"].ToString() + "\n" + "\n" + "\n";
                            }
                        }

                        if (!str.Equals(_configManager.ConfigInfo.ZiXun))
                        {
                            _configManager.ConfigInfo.ZiXun = str;
                            return true;
                        }
                    }
                }
                else
                {
                    //Log.LogInstance.Write("获取公告失败！");
                    Log.LogInstance.Write("获取资讯失败！", MessageType.Error);
                    //MessageBox.Show("获取公告失败！");
                }
            }
            catch (Exception ex)
            {
                //Log.LogInstance.Write("获取公告发生异常！DownloadGroupInfo:" + ex.Message);
                Log.LogInstance.Write("获取资讯发生异常！DownloadGroupInfo:" + ex.Message, MessageType.Error);
                //MessageBox.Show("获取公告发生异常！DownloadGroupInfo:" + ex.Message);
            }
            return false;
        }

        public bool DownloadTitleAndLogo()
        {
            try
            {
                string ret = string.Empty;
                MethodInfo method = _terminalServiceClientType.GetMethod("grouplogo");
                string[] parameter = { "{\"condition\":{\"group_id\":\"" + _configManager.ConfigInfo.InstitutionID + "\"}}" };
                ret = (string)method.Invoke(_terminalServiceClientInstance, parameter);
                //ret = _terminalServiceClient.newnoticeinfo("{\"condition\":{\"group_id\":\"" + _configManager.ConfigInfo.InstitutionID + "\"}}");
                var mJObj = JObject.Parse(ret);
                if (((string)mJObj["returnMessage"]).Equals("success"))
                {
                    string rt = (string)mJObj["receiveTime"];
                    DateTime dt = Convert.ToDateTime(rt);

                    var result = (JObject)mJObj["result"];

                    if (_configManager.ConfigInfo != null && result["group_name"] != null)
                    {
                        _configManager.ConfigInfo.InstitutionName = result["group_name"].ToString();
                    }

                    //下载图片
                    string url = result["logo_url"].ToString();
                    using (System.Net.WebClient wc = new System.Net.WebClient())
                    {

                        wc.Headers.Add("User-Agent", "Chrome");

                        wc.DownloadFile(url, @"logo.png");//保存到本地的文件名和路径，请自行更改 

                    }
                    //Get_img(url);
                }
                else
                {
                    //Log.LogInstance.Write("获取公告失败！");
                    Log.LogInstance.Write("获取标题和logo失败！", MessageType.Error);
                    //MessageBox.Show("获取公告失败！");
                }
            }
            catch (Exception ex)
            {
                //Log.LogInstance.Write("获取公告发生异常！DownloadGroupInfo:" + ex.Message);
                Log.LogInstance.Write("获取资讯发生异常！DownloadGroupInfo:" + ex.Message, MessageType.Error);
                //MessageBox.Show("获取公告发生异常！DownloadGroupInfo:" + ex.Message);
            }
            return false;
        }

        private void DownloadGroupInfo()
        {
            try
            {
                string ret = string.Empty;
                MethodInfo method = _terminalServiceClientType.GetMethod("grouptree");
                string[] parameter = { "{\"condition\":{\"group_id\":\"" + _configManager.ConfigInfo.InstitutionID + "\"}}" };
                ret = (string)method.Invoke(_terminalServiceClientInstance, parameter);
                //ret = _terminalServiceClient.grouptree("{\"condition\":{\"group_id\":\"" + _configManager.ConfigInfo.InstitutionID + "\"}}");
                var mJObj = JObject.Parse(ret);
                if (((string)mJObj["returnMessage"]).Equals("success"))
                {
                    string rt = (string)mJObj["receiveTime"];
                    DateTime dt = Convert.ToDateTime(rt);

                    var mName = (JArray)mJObj["result"];

                    int iRowAllNum = (int)mJObj["rowAllNum"];

                    if (_configManager.ConfigInfo != null)
                    {
                        //保存幼儿园名字到config文件中，用于主界面从config中读取要显示的xxx欢迎你
                        _configManager.ConfigInfo.InstitutionName = (((JObject)mName[0])["GROUP_NAME"]).ToString();
                    }
                    for (var i = 0; i < mName.Count; i++)
                    {
                        string[] str = { ((JObject)mName[i])["GROUP_ID"].ToString(), ((JObject)mName[i])["SHORT_NAME"].ToString()/*newstr*/, ((JObject)mName[i])["GROUP_TYPE"].ToString(), (string)((JObject)mName[i])["PARENT_GROUPID"], ((JObject)mName[i])["AREA_ID"].ToString() };
                        _mySqlManager.AddDataToTable(_configManager.ConfigInfo.InstitutionTableName, str);
                    }
                }
                else
                {
                    Log.LogInstance.Write("获取机构数据失败！", MessageType.Error);
                    //MessageBox.Show("获取机构数据失败！");
                }
            }
            catch (Exception ex)
            {
                Log.LogInstance.Write("获取机构数据发生异常！DownloadGroupInfo:" + ex.Message, MessageType.Error);
                //MessageBox.Show("获取机构数据发生异常！DownloadGroupInfo:" + ex.Message);
            }
        }

        private void DownloadStudentInfo(bool isFirstDownload)
        {
            try
            {
                string ret = string.Empty;
                if (isFirstDownload)
                {
                    MethodInfo method = _terminalServiceClientType.GetMethod("studentsinfoall");
                    string[] parameter = { "{\"condition\":{\"group_id\":\"" + _configManager.ConfigInfo.InstitutionID + "\"}}" };
                    ret = (string)method.Invoke(_terminalServiceClientInstance, parameter);
                    //ret = _terminalServiceClient.studentsinfoall("{\"condition\":{\"group_id\":\"" + _configManager.ConfigInfo.InstitutionID + "\"}}");
                }
                else
                {
                    MethodInfo method = _terminalServiceClientType.GetMethod("studentsinfoadd");
                    string[] parameter = { "{\"condition\":{\"group_id\":\"" + _configManager.ConfigInfo.InstitutionID + "\",\"zlrq\":\"" + _configManager.ConfigInfo.StudentTableUpdateTime + "\"}}" };
                    ret = (string)method.Invoke(_terminalServiceClientInstance, parameter);
                    //ret = _terminalServiceClient.studentsinfoadd("{\"condition\":{\"group_id\":\"" + _configManager.ConfigInfo.InstitutionID + "\",\"zlrq\":\"" + _configManager.ConfigInfo.StudentTableUpdateTime + "\"}}");
                }
                var mJObj = JObject.Parse(ret);
                if (((string)mJObj["returnMessage"]).Equals("success"))
                {
                    var mName = (JArray)mJObj["result"];

                    int iRowAllNum = (int)mJObj["rowAllNum"];

                    //if (isFirstDownload)
                    //{
                    //    _configManager.ConfigInfo.StudentSumNumber = iRowAllNum.ToString();
                    //}
                    //else
                    //{
                    //    _configManager.ConfigInfo.StudentSumNumber = (iRowAllNum + int.Parse(_configManager.ConfigInfo.StudentSumNumber)).ToString();
                    //}

                    for (var i = 0; i < mName.Count; i++)
                    {
                        string[] str = { ((JObject)mName[i])["STUDENT_ID"].ToString(), ((JObject)mName[i])["XJH"].ToString(), ((JObject)mName[i])["STUDENT_NO"].ToString(), ((JObject)mName[i])["STUDENT_NAME"].ToString(), ((JObject)mName[i])["SEX"].ToString(), ((JObject)mName[i])["BIRTHDAY"].ToString(), ((JObject)mName[i])["ENTRANCE_DATE"].ToString(), ((JObject)mName[i])["GROUP_ID"].ToString(), ((JObject)mName[i])["XJ_STATE"].ToString(), ((JObject)mName[i])["YXBZ"].ToString(), "0"/*未上学*/ };
                        _mySqlManager.AddDataToTable(_configManager.ConfigInfo.StudentTableName, str);
                    }
                    _configManager.ConfigInfo.StudentSumNumber = _mySqlManager.SumStudent(-1);
                    _configManager.ConfigInfo.StudentTableUpdateTime = (string)mJObj["receiveTime"];
                }
                else
                {
                    Log.LogInstance.Write("获取学生数据失败！", MessageType.Error);
                    //MessageBox.Show("获取学生数据失败！");
                }
            }
            catch (Exception ex)
            {
                Log.LogInstance.Write("获取学生数据发生异常！DownloadStudentInfo:" + ex.Message, MessageType.Error);
                //MessageBox.Show("获取学生数据发生异常！DownloadStudentInfo:" + ex.Message);
            }
        }

        private void DownloadTeacherInfo(bool isFirstDownload)
        {
            try
            {
                string ret = string.Empty;
                if (isFirstDownload)
                {
                    MethodInfo method = _terminalServiceClientType.GetMethod("teacherinfoall");
                    string[] parameter = { "{\"condition\":{\"group_id\":\"" + _configManager.ConfigInfo.InstitutionID + "\"}}" };
                    ret = (string)method.Invoke(_terminalServiceClientInstance, parameter);
                    //ret = _terminalServiceClient.studentsinfoall("{\"condition\":{\"group_id\":\"" + _configManager.ConfigInfo.InstitutionID + "\"}}");
                }
                else
                {
                    MethodInfo method = _terminalServiceClientType.GetMethod("teacherinfoadd");
                    string[] parameter = { "{\"condition\":{\"group_id\":\"" + _configManager.ConfigInfo.InstitutionID + "\",\"zlrq\":\"" + _configManager.ConfigInfo.StudentTableUpdateTime + "\"}}" };
                    ret = (string)method.Invoke(_terminalServiceClientInstance, parameter);
                    //ret = _terminalServiceClient.studentsinfoadd("{\"condition\":{\"group_id\":\"" + _configManager.ConfigInfo.InstitutionID + "\",\"zlrq\":\"" + _configManager.ConfigInfo.StudentTableUpdateTime + "\"}}");
                }
                var mJObj = JObject.Parse(ret);
                if (((string)mJObj["returnMessage"]).Equals("success"))
                {
                    var mName = (JArray)mJObj["result"];

                    int iRowAllNum = (int)mJObj["rowAllNum"];

                    //if (isFirstDownload)
                    //{
                    //    _configManager.ConfigInfo.StudentSumNumber = iRowAllNum.ToString();
                    //}
                    //else
                    //{
                    //    _configManager.ConfigInfo.StudentSumNumber = (iRowAllNum + int.Parse(_configManager.ConfigInfo.StudentSumNumber)).ToString();
                    //}

                    for (var i = 0; i < mName.Count; i++)
                    {
                        string[] str = { ((JObject)mName[i])["USER_ID"].ToString(), 
                                           "",
                                           "", 
                                           ((JObject)mName[i])["NAME"].ToString(), 
                                           ((JObject)mName[i])["SEX"].ToString(),
                                           "", 
                                           "",
                                           ((JObject)mName[i])["GROUP_ID"].ToString(),
                                           "", 
                                           "", "0"/*未上学*/ };
                        _mySqlManager.AddDataToTable(_configManager.ConfigInfo.StudentTableName, str);
                    }
                    _configManager.ConfigInfo.StudentSumNumber = _mySqlManager.SumStudent(-1);
                    _configManager.ConfigInfo.StudentTableUpdateTime = (string)mJObj["receiveTime"];
                }
                else
                {
                    Log.LogInstance.Write("获取教师数据失败！", MessageType.Error);
                    //MessageBox.Show("获取学生数据失败！");
                }
            }
            catch (Exception ex)
            {
                Log.LogInstance.Write("获取教师数据发生异常！DownloadTeacherInfo:" + ex.Message, MessageType.Error);
                //MessageBox.Show("获取学生数据发生异常！DownloadStudentInfo:" + ex.Message);
            }
        }

        private void DownloadParentInfo(bool isFirstDownload)
        {
            try
            {
                string ret = string.Empty;
                if (isFirstDownload)
                {
                    //清空parentpicture文件夹下的照片文件
                    DirectoryInfo directory = new DirectoryInfo(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName.Replace(System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName, "") + Constants.PARENTPICTURE_FOLDERNAME);
                    if (!Directory.Exists(directory.Name))
                    {
                        Directory.CreateDirectory(directory.Name);
                    }
                    else
                    {
                        FileInfo[] files = directory.GetFiles();
                        foreach (FileInfo file in files)
                        {
                            File.Delete(file.DirectoryName + @"\" + file.Name);
                        }
                    }
                    MethodInfo method = _terminalServiceClientType.GetMethod("parentinfoall");
                    string[] parameter = { "{\"condition\":{\"group_id\":\"" + _configManager.ConfigInfo.InstitutionID + "\"}}" };
                    ret = (string)method.Invoke(_terminalServiceClientInstance, parameter);
                    //ret = _terminalServiceClient.parentinfoall("{\"condition\":{\"group_id\":\"" + _configManager.ConfigInfo.InstitutionID + "\"}}");
                }
                else
                {
                    //这个增量时间怎么搞
                    MethodInfo method = _terminalServiceClientType.GetMethod("parentinfoadd");
                    string[] parameter = { "{\"condition\":{\"group_id\":\"" + _configManager.ConfigInfo.InstitutionID + "\",\"zlrq\":\"" + _configManager.ConfigInfo.FamilyTableUpdateTime + "\"}}" };
                    ret = (string)method.Invoke(_terminalServiceClientInstance, parameter);
                    //ret = _terminalServiceClient.parentinfoadd("{\"condition\":{\"group_id\":\"" + _configManager.ConfigInfo.InstitutionID + "\",\"zlrq\":\"" + _configManager.ConfigInfo.FamilyTableUpdateTime + "\"}}");
                }
                var mJObj = JObject.Parse(ret);
                if (((string)mJObj["returnMessage"]).Equals("success"))
                {
                    var mName = (JArray)mJObj["result"];

                    int iRowAllNum = (int)mJObj["rowAllNum"];

                    for (var i = 0; i < mName.Count; i++)
                    {
                        MethodInfo method = _terminalServiceClientType.GetMethod("parentpic");
                        string[] parameter = { "{\"condition\":{\"user_id\":\"" + ((JObject)mName[i])["USER_ID"].ToString() + "\"}}" };
                        string picRet = (string)method.Invoke(_terminalServiceClientInstance, parameter);

                        //string picRet = _terminalServiceClient.parentpic("{\"condition\":{\"user_id\":\"" + ((JObject)mName[i])["USER_ID"].ToString() + "\"}}");
                        var mPicObj = JObject.Parse(picRet);
                        string[] str;
                        string picPath = string.Empty;
                        if (((string)mPicObj["returnMessage"]).Equals("success"))
                        {
                            System.Drawing.Bitmap bitMap = null;
                            ImageToBase64.Base64StringToImage((string)((JObject)mPicObj["result"])["picture"], ref bitMap);
                            if (bitMap != null)
                            {
                                picPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName.Replace(System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName, "") + Constants.PARENTPICTURE_FOLDERNAME + "\\" + ((JObject)mName[i])["USER_ID"].ToString() + DateTime.Now.ToString("HHmmssfff") + ".jpg";

                                //GDI+ 中发生一般性错误
                                bitMap.Save(picPath, System.Drawing.Imaging.ImageFormat.Jpeg);
                                picPath = picPath.Replace("\\", "\\\\");
                            }
                        }
                        if (string.IsNullOrEmpty(picPath))
                        {
                            str = new string[] { ((JObject)mName[i])["STUDENT_ID"].ToString(), ((JObject)mName[i])["USER_ID"].ToString(), ((JObject)mName[i])["RELATIONSHIP"].ToString(), ((JObject)mName[i])["NAME"].ToString(), ((JObject)mName[i])["SEX"].ToString(), "0" /*,((JObject)mName[i])["YXBZ"].ToString()*/ };

                        }
                        else
                        {
                            str = new string[] { ((JObject)mName[i])["STUDENT_ID"].ToString(), ((JObject)mName[i])["USER_ID"].ToString(), ((JObject)mName[i])["RELATIONSHIP"].ToString(), ((JObject)mName[i])["NAME"].ToString(), ((JObject)mName[i])["SEX"].ToString(), picPath /*,((JObject)mName[i])["YXBZ"].ToString()*/ };
                        }
                        _mySqlManager.AddDataToTable(_configManager.ConfigInfo.FamilyTableName, str);
                    }
                    _configManager.ConfigInfo.FamilyTableUpdateTime = (string)mJObj["receiveTime"];
                }
                else
                {
                    Log.LogInstance.Write("获取家长数据失败！", MessageType.Error);
                    //MessageBox.Show("获取家长数据失败！");
                }
            }
            catch (Exception ex)
            {
                Log.LogInstance.Write("获取家长数据发生异常！DownloadParentInfo:" + ex.Message, MessageType.Error);
                //MessageBox.Show("获取家长数据发生异常！DownloadParentInfo:" + ex.Message);
            }
        }

        private void DownloadCardInfo(bool isFirstDownload)
        {
            try
            {
                string ret = string.Empty;
                if (isFirstDownload)
                {
                    MethodInfo method = _terminalServiceClientType.GetMethod("iccardinfoall");
                    string[] parameter = { "{\"condition\":{\"group_id\":\"" + _configManager.ConfigInfo.InstitutionID + "\"}}" };
                    ret = (string)method.Invoke(_terminalServiceClientInstance, parameter);

                    //ret = _terminalServiceClient.iccardinfoall("{\"condition\":{\"group_id\":\"" + _configManager.ConfigInfo.InstitutionID + "\"}}");
                }
                else
                {
                    MethodInfo method = _terminalServiceClientType.GetMethod("iccardinfoadd");
                    string[] parameter = { "{\"condition\":{\"group_id\":\"" + _configManager.ConfigInfo.InstitutionID + "\",\"zlrq\":\"" + _configManager.ConfigInfo.CardTableUpdateTime + "\"}}" };
                    ret = (string)method.Invoke(_terminalServiceClientInstance, parameter);

                    //这个增量时间怎么搞
                    //ret = _terminalServiceClient.iccardinfoadd("{\"condition\":{\"group_id\":\"" + _configManager.ConfigInfo.InstitutionID + "\",\"zlrq\":\"" + _configManager.ConfigInfo.CardTableUpdateTime + "\"}}");
                }
                var mJObj = JObject.Parse(ret);
                if (((string)mJObj["returnMessage"]).Equals("success"))
                {
                    var mName = (JArray)mJObj["result"];

                    int iRowAllNum = (int)mJObj["rowAllNum"];

                    for (var i = 0; i < mName.Count; i++)
                    {
                        string[] str = { ((JObject)mName[i])["ICCARD_ID"].ToString(),
                                           ((JObject)mName[i])["ICCARD_NO"].ToString(),
                                           ((JObject)mName[i])["STUDENT_ID"].ToString(), 
                                           ((JObject)mName[i])["USER_ID"].ToString(), 
                                           ((JObject)mName[i])["YXBZ"].ToString() };
                        _mySqlManager.AddDataToTable(_configManager.ConfigInfo.CardTableName, str);
                    }
                    _configManager.ConfigInfo.CardTableUpdateTime = (string)mJObj["receiveTime"];
                }
                else
                {
                    Log.LogInstance.Write("获取IC卡数据失败！", MessageType.Error);
                    //MessageBox.Show("获取IC卡数据失败！");
                }
            }
            catch (Exception ex)
            {
                Log.LogInstance.Write("获取IC卡数据发生异常！DownloadCardInfo:" + ex.Message, MessageType.Error);
                //MessageBox.Show("获取IC卡数据发生异常！DownloadCardInfo:" + ex.Message);
            }
        }

        private void DownloadCardTeacherInfo(bool isFirstDownload)
        {
            try
            {
                string ret = string.Empty;
                if (isFirstDownload)
                {
                    MethodInfo method = _terminalServiceClientType.GetMethod("iccardteacherall");
                    string[] parameter = { "{\"condition\":{\"group_id\":\"" + _configManager.ConfigInfo.InstitutionID + "\"}}" };
                    ret = (string)method.Invoke(_terminalServiceClientInstance, parameter);

                    //ret = _terminalServiceClient.iccardinfoall("{\"condition\":{\"group_id\":\"" + _configManager.ConfigInfo.InstitutionID + "\"}}");
                }
                else
                {
                    MethodInfo method = _terminalServiceClientType.GetMethod("iccardteacheradd");
                    string[] parameter = { "{\"condition\":{\"group_id\":\"" + _configManager.ConfigInfo.InstitutionID + "\",\"zlrq\":\"" + _configManager.ConfigInfo.CardTableUpdateTime + "\"}}" };
                    ret = (string)method.Invoke(_terminalServiceClientInstance, parameter);

                    //这个增量时间怎么搞
                    //ret = _terminalServiceClient.iccardinfoadd("{\"condition\":{\"group_id\":\"" + _configManager.ConfigInfo.InstitutionID + "\",\"zlrq\":\"" + _configManager.ConfigInfo.CardTableUpdateTime + "\"}}");
                }
                var mJObj = JObject.Parse(ret);
                if (((string)mJObj["returnMessage"]).Equals("success"))
                {
                    var mName = (JArray)mJObj["result"];

                    int iRowAllNum = (int)mJObj["rowAllNum"];

                    for (var i = 0; i < mName.Count; i++)
                    {
                        string[] str = { ((JObject)mName[i])["ICCARD_ID"].ToString(),
                                           ((JObject)mName[i])["ICCARD_NO"].ToString(),
                                           "", 
                                           ((JObject)mName[i])["USER_ID"].ToString(), 
                                           ((JObject)mName[i])["YXBZ"].ToString() };
                        _mySqlManager.AddDataToTable(_configManager.ConfigInfo.CardTableName, str);
                    }
                    _configManager.ConfigInfo.CardTableUpdateTime = (string)mJObj["receiveTime"];
                }
                else
                {
                    Log.LogInstance.Write("获取IC卡数据失败！", MessageType.Error);
                    //MessageBox.Show("获取IC卡数据失败！");
                }
            }
            catch (Exception ex)
            {
                Log.LogInstance.Write("获取IC卡数据发生异常！DownloadCardInfo:" + ex.Message, MessageType.Error);
                //MessageBox.Show("获取IC卡数据发生异常！DownloadCardInfo:" + ex.Message);
            }
        }

        private void DownloadFile(string fileName, string deFilePath)
        {
            MethodInfo method = _terminalServiceClientType.GetMethod("updatefile");
            string[] parameter = { "{\"condition\":{\"updatefilename\":\"" + fileName + "\"}}" };
            string fileRet = (string)method.Invoke(_terminalServiceClientInstance, parameter);
            var mJObj = JObject.Parse(fileRet);
            if (!((string)mJObj["returnMessage"]).Equals("success"))
            {
                return;
            }
            string rt = (string)((JObject)mJObj["result"])["updatefile"];
            byte[] Pbytes = Convert.FromBase64String(rt);

            //string updatePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName.Replace(System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName, "") + "Update";
            if (!Directory.Exists(deFilePath))
            {
                Directory.CreateDirectory(deFilePath);
            }
            string updateXmlPath = deFilePath + @"\" + fileName;
            //File.Create(updateXmlPath);
            FileStream fs = new FileStream(updateXmlPath, FileMode.Create);
            BinaryWriter bw = new BinaryWriter(fs,Encoding.Unicode);
            bw.Write(Pbytes);
            fs.Flush();
            bw.Flush();
            bw.Close();
            fs.Close();
        }

        public bool UpdateApplication()
        {
            try
            {
                string newVerson = string.Empty;
                string fileName = string.Empty;
                string newVersonDefault = string.Empty;
                string fileNameDefault = string.Empty;
                string updatePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName.Replace(System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName, "") + "Update";
                DownloadFile("update.xml", updatePath);
                FileStream fss = new FileStream(updatePath + @"\update.xml" , FileMode.Open);//pdateXmlPath, FileMode.Open);//fileRet由base64Binary转成stream   

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(fss);
                XmlNode list = xmlDoc.SelectSingleNode("Update");
                foreach (XmlNode node in list)
                {
                    if (node.Name == "Institution")
                    {
                        if (node.Attributes["ID"].Value.ToLower() == _configManager.ConfigInfo.InstitutionID.ToLower())
                        {
                            foreach (XmlNode xml in node)
                            {
                                if (xml.Name == "Verson")
                                    newVerson = xml.InnerText;
                                else
                                    fileName = xml.InnerText;
                            }
                            break;
                        }
                        else if (node.Attributes["ID"].Value.ToLower() == "default")
                        {
                            foreach (XmlNode xml in node)
                            {
                                if (xml.Name == "Verson")
                                    newVersonDefault = xml.InnerText;
                                else
                                    fileNameDefault = xml.InnerText;
                            }
                            //break;
                        }
                    }
                }
                fss.Close();
                //无匹配版本号则为通用版本
                if (!string.IsNullOrEmpty(newVerson) && !string.IsNullOrEmpty(fileName))
                {
                    string versionFilePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName.Replace(System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName, "") + "Version.xml";

                    XmlDocument xmlDocument = new XmlDocument();
                    xmlDocument.Load(versionFilePath);
                    if (xmlDocument == null)
                    {
                        return false;
                    }
                    XmlElement xmlElement = xmlDocument.DocumentElement;
                    if (xmlElement == null)
                    {
                        return false;
                    }

                    string version = xmlElement.SelectSingleNode("Version").InnerText;

                    Version newver = new Version(newVerson);
                    Version oldver = new Version(version);

                    int tm = newver.CompareTo(oldver);

                    if (tm > 0)
                    {
                        DownloadFile(fileName, updatePath);

                        return true;
                        //save as zip file to update folder
                        //launch update.exe(version)->sleep->upzip->copy to local->delete update folder->update version.xml->launch keeper
                        //shut down and kill keeper
                    }
                }
                else if (!string.IsNullOrEmpty(newVersonDefault) && !string.IsNullOrEmpty(fileNameDefault))
                {
                    string versionFilePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName.Replace(System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName, "") + "Version.xml";

                    XmlDocument xmlDocument = new XmlDocument();
                    xmlDocument.Load(versionFilePath);
                    if (xmlDocument == null)
                    {
                        return false;
                    }
                    XmlElement xmlElement = xmlDocument.DocumentElement;
                    if (xmlElement == null)
                    {
                        return false;
                    }

                    string version = xmlElement.SelectSingleNode("Version").InnerText;

                    Version newver = new Version(newVersonDefault);
                    Version oldver = new Version(version);

                    //主版本号相同才会判断更新
                    if (newver.Major == oldver.Major)
                    {
                        int tm = newver.CompareTo(oldver);

                        if (tm > 0)
                        {
                            DownloadFile(fileNameDefault, updatePath);
                            //save as zip file to update folder
                            //launch update.exe(version)->sleep->upzip->copy to local->delete update folder->update version.xml->launch keeper
                            //shut down and kill keeper
                            //DownloadFile(fileName, updatePath);
                            return true;
                        }
                        else
                        {
                            DeleteFolder(updatePath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogInstance.Write("升级程序发生异常！UpdateApplication:" + ex.Message, MessageType.Error);
                //MessageBox.Show("升级程序发生异常！UpdateApplication:" + ex.Message);
            }
            return false;
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

        private void GetMethod()
        {
            ServiceDescriptionImporter importer = new ServiceDescriptionImporter();//创建客户端代理代理类。这个需要把项目的目标框架改为.net framework4

            importer.ProtocolName = "Soap";//指定访问协议
            importer.Style = ServiceDescriptionImportStyle.Client;//生成客户端代理。
            importer.CodeGenerationOptions = CodeGenerationOptions.GenerateProperties | CodeGenerationOptions.GenerateNewAsync;

            //importer.AddServiceDescription(description, null, null);//添加WSDL文档

            CodeNamespace nmspace = new CodeNamespace();//命名空间
            nmspace.Name = "SwipCardSystem.Controller";//这里是引用服务后本地自定义的命名空间（55行所用跟此相同）
            CodeCompileUnit unit = new CodeCompileUnit();
            unit.Namespaces.Add(nmspace);

            ServiceDescriptionImportWarnings warning = importer.Import(nmspace, unit);
            CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");

            CompilerParameters parameter = new CompilerParameters();
            parameter.GenerateExecutable = false;

            parameter.OutputAssembly = "WebServiceManager.dll";//输出程序集的名称
            parameter.ReferencedAssemblies.Add("System.dll");
            parameter.ReferencedAssemblies.Add("System.XML.dll");
            parameter.ReferencedAssemblies.Add("System.Web.Services.dll");
            parameter.ReferencedAssemblies.Add("System.Data.dll");

            CompilerResults result = provider.CompileAssemblyFromDom(parameter, unit);
            if (!result.Errors.HasErrors)
            {
                Assembly asm = Assembly.LoadFrom("MyTest.dll");//加载前面生成的程序集
                Type t = asm.GetType("SwipCardSystem.Controller.WebServiceManager");//格式为：命名空间（33行定义的）.类名

                object o = Activator.CreateInstance(t);
                MethodInfo method = t.GetMethod("GetPersons");//GetPersons是服务端的方法名称，你想调用服务端的什么方法都可以在这里改，最好封闭一下
                String[] item = (String[])method.Invoke(o, null);
                //注：method.Invoke(o,null)返回的是一个Object，如果你服务端返回的是DataSet，这里也是用(DataSet)method.Invoke(o,null)转一下就行了
                foreach (string str in item)
                {
                    Console.WriteLine(str);
                }
            }

            //上面是根据WebService地址，模拟生成的一个代理类，如果你想看看生成的代码文件是什么样子，可以用以下代码保存下来，默认是保存在 bin目录下面
            TextWriter writer = File.CreateText("WebServiceManager.cs");
            provider.GenerateCodeFromCompileUnit(unit, writer, null);
            writer.Flush();
            writer.Close();
        }


    }
}
