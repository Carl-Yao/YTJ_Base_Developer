using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Windows;
using SwipCardSystem.Medol;
using MySql.Data.MySqlClient;

namespace SwipCardSystem.Controller
{
    public class MySqlManager
    {
        private static MySqlManager _mySqlMagager = null;
        private ConfigManager _configManager = null;
        private static object _object = new object();
        private bool _isInitilize = false;
        //private DataSet _kaoqinDataSet = null;
        //private DataSet _cardDataSet = null;
        //private DataSet _familyDataSet = null;
        //private DataSet _institutionDataSet = null;
        //private DataSet _studentDataSet = null;
        public bool IsInitilize
        {
            get
            {
                return _isInitilize;
            }
        }
        MySqlManager()
        {
            Initilize();
        }

        public static MySqlManager CreateSingleton()
        {
            lock (_object)
            {
                if (_mySqlMagager == null)
                {
                    _mySqlMagager = new MySqlManager();
                }
            }
            return _mySqlMagager;
        }

        public void CreateDataTable(bool isOnlyCreateKaoqinTable)
        {
            try
            {
                if (!isOnlyCreateKaoqinTable)
                {
                    try
                    {
                        MySqlHelper.ExecuteNonQuery(MySqlHelper.Conn, CommandType.Text, "select * from " + _configManager.ConfigInfo.InstitutionTableName, null);
                    }
                    catch (MySqlException ex)
                    {
                        switch (ex.Number)
                        {
                            case 1146:
                                //group_id  VARCHAR(16), group_name VARCHAR(100), group_type VARCHAR(2), parent_groupid VARCHAR(16),area_id VARCHAR(16), yxbz VARCHAR(2)
                                MySqlHelper.ExecuteNonQuery(MySqlHelper.Conn, CommandType.Text, @"create table " + _configManager.ConfigInfo.InstitutionTableName + "( group_id  VARCHAR(32), group_name VARCHAR(100), group_type VARCHAR(2), parent_groupid VARCHAR(32),area_id VARCHAR(32))", null);
                                break;
                            default:
                                Log.LogInstance.Write(ex.Message, MessageType.Error);
                                //MessageBox.Show(ex.Message);
                                break;
                        }
                    }
                    //other data table
                    try
                    {
                        MySqlHelper.ExecuteNonQuery(MySqlHelper.Conn, CommandType.Text, "select * from " + _configManager.ConfigInfo.StudentTableName, null);
                    }
                    catch (MySqlException ex)
                    {
                        switch (ex.Number)
                        {
                            case 1146:
                                //(STUDENT_ID VARCHAR(16), XJH VARCHAR(16),STUDENT_NO VARCHAR(16),STUDENT_NAME VARCHAR(30),SEX VARCHAR(2), BIRTHDAY VARCHAR(16),NATION VARCHAR(16),ORIGIN VARCHAR(20),ENTRANCE_DATE VARCHAR(16),GROUP_ID VARCHAR(16),XJ_STATE VARCHAR(2),YXBZ VARCHAR(2))
                                MySqlHelper.ExecuteNonQuery(MySqlHelper.Conn, CommandType.Text, @"create table " + _configManager.ConfigInfo.StudentTableName + "(STUDENT_ID VARCHAR(32), XJH VARCHAR(32),STUDENT_NO VARCHAR(32),STUDENT_NAME VARCHAR(30),SEX VARCHAR(2), BIRTHDAY VARCHAR(32),ENTRANCE_DATE VARCHAR(32),GROUP_ID VARCHAR(32),XJ_STATE VARCHAR(2),YXBZ VARCHAR(2), is_go_school VARCHAR(1))", null);
                                break;
                            default:
                                Log.LogInstance.Write(ex.Message, MessageType.Error);
                                //MessageBox.Show(ex.Message);
                                break;
                        }
                    }
                    try
                    {
                        MySqlHelper.ExecuteNonQuery(MySqlHelper.Conn, CommandType.Text, "select * from " + _configManager.ConfigInfo.FamilyTableName, null);
                    }
                    catch (MySqlException ex)
                    {
                        switch (ex.Number)
                        {
                            case 1146:
                                //STUDENT_ID VARCHAR(16), USER_ID VARCHAR(16),RELATIONSHIP VARCHAR(16),NAME VARCHAR(30),SEX VARCHAR(2), CARDID VARCHAR(26),TELEPHONE VARCHAR(16),MOBILE VARCHAR(16),EMAIL VARCHAR(26),QQ VARCHAR(16), ADDRESS VARCHAR(100), RIGION VARCHAR(100), COMPANY VARCHAR(100),POST VARCHAR(36),USER_TYPE VARCHAR(36)
                                MySqlHelper.ExecuteNonQuery(MySqlHelper.Conn, CommandType.Text, @"create table " + _configManager.ConfigInfo.FamilyTableName + "(STUDENT_ID VARCHAR(32), USER_ID VARCHAR(32), RELATIONSHIP VARCHAR(32),NAME VARCHAR(32),SEX VARCHAR(2),PICTURE VARCHAR(150))", null);
                                break;
                            default:
                                Log.LogInstance.Write(ex.Message, MessageType.Error);
                                //MessageBox.Show(ex.Message);
                                break;
                        }
                    }
                    try
                    {
                        MySqlHelper.ExecuteNonQuery(MySqlHelper.Conn, CommandType.Text, "select * from " + _configManager.ConfigInfo.CardTableName, null);
                    }
                    catch (MySqlException ex)
                    {
                        switch (ex.Number)
                        {
                            case 1146:
                                //ICCARD_ID VARCHAR(16), ICCARD_NO(16),STUDENT_ID VARCHAR(16),USER_ID VARCHAR(16),EQUP_ID VARCHAR(16), YXBZ VARCHAR(2)
                                MySqlHelper.ExecuteNonQuery(MySqlHelper.Conn, CommandType.Text, @"create table " + _configManager.ConfigInfo.CardTableName + "(ICCARD_ID VARCHAR(32), ICCARD_NO VARCHAR(32),STUDENT_ID VARCHAR(32), USER_ID VARCHAR(32), YXBZ VARCHAR(2))", null);
                                break;
                            default:
                                Log.LogInstance.Write(ex.Message, MessageType.Error);
                                //MessageBox.Show(ex.Message);
                                break;
                        }
                    }
                }
                else
                {
                    try
                    {
                        MySqlHelper.ExecuteNonQuery(MySqlHelper.Conn, CommandType.Text, "select * from " + _configManager.ConfigInfo.KaoqinTableName, null);
                    }
                    catch (MySqlException ex)
                    {
                        switch (ex.Number)
                        {
                            case 1146:
                                MySqlHelper.ExecuteNonQuery(MySqlHelper.Conn, CommandType.Text, @"create table " + _configManager.ConfigInfo.KaoqinTableName + "(group_id VARCHAR(32), class_id VARCHAR(32), record_id VARCHAR(32),equp_id VARCHAR(32), iccard_id VARCHAR(32),iccard_no VARCHAR(32),student_id VARCHAR(32),template_val VARCHAR(10),record_time VARCHAR(20), picture VARCHAR(150), hasUpload VARCHAR(1))", null);
                                break;
                            default:
                                Log.LogInstance.Write(ex.Message, MessageType.Error);
                                //MessageBox.Show(ex.Message);
                                break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                //Log.LogInstance.Write("CreateDataTable:" + e.Message);
                Log.LogInstance.Write("CreateDataTable-->" + e.Message, MessageType.Error);
                //MessageBox.Show("CreateDataTable-->" + e.Message);
            }
        }

        public void AddDataToTable(string tableName, string[] datas)
        {
            string strData = string.Empty;
            foreach (var item in datas)
            {
                if (!string.IsNullOrEmpty(strData))
                {
                    strData += ",";
                }
                strData += "'" + item + "'";
            }
            MySqlHelper.ExecuteNonQuery(MySqlHelper.Conn, CommandType.Text, "INSERT INTO " + tableName + " VALUES (" + strData + ")", null);
        }

        //-1总数 0离校总数 1到校总数
        public string SumStudent(int byGoSchool)
        {
            string ret = "0";
            MySqlDataReader rd = null;
            try
            {
                
                switch (byGoSchool)
                {
                        //总人数
                    case -1:
                        rd =  MySqlHelper.ExecuteReader(MySqlHelper.Conn, CommandType.Text, "Select Count(1) FROM " + _configManager.ConfigInfo.StudentTableName, null);
                        break;
                        //未到人数
                    case 0:
                        rd =  MySqlHelper.ExecuteReader(MySqlHelper.Conn, CommandType.Text, "Select Count(1) FROM " + _configManager.ConfigInfo.StudentTableName + " WHERE is_go_school = 0", null);
                        break;
                        //已到人数
                    case 1:
                        rd =  MySqlHelper.ExecuteReader(MySqlHelper.Conn, CommandType.Text, "Select Count(1) FROM " + _configManager.ConfigInfo.StudentTableName + " WHERE is_go_school = 1", null);
                        break;
                    default:
                        break;
                }
                int studentNum = 0;
                if (rd != null)
                {
                    while (rd.Read())
                    {
                        studentNum = Convert.ToInt32((Int64)rd["Count(1)"]);
                    }
                    if (studentNum > 0)
                    {
                        ret = studentNum.ToString();
                    }
                    rd.Close();
                    MySqlHelper.conn.Close();
                }
            }
            catch (Exception ex)
            {
                //Log.LogInstance.Write("计算学生总数错误！SumStudent:" + ex.Message);
                Log.LogInstance.Write("计算学生总数错误！SumStudent-->" + ex.Message, MessageType.Error);
                //MessageBox.Show("计算学生总数错误！SumStudent-->" + ex.Message);
            }
            return ret;
        }

        public bool ClearStudentGoSchoolNum()
        {
            MySqlHelper.ExecuteNonQuery(MySqlHelper.Conn, CommandType.Text, @"update " + _configManager.ConfigInfo.StudentTableName + " set is_go_school=0", null);
            return true;
        }

        public bool IsTeacherRecord(string cardNo)
        {
            MySqlDataReader rd = MySqlHelper.ExecuteReader(MySqlHelper.Conn, CommandType.Text, "select * from tb_card where tb_card.ICCARD_NO = " + cardNo, null);
            //string groupId = string.Empty;
            bool isMatch = false;
            while (rd.Read())
            {
                try
                {
                    if (rd["STUDENT_ID"] is string && string.IsNullOrEmpty(rd["STUDENT_ID"] as string))
                    {
                        isMatch = true;
                    }
                }
                catch
                {
                    rd.Close();
                    MySqlHelper.conn.Close();
                }
                
            }
            rd.Close();
            MySqlHelper.conn.Close();
            return isMatch;
        }

        public bool GetOtherInfoByCardNo(string cardNo, ref string userId, ref string studentName, ref string cardId, ref string studentNO, ref string studentId, ref string studentGroup, ref string[] parentNames, ref string[] parentRelationships, ref string[] picturePath, ref string goSchoolStudentNum, ref string classId)
        {
            bool bRet = true;
            try
            {
                //3931340327                
                MySqlDataReader rd = MySqlHelper.ExecuteReader(MySqlHelper.Conn, CommandType.Text, "select tb_student.GROUP_ID, tb_student.STUDENT_ID, ICCARD_ID, USER_ID, STUDENT_NAME,STUDENT_NO,GROUP_ID from tb_card left join tb_student on tb_card.STUDENT_ID = tb_student.STUDENT_ID where tb_card.ICCARD_NO = " + cardNo, null);
                //string groupId = string.Empty;
                bool isMatch = false;
                while (rd.Read())
                {
                    isMatch = true;
                    try
                    {
                        userId = (string)rd["USER_ID"];
                    }
                    catch
                    {
                    }
                    if (rd["STUDENT_NAME"] is string)
                    {
                        studentName = (string)rd["STUDENT_NAME"];
                    }
                    else
                    {
                        isMatch = false;
                        break;
                    }
                    if (rd["STUDENT_NO"] is string)
                    studentNO = (string)rd["STUDENT_NO"];
                    if (rd["GROUP_ID"] is string)
                    classId = (string)rd["GROUP_ID"];
                    if (rd["STUDENT_ID"] is string)
                    studentId = (string)rd["STUDENT_ID"];
                    if (rd["ICCARD_ID"] is string)
                    cardId = (string)rd["ICCARD_ID"];
                }               

                rd.Close();
                MySqlHelper.conn.Close();
                if (!isMatch)
                {
                    MySqlDataReader rd1 = MySqlHelper.ExecuteReader(MySqlHelper.Conn, CommandType.Text, "select tb_student.GROUP_ID, tb_student.STUDENT_ID, ICCARD_ID, USER_ID, STUDENT_NAME,STUDENT_NO,GROUP_ID from tb_card left join tb_student on tb_card.USER_ID = tb_student.STUDENT_ID where tb_card.ICCARD_NO = " + cardNo, null);
                    //string groupId = string.Empty;

                    while (rd1.Read())
                    {
                        isMatch = true;
                        try
                        {
                            userId = (string)rd["USER_ID"];
                        }
                        catch
                        {
                        }
                        studentName = (string)rd1["STUDENT_NAME"];
                        studentNO = (string)rd1["STUDENT_NO"];
                        classId = (string)rd1["GROUP_ID"];
                        studentId = (string)rd1["STUDENT_ID"];
                        cardId = (string)rd1["ICCARD_ID"];
                    }

                    rd1.Close();
                    MySqlHelper.conn.Close();
                }
                if (!isMatch)
                {
                    VoiceManager.Speak("");
                    VoiceManager.Beep();
                    VoiceManager.Beep();
                    VoiceManager.Speak("无效卡！");
                    return false;
                }

                MySqlDataReader rdd = MySqlHelper.ExecuteReader(MySqlHelper.Conn, CommandType.Text, "select group_name from tb_institution where group_id = " + classId, null);
                while (rdd.Read())
                {
                    studentGroup = (string)rdd["group_name"];
                }
                rdd.Close();
                MySqlHelper.conn.Close();

                if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(studentId))
                {
                    return true;
                }

                MySqlHelper.ExecuteNonQuery(MySqlHelper.Conn, CommandType.Text, @"update " + _configManager.ConfigInfo.StudentTableName + " set is_go_school=1 where STUDENT_ID=" + studentId, null);
                     
                goSchoolStudentNum = SumStudent(1);
               
                MySqlDataReader rddd = MySqlHelper.ExecuteReader(MySqlHelper.Conn, CommandType.Text, "select RELATIONSHIP,NAME, PICTURE from tb_family where STUDENT_ID = " + studentId, null);
                int i = 0;
                while (rddd.Read())
                {
                    parentNames[i] = (string)rddd["NAME"];
                    parentRelationships[i] = (string)rddd["RELATIONSHIP"];
                    picturePath[i++] = (string)rddd["PICTURE"];
                }
                rddd.Close();
                MySqlHelper.conn.Close();
            }
            catch (Exception ex)
            {
                bRet = false;

                Log.LogInstance.Write("根据考勤卡号获取其他信息时出错！GetOtherInfoByCardNo-->" + ex.Message, MessageType.Warning);

                //MessageBox.Show("根据考勤卡号获取其他信息时出错！GetOtherInfoByCardNo-->" + ex.Message);
            }
            return bRet;
        }

        public bool GetAllClass(ref List<string> groupList)
        {
            bool bRet = true;
            try
            {
                MySqlDataReader rd = MySqlHelper.ExecuteReader(MySqlHelper.Conn, CommandType.Text, "select * from tb_institution where group_type=3", null);
                string groupId = string.Empty;
                if (rd != null)
                {
                    while (rd.Read())
                    {
                        if (((string)rd["group_id"]).StartsWith(_configManager.ConfigInfo.InstitutionID))
                        {
                            groupList.Add((string)rd["group_name"]);
                        }
                    }
                    rd.Close();
                    MySqlHelper.conn.Close();
                }
            }
            catch
            {

            }
            return bRet;
        }

        public bool GetAllKaoqinRecord(ref List<KaoqinInfo> kaoqins)
        {
            bool bRet = true;
            try
            {
                MySqlDataReader rd = MySqlHelper.ExecuteReader(MySqlHelper.Conn, CommandType.Text, "select * from tb_kaoqin", null);
                string groupId = string.Empty;
                if (rd != null)
                {
                    while (rd.Read())
                    {
                        KaoqinInfo kaoqinInfo = new KaoqinInfo();
                        kaoqinInfo.RecordId = (string)rd["record_id"];
                        kaoqinInfo.ClassId = (string)rd["class_id"];
                        kaoqinInfo.EqupId = (string)rd["equp_id"];
                        kaoqinInfo.ICCardId = (string)rd["iccard_id"];
                        kaoqinInfo.ICCardNo = (string)rd["iccard_no"];
                        kaoqinInfo.StudentID = (string)rd["STUDENT_ID"];
                        kaoqinInfo.TemplateVal = (string)rd["template_val"];
                        kaoqinInfo.RecordTime = (string)rd["record_time"];
                        kaoqinInfo.PicturePath = (string)rd["picture"];
                        kaoqins.Add(kaoqinInfo);
                    }
                    rd.Close();
                    MySqlHelper.conn.Close();
                }
            }
            catch
            {

            }
            return bRet;
        }

        public bool SaveKaoqinRecord(KaoqinInfo kaoqinInfo)
        {
            bool bRes = false;

            MySqlHelper.ExecuteNonQuery(MySqlHelper.Conn, CommandType.Text, @"INSERT INTO " + _configManager.ConfigInfo.KaoqinTableName + " VALUES('" + _configManager.ConfigInfo.InstitutionID
                + "', '" + kaoqinInfo.ClassId + "', '" + kaoqinInfo.RecordId + "', '" + kaoqinInfo.EqupId + "', '" + kaoqinInfo.ICCardId
                +"', '" + kaoqinInfo.ICCardNo + "', '" + kaoqinInfo.StudentID + "', '" + (string.IsNullOrEmpty(kaoqinInfo.TemplateVal)? "0":kaoqinInfo.TemplateVal)
                +"', '" + kaoqinInfo.RecordTime + "', '" + kaoqinInfo.PicturePath + "', '" + "0" + "')", null);
            
            return bRes;
        }

        public bool ClearDataTable(bool isOnlyDropKaoqinTable)
        {

            string[] tableNemes;
            if (isOnlyDropKaoqinTable)
            {
                tableNemes = new string[] { "tb_kaoqin" };
            }
            else
            {
                tableNemes = new string[] { "tb_student", "tb_card", "tb_family", "tb_institution" };
            }
            foreach (string tableName in tableNemes)
            {
                try
                {
                    MySqlHelper.ExecuteNonQuery(MySqlHelper.Conn, CommandType.Text, "DROP TABLE " + tableName, null);
                }
                catch (Exception e)
                {
                    //Log.LogInstance.Write("删除数据表失败！" + e.Message);
                    //VoiceManager.Speak("删除数据表失败！");
                    Log.LogInstance.Write("删除数据表失败！" + e.Message, MessageType.Error);
                    //MessageBox.Show("删除数据表失败！" + e.Message);
                    return false;
                }
            }

            return true;

        }

        public bool DeleteOneRecordData(string recordID)
        {
            try//delete from tablename where fieldname1 = '16469' or fieldname2 = '2013-09-21' or fieldname3 is null提问者评价 谢谢!
            {
                MySqlHelper.ExecuteNonQuery(MySqlHelper.Conn, CommandType.Text, "delete from tb_kaoqin where record_id = " + recordID, null);
            }
            catch (Exception e)
            {
                //Log.LogInstance.Write("删除数据表失败！" + e.Message);
                //VoiceManager.Speak("删除数据表失败！");
                Log.LogInstance.Write("DeleteOneRecordData---删除数据失败！" + e.Message, MessageType.Error);
                //MessageBox.Show("删除数据表失败！" + e.Message);
                return false;
            }

            return true;

        }

        internal void Initilize()
        {
            if (!_isInitilize)
            {
                _configManager = ConfigManager.CreateSingleton();
                //CreateDataTable();
                //先注释掉
                //GetKaoqinDataSet();
                _isInitilize = true;
            }
        }
    }   
}
