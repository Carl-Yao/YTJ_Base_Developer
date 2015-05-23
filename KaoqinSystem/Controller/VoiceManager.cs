using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotNetSpeech;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using Microsoft.Win32;
using System.Windows; 

        
namespace SwipCardSystem.Controller
{
    public static class VoiceManager
    {
        private static SpVoice _voice = new SpVoice();
        private static int _currentVoiceDevice = -1;

        public delegate void CallBack(bool b, int InputWordPosition, int InputWordLength);

        public static void Speak(string str, int useVoiceDevice = 1, CallBack CallBack = null)
        {
            //if (_voice.Voice == null)
            //{
            if (_currentVoiceDevice != useVoiceDevice)
            {
                SetDefaultDevice(useVoiceDevice);
            }
            _voice.Voice = _voice.GetVoices(string.Empty, string.Empty).Item(0);

            _voice.Speak(str, SpeechVoiceSpeakFlags.SVSFlagsAsync);
            if (CallBack != null)
            {
                Thread thread = new Thread(new ParameterizedThreadStart(Call));
                thread.IsBackground = true;
                thread.Start((Object)CallBack);
            }
        }
            /// <summary>  
        /// 朗读文本  
        /// </summary>  
        /// <param name="str">要朗读的文本</param>  
        /// <param name="CallBack">回调地址</param>  
        /// <returns>返回bool</returns>  
        //public bool Speak(string str, CallBack CallBack)  
        //{  
        //    int n = voice.Speak(str, SpeechVoiceSpeakFlags.SVSFlagsAsync);
        //    Thread thread = new Thread(new ParameterizedThreadStart(Call));
        //    thread.IsBackground = true;
        //    thread.Start((Object)CallBack); 
        //    return !(n!=1);  
        //}  
  
  
        /// <summary>  
        /// 回调函数线程子程序  
        /// </summary>  
        /// <param name="callBack"></param>  
        private static void Call(Object callBack)  
        {  
            int InputWordLength = 0;    //局部_朗读长度  
            int InputWordPosition = 0; //局部_朗读位置  
  
            CallBack CallBack = (CallBack)callBack;

            while ((int)_voice.Status.RunningState != 1)  
            {
                if (InputWordPosition != _voice.Status.InputWordPosition || InputWordLength != _voice.Status.InputWordLength)  
                {
                    InputWordPosition = _voice.Status.InputWordPosition;
                    InputWordLength = _voice.Status.InputWordLength;  
  
                    //回调                    
                    CallBack(false, InputWordPosition, InputWordLength);  
                }  
            }  
            CallBack(true, InputWordPosition, InputWordLength);  
        }  

        public static void SetDefaultDevice(int iCount)
        {
            try
            {
                //iCount = iCount == 0 ? 1 : 0;
                Process pro = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.Arguments = iCount.ToString();
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;
                startInfo.FileName = "SetAudioPlaybackDevice.exe";
                pro.StartInfo = startInfo;
                pro.Start();
                pro.WaitForExit();
                _currentVoiceDevice = iCount;
            }
            catch (Exception e)
            {
                Log.LogInstance.Write(e.Message, MessageType.Error);
            }
        }

        public static void textToFile(string str)
        {
            try
            {
                SpeechVoiceSpeakFlags SpFlags = SpeechVoiceSpeakFlags.SVSFlagsAsync;
                SpVoice Voice = new SpVoice();
                SpeechStreamFileMode SpFileMode = SpeechStreamFileMode.SSFMCreateForWrite;
                SpFileStream spfileStream = new SpFileStream();
                spfileStream.Open("C:\\Users\\User\\Desktop\\aa.wav", SpFileMode, false);
                Voice.AudioOutputStream = spfileStream;
                Voice.Speak(str, SpFlags);
                Voice.WaitUntilDone(Timeout.Infinite);
                spfileStream.Close();
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message, "");
            }
        }

        [DllImport("kernel32.dll")]
        public static extern bool Beep(int freq, int duration);

        public static void Beep()
        {
            //调用  
            Beep(3000, 500);
        }  
    }
}
