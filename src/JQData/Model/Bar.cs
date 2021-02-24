//-----------------------------------------------------------------------
// <copyright file="JQClient.cs" Author="ryangle">
// https://github.com/ryangle/JQData
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

namespace JQData.Model
{
    public class Bar
    {
        public string Date;
        public double Open;
        public double Close;
        public double High;
        public double Low;
        public double Volume;
        public double Money;

        public readonly string Header = "date,open,close,high,low,volume,money";

        public override string ToString()
        {
            return $"{Date},{Open},{Close},{High},{Low},{Volume},{Money}";
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            var item = obj as Bar;
            if (item == null)
            {
                return false;
            }
            return item.Date == Date && item.Open == Open && item.Close == Close && item.High == High && item.Low == Low && item.Volume == Volume && item.Money == Money;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
    }
}
