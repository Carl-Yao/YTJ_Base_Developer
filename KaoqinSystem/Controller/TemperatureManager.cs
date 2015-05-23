using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic; 
using System.Linq; 

namespace SwipCardSystem.Controller
{
    public delegate void UpdateTemeratureHandler();

    public class TemperatureManager : IDisposable
    {
        public TemperatureManager()
        {
            TemperatureMatch = new Dictionary<string, string> { { "1", "1" }, { "2", "2" }, { "3", "3" }, { "4", "4" }, { "5", "5" }, { "6", "6" }, { "7", "7" }, { "8", "8" }, { "9", "9" }, { "10", "0" }, { "11", "1" }, { "12", "2" }, { "13", "3" }, { "14", "4" }, { "15", "5" }, { "16", "6" }, { "204", "0" }, { "246","0" } };
        }

        private Dictionary<string, string> TemperatureMatch = null;

        private System.IO.Ports.SerialPort serialPort1 = new System.IO.Ports.SerialPort();

        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        //定义事件
        private event UpdateTemeratureHandler temeratureChange;

        private bool isDisposed = false; // 是否已释放资源的标志

        private string _temperatureValue = string.Empty;
        public string TemperatureValue
        {
            get
            {
                return _temperatureValue;
            }
        }
        //load完界面后调用比较好
        public bool Initilize(UpdateTemeratureHandler temperatureUpdate)
        {
            bool bRes = false;
            this.components = new System.ComponentModel.Container();
            this.serialPort1 = new System.IO.Ports.SerialPort(this.components);

            ////////////初始化端口
            if (serialPort1.IsOpen)
            {
                serialPort1.Close();
            }
            //设置波特率为9600
            serialPort1.BaudRate = 9600;
            
            //设置 DataReceived 事件发生前内部输入缓冲区中的字节数为8
            serialPort1.ReceivedBytesThreshold = 8;
            //将事件处理方法添加到事件中去
            temeratureChange += new UpdateTemeratureHandler(temperatureUpdate);
            int i = 6;
            string[] comPorts = System.IO.Ports.SerialPort.GetPortNames();
            while (true)
            {
                try
                {
                    if (i < 2)
                    {
                        break;
                    }
                    
                    //设置端口号，这里使用COM3-4端口
                    serialPort1.PortName = "COM" + i.ToString();

                    serialPort1.Open();
                    bRes = true;
                    Log.LogInstance.Write(i.ToString(), MessageType.Information);
                    break;
                }
                catch (Exception ex)
                {
                    bRes = false;
                    i--;
                    //Log.LogInstance.Write("未能连接体温计，请检查是否已经连接串口.\n" + ex.Message);
                }
            }
            this.serialPort1.DataReceived -= new System.IO.Ports.SerialDataReceivedEventHandler(this.serialPort1_DataReceived);
            this.serialPort1.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(this.serialPort1_DataReceived);

            this.serialPort1.DtrEnable = true;
            this.serialPort1.RtsEnable = true;
            return bRes;
        }

        public void Dispose()
        {
            Dispose(true);// 释放托管和非托管资源

            //将对象从垃圾回收器链表中移除，
            // 从而在垃圾回收器工作时，只释放托管资源，而不执行此对象的析构函数
            GC.SuppressFinalize(this);
        }

        //由垃圾回收器调用，释放非托管资源
        ~TemperatureManager()
        {
            Dispose(false);// 释放非托管资源
        }

        //参数为true表示释放所有资源，只能由使用者调用
        //参数为false表示释放非托管资源，只能由垃圾回收器自动调用
        //如果子类有自己的非托管资源，可以重载这个函数，添加自己的非托管资源的释放
        //但是要记住，重载此函数必须保证调用基类的版本，以保证基类的资源正常释放
        protected virtual void Dispose(bool disposing)
        {
            if (!this.isDisposed)// 如果资源未释放 这个判断主要用了防止对象被多次释放
            {
                if (disposing)
                {
                    //Comp.Dispose();// 释放托管资源
                }

                if (disposing && (components != null))
                {
                    components.Dispose();
                }

                //closeHandle(handle);// 释放非托管资源
                // handle= IntPtr.Zero;
            }

            this.isDisposed = true; // 标识此对象已释放
        }

        /// <summary>
        /// 接收数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void serialPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            bool b = serialPort1.IsOpen;
            if (!b)
            {
                return;
            }
            int bytes = serialPort1.BytesToRead;
            byte[] receivedData = new byte[bytes];
            Thread.Sleep(300);
            serialPort1.Read(receivedData, 0, bytes);

            // this.textBox1.Text = byteToHexStr(receivedData);

            _temperatureValue = byteToHexStr(receivedData);
            Log.LogInstance.Write(_temperatureValue, MessageType.Information);
            temeratureChange();
            serialPort1.DiscardInBuffer();            
        }
        private string byteToHexStr(byte[] byteDate)
        {
            try
            {
                Log.LogInstance.Write("in",MessageType.Information);
                if (byteDate[0]!= 2)
                {
                    return TemperatureMatch[byteDate[4].ToString()] + TemperatureMatch[byteDate[5].ToString()] + "." + TemperatureMatch[byteDate[7].ToString()];
                }
                else
                {
                    Log.LogInstance.Write("else", MessageType.Information);
                    double tw = ((((byteDate[2] - 48) * 100 + (byteDate[3] - 48) * 10 + (byteDate[4] - 48) + (byteDate[5] - 48) * 0.1) - 32) / 1.8 - 0.05);//.ToString("0.0");
                    Log.LogInstance.Write(tw.ToString(), MessageType.Information);
                    if (tw > 100 || tw < 0)
                    {
                        Log.LogInstance.Write("tw>100", MessageType.Information);
                        tw = ((((byteDate[2] - 112) * 100 + (byteDate[3] - 112) * 10 + (byteDate[4] - 112) + (byteDate[5] - 112) * 0.1) - 32) / 1.8 - 0.05);
                        Log.LogInstance.Write(tw.ToString(), MessageType.Information);
                    }
                    
                    return tw.ToString("0.0");//
                }
            }
            catch
            {
                Log.LogInstance.Write(byteDate.ToString(), MessageType.Error);
                return "0";
            }
        }
    }
}
