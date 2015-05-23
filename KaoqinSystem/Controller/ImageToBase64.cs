using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace SwipCardSystem.Controller
{
    public class ImageToBase64
    {
        ////图片 转为    base64编码的文本
        //private void button1_Click(object sender, EventArgs e)
        //{
        //    OpenFileDialog dlg = new OpenFileDialog();
        //    dlg.Title = "选择要转换的图片";
        //    dlg.Filter = "Image files (*.jpg;*.bmp;*.gif)|*.jpg*.jpeg;*.gif;*.bmp|AllFiles (*.*)|*.*";
        //    if (DialogResult.OK == dlg.ShowDialog())
        //    {
        //        ImgToBase64String(dlg.FileName);
        //    }
        //}
        //图片 转为    base64编码的文本
        public static void ImgToBase64String(Bitmap bmp/*string Imagefilename*/, ref String strbaser64)
        {            
            try
            {
                //Bitmap bmp = new Bitmap(Imagefilename);
                //this.pictureBox1.Image = bmp;
                //FileStream fs = new FileStream(Imagefilename + ".txt", FileMode.Create);
                //StreamWriter sw = new StreamWriter(fs);

                MemoryStream ms = new MemoryStream();
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                byte[] arr = new byte[ms.Length];
                ms.Position = 0;
                ms.Read(arr, 0, (int)ms.Length);
                ms.Close();
                strbaser64 = Convert.ToBase64String(arr);
                //sw.Write(strbaser64);

                //sw.Close();
                //fs.Close();
                //Log.LogInstance.Write("转换成功!");                
            }
            catch (Exception ex)
            {
                Log.LogInstance.Write("ImgToBase64String 转换失败/nException:" + ex.Message, MessageType.Error);
                //MessageBox.Show("ImgToBase64String 转换失败/nException:" + ex.Message);
            }            
        }

        ////base64编码的文本 转为    图片
        //private void button2_Click(object sender, EventArgs e)
        //{
        //    OpenFileDialog dlg = new OpenFileDialog();
        //    dlg.Title = "选择要转换的base64编码的文本";
        //    dlg.Filter = "txt files|*.txt";
        //    if (DialogResult.OK == dlg.ShowDialog())
        //    {
        //        Base64StringToImage(dlg.FileName);
        //    }
        //}
        //base64编码的文本 转为    图片
        public static void Base64StringToImage(/*string txtFileName*/String inputStr, ref Bitmap bmp)
        {            
            try
            {
                //FileStream ifs = new FileStream(txtFileName, FileMode.Open, FileAccess.Read);
                //StreamReader sr = new StreamReader(ifs);

                //String inputStr = sr.ReadToEnd();
                byte[] arr = Convert.FromBase64String(inputStr);
                MemoryStream ms = new MemoryStream(arr);
                Bitmap bmp2 = new Bitmap(ms);
                bmp = new Bitmap(358, 441, PixelFormat.Format16bppRgb555);
                Graphics g = Graphics.FromImage(bmp);
                g.DrawImage(bmp2, new Rectangle(0, 0, 358, 441));
                g.Dispose();
                bmp2.Dispose();//释放bmp文件资源
                //bmp.Save(txtFileName + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                //bmp.Save(txtFileName + ".bmp", ImageFormat.Bmp);
                //bmp.Save(txtFileName + ".gif", ImageFormat.Gif);
                //bmp.Save(txtFileName + ".png", ImageFormat.Png);
                ms.Close();
                //sr.Close();
                //ifs.Close();
                //this.pictureBox1.Image = bmp;
                //Log.LogInstance.Write("转换成功！");                
            }
            catch (Exception ex)
            {
                //Log.LogInstance.Write("Base64StringToImage 转换失败/nException："+ex.Message);
                Log.LogInstance.Write("Base64StringToImage 转换失败/nException：" + ex.Message, MessageType.Error);
                //MessageBox.Show("Base64StringToImage 转换失败/nException：" + ex.Message);
            }            
        }
    }
}