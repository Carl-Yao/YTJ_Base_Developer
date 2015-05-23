using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace M1Card.Common
{
    /// <summary>
    /// 明华URF-R330 API声明类
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe class URF
    {
        /// <summary>
        /// 连接设备
        /// </summary>
        /// <param name="port">USB端口，默认传0</param>
        /// <param name="baud">波特率</param>
        [DllImport("mwrf32.dll", EntryPoint = "rf_init", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.ThisCall)]
        public static extern int rf_init(int port, long baud);

        /// <summary>
        /// 断开设备
        /// </summary>
        /// <param name="icdev">设备id</param>
        [DllImport("mwrf32.dll", EntryPoint = "rf_exit", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern int rf_exit(int icdev);
        //获取设备版本号
        [DllImport("mwrf32.dll", EntryPoint = "rf_get_status")]
        public static extern int rf_get_status(int icdev, Byte[] version);
        //蜂鸣器
        [DllImport("mwrf32.dll", EntryPoint = "rf_beep", SetLastError = true, CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
        public static extern Int16 rf_beep(int icdev, int version);

        //中止对该卡操作
        [DllImport("mwrf32.dll", EntryPoint = "rf_halt")]
        public static extern Int16 rf_halt(int icdev);

        //寻卡请求
        [DllImport("mwrf32.dll", EntryPoint = "rf_request", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern Int16 rf_request(int icdev, Byte IDLE, Int16* state);
         

        //寻卡
        [DllImport("mwrf32.dll", EntryPoint = "rf_card")]
        public static extern int rf_card(int icdev, int mode, ref ulong Snr);

        //卡防冲突，返回卡的序列号
        [DllImport("mwhrf_bj.dll", EntryPoint = "rf_anticoll", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern int rf_anticoll(int icdev, char _Bcnt, Int64* _Snr);

        ////将密码装入读写模块RAM中      

        [DllImport("mwrf32.dll")]
        public static extern short rf_load_key(int icdev, int mode, int secnr, [In] byte[] nkey);  //密码装载到读写模块中      

        [DllImport("mwrf32.dll", EntryPoint = "rf_load_key_hex")]
        public static extern Int16 rf_load_key_hex(int icdev, int mode, int secnr, string keybuff);


        //验证某一扇区密码     
        [DllImport("mwrf32.dll")]
        public static extern short rf_authentication(int icdev, int _Mode, int _SecNr);

        //验证某一扇区密码 2
        [DllImport("mwrf32.dll", EntryPoint = "rf_authentication_2")]
        public static extern int rf_authentication_2(int icdev, int _Mode, int KeyNr, int Adr);


        //向卡中写入数据
        [DllImport("mwrf32.dll", EntryPoint = "rf_write")]
        public static extern int rf_write(int icdev, int _Adr, char[] _Data);

        [DllImport("mwrf32.dll")]
        public static extern short rf_write(int icdev, int adr, [In] string sdata);  //向卡中写入数据

        //读取数据     
        [DllImport("mwrf32.dll")]
        public static extern short rf_read(int icdev, int adr, [MarshalAs(UnmanagedType.LPStr)] StringBuilder sdata);  //从卡中读数据

        //读取数据     
        [DllImport("mwrf32.dll")]
        public static extern short rf_read_hex(int icdev, int adr, [MarshalAs(UnmanagedType.LPStr)] StringBuilder sdata);  //从卡中读数据


    }
}
