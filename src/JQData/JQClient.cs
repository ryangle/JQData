//-----------------------------------------------------------------------
// <copyright file="JQClient.cs" Author="ryangle">
// https://github.com/ryangle/JQData
// </copyright>
//-----------------------------------------------------------------------
using JQData.Model;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace JQData
{
    /// <summary>
    /// 每2秒一次请求，否则会限制访问
    /// 调用接口前，先使用<see cref="GetToken"/>获取Token 
    /// webapi使用文档：https://www.joinquant.com/help/api/help#name:JQDataHttp
    /// </summary>
    public class JQClient
    {
        private HttpClient _httpClient;
        private string _baseUrl = "https://dataapi.joinquant.com/apis";
        /// <summary>
        /// Token
        /// </summary>
        public string Token { private set; get; }
        private string PostRequest(string body)
        {
            var bodyContent = new StringContent(body);
            var resultReq = _httpClient.PostAsync(_baseUrl, bodyContent).Result;
            return resultReq.Content.ReadAsStringAsync().Result;
        }
        /// <summary>
        /// 获取用户凭证
        /// <br/>
        /// 调用其他获取数据接口之前，需要先调用本接口获取token。token被作为用户认证使用，当天有效
        /// </summary>
        /// <param name="mob">mob是申请JQData时所填写的手机号</param>
        /// <param name="pwd">Password为聚宽官网登录密码，新申请用户默认为手机号后6位</param>
        /// <returns>
        /// 返回 token
        /// 
        /// 5b6a9ba7b0f572bb6c287e280ed
        /// </returns>
        public string GetToken(string mob, string pwd)
        {
            try
            {
                _httpClient?.Dispose();

                _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var body = JsonSerializer.Serialize(new
                {
                    method = "get_token",
                    mob,
                    pwd
                });
                Token = PostRequest(body);
            }
            catch
            {
                Token = string.Empty;
            }
            return Token;
        }
        /// <summary>
        /// 获取用户当前可用凭证
        /// 当存在用户有效token时，直接返回原token，如果没有token或token失效则生成新token并返回
        /// </summary>
        /// <param name="mob"></param>
        /// <param name="pwd"></param>
        /// <returns>
        /// 返回 token
        /// 
        /// 5b6a9ba7b0f572bb6c287e280ed
        /// </returns>
        public string GetCurrentToken(string mob, string pwd)
        {
            try
            {
                _httpClient?.Dispose();

                _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var body = JsonSerializer.Serialize(new
                {
                    method = "get_current_token",
                    mob,
                    pwd
                });
                Token = PostRequest(body);
            }
            catch
            {
                Token = string.Empty;
            }
            return Token;
        }
        /// <summary>
        /// 获取查询剩余条数
        /// </summary>
        /// <returns>100000</returns>
        public int GetQueryCount()
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_query_count",
                Token
            });
            var result = PostRequest(body);
            _ = int.TryParse(result, out var c);
            return c;
        }
        /// <summary>
        /// 获取所有标的信息
        /// 获取平台支持的所有股票、基金、指数、期货信息
        /// </summary>
        /// <param name="code">stock(股票)，fund,index(指数)，futures,etf(ETF基金)，lof,fja（分级A），fjb（分级B）</param>
        /// <param name="date">为空表示所有日期的标的</param>
        /// <returns>
        /// code: 标的代码
        /// display_name: 中文名称
        /// name: 缩写简称
        /// start_date: 上市日期
        /// end_date: 退市日期，如果没有退市则为2200-01-01
        /// type: 类型: stock(股票)，index(指数)，etf(ETF基金)，fja（分级A），fjb（分级B），fjm（分级母基金），mmf（场内交易的货币基金）open_fund（开放式基金）, bond_fund（债券基金）, stock_fund（股票型基金）, QDII_fund（QDII 基金）, money_market_fund（场外交易的货币基金）, mixture_fund（混合型基金）, options（期权）
        /// 
        /// code,display_name,name,start_date,end_date,type
        /// 000001.XSHE,平安银行,PAYH,1991-04-03,2200-01-01,stock
        /// 000002.XSHE,万科A,WKA,1991-01-29,2200-01-01,stock
        /// </returns>
        public Security[] GetAllSecurities(string code, string date)
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_all_securities",
                Token,
                code,
                date
            });
            var securityInfo = PostRequest(body);
            var securitiesStr = securityInfo.Split('\n');

            if (securitiesStr.Length > 0)
            {
                if (!securitiesStr[0].StartsWith("code,display_name,name,start_date,end_date,type"))
                {
                    throw new Exception(securitiesStr[0]);
                }
            }

            var result = new List<Security>();
            for (int i = 1; i < securitiesStr.Length; i++)
            {
                var s = securitiesStr[i].Split(',');
                result.Add(new Security
                {
                    Code = s[0],
                    DisplayName = s[1],
                    Name = s[2],
                    StartDate = s[3],
                    EndDate = s[4],
                    Type = s[5]
                });

            }
            return result.ToArray();
        }
        /// <summary>
        /// 获取单个标的信息
        /// 获取股票/基金/指数的信息
        /// </summary>
        /// <param name="code">stock(股票)，fund,index(指数)，futures,etf(ETF基金)，lof,fja（分级A），fjb（分级B）</param>
        /// <returns>
        /// code: 标的代码
        /// display_name: 中文名称
        /// name: 缩写简称
        /// start_date: 上市日期, [datetime.date]
        /// 类型
        /// end_date: 退市日期， [datetime.date] 类型, 如果没有退市则为2200-01-01
        /// type: 类型，stock(股票)，index(指数)，etf(ETF基金)，fja（分级A），fjb（分级B）
        /// parent: 分级基金的母基金代码
        /// 
        /// code,display_name,name,start_date,end_date,type,parent
        /// 502050.XSHG,上证50B,SZ50B,2015-04-27,2200-01-01,fjb,502048.XSHG
        /// </returns>
        public Security GetSecurityInfo(string code)
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_security_info",
                Token,
                code
            });
            var result = PostRequest(body);
            var securitiesStr = result.Split('\n');

            if (securitiesStr.Length > 0)
            {
                if (!securitiesStr[0].StartsWith("code,display_name,name,start_date,end_date,type"))
                {
                    throw new Exception(securitiesStr[0]);
                }
            }
            var s = new Security();
            if (securitiesStr.Length == 2)
            {
                var t = securitiesStr[1].Split(',');
                s.Code = t[0];
                s.DisplayName = t[1];
                s.Name = t[2];
                s.StartDate = t[3];
                s.EndDate = t[4];
                s.Type = t[5];
            }
            return s;
        }

        /// <summary>
        /// 获取指定时间周期的行情数据
        /// </summary>
        /// <remarks>
        /// 获取各种时间周期的bar数据，bar的分割方式与主流股票软件相同， 同时还支持返回当前时刻所在 bar 的数据
        /// </remarks>
        /// <param name="code">证券代码</param>
        /// <param name="count">大于0的整数，表示获取bar的条数，不能超过5000</param>
        /// <param name="unit">bar的时间单位, 支持如下周期：1m, 5m, 15m, 30m, 60m, 120m, 1d, 1w, 1M</param>
        /// <param name="end_date">查询的截止时间，默认是今天</param>
        /// <param name="fq_ref_date">复权基准日期，该参数为空时返回不复权数据</param>
        /// <returns>
        /// date: 日期
        /// open: 开盘价
        /// close: 收盘价
        /// high: 最高价
        /// low: 最低价
        /// volume: 成交量
        /// money: 成交额 当unit为1d时，包含以下返回值:
        /// paused: 是否停牌，0 正常；1 停牌
        /// high_limit: 涨停价
        /// low_limit: 跌停价
        /// avg: 当天均价
        /// pre_close：前收价 当code为期货和期权时，包含以下返回值:
        /// open_interest 持仓量
        /// 
        /// date,open,close,high,low,volume,money,paused,high_limit,low_limit,avg,pre_close
        /// 2018-07-09,9.27,9.50,9.53,9.27,22407527,212109327.00,0,10.20,8.34,9.47,9.27
        /// 2018-07-10,9.51,9.47,9.55,9.40,12534270,118668133.00,0,10.45,8.55,9.47,9.50
        /// </returns>
        public Bar[] GetPrice(string code, int count, string unit, string end_date, string fq_ref_date)
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_price",
                Token,
                code,
                count,
                unit,
                end_date,
                fq_ref_date
            });
            var securityInfo = PostRequest(body);
            var barStrs = securityInfo.Split('\n');

            if (barStrs.Length > 0)
            {
                if (!barStrs[0].StartsWith("date,open,close,high,low,volume,money"))
                {
                    throw new Exception(barStrs[0]);
                }
            }

            var bars = new List<Bar>();
            for (int i = 1; i < barStrs.Length; i++)
            {
                var bar = barStrs[i].Split(',');
                bars.Add(new Bar
                {
                    Date = bar[0],
                    Open = double.Parse(bar[1]),
                    Close = double.Parse(bar[2]),
                    High = double.Parse(bar[3]),
                    Low = double.Parse(bar[4]),
                    Volume = double.Parse(bar[5]),
                    Money = double.Parse(bar[6])
                });
            }
            return bars.ToArray();
        }
        /// <summary>
        /// 同GetPrice
        /// </summary>
        /// <param name="code"></param>
        /// <param name="count"></param>
        /// <param name="unit"></param>
        /// <param name="end_date"></param>
        /// <param name="fq_ref_date"></param>
        /// <returns></returns>
        public Bar[] GetBars(string code, int count, string unit, string end_date, string fq_ref_date)
        {
            return GetPrice(code, count, unit, end_date, fq_ref_date);
        }
        /// <summary>
        /// 获取指定时间段的行情数据
        /// 指定开始时间date和结束时间end_date时间段，获取行情数据
        /// </summary>
        /// <remarks>
        /// 当unit是1w或1M时，第一条数据是开始时间date所在的周或月的行情。当unit为分钟时，第一条数据是开始时间date所在的一个unit切片的行情。 最大获取1000个交易日数据
        /// </remarks>
        /// <example>
        /// ```
        /// {
        /// "code": "600000.XSHG",
        /// "unit": "30m",
        /// "date": "2018-12-04 09:45:00",    
        /// "end_date": "2018-12-04 10:40:00",
        /// "fq_ref_date": "2018-12-18"
        /// }
        /// ```
        /// </example>
        /// <param name="code">证券代码</param>
        /// <param name="unit">bar的时间单位, 支持如下周期：1m, 5m, 15m, 30m, 60m, 120m, 1d, 1w, 1M。其中m表示分钟，d表示天，w表示周，M表示月</param>
        /// <param name="date">开始时间，不能为空，格式2018-07-03或2018-07-03 10:40:00，如果是2018-07-03则默认为2018-07-03 00:00:00</param>
        /// <param name="end_date">结束时间，不能为空，格式2018-07-03或2018-07-03 10:40:00，如果是2018-07-03则默认为2018-07-03 23:59:00</param>
        /// <param name="fq_ref_date">复权基准日期，该参数为空时返回不复权数据</param>
        /// <returns>
        /// date: 日期
        /// open: 开盘价
        /// close: 收盘价
        /// high: 最高价
        /// low: 最低价
        /// volume: 成交量
        /// money: 成交额 当unit为1d时，包含以下返回值:
        /// paused: 是否停牌，0 正常；1 停牌
        /// high_limit: 涨停价
        /// low_limit: 跌停价 当code为期货和期权时，包含以下返回值:
        /// open_interest 持仓量
        /// 
        /// date,open,close,high,low,volume,money
        /// 2018-12-04 10:00,11.00,11.03,11.07,10.97,4302800,47472956.00
        /// 2018-12-04 10:30,11.04,11.04,11.06,10.98,3047800,33599476.00
        /// </returns>
        public string GetPricePeriod(string code, string unit, string date, string end_date, string fq_ref_date)
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            string body = JsonSerializer.Serialize(new
            {
                method = "get_price_period",
                Token,
                code,
                unit,
                date,
                end_date,
                fq_ref_date
            });
            var bodyContent = new StringContent(body);
            var resultReq = _httpClient.PostAsync(_baseUrl, bodyContent).Result;
            var result = resultReq.Content.ReadAsStringAsync().Result;
            return result;
        }
        /// <summary>
        /// 同GetPricePeriod
        /// </summary>
        /// <returns></returns>
        public string GetBarsPeriod(string code, string unit, string date, string end_date, string fq_ref_date)
        {
            return GetPricePeriod(code, unit, date, end_date, fq_ref_date);
        }
        /// <summary>
        /// 获取标的当前价
        /// 获取标的的当期价，等同于最新tick中的当前价
        /// </summary>
        /// <param name="code">标的代码，多个标的使用,分隔。建议每次请求的标的都是相同类型</param>
        /// <returns>
        /// code: 标的代码
        /// current: 当前价格
        /// 
        /// code,current
        /// 000001.XSHE,13.35
        /// 600600.XSHG,42.4
        /// </returns>
        public string GetCurrentPrice(string code)
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_current_price",
                Token,
                code
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// 获取股票和基金复权因子
        /// 根据交易时间获取股票和基金复权因子值 
        /// </summary>
        /// <param name="code">单只标的代码</param>
        /// <param name="fq">复权选项 - pre 前复权； post后复权</param>
        /// <param name="date">开始日期</param>
        /// <param name="end_date">结束日期</param>
        /// <returns>
        /// date: 对应交易日
        /// 标的因子值
        /// 
        /// date,000001.XSHE
        /// 2019-06-25,0.989576
        /// 2019-06-26,1.000000
        /// </returns>
        public string GetFqFactor(string code, string fq, string date, string end_date)
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_fq_factor",
                Token,
                code,
                fq,
                date,
                end_date
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// 获取停牌股票列表
        /// 获取某日停牌股票列表
        /// </summary>
        /// <param name="date">查询日期，date为空时默认为今天</param>
        /// <returns>
        /// 股票代码列表
        /// 
        /// 000029.XSHE
        /// 000333.XSHE
        /// </returns>
        public string GetPauseStocks(string date)
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_pause_stocks",
                Token,
                date
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// 获取集合竞价时的tick数据
        /// 获取指定时间区间内集合竞价时的tick数据
        /// </summary>
        /// <param name="code">标的代码， 多个标的使用,分隔。支持最多100个标的查询。</param>
        /// <param name="date">开始日期</param>
        /// <param name="end_date">结束日期</param>
        /// <returns>
        /// code: 标的代码
        /// time 时间 datetime
        /// current 当前价 float
        /// volume 累计成交量（股）
        /// money 累计成交额
        /// a1_v ~a5_v: 五档卖量
        /// a1_p~a5_p: 五档卖价
        /// b1_v~b5_v: 五档买量
        /// b1_p~b5_p: 五档买价
        /// 
        /// code,time,current,volume,money,a1_v,a2_v,a3_v,a4_v,a5_v,a1_p,a2_p,a3_p,a4_p,a5_p,b1_v,b2_v,b3_v,b4_v,b5_v,b1_p,b2_p,b3_p,b4_p,b5_p
        /// 000001.XSHE,2019-09-20 09:25:03,14.9500,3917700,5856.9600,511751,55200,46700,471356,806000,14.9500,14.9600,14.9700,14.9800,14.9900,556400,229100,151400,179600,115500,14.9400,14.9300,14.9200,14.9100,14.9000
        /// 000002.XSHE,2019-09-20 09:25:03,26.8900,260700,701.0200,17280,16500,3700,1500,4700,26.8900,26.9000,26.9100,26.9200,26.9300,700,53000,700,6100,44100,26.8700,26.8500,26.8400,26.8200,26.8100
        /// </returns>
        public string GetCallAuction(string code, string date, string end_date)
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_call_auction",
                Token,
                code,
                date,
                end_date
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// 获取最新的 tick 数据
        /// </summary>
        /// <param name="code">标的代码， 支持股票、指数、基金、期货等。 不可以使用主力合约和指数合约代码。</param>
        /// <returns>
        /// time: 时间
        /// current: 当前价
        /// high: 截至到当前时刻的日内最高价
        /// low: 截至到当前时刻的日内最低价
        /// volume: 累计成交量
        /// money: 累计成交额
        /// position: 持仓量，期货使用
        /// a1_v~a5_v: 五档卖量
        /// a1_p~a5_p: 五档卖价
        /// b1_v~b5_v: 五档买量
        /// b1_p~b5_p: 五档买价
        /// 
        /// time,current,high,low,volume,money,position,a1_v,a2_v,a3_v,a4_v,a5_v,a1_p,a2_p,a3_p,a4_p,a5_p,b1_v,b2_v,b3_v,b4_v,b5_v,b1_p,b2_p,b3_p,b4_p,b5_p
        /// 20190129150003.000,11.0,11.07,10.77,82663110,904847854.07,,302833,195900,453000,437662,861700,11.0,11.01,11.02,11.03,11.04,502000,113100,102100,186800,176700,10.99,10.98,10.97,10.96,10.95
        /// </returns>
        public string GetCurrentTick(string code)
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_current_tick",
                Token,
                code
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// 获取多标的最新的 tick 数据
        /// </summary>
        /// <param name="code">标的代码， 多个标的使用,分隔。每次请求的标的必须是相同类型。标的类型包括： 股票、指数、场内基金、期货、期权</param>
        /// <returns>
        /// code: 标的代码
        /// time: 时间
        /// current: 当前价
        /// high: 截至到当前时刻的日内最高价
        /// low: 截至到当前时刻的日内最低价
        /// volume: 累计成交量
        /// money: 累计成交额
        /// position: 持仓量，期货使用
        /// a1_v~a5_v: 五档卖量
        /// a1_p~a5_p: 五档卖价
        /// b1_v~b5_v: 五档买量
        /// b1_p~b5_p: 五档买价
        /// 
        /// code,time,current,high,low,volume,money,position,a1_v,a2_v,a3_v,a4_v,a5_v,a1_p,a2_p,a3_p,a4_p,a5_p,b1_v,b2_v,b3_v,b4_v,b5_v,b1_p,b2_p,b3_p,b4_p,b5_p
        /// 000001.XSHE,20190408113000.000,14.05,14.43,13.89,117052113,1666868688.81,,9200,600,9200,50641,71600,14.06,14.07,14.08,14.09,14.1,53900,21800,147100,94536,213900,14.05,14.04,14.03,14.02,14.01
        /// 000002.XSHE,20190408113000.000,31.58,32.8,31.54,42866139,1379238397.85,,700,9400,700,6700,1100,31.58,31.6,31.62,31.66,31.68,12732,8900,10600,87800,44500,31.55,31.54,31.53,31.52,31.51
        /// </returns>
        public string GetCurrentTicks(string code)
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_current_ticks",
                Token,
                code
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// 获取tick数据
        /// 股票部分， 支持 2010-01-01 至今的tick数据，提供买五卖五数据 
        /// 
        /// 期货部分， 支持 2010-01-01 至今的tick数据，提供买一卖一数据。 如果要获取主力合约的tick数据，可以先使用get_dominant_future获取主力合约对应的标的 期权部分，支持 2017-01-01 至今的tick数据，提供买五卖五数据
        /// </summary>
        /// <remarks>
        /// 如果时间跨度太大、数据量太多则可能导致请求超时，所有请控制好data-end_date之间的间隔！
        /// </remarks>
        /// <param name="code">证券代码</param>
        /// <param name="count">取出指定时间区间内前多少条的tick数据，如不填count，则返回end_date一天内的全部tick</param>
        /// <param name="end_date">结束日期，格式2018-07-03或2018-07-03 10:40:00</param>
        /// <param name="skip">默认为true，过滤掉无成交变化的tick数据； 当skip=false时，返回的tick数据会保留从2019年6月25日以来无成交有盘口变化的tick数据。 由于期权成交频率低，所以建议请求期权数据时skip设为false</param>
        /// <returns>
        /// time: 时间
        /// current: 当前价
        /// high: 当日最高价
        /// low: 当日最低价
        /// volume: 累计成交量（手）
        /// money: 累计成交额
        /// position: 持仓量，期货使用
        /// a1_v~a5_v: 五档卖量
        /// a1_p~a5_p: 五档卖价
        /// b1_v~b5_v: 五档买量
        /// b1_p~b5_p: 五档买价
        /// 
        /// time,current,high,low,volume,money,position,a1_p,a1_v,b1_p,b1_v
        /// 2019-03-27 21:40:11,48480.0,48500.0,48430.0,5458.0,1322750300.0,128176.0,48480.0,8.0,48470.0,18.0
        /// 2019-03-27 21:40:11.500000,48480.0,48500.0,48430.0,5464.0,1324204700.0,128178.0,48490.0,39.0,48470.0,26.0
        /// </returns>
        public string GetTicks(string code, string count, string end_date, bool skip = true)
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_ticks",
                Token,
                code,
                count,
                end_date,
                skip
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// 按时间段获取tick数据
        /// 股票部分， 支持 2010-01-01 至今的tick数据，提供买五卖五数据 期货部分， 支持 2010-01-01 至今的tick数据，提供买一卖一数据。 如果要获取主力合约的tick数据，可以先使用get_dominant_future获取主力合约对应的标的 期权部分，支持 2017-01-01 至今的tick数据，提供买五卖五数据
        /// </summary>
        /// <remarks>
        /// 如果时间跨度太大、数据量太多则可能导致请求超时，所有请控制好data-end_date之间的间隔！
        /// </remarks>
        /// <param name="code">证券代码</param>
        /// <param name="date">开始时间，格式2018-07-03或2018-07-03 10:40:00</param>
        /// <param name="end_date">结束时间，格式2018-07-03或2018-07-03 10:40:00</param>
        /// <param name="skip">默认为true，过滤掉无成交变化的tick数据； 当skip=false时，返回的tick数据会保留从2019年6月25日以来无成交有盘口变化的tick数据。</param>
        /// <returns>
        /// time: 时间
        /// current: 当前价
        /// high: 当日最高价
        /// low: 当日最低价
        /// volume: 累计成交量（手）
        /// money: 累计成交额
        /// position: 持仓量，期货使用
        /// a1_v~a5_v: 五档卖量
        /// a1_p~a5_p: 五档卖价
        /// b1_v~b5_v: 五档买量
        /// b1_p~b5_p: 五档买价
        /// 
        /// time,current,high,low,volume,money,position,a1_p,a1_v,b1_p,b1_v
        /// 2019-03-27 21:40:11,48480.0,48500.0,48430.0,5458.0,1322750300.0,128176.0,48480.0,8.0,48470.0,18.0
        /// 2019-03-27 21:40:11.500000,48480.0,48500.0,48430.0,5464.0,1324204700.0,128178.0,48490.0,39.0,48470.0,26.0
        /// </returns>
        public string GetTicksPeriod(string code, string date, string end_date, bool skip)
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_ticks_period",
                Token,
                code,
                date,
                end_date,
                skip
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// 获取基金净值/股票是否st/期货结算价和持仓量等
        /// </summary>
        /// <param name="code">证券代码</param>
        /// <param name="date">开始日期</param>
        /// <param name="end_date">结束日期</param>
        /// <returns>
        /// date: 日期
        /// is_st: 是否是ST，是则返回 1，否则返回 0。股票使用
        /// acc_net_value: 基金累计净值。基金使用
        /// unit_net_value: 基金单位净值。基金使用
        /// futures_sett_price: 期货结算价。期货使用
        /// futures_positions: 期货持仓量。期货使用
        /// adj_net_value: 场外基金的复权净值。场外基金使用
        /// 
        /// date,is_st
        /// 2018-05-29,0
        /// 2018-05-30,0
        /// </returns>
        public string GetExtras(string code, string date, string end_date)
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_extras",
                Token,
                code,
                date,
                end_date
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// 基金基础信息数据接口
        /// 获取单个基金的基本信息
        /// </summary>
        /// <param name="code">基金代码</param>
        /// <param name="date">查询日期， 默认日期是今天。</param>
        /// <returns>
        /// fund_name: 基金全称
        /// fund_type: 基金类型
        /// fund_establishment_day: 基金成立日
        /// fund_manager: 基金管理人及基本信息
        /// fund_management_fee: 基金管理费
        /// fund_custodian_fee: 基金托管费
        /// fund_status: 基金申购赎回状态
        /// fund_size: 基金规模（季度）
        /// fund_share: 基金份额（季度）
        /// fund_asset_allocation_proportion: 基金资产配置比例（季度）
        /// heavy_hold_stocks: 基金重仓股（季度）
        /// heavy_hold_stocks_proportion: 基金重仓股占基金资产净值比例（季度）
        /// heavy_hold_bond: 基金重仓债券（季度）
        /// heavy_hold_bond_proportion: 基金重仓债券占基金资产净值比例（季度）
        /// 
        /// <code>
        /// {
        /// "fund_name": "海富通欣荣灵活配置混合型证券投资基金C类",
        /// "fund_type": "混合型",
        /// "fund_establishment_day": "2016-09-22",
        /// "fund_manager": "海富通基金管理有限公司",
        /// "fund_management_fee": "",
        /// "fund_custodian_fee": "",
        /// "fund_status": "",
        /// "fund_size": "",
        /// "fund_share": 32345.96,
        /// "fund_asset_allocation_proportion": "",
        /// "heavy_hold_stocks": ["600519","601318","601398","000651","600887","600028","000338","600048","601939","000858"],
        /// "heavy_hold_stocks_proportion": 37.16,
        /// "heavy_hold_bond": ["111893625","018005","113014"],
        /// "heavy_hold_bond_proportion": 13.209999999999999
        /// }
        /// </code>
        /// </returns>
        public string GetFundInfo(string code, string date)
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_fund_info",
                Token,
                code,
                date
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// 获取指数成份股
        /// 获取一个指数给定日期在平台可交易的成分股列表
        /// </summary>
        /// <param name="code">指数代码</param>
        /// <param name="date">查询日期</param>
        /// <returns>
        /// 股票代码
        /// 
        /// 000001.XSHE
        /// 000002.XSHE
        /// </returns>
        public string GetIndexStocks(string code, string date)
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_index_stocks",
                Token,
                code,
                date
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// 获取指数成份股权重（月度）
        /// 获取指数成份股给定日期的权重数据，每月更新一次
        /// </summary>
        /// <param name="code">代表指数的标准形式代码， 形式：指数代码.交易所代码，例如"000001.XSHG"。</param>
        /// <param name="date">查询权重信息的日期，形式："%Y-%m-%d"，例如"2018-05-03"；</param>
        /// <returns>
        /// code: 指数代码        display_name: 股票名称        date: 日期        weight: 权重
        /// 
        /// code,display_name,date,weight
        /// 000001.XSHE,平安银行,2018-01-09,0.9730
        /// 000002.XSHE,万科A,2018-01-09,1.2870
        /// </returns>
        public string GetIndexWeights(string code, string date)
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_index_weights",
                Token,
                code,
                date
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// 获取行业列表
        /// 按照行业分类获取行业列表
        /// </summary>
        /// <param name="code">行业代码:sw_l1: 申万一级行业,jq_l1: 聚宽一级行业,jq_l2: 聚宽二级行业,zjw: 证监会行业</param>
        /// <returns>
        /// index: 行业代码
        /// name: 行业名称
        /// start_date: 开始日期
        /// 
        /// index,name,start_date
        /// 850111,种子生产III,2014-02-21
        /// 850112,粮食种植III,2014-02-21
        /// </returns>
        public string GetIndestries(string code)
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_industries",
                Token,
                code
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// 查询股票所属行业
        /// </summary>
        /// <param name="code">行业编码</param>
        /// <param name="date">查询日期</param>
        /// <returns>
        /// 返回股票代码的list
        /// 
        /// 000001.XSHE
        /// 000002.XSHE
        /// </returns>
        public string GetIndustry(string code, string date)
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_industry",
                Token,
                code,
                date
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// 获取行业成份股
        /// 获取在给定日期一个行业的所有股票
        /// </summary>
        /// <param name="code">行业编码</param>
        /// <param name="date">查询日期</param>
        /// <returns>
        /// 返回股票代码的list
        /// 
        /// 000001.XSHE
        /// 000002.XSHE
        /// </returns>
        public string GetIndestryStocks(string code, string date)
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_industry_stocks",
                Token,
                code,
                date
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// 获取概念列表
        /// 获取概念板块列表
        /// </summary>
        /// <returns>
        /// code: 概念代码 name: 概念名称 start_date: 开始日期
        /// 
        /// code,name,start_date
        /// GN001,参股金融,2013-12-31
        /// GN028,智能电网,2013-12-31
        /// </returns>
        public string GetConcepts()
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_concepts",
                Token
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// 获取概念成份股
        /// 获取在给定日期一个概念板块的所有股票
        /// </summary>
        /// <param name="code">概念板块编码</param>
        /// <param name="date">查询日期,</param>
        /// <returns>
        /// 股票代码
        /// 000791.XSHE
        /// 000836.XSHE
        /// </returns>
        public string GetConceptStocks(string code, string date)
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_concept_stocks",
                Token,
                code,
                date
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// 获取资金流信息
        /// 获取一只股票在一个时间段内的资金流向数据，仅包含股票数据，不可用于获取期货数据
        /// </summary>
        /// <returns></returns>
        /// <param name="code">股票代码</param>
        /// <param name="date">开始日期</param>
        /// <param name="end_date">结束日期</param>
        /// <returns>
        /// date: 日期
        /// sec_code: 股票代码
        /// change_pct: 涨跌幅(%)
        /// net_amount_main: 主力净额(万) : 主力净额 = 超大单净额 + 大单净额
        /// net_pct_main: 主力净占比(%) : 主力净占比 = 主力净额 / 成交额
        /// net_amount_xl: 超大单净额(万) : 超大单：大于等于50万股或者100万元的成交单
        /// net_pct_xl: 超大单净占比(%) : 超大单净占比 = 超大单净额 / 成交额
        /// net_amount_l: 大单净额(万) : 大单：大于等于10万股或者20万元且小于50万股或者100万元的成交单
        /// net_pct_l: 大单净占比(%) : 大单净占比 = 大单净额 / 成交额
        /// net_amount_m: 中单净额(万) : 中单：大于等于2万股或者4万元且小于10万股或者20万元的成交单
        /// net_pct_m: 中单净占比(%) : 中单净占比 = 中单净额 / 成交额
        /// net_amount_s: 小单净额(万) : 小单：小于2万股或者4万元的成交单
        /// net_pct_s: 小单净占比(%) : 小单净占比 = 小单净额 / 成交额
        /// 
        /// 
        /// date,sec_code,change_pct,net_amount_main,net_pct_main,net_amount_xl,net_pct_xl,net_amount_l,net_pct_l,net_amount_m,net_pct_m,net_amount_s,net_pct_s
        /// 2016-02-01,000001.XSHE,-2.00,-6940.54,-16.82,-5296.92,-12.84,-1643.63,-3.98,3782.95,9.17,3157.59,7.65
        /// 2016-02-02,000001.XSHE,1.53,1375.48,3.74,2235.87,6.09,-860.39,-2.34,-194.21,-0.53,-1181.27,-3.22
        /// </returns>
        public string GetMoneyFlow(string code, string date, string end_date)
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_money_flow",
                Token,
                code,
                date,
                end_date
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// 获取龙虎榜数据
        /// run_query api 是模拟了JQDataSDK run_query方法获取财务、宏观、期权等数据 可查询的数据内容请查看JQData文档
        /// </summary>
        /// <param name="table">要查询的数据库和表名，格式为 database + . + tablename 如finance.STK_XR_XD</param>
        /// <param name="columns">所查字段，为空时则查询所有字段，多个字段中间用,分隔。如id,company_id，columns不能有空格等特殊字符</param>
        /// <param name="conditions">查询条件，可以为空，格式为report_date#&gt;=#2006-12-01&amp;report_date#&lt;=#2006-12-31，条件内部#号分隔，格式： column # 判断符 # value，多个条件使用&amp;号分隔，表示and，conditions不能有空格等特殊字符</param>
        /// <param name="count">查询条数，count为空时默认1条，最多查询1000条</param>
        /// <returns>
        /// 返回的结果顺序为生成时间的顺序
        /// 
        /// company_id,company_name,code,report_date
        /// 420600103,福建省青山纸业股份有限公司,600103.XSHG,2006-12-01
        /// </returns>
        public string GetBillboardList(string table, string columns, string conditions, int count)
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "run_query",
                Token,
                table,
                columns,
                conditions,
                count
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// 获取融资融券信息
        /// 获取一只股票在一个时间段内的融资融券信息
        /// </summary>
        /// <param name="code">股票代码</param>
        /// <param name="date">开始日期</param>
        /// <param name="end_date">结束日期</param>
        /// <returns>
        /// date: 日期
        /// sec_code: 股票代码
        /// fin_value: 融资余额(元）
        /// fin_buy_value: 融资买入额（元）
        /// fin_refund_value: 融资偿还额（元）
        /// sec_value: 融券余量（股）
        /// sec_sell_value: 融券卖出量（股）
        /// sec_refund_value: 融券偿还量（股）
        /// fin_sec_value: 融资融券余额（元）
        /// 
        /// date,sec_code,fin_value,fin_buy_value,fin_refund_value,sec_value,sec_sell_value,sec_refund_value,fin_sec_value
        /// 2016-01-04,000001.XSHE,3472611852,152129217,169414153,594640,184100,317900,3479349123
        /// </returns>
        public string GetMtss(string code, string date, string end_date)
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_mtss",
                Token,
                code,
                date,
                end_date
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// 获取融资标的列表
        /// </summary>
        /// <param name="date">查询日期，默认为前一交易日</param>
        /// <returns>
        /// 返回指定日期上交所、深交所披露的的可融资标的列表
        /// 
        /// 000001.XSHE
        /// 000002.XSHE
        /// </returns>
        public string GetMargincashStocks(string date)
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_margincash_stocks",
                Token,
                date
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// 获取融券标的列表
        /// </summary>
        /// <param name="date">查询日期，默认为前一交易日</param>
        /// <returns>
        /// 返回指定日期上交所、深交所披露的的可融券标的列表
        /// 000001.XSHE
        /// 000002.XSHE
        /// </returns>
        public string GetMarginsecStocks(string date)
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_marginsec_stocks",
                Token,
                date
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// 获取限售解禁数据
        /// 获取指定日期区间内的限售解禁数据
        /// </summary>
        /// <param name="code">股票代码</param>
        /// <param name="date">开始日期</param>
        /// <param name="end_date">结束日期</param>
        /// <returns>
        /// day: 解禁日期,code: 股票代码,num: 解禁股数,rate1: 解禁股数/总股本,rate2: 解禁股数/总流通股本
        /// 
        /// day,code,num,rate1,rate2
        /// 2010-09-29,600000.XSHG,1175406872.0000,0.1024,0.1141
        /// </returns>
        public string GetLockedShares(string code, string date, string end_date)
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_locked_shares",
                Token,
                code,
                date,
                end_date
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// 获取指定范围交易日
        /// 获取指定日期范围内的所有交易日
        /// </summary>
        /// <param name="date">开始日期</param>
        /// <param name="end_date">结束日期</param>
        /// <returns>
        /// 交易日日期
        /// 2018-10-09
        /// 2018-10-10
        /// </returns>
        public string GetTradeDays(string date, string end_date)
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_trade_days",
                Token,
                date,
                end_date
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// 获取所有交易日
        /// </summary>
        /// <returns>
        /// 2005-01-04
        /// 2005-01-05
        /// </returns>
        public string GetAllTradeDays()
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_all_trade_days",
                Token
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// 获取期货可交易合约列表
        /// 获取某期货品种在指定日期下的可交易合约标的列表
        /// </summary>
        /// <param name="code">期货合约品种，如 AG (白银)</param>
        /// <param name="date">指定日期</param>
        /// <returns>
        /// 某一期货品种在指定日期下的可交易合约标的列表
        /// <code>
        /// AU1701.XSGE
        /// AU1702.XSGE
        /// </code>
        /// </returns>
        public string GetFutureContracts(string code, string date)
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_future_contracts",
                Token,
                code,
                date
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// 获取主力合约
        /// </summary>
        /// <param name="code"> 期货合约品种，如 AG (白银)</param>
        /// <param name="date">指定日期参数，获取历史上该日期的主力期货合约</param>
        /// <returns>
        /// 主力合约对应的期货合约
        /// AU1812.XSGE
        /// </returns>
        public string GetDominantFuture(string code, DateTime date)
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_dominant_future",
                Token,
                code,
                date = date.ToString("yyyy-MM-dd")
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// 模拟JQDataSDK的run_query方法
        /// run_query api 是模拟了JQDataSDK run_query方法获取财务、宏观、期权等数据 
        /// </summary>
        /// <param name="table">要查询的数据库和表名，格式为 database + . + tablename 如finance.STK_XR_XD</param>
        /// <param name="columns">所查字段，为空时则查询所有字段，多个字段中间用,分隔。如id,company_id，columns不能有空格等特殊字符</param>
        /// <param name="conditions">查询条件，可以为空，格式为report_date#&gt;=#2006-12-01&amp;report_date#&lt;=#2006-12-31，条件内部#号分隔，格式： column # 判断符 # value，多个条件使用&amp;号分隔，表示and，conditions不能有空格等特殊字符</param>
        /// <param name="count">查询条数，count为空时默认1条，最多查询1000条</param>
        /// <returns>
        /// 返回的结果顺序为生成时间的顺序
        /// <code>
        /// company_id,company_name,code,report_date
        /// 420600103,福建省青山纸业股份有限公司,600103.XSHG,2006-12-01
        /// </code>
        /// </returns>
        public string RunQuery(string table, string columns, string conditions, int count)
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "run_query",
                Token,
                table,
                columns,
                conditions,
                count
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// 获取基本财务数据
        /// 查询股票的市值数据、资产负债数据、现金流数据、利润数据、财务指标数据.
        /// </summary>
        /// <param name="table">要查询表名，可选项balance，income，cash_flow，indicator，valuation，bank_indicator，security_indicator，insurance_indicator</param>
        /// <param name="columns">所查字段，为空时则查询所有字段，多个字段中间用,分隔。如id,company_id，columns不能有空格等特殊字符</param>
        /// <param name="code">证券代码，多个标的使用,分隔</param>
        /// <param name="date">查询日期2019-03-04或者年度2018或者季度2018q1 2018q2 2018q3 2018q4</param>
        /// <param name="count">查询条数，最多查询1000条。不填count时按date查询</param>
        /// <returns>
        /// 返回的结果按日期顺序
        /// <code>
        /// code,day,pb_ratio,ps_ratio,capitalization,circulating_cap
        /// 000001.XSHE,2016-12-03,0.9198,1.5328,1717041.1250,1463118.0000
        /// </code>
        /// </returns>
        public string GetFundamentals(string table, string columns, string code, string date, int count)
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_fundamentals",
                Token,
                table,
                columns,
                code,
                date,
                count
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// 获取聚宽因子库中所有因子的信息
        /// </summary>
        /// <returns>
        /// factor 因子代码,factor_intro 因子名称,category 因子分类,category_intro 分类名称
        /// <code>
        /// factor,factor_intro,category,category_intro
        /// administration_expense_ttm,管理费用TTM,basics,基础科目及衍生类因子
        /// </code>
        /// </returns>
        public string GetAllFactors()
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_all_factors",
                Token
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// 聚宽因子库数据
        /// run_query api 是模拟了JQDataSDK run_query方法获取财务、宏观、期权等数据
        /// </summary>
        /// <param name="table">要查询的数据库和表名，格式为 database + . + tablename 如finance.STK_XR_XD</param>
        /// <param name="columns">所查字段，为空时则查询所有字段，多个字段中间用,分隔。如id,company_id，columns不能有空格等特殊字符</param>
        /// <param name="conditions">查询条件，可以为空，格式为report_date#&gt;=#2006-12-01&amp;report_date#&lt;=#2006-12-31，条件内部#号分隔，格式： column # 判断符 # value，多个条件使用&amp;号分隔，表示and，conditions不能有空格等特殊字符</param>
        /// <param name="count">查询条数，count为空时默认1条，最多查询1000条</param>
        /// <returns>
        /// 返回的结果顺序为生成时间的顺序
        /// company_id,company_name,code,report_date
        /// 420600103,福建省青山纸业股份有限公司,600103.XSHG,2006-12-01
        /// </returns>
        public string GetFactorValues(string table, string columns, string conditions, int count)
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "run_query",
                Token,
                table,
                columns,
                conditions,
                count
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// 获取 Alpha 101 因子
        /// 因子来源： 根据 WorldQuant LLC 发表的论文 101 Formulaic Alphas 中给出的 101 个 Alphas 因子公式，我们将公式编写成了函数，方便大家使用
        /// 
        /// </summary>
        /// <param name="code">标的代码， 多个标的使用,分隔。建议每次请求的标的都是相同类型。支持最多1000个标的查询</param>
        /// <param name="func_name"> 查询函数名称，如alpha_001，alpha_002等</param>
        /// <param name="date">查询日期</param>
        /// <returns>
        /// 股票代码,因子值
        /// code,alpha_001
        /// 000001.XSHE,0.17
        /// </returns>
        public string GetAlpha101(string code, string func_name, string date)
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_alpha101",
                Token,
                code,
                func_name,
                date
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// 获取 Alpha 191 因子
        /// 因子来源： 根据国泰君安数量化专题研究报告 - 基于短周期价量特征的多因子选股体系给出了 191 个短周期交易型阿尔法因子。为了方便用户快速调用，我们将所有Alpha191因子基于股票的后复权价格做了完整的计算。用户只需要指定fq='post'即可获取全新计算的因子数据。
        /// </summary>
        /// <param name="code">标的代码， 多个标的使用,分隔。建议每次请求的标的都是相同类型。支持最多1000个标的查询</param>
        /// <param name="func_name">查询函数名称，如alpha_001，alpha_002等</param>
        /// <param name="date">查询日期</param>
        /// <returns>
        /// 股票代码,因子值
        /// <code>
        /// code,alpha_003
        /// 000001.XSHE,0.55000000
        /// 000002.XSHE,0.27000000
        /// 000004.XSHE,-0.17000000
        /// </code>
        /// </returns>
        public string GetAlpha191(string code, string func_name, string date)
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_alpha191",
                Token,
                code,
                func_name,
                date
            });
            var result = PostRequest(body);
            return result;
        }
    }
}
