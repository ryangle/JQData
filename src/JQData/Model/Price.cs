//-----------------------------------------------------------------------
// <copyright file="Price.cs" Author="ryangle">
// https://github.com/ryangle/JQData
// </copyright>
//-----------------------------------------------------------------------

namespace JQData.Model
{
    public class Price
    {
        public string Code;
        public double Current;

        public readonly string Header = "code,current";
        public override string ToString()
        {
            return $"{Code},{Current}";
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            var item = obj as Price;
            if (item == null)
            {
                return false;
            }
            return item.Code == Code && item.Current == Current;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
    }
}
