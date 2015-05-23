using System;
using System.Threading;
using TimerTask;

 

 

namespace TimerTask
{


    /// <summary>
    /// 定时任务委托方法
    /// </summary>
    public delegate void TimerTaskDelegate();


    /// <summary>
    /// 有参数的定时任务委托方法
    /// </summary>
    public delegate void ParmTimerTaskDelegate(object[] parm);

 

    /// <summary>
    /// 定时任务接口类
    /// </summary>
    public interface ITimerTask
    {
        void Run();
    }

    /// <summary>
    /// 定时任务服务类
    /// 作者：Duyong 
    /// 编写日期：2010-07-25 
    ///</summary> 
    public class TimerTaskService
    {

        #region  定时任务实例成员

        private TimerInfo timerInfo;  //定时信息

        private DateTime NextRunTime;  //下一次执行时间

        private TimerTaskDelegate TimerTaskDelegateFun = null; //执行具体任务的委托方法

        private ParmTimerTaskDelegate ParmTimerTaskDelegateFun = null; //带参数的执行具体任务的委托方法
        private object[] parm = null; //参数

        private ITimerTask TimerTaskInstance = null; //执行具体任务的实例

        /// <summary>
        /// 根据定时信息构造定时任务服务
        /// </summary>
        /// <param name="_timer"></param>
        private TimerTaskService(TimerInfo _timer)
        {
            timerInfo = _timer;            
        }

        /// <summary>
        /// 根据定时信息和执行具体任务的实例构造定时任务服务
        /// </summary>
        /// <param name="_timer">定时信息</param>
        /// <param name="_interface">执行具体任务的实例</param>
        private TimerTaskService(TimerInfo _timer, ITimerTask _interface)
        {
            timerInfo = _timer;
            TimerTaskInstance = _interface;
        }

        /// <summary>
        /// 根据定时信息和执行具体任务的委托方法构造定时任务服务
        /// </summary>
        /// <param name="_timer">定时信息</param>
        /// <param name="trd">执行具体任务的委托方法</param>
        private TimerTaskService(TimerInfo _timer, TimerTaskDelegate trd)
        {
            timerInfo = _timer;
            TimerTaskDelegateFun = trd;
        }

        /// <summary>
        /// 根据定时信息和执行具体任务的委托方法构造定时任务服务
        /// </summary>
        /// <param name="_timer">定时信息</param>
        /// <param name="ptrd">带参数执行具体任务的委托方法</param>
        private TimerTaskService(TimerInfo _timer, ParmTimerTaskDelegate ptrd)
        {
            timerInfo = _timer;
            ParmTimerTaskDelegateFun = ptrd;
        }

        /// <summary>
        /// 设置参数
        /// </summary>
        /// <param name="_parm"></param>
        private void setParm(object[] _parm)
        {
            parm = _parm;
        }


        /// <summary>
        /// 启动定时任务
        /// </summary>
        public void Start()
        {
            //检查定时器
            CheckTimer(timerInfo);
        }


        /// <summary>
        /// 检查定时器
        /// </summary>
        private void CheckTimer(TimerInfo timerInfo)
        {

            //计算下次执行时间
            getNextRunTime();

            while (true)
            {
                DateTime DateTimeNow = DateTime.Now;

                //时间比较
                bool dateComp = DateTimeNow.Year == NextRunTime.Year && DateTimeNow.Month == NextRunTime.Month && DateTimeNow.Day == NextRunTime.Day;

                bool timeComp = DateTimeNow.Hour == NextRunTime.Hour && DateTimeNow.Minute == NextRunTime.Minute && DateTimeNow.Second == NextRunTime.Second;

                

                //如果当前时间等式下次运行时间,则调用线程执行方法
                if (dateComp && timeComp)
                {
                    //调用执行处理方法
                    if (TimerTaskDelegateFun != null)
                    {
                        TimerTaskDelegateFun();
                    }
                    else if (ParmTimerTaskDelegateFun != null)
                    {
                        ParmTimerTaskDelegateFun(parm);
                    }
                    else if (TimerTaskInstance != null)
                    {
                        TimerTaskInstance.Run();
                    }
                    else
                    {
                        Run();
                    }
                    

                    //重新计算下次执行时间
                    getNextRunTime();
                }
                Thread.Sleep(10);
            }

        }

        /// <summary>
        /// 执行方法
        /// </summary>
        protected void Run()
        {
            //TODO.....
           
        }


