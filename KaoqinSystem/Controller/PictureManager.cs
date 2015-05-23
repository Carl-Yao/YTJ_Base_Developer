using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

/// <summary>
/// Class1 的摘要说明
/// </summary>
public class MyGDI
{  
    //创建 圆角图片的方法
    //方法参数的说明
    public static void CreateRoundedCorner(string sSrcFilePath, string sDstFilePath, string sCornerLocation)
    {
        System.Drawing.Image image = System.Drawing.Image.FromFile(sSrcFilePath);
        Graphics g = Graphics.FromImage(image);
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.CompositingQuality = CompositingQuality.HighQuality;
        Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);
        GraphicsPath rectPath = CreateRoundRectanglePath(rect, image.Width / 10, sCornerLocation); //构建圆角外部路径
        Brush b = new SolidBrush(Color.White);//圆角背景白色
        g.DrawPath(new Pen(b), rectPath);
        g.FillPath(b, rectPath);
        g.Dispose();
        image.Save(sDstFilePath, ImageFormat.Jpeg);
        image.Dispose();
    } 

    private static GraphicsPath CreateRoundRectanglePath(Rectangle rect, int radius, string sPosition)
    {
        GraphicsPath rectPath = new GraphicsPath();
        switch (sPosition)
        {
            case "TopLeft":
                {
                    rectPath.AddArc(rect.Left, rect.Top, radius * 2, radius * 2, 180, 90);
                    rectPath.AddLine(rect.Left, rect.Top, rect.Left, rect.Top + radius);
                    break;
                }

            case "TopRight":
                {
                    rectPath.AddArc(rect.Right - radius * 2, rect.Top, radius * 2, radius * 2, 270, 90);
                    rectPath.AddLine(rect.Right, rect.Top, rect.Right - radius, rect.Top);
                    break;
                }

            case "BottomLeft":
                {
                    rectPath.AddArc(rect.Left, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
                    rectPath.AddLine(rect.Left, rect.Bottom - radius, rect.Left, rect.Bottom);
                    break;
                }

            case "BottomRight":
                {
                    rectPath.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
                    rectPath.AddLine(rect.Right - radius, rect.Bottom, rect.Right, rect.Bottom);
                    break;
                }
        
        }
        return rectPath;
    }
}