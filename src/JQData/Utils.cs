//-----------------------------------------------------------------------
// <copyright file="Utils.cs" Author="ryangle">
// https://github.com/ryangle/JQData
// </copyright>
//-----------------------------------------------------------------------

namespace JQData
{
    /// <summary>
    /// 常用工具类
    /// </summary>
    public class Utils
    {
        /// <summary>
        /// 根据交易所代码获取交易所缩写
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static string GetExchange(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return string.Empty;
            }
            switch (code.ToUpper())
            {
                case "XDCE":
                    return "DCE";
                case "XZCE":
                    return "CZCE";
                case "XINE":
                    return "INE";
                case "CCFX":
                    return "CFFEX";
                case "XSGE":
                    return "SHFE";
            }
            return string.Empty;
        }
    }
}
