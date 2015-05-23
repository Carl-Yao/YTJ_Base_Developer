using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using System;
using SwipCardSystem.Controller;
namespace SCKeeper
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            if (_thread != null && _thread.IsAlive)
            {
                _thread.Abort();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        Thread _thread = null;

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Text = "Form1";
            this.Hide();
            _thread = new Thread(KeepSCRunning);
            _thread.Start();          
        }

        private void KeepSCRunning()
        {
            int i = 5;
            while (i > 0)
            {
                i--;
                if (NetworkManager.IsInterentConnected())
                {
                    Thread.Sleep(10000);
                    break;
                }
                else
                {
                    Thread.Sleep(5000);
                }
            }
            while (true)
            {
                try
                {                    
                    Process[] ps = Process.GetProcessesByName("SwipCardSystem");

                    if (ps.Length == 0)
                    {
                        Process process = new Process();

                        process.StartInfo.FileName = System.Windows.Forms.Application.StartupPath + "\\SwipCardSystem.exe";

                        process.Start();
                    }
                    //else
                    //{
                    //    foreach (Process p in ps)
                    //    {
                    //        if (!p.Responding)
                    //        {
                    //            //不响应也干掉？
                    //            p.Kill();
                    //            Process process = new Process();
                    //            process.StartInfo.FileName = System.IO.Directory.GetCurrentDirectory() + "\\SwipCardSystem.exe";
                    //            process.Start();
                    //        }
                    //    }
                    //}
                }
                catch (Exception ex)
                {
                    //MessageBox.Show("KeepSCRunning Error:" + ex.Message + "%%" + System.IO.Directory.GetCurrentDirectory() + "&&" + System.Windows.Forms.Application.StartupPath + "**" + System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                }
                finally
                {
                    Thread.Sleep(5000);
                }
            }
        }
        #endregion
    }
}

