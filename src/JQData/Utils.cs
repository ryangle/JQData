//-----------------------------------------------------------------------
// <copyright file="JQClient.cs" Author="ryangle">
// https://github.com/ryangle/JQData
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

namespace JQData
{
    public class Utils
    {
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