        /// <summary>
        /// 计算下一次执行时间
        /// </summary>
        /// <returns></returns>
        private void getNextRunTime()
        {
            DateTime now = DateTime.Now;
            int nowHH = now.Hour;
            int nowMM = now.Minute;
            int nowSS = now.Second;

            int timeHH = timerInfo.Hour;
            int timeMM = timerInfo.Minute;
            int timeSS = timerInfo.Second;

            //设置执行时间对当前时间进行比较
            bool nowTimeComp = nowHH < timeHH || (nowHH <= timerInfo.Hour && nowMM < timeMM) || (nowHH <= timerInfo.Hour && nowMM <= timeMM && nowSS < timeSS);


            //每天
            if ("EveryDay".Equals(timerInfo.TimerType))
            {

                if (nowTimeComp)
                {
                    NextRunTime = new DateTime(now.Year, now.Month, now.Day, timeHH, timeMM, timeSS);
                }
                else
                {
                    NextRunTime = new DateTime(now.Year, now.Month, now.Day, timeHH, timeMM, timeSS).AddDays(1);
                }
            }
            //每周一次
            else if ("DayOfWeek".Equals(timerInfo.TimerType))
            {
                DayOfWeek ofweek = DateTime.Now.DayOfWeek;

                int dayOfweek = Convert.ToInt32(DateTime.Now.DayOfWeek);

                if (ofweek == DayOfWeek.Sunday) dayOfweek = 7;

                if (dayOfweek < timerInfo.DateValue)
                {
                    int addDays = timerInfo.DateValue - dayOfweek;
                    NextRunTime = new DateTime(now.Year, now.Month, now.Day, timeHH, timeMM, timeSS).AddDays(addDays);
                }
                else if (dayOfweek == timerInfo.DateValue && nowTimeComp)
                {
                    NextRunTime = new DateTime(now.Year, now.Month, now.Day, timeHH, timeMM, timeSS);

                }
                else
                {
                    int addDays = 7 - (dayOfweek - timerInfo.DateValue);
                    NextRunTime = new DateTime(now.Year, now.Month, now.Day, timeHH, timeMM, timeSS).AddDays(addDays);
                }
            }
            //每月一次
            else if ("DayOfMonth".Equals(timerInfo.TimerType))
            {
                if (now.Day < timerInfo.DateValue)
                {
                    NextRunTime = new DateTime(now.Year, now.Month, timerInfo.DateValue, timeHH, timeMM, timeSS);
                }
                else if (now.Day == timerInfo.DateValue && nowTimeComp)
                {
                    NextRunTime = new DateTime(now.Year, now.Month, now.Day, timeHH, timeMM, timeSS);
                }
                else
                {
                    NextRunTime = new DateTime(now.Year, now.Month, timerInfo.DateValue, timeHH, timeMM, timeSS).AddMonths(1);
                }
            }
            //指定日期
            else if ("DesDate".Equals(timerInfo.TimerType))
            {
                NextRunTime = new DateTime(timerInfo.Year, timerInfo.Month, timerInfo.Day, timeHH, timeMM, timeSS);
            }
            //循环指定天数
            else if ("LoopDays".Equals(timerInfo.TimerType))
            {
                NextRunTime = new DateTime(now.Year, now.Month, now.Day, timeHH, timeMM, timeSS).AddDays(timerInfo.DateValue);
            }
        }


        #endregion


        #region 创建定时任务静态方法

        /// <summary>
        /// 使用委托方法创建定时任务
        /// <param name="info"></param>
        /// <param name="_trd"></param>
        /// <returns></returns>
        public static Thread CreateTimerTaskService(TimerInfo info, TimerTaskDelegate _trd)
        {
            TimerTaskService tus = new TimerTaskService(info, _trd);
            //创建启动线程
            Thread ThreadTimerTaskService = new Thread(new ThreadStart(tus.Start));
            return ThreadTimerTaskService;
        }

        /// <summary>
        ///  使用带参数的委托方法创建定时任务
        /// </summary>
        /// <param name="info"></param>
        /// <param name="_ptrd"></param>
        /// <param name="parm"></param>
        /// <returns></returns>
        public static Thread CreateTimerTaskService(TimerInfo info, ParmTimerTaskDelegate _ptrd, object[] parm)
        {
            TimerTaskService tus = new TimerTaskService(info, _ptrd);
            tus.setParm(parm);

            //创建启动线程
            Thread ThreadTimerTaskService = new Thread(new ThreadStart(tus.Start));
            return ThreadTimerTaskService;
        }

        /// <summary>
        /// 使用实现定时接口ITimerTask的实例创建定时任务
        /// </summary>
        /// <param name="info"></param>
        /// <param name="_ins"></param>
        /// <returns></returns>
        public static Thread CreateTimerTaskService(TimerInfo info, ITimerTask _ins)
        {
            TimerTaskService tus = new TimerTaskService(info, _ins);
            //创建启动线程
            Thread ThreadTimerTaskService = new Thread(new ThreadStart(tus.Start));
            return ThreadTimerTaskService;
        }


        #endregion
    }

 

 

 

