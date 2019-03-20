using System;
using System.Collections.Generic;
using System.Text;

namespace JQData.Model
{
    public class Security
    {
        /// <summary>
        /// 标的代码
        /// </summary>
        public string Code;
        /// <summary>
        /// 中文名称
        /// </summary>
        public string DisplayName;
        /// <summary>
        /// 缩写简称
        /// </summary>
        public string Name;
        /// <summary>
        /// 上市日期
        /// </summary>
        public string StartDate;
        /// <summary>
        /// 退市日期，如果没有退市则为2200-01-01
        /// </summary>
        public string EndDate;
        /// <summary>
        ///类型stock(股票)，index(指数)，etf(ETF基金)，fja（分级A），fjb（分级B）
        /// </summary>
        public string Type;
    }
}
