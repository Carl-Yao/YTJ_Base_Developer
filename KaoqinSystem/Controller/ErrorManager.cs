using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SwipCardSystem.Controller;

namespace M1Card.Common
{
    /// <summary>
    /// 错误处理
    /// </summary>
    public class ErrorManager
    {
        /// <summary>
        /// 检查错误码
        /// </summary>
        /// <param name="errorCode">错误码</param>
        public static void CheckError(int errorCode)
        {
            string value;
            if (ErrorType.errors.TryGetValue(errorCode, out value))
            {
                Log.LogInstance.Write(value.ToString(), MessageType.Error); 
            }
        }
    }

    /// <summary>
    /// 错误类型
    /// </summary>
    public class ErrorType
    {
        public static Dictionary<int, string> errors = new Dictionary<int, string>();

        static ErrorType()
        {
            errors.Add(1, "无卡");
            errors.Add(2, "CRC校验错误");
            errors.Add(3, "值溢出");
            errors.Add(4, "未验证密码");
            errors.Add(5, "奇偶校验错");
            errors.Add(6, "通讯出错");
            errors.Add(8, "错误的序列号");
            errors.Add(10, "验证密码失败");
            errors.Add(11, "接收的数据位错误");
            errors.Add(12, "接收的数据字节错误");
            errors.Add(14, "Transfer错误");
            errors.Add(15, "写失败");
            errors.Add(16, "加值失败");
            errors.Add(17, "减值失败");
            errors.Add(18, "读失败");
            errors.Add(-0x10, "PC与读写器通讯错误");
            errors.Add(-0x11, "通讯超时");
            errors.Add(-0x20, "打开通信口失败");
            errors.Add(-0x24, "串口已被占用");
            errors.Add(-0x30, "地址格式错误");
            errors.Add(-0x31, "该块数据不是值格式");
            errors.Add(-0x32, "长度错误");
            errors.Add(-0x40, "值操作失败");
            errors.Add(-0x50, "卡中的值不够减");
        }
    }

    /// <summary>
    /// URF设备异常类
    /// </summary>
    public class URFException : Exception
    {
        /// <summary>
        /// 错误代码
        /// </summary>
        public int ErrorCode;
        /// <summary>
        /// 构建一个拥有指定错误代码的异常
        /// </summary>
        /// <param name="errorCode">错误代码</param>
        public URFException(int errorCode)
        {
            this.ErrorCode = errorCode;
        }
    }
}
