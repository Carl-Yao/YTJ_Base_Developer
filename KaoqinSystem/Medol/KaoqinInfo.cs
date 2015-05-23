using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace SwipCardSystem.Medol
{
    public class KaoqinInfo
    {
        public string RecordId
        {
            get;
            set;
        }

        public string ClassId
        {
            get;
            set;
        }

        public string EqupId
        {
            get;
            set;
        }

        public string ICCardId
        {
            get;
            set;
        }

        public string ICCardNo
        {
            get;
            set;
        }

        public string StudentID
        {
            get;
            set;
        }

        public string TemplateVal
        {
            get;
            set;
        }

        public string RecordTime
        {
            get;
            set;
        }

        //base64 path
        public string PicturePath
        {
            set;
            get;
        }

        public string PictureBase64
        {
            set;
            get;
        }

        public Bitmap PicBitMap
        {
            set;
            get;
        }
    }

}
