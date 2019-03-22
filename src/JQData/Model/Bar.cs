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
    }
}
