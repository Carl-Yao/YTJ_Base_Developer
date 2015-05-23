using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.NetworkInformation;
using System.Threading;

namespace SwipCardSystem.Controller
{
    public class NetworkManager
    {        
        public static bool IsInterentConnected()
        {
            bool isInternetConnect = true;
            try
            {
                Ping p = new Ping();//创建Ping对象p            
                PingReply pr = p.Send("www.baidu.com");//
                if (pr.Status != IPStatus.Success)//如果ping失败
                {
                    int times = 0;//重新连接次数;
                    do
                    {
                        if (times >= 5)
                        {
                            //Console.WriteLine("重新尝试连接超过12次,连接失败程序结束");
                            isInternetConnect = false;
                            break;
                        }
                        Thread.Sleep(200);//等待十分钟(方便测试的话，你可以改为1000)         
                        pr = p.Send("www.baidu.com");
                        //Console.WriteLine(pr.Status);               
                        times++;
                    }
                    while (pr.Status != IPStatus.Success);
                    //Console.WriteLine("连接成功");               
                    times = 0;//连接成功，重新连接次数清为0;            
                }
            }
            catch
            {
                isInternetConnect = false;
            }
            return isInternetConnect;
        }
    }
}
