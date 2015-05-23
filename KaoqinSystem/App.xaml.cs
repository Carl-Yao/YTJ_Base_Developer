using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Diagnostics;
using System.Threading;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SwipCardSystem
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            //Application.Current.ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;
            //LoginWindow window = new LoginWindow();
            //bool? dialogResult = window.ShowDialog();
            //if ((dialogResult.HasValue == true) &&
            //    (dialogResult.Value == true))
            //{
            //    base.OnStartup(e);
            //    Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            //}
            //else
            //{
            //    this.Shutdown();
            //}
            CheckRunning();
        }

        [STAThread]
        private void CheckRunning()
        {
            Process instance = GetExistProcess();
            if (instance != null)
            {
                //SetForegroud(instance);
                this.Shutdown();                
            }

            //bool createdNew;
            ////系统能够识别有名称的互斥，因此可以使用它禁止应用程序启动两次
            ////第二个参数可以设置为产品的名称:Application.ProductName
            //Mutex mutex = new Mutex(true, "SwipCardSystem", out createdNew);

            ////如果已运行，则在前端显示
            ////createdNew == false，说明程序已运行
            //if (!createdNew)
            //{
            //    Process instance = GetExistProcess();
            //    if (instance != null)
            //    {
            //        SetForegroud(instance);
            //        this.Shutdown();
            //        return;
            //    }
            //}
        }

        /// <summary>
        /// 查看程序是否已经运行
        /// </summary>
        /// <returns></returns>
        private static Process GetExistProcess()
        {
            Process currentProcess = Process.GetCurrentProcess();
            foreach (Process process in Process.GetProcessesByName(currentProcess.ProcessName))
            {
                if ((process.Id != currentProcess.Id) &&
                    (Assembly.GetExecutingAssembly().Location == currentProcess.MainModule.FileName))
                {
                    return process;
                }
            }
            return null;
        }
        /// <summary>
        /// 使程序前端显示
        /// </summary>
        /// <param name="instance"></param>
        private static void SetForegroud(Process instance)
        {
            IntPtr mainFormHandle = instance.MainWindowHandle;
            if (mainFormHandle != IntPtr.Zero)
            {
                ShowWindowAsync(mainFormHandle, 1);
                SetForegroundWindow(mainFormHandle);
            }
        }

        [DllImport("User32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("User32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int cmdShow);
    }  
}