    /// <summary>
    /// 定时信息类
    /// TimerType   类型：EveryDay(每天),DayOfWeek(每周),DayOfMonth(每月),DesDate(指定日期),LoopDays(循环天数)
    /// DateValue 日期值：TimerType="DayOfWeek"时,值为1-7表示周一到周日;TimerType="DayOfMonth"时,值为1-31表示1号到31号,
    ///                   TimerType="LoopDays"时,值为要循环的天数,TimerType为其它值时,此值无效
    /// Year   年：TimerType="DesDate"时,此值有效
    /// Month  月：TimerType="DesDate"时,此值有效
    /// Day    日：TimerType="DesDate"时,此值有效
    /// Hour   时：]
    /// Minute 分： > 设置的执行时间
    /// Second 秒：]
    /// </summary>
    public class TimerInfo
    {
        public string TimerType;
        public int DateValue;
        public int Year;
        public int Month;
        public int Day;
        public int Hour = 00;
        public int Minute = 00;
        public int Second = 00;
    }

}

//**********************************End  TimerTask.dll ****************************************//

//namespace Test
//{

//    /// <summary>
//    /// 定时任务测试类
//    /// 提供三种调用方法
//    /// TimerInfo持久化代码省略，此信息可保存到数据库或配置文件
//    /// </summary>
//    public class TestTimerTask:ITimerTask
//    {


//        /// <summary>
//        /// 程序入口
//        /// </summary>
//        [STAThread]
//        public static void Main()
//        {
//            ///程序调用举例
//            ///
//            //每天12点执行
//            TimerTask.TimerInfo timerInfo = new TimerTask.TimerInfo();
//            timerInfo.TimerType = "EveryDay";
//            timerInfo.Hour = 12;
//            timerInfo.Minute =00;
//            timerInfo.Second = 00;

//            ////每周日12点执行
//            //TimerUpdate.TimerInfo timerInfo = new TimerUpdate.TimerInfo();
//            //timerInfo.TimerType = "DayOfWeek";
//            //timerInfo.DateValue = 7;
//            //timerInfo.Hour = 12;
//            //timerInfo.Minute = 00;
//            //timerInfo.Second = 00;

//            ////每月1号12点执行
//            //TimerUpdate.TimerInfo timerInfo = new TimerUpdate.TimerInfo();
//            //timerInfo.TimerType = "DayOfMonth";
//            //timerInfo.DateValue = 1;
//            //timerInfo.Hour = 12;
//            //timerInfo.Minute = 00;
//            //timerInfo.Second = 00;

//            ////指定某一天12点执行
//            //TimerUpdate.TimerInfo timerInfo = new TimerUpdate.TimerInfo();
//            //timerInfo.TimerType = "DesDate";
//            //timerInfo.Year = 2010;
//            //timerInfo.Month = 12;
//            //timerInfo.Day = 01;
//            //timerInfo.Hour = 12;
//            //timerInfo.Minute = 00;
//            //timerInfo.Second = 00;

//            ////5天后12点执行
//            //TimerUpdate.TimerInfo timerInfo = new TimerUpdate.TimerInfo();
//            //timerInfo.TimerType = "LoopDays";
//            //timerInfo.DateValue = 5;
//            //timerInfo.Hour = 12;
//            //timerInfo.Minute = 00;
//            //timerInfo.Second = 00;

//            //第一种调用方法
//            TestTimerTask test1 = new TestTimerTask();
//            TimerTaskDelegate trd = new TimerTaskDelegate(test1.TestTimerTask1);
//            //创建定时任务线程
//            Thread ThreadTimerTaskService1 = TimerTaskService.CreateTimerTaskService(timerInfo, trd);
//            ThreadTimerTaskService1.Start();


//            //第二种调用方法
//            TestTimerTask test2 = new TestTimerTask();
//            ParmTimerTaskDelegate ptrd = new ParmTimerTaskDelegate(test2.TestTimerTask2);
//            object[] p = new object[] { "参数1", "参数2" };
//            //创建定时任务线程
//            Thread ThreadTimerTaskService2 = TimerTaskService.CreateTimerTaskService(timerInfo, ptrd, p);
//            ThreadTimerTaskService2.Start();

//            //第三种调用方法
//            TestTimerTask test3 = new TestTimerTask(); //TestTimerTask已实现ITimerTask接口
//            //创建定时任务线程
//            Thread ThreadTimerTaskService3 = TimerTaskService.CreateTimerTaskService(timerInfo, test3);
//            ThreadTimerTaskService3.Start();

//        }


       
//        public void TestTimerTask1()
//        {
//            Console.WriteLine("不带参数的定时任务已执行！");
//            Console.ReadLine();
//        }


//        public void TestTimerTask2(object[] parm)
//        {
//            Console.WriteLine("带object[]参数的定时任务已执行！");
//            if (parm != null && parm.Length > 0)
//            {
//                foreach (object o in parm)
//                {
//                    Console.WriteLine("参数内容：" + o.ToString());
//                }
                
//            }
//            Console.ReadLine();
//        }

 


//        /// <summary>
//        /// 实现ITimerTask接口中的Run方法
//        /// </summary>
//        public void Run()
//        {
//            Console.WriteLine("ITimerTask接口实现的定时任务已执行！");
//            Console.ReadLine();
//        }
//    }
//}
