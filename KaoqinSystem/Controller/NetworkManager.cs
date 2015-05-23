using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace SwipCardSystem.Controller
{
    public class NetworkManager
    {
        private static object _single = new object();
        public delegate void MyValueChanged(object sender, EventArgs e);
        private NetworkManager()
        {
            JudgeInterentConnected();
            Task.Factory.StartNew(()=>
                {
                    while (true)
                    {
                        lock (_object)
                        {
                            Thread.Sleep(60000);
                           JudgeInterentConnected();
                        }
                    }
                });
            
        }

        private object _object = new object();
        private static NetworkManager _networkManger = null;

        public static NetworkManager CreateSingleton()
        {
            lock (_single)
            {
                if (_networkManger == null)
                {
                    _networkManger = new NetworkManager();
                }
            }
            return _networkManger;
        }

        private bool _isInternetConnecting = false;
        public bool IsInternetConnecting
        {
            get
            {
                return _isInternetConnecting;
            }
        }        

        public event MyValueChanged OnMyValueChanged;

        private void JudgeInterentConnected()
        {
            bool isInternetConnect = false;
            try
            {
                Ping p = new Ping();//创建Ping对象p            
                PingReply pr = null;//p.Send("www.baidu.com");//
                //if (pr.Status != IPStatus.Success)//如果ping失败
                {
                    int times = 0;//重新连接次数;
                    do
                    {
                        try
                        {
                            if (times >= 3)
                            {
                                //Console.WriteLine("重新尝试连接超过12次,连接失败程序结束");
                                isInternetConnect = false;
                                break;
                            }  
                            if (times == 2)
                            {
                                pr = p.Send("www.hao123.com");
                            }
                            else if (times == 1)
                            {
                                pr = p.Send("www.mecsp.net");
                            }
                            else
                            {
                                pr = p.Send("www.baidu.com");
                            }
                        }
                        catch
                        {
                            Thread.Sleep(1000);
                        }
                        //Console.WriteLine(pr.Status);               
                        times++;
                    }
                    while (pr == null || pr.Status != IPStatus.Success);
                    if (pr != null && pr.Status == IPStatus.Success)
                    {
                        isInternetConnect = true;
                    }
                    //Console.WriteLine("连接成功");               
                    times = 0;//连接成功，重新连接次数清为0;            
                }
            }
            catch (Exception ex)
            {
                isInternetConnect = false;
            }
            if (!_isInternetConnecting && isInternetConnect && OnMyValueChanged != null)
            {
                _isInternetConnecting = isInternetConnect;
                //触发网络连接上了事件
                OnMyValueChanged(this, null);
            }
            else
            {
                _isInternetConnecting = isInternetConnect;
            }
        }
    }
}
