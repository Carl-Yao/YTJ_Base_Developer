using SwipCardSystem.Controller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApplication1
{

    public class DSAManager
    {
        Comm comm = null;
        bool is_read_card = true;
        bool sendCardToOut = false;
        string receive = string.Empty;
        public bool IsDisable
        {
            set
            {
                _isDisable = value;
            }
            get
            {
                return _isDisable;
            }
        }
        private bool _isDisable = false;

        private static DSAManager _dSAManager = null;
        private static object _object = new object();
        //private DataSet _kaoqinDataSet = null;
        //private DataSet _cardDataSet = null;
        //private DataSet _familyDataSet = null;
        //private DataSet _institutionDataSet = null;
        //private DataSet _studentDataSet = null;

        public static DSAManager CreateSingleton()
        {
            lock (_object)
            {
                if (_dSAManager == null)
                {
                    _dSAManager = new DSAManager();
                }
            }
            return _dSAManager;
        }

        DSAManager()
        {
            _isDisable = true;
            if (_isDisable)
            {
                return;
            }
             InitDSACom();
            OpenDSA();
            Thread.Sleep(1000);

            YinXiangClose();
        }

        ~DSAManager()
        {
            if (_isDisable)
            {
                return;
            }
            YinXiangClose();
           // Thread.Sleep(500);
            //das.SetLINEVolume(30);
            //das.
            CloseDSA();
        }

        public void SetHZ(double hz)
        {
            if (_isDisable)
            {
                return;
            }
            try
            {
                YinXiangClose();
                Thread.Sleep(500);
                //76M开始，0.2M递加，音响的背面第一位控制位，为0时不受控制，一直是打开的；为1时可用后7位控制所属频率，1~7（高位~地位）。 123位拨动(可用范围76-87优选,87-101.4广播常用频率段非优选)
                SetHertz(hz);
                Thread.Sleep(500);
                YinXiangOpen();
            }
            catch
            {
                Log.LogInstance.Write("设置音响频率失败！",MessageType.Error);
            }
        }
        public void InitDSACom()
        {
            comm = new Comm();
            //ConfigClass config = new ConfigClass();
            //comm.serialPort.PortName = config.ReadConfig("SendHealCard");
            //comm.serialPort.PortName = "COM3";
            //波特率
            comm.serialPort.BaudRate = 57600;
            //数据位
            comm.serialPort.DataBits = 8;
            //两个停止位
            comm.serialPort.StopBits = System.IO.Ports.StopBits.One;
            //无奇偶校验位
            comm.serialPort.Parity = System.IO.Ports.Parity.None;
            comm.serialPort.ReadTimeout = 100;
            comm.serialPort.WriteTimeout = -1;
            int i = 2;
            string[] comPorts = System.IO.Ports.SerialPort.GetPortNames();
            while (true)
            {
                try
                {
                    if (i > 6)
                    {
                        break;
                    }

                    //设置端口号，这里使用COM3-4端口
                    comm.serialPort.PortName = "COM" + i.ToString();

                    comm.Open();
                    //bRes = true;
                    Log.LogInstance.Write(i.ToString(), MessageType.Information);
                    break;
                }
                catch (Exception ex)
                {
                    //bRes = false;
                    i++;
                    //Log.LogInstance.Write("未能连接体温计，请检查是否已经连接串口.\n" + ex.Message);
                }
            }
            //comm.Open();
            if (comm.IsOpen)
            {
                comm.DataReceived += new Comm.EventHandle(comm_DataReceived);
            }           
        }

        #region Interface of DSA

        public bool CloseDSA()
        {
            byte[] parameter = new byte[3];
            parameter[0] = 0x01;
            parameter[1] = 0x01;
            parameter[2] = 0x02;
            SendCardToOut(parameter);
            return true;
        }

        public bool OpenDSA()
        {
            byte[] parameter = new byte[3];
            parameter[0] = 0x01;
            parameter[1] = 0x01;
            parameter[2] = 0x03;
            SendCardToOut(parameter);
            return true;
        }

        public bool LINESilence()
        {
            byte[] parameter = new byte[3];
            parameter[0] = 0x01;
            parameter[1] = 0x01;
            parameter[2] = 0x0A;
            SendCardToOut(parameter);
            return true;
        }

        public bool MICSilence()
        {
            return true;
        }

        public bool LINEOpen()
        {
            return true;
        }

        public bool MicOpen()
        {
            return true;
        }

        public bool HunXiangOpen()
        {
            return true;
        }

        public bool HunXiangClose()
        {
            return true;
        }
        public bool BeiGuangOpen()
        {
            byte[] parameter = new byte[3];
            parameter[0] = 0x01;
            parameter[1] = 0x01;
            parameter[2] = 0x0A;
            SendCardToOut(parameter);
            return true;
        }
        public bool BeiGuangClose()
        {
            byte[] parameter = new byte[3];
            parameter[0] = 0x01;
            parameter[1] = 0x01;
            parameter[2] = 0x0B;
            SendCardToOut(parameter);
            return true;
        }
        public bool YinXiangOpen()
        {
            byte[] parameter = new byte[3];
            parameter[0] = 0x01;
            parameter[1] = 0x01;
            parameter[2] = 0x0C;
            SendCardToOut(parameter);
            return true;
        }
        public bool YinXiangClose()
        {
            if (_isDisable)
            {
                return true;
            }
            byte[] parameter = new byte[3];
            parameter[0] = 0x01;
            parameter[1] = 0x01;
            parameter[2] = 0x0D;
            SendCardToOut(parameter);
            return true;
        }
        //hertzs单位MHz，760~1080
        //例如106.2MHz,->1062->转成16进制为04 26
        public bool SetHertz(Double hertzs)
        {
            byte[] parameter = new byte[5];
            parameter[0] = 0x01;
            parameter[1] = 0x03;
            parameter[2] = 0x0E;
            Int32 iHertzs = (Int32)(hertzs*10);
            byte[] bhertz = BitConverter.GetBytes(iHertzs);
            //string strHertzs = iHertzs.ToString("X4");
            //strHertzs.Substring(0, 2)
            //02F8~0438
            parameter[3] = bhertz[1];
            parameter[4] = bhertz[0];
            SendCardToOut(parameter);
            return true;
        }
        //0~30
        public bool SetLINEVolume(int iVolume)
        {
            byte[] parameter = new byte[4];
            parameter[0] = 0x01;
            parameter[1] = 0x02;
            parameter[2] = 0x0F;
            parameter[3] = BitConverter.GetBytes(iVolume)[0];
            SendCardToOut(parameter);
            return true;
        }
        public bool SetMICVolume(int iVolume)
        {
            byte[] parameter = new byte[4];
            parameter[0] = 0x01;
            parameter[1] = 0x02;
            parameter[2] = 0x10;
            parameter[3] = BitConverter.GetBytes(iVolume)[0];
            SendCardToOut(parameter);
            return true;
        }
        //0~30
        public bool SetTransmitterPower(int iPower)
        {
            byte[] parameter = new byte[4];
            parameter[0] = 0x01;
            parameter[1] = 0x02;
            parameter[2] = 0x11;
            parameter[3] = BitConverter.GetBytes(iPower)[0];
            SendCardToOut(parameter);
            return true;
        }
        public bool SetZhuBoBaoHu(byte[] hertzs)
        {
            return true;
        }
        public bool SetWenDuBaoHu(byte[] hertzs)
        {
            return true;
        }
        public bool QueryForwardPower()
        {
            return true;
        }
        public bool QueryReversePower()
        {
            return true;
        }
        public bool QueryZhuBoRatio()
        {
            return true;
        }
        public bool QueryWorkTemperature()
        {
            return true;
        }
        public bool QueryWorkState()
        {
            byte[] parameter = new byte[3];
            parameter[0] = 0x01;
            parameter[1] = 0x01;
            parameter[2] = 0x18;
            
            SendCardToOut(parameter);
            //OK+工作状态参数(1字节)
            return true;
        }
        public bool QuerySerialNumber()
        {
            byte[] parameter = new byte[3];
            parameter[0] = 0x01;
            parameter[1] = 0x01;
            parameter[2] = 0x19;
            SendCardToOut(parameter);
            //OK+序列号(7字节)
            return true;
        }
        public bool QueryVersion()
        {
            byte[] parameter = new byte[3];
            parameter[0] = 0x01;
            parameter[1] = 0x01;
            parameter[2] = 0x1A;
            SendCardToOut(parameter);
            //OK+版本号(2字节)
            return true;
        }
        public bool QueryType()
        {
            byte[] parameter = new byte[3];
            parameter[0] = 0x01;
            parameter[1] = 0x01;
            parameter[2] = 0x1B;
            SendCardToOut(parameter);
            //OK+机器型号(3字节)
            return true;
        }
        public bool QueryWorkHertz()
        {
            byte[] parameter = new byte[3];
            parameter[0] = 0x01;
            parameter[1] = 0x01;
            parameter[2] = 0x1C;
            SendCardToOut(parameter);
            //OK+DH+DL
            return true;
        }
        public bool QueryLINEVolume()
        {
            byte[] parameter = new byte[3];
            parameter[0] = 0x01;
            parameter[1] = 0x01;
            parameter[2] = 0x1D;
            SendCardToOut(parameter);
            //OK+LINE音量参数 （数据范围为：1H~1EH）1字节
            return true;
        }
        public bool QueryMICVolume()
        {
            byte[] parameter = new byte[3];
            parameter[0] = 0x01;
            parameter[1] = 0x01;
            parameter[2] = 0x1E;
            SendCardToOut(parameter);
            //OK+MIC音量参数 （数据范围为：1H~1EH）1字节
            return true;
        }
        public bool QuerySettingPower()
        {
            byte[] parameter = new byte[3];
            parameter[0] = 0x01;
            parameter[1] = 0x01;
            parameter[2] = 0x1F;
            SendCardToOut(parameter);
            //OK+设定的功率参数 （数据范围为：0H~96H）1字节
            return true;
        }
        public bool QueryTemperatureProtected()
        {
            return true;
        }
        public bool QueryZhuBoProtected()
        {
            return true;
        }
        public bool QueryVolumeProgressData()
        {
            return true;
        }
        #endregion

        /// <summary>
        /// 发送指令
        /// </summary>
        private void SendCardToOut(byte[] send)
        {
            is_read_card = false;
            sendCardToOut = true;
            //byte[] send = { 0x01, 0x01, 0x43, 0x34, 0x03, 0x30 };
            if (comm.IsOpen)
            {
                comm.WritePort(send, 0, send.Length);
            }
        }
        private void comm_DataReceived(byte[] readBuffer)
        {
            if (_isDisable)
            {
                return;
            }
            //log.Info(HexCon.ByteToString(readBuffer));
            //if (readBuffer1.Length == 1)
            //{
                receive = ByteToStr(readBuffer);
                //string str = "06";
                //if (string.Equals(receive.Trim(), str, StringComparison.CurrentCultureIgnoreCase))
                //{
                //    try
                //    {
                //        if (is_read_card)
                //        {
                //            byte[] send = new byte[1];
                //            send[0] = 0x05;
                //            comm.WritePort(send, 0, send.Length);
                //            Thread.Sleep(500);
                //            comm.DataReceived -= new Comm.EventHandle(comm_DataReceived);
                //            InitReadComm();
                //        }
                //if (sendCardToOut)
                //{
                //byte[] send = new byte[1];
                //send[0] = 0x05;
                //comm.WritePort(send, 0, send.Length);
                //readComm.DataReceived -= new Comm.EventHandle(readComm_DataReceived);
                //readComm.Close();

                //log.Info("发卡完成！");
                //lblMsg.Text = "发卡成功！";
                //lblSendCardMsg.Text = "发卡完成，请收好卡！";
                //timer1.Tick -= new EventHandler(timer1_Tick);
                //PlaySound();
                //this.btnOK.Enabled = true;
                //}
                //        }
                //        catch (Exception ex)
                //        {
                //            //log.Info(ex.ToString());
                //        }
                //    }
                //}
            //}
        }
        private string ByteToStr(byte[] byteDate)
        {
            byteDate[0].ToString();
            //4f 4b表示ok，说明线已连接，如果是频率在读ok后面的字符
            return "OK";
        }
    }
}
