using ConsoleApplication1;
using SwipCardSystem.Controller;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SwipCardSystem.View
{
    /// <summary>
    /// GuangBoSystem.xaml 的交互逻辑
    /// </summary>
    public partial class GuangBoSystem : Window
    {
        private ObservableCollection<SoundFreuencyItem> ObservableObj;
        private SoundConfigManager _soundConfigManage;
        public GuangBoSystem()
        {
            InitializeComponent();
            ObservableObj = new ObservableCollection<SoundFreuencyItem>();
            FrequencyList.DataContext = ObservableObj;
            List<string> list = new List<string>();
            _soundConfigManage = SoundConfigManager.CreateSingleton();
            //if (!MySqlManager.CreateSingleton().GetAllClass(ref list))
            //{
            //    return;
            //}

            SelectSound.Items.Add("所有");
            //SelectSound.SelectedIndex = 0;
            for (int i = 0; i < _soundConfigManage.arrListSoundNames.Count;i++ )
            {
                Add(new SoundFreuencyItem
                {
                    SoundName = (string)_soundConfigManage.arrListSoundNames[i],
                    Frequency = (string)_soundConfigManage.arrListSoundFrequency[i]
                });
                SelectSound.Items.Add((string)_soundConfigManage.arrListSoundNames[i]);
            }

            for (double start = 76.0; start <= 86.0; start += 0.1)
            {
                Sound.Items.Add(start.ToString("0.0"));
            }
            Sound.SelectedIndex = 0;
        }
        public class SoundFreuencyItem
        {
            public string SoundName { get; set; }
            public string Frequency { get; set; }
        }
               /// <summary>
        /// Add: 添加一个对象到集合
        /// </summary>
        /// <param name="dto"></param>
        /// created 2013.08.26
        public void Add(SoundFreuencyItem dto) { this.ObservableObj.Add(dto); }

        /// <summary>
        /// Remove: 根据名称移除从集合中移除指定对象
        /// </summary>
        /// <param name="personName"></param>
        /// created 2013.08.26
        public void Remove(string personName) {
            var obj = (from x in ObservableObj
                       where x.SoundName.Equals(personName, StringComparison.CurrentCultureIgnoreCase)
                       select x).FirstOrDefault();
            if (obj != null) {
                ObservableObj.Remove(obj);
            }
        }

        private void ChangePlanDataClick(object sender, RoutedEventArgs e)
        {
            int s = FrequencyList.SelectedIndex;
            if (s > -1)
            {

                ObservableObj[s] = new SoundFreuencyItem()
                {
                    Frequency = Sound.Text,
                    SoundName = ObservableObj[s].SoundName
                };
                _soundConfigManage.arrListSoundFrequency[s] = Sound.Text;
                _soundConfigManage.RefreshXmlList(_soundConfigManage.arrListSoundNames, _soundConfigManage.arrListSoundFrequency);
                // new PlanItem() { FileCount = fileCount, Date = date, BeginTime = beginTime, EndTime = endTime, Order = order, Sound = sound };
            }
            else
            {
                System.Windows.MessageBox.Show("请在定时播放音乐计划列表中选择一条要更新的计划！");
            }
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            Close();
            //Hide();
        }

        private void SelectSound_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
             string str = SelectSound.Items[SelectSound.SelectedIndex].ToString();
             VoiceManager.SetDefaultDevice(0);
             if ("所有".Equals(str))
             {
                 VoiceManager.SetDefaultDevice(0);
                 DSAManager.CreateSingleton().SetHZ(86.5);
             }
             else
             {
                 for (int i = 0; i < _soundConfigManage.arrListSoundNames.Count; i++)
                 {
                     if (str.Equals(_soundConfigManage.arrListSoundNames[i]))
                     {
                         //VoiceManager.Speak("", 0);
                         VoiceManager.SetDefaultDevice(0);
                         //if ()
                         DSAManager.CreateSingleton().SetHZ(Double.Parse(_soundConfigManage.arrListSoundFrequency[i].ToString()));
                     }

                 }
             }
        }
    }
}
