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
        /// <returns>Token</returns>
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
        /// <returns></returns>
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
        /// <returns></returns>
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
            int.TryParse(result, out var c);
            return c;
        }
        /// <summary>
        /// 获取所有标的信息
        /// 获取平台支持的所有股票、基金、指数、期货信息
        /// </summary>
        /// <param name="code">stock(股票)，fund,index(指数)，futures,etf(ETF基金)，lof,fja（分级A），fjb（分级B）</param>
        /// <param name="date">为空表示所有日期的标的</param>
        /// <returns></returns>
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
                date,
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
        /// <param name="code"></param>
        /// <param name="count"></param>
        /// <param name="unit">bar的时间单位, 支持如下周期：1m, 5m, 15m, 30m, 60m, 120m, 1d, 1w, 1M</param>
        /// <param name="end_date"></param>
        /// <param name="fq_ref_date"></param>
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
        /// 获取指定时间周期的行情数据,与GetPrice相同
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
        /// <returns></returns>
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
        /// <returns>当前价格</returns>
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
        /// <returns></returns>
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
        /// <returns></returns>
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
        /// <returns></returns>
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
        /// <returns></returns>
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
        /// <returns></returns>
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
        /// <param name="code">证券代码</param>
        /// <param name="count">取出指定时间区间内前多少条的tick数据，如不填count，则返回end_date一天内的全部tick</param>
        /// <param name="end_date">结束日期，格式2018-07-03或2018-07-03 10:40:00</param>
        /// <param name="skip">默认为true，过滤掉无成交变化的tick数据； 当skip=false时，返回的tick数据会保留从2019年6月25日以来无成交有盘口变化的tick数据。 由于期权成交频率低，所以建议请求期权数据时skip设为false</param>
        /// <returns></returns>
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
        /// <returns></returns>
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
        /// todo:获取基金净值/股票是否st/期货结算价和持仓量等
        /// </summary>
        /// <returns></returns>
        public string GetExtras()
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_extras",
                Token,
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// todo:基金基础信息数据接口
        /// 获取单个基金的基本信息
        /// </summary>
        /// <returns></returns>
        public string GetFundInfo()
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_fund_info",
                Token,
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// todo:获取指数成份股
        /// 获取一个指数给定日期在平台可交易的成分股列表
        /// </summary>
        /// <returns></returns>
        public string GetIndexStocks()
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_index_stocks",
                Token,
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// todo:获取指数成份股权重（月度）
        /// 获取指数成份股给定日期的权重数据，每月更新一次
        /// </summary>
        /// <returns></returns>
        public string GetIndexWeights()
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_index_weights",
                Token,
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// todo:获取行业列表
        /// 按照行业分类获取行业列表
        /// </summary>
        /// <returns></returns>
        public string GetIndestries()
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_industries",
                Token,
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// todo:查询股票所属行业
        /// </summary>
        /// <returns></returns>
        public string GetIndustry()
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_industry",
                Token,
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// todo:获取行业成份股
        /// 获取在给定日期一个行业的所有股票
        /// </summary>
        /// <returns></returns>
        public string GetIndestryStocks()
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_industry_stocks",
                Token,
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// todo:获取概念列表
        /// 获取概念板块列表
        /// </summary>
        /// <returns></returns>
        public string GetConcepts()
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_concepts",
                Token,
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// todo:获取概念成份股
        /// 获取在给定日期一个概念板块的所有股票
        /// </summary>
        /// <returns></returns>
        public string GetConceptStocks()
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_concept_stocks",
                Token,
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// todo:获取资金流信息
        /// 获取一只股票在一个时间段内的资金流向数据，仅包含股票数据，不可用于获取期货数据
        /// </summary>
        /// <returns></returns>
        public string GetMoneyFlow()
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_money_flow",
                Token,
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// todo:获取龙虎榜数据
        /// run_query api 是模拟了JQDataSDK run_query方法获取财务、宏观、期权等数据 可查询的数据内容请查看JQData文档
        /// </summary>
        /// <returns></returns>
        public string GetBillboardList()
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "run_query",
                Token,
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// todo:获取融资融券信息
        /// 获取一只股票在一个时间段内的融资融券信息
        /// </summary>
        /// <returns></returns>
        public string GetMtss()
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_mtss",
                Token,
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// todo:获取融资标的列表
        /// </summary>
        /// <returns></returns>
        public string GetMargincashStocks()
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_margincash_stocks",
                Token,
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// todo:获取融券标的列表
        /// </summary>
        /// <returns></returns>
        public string GetMarginsecStocks()
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_marginsec_stocks",
                Token,
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// todo:获取限售解禁数据
        /// 获取指定日期区间内的限售解禁数据
        /// </summary>
        /// <returns></returns>
        public string GetLockedShares()
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_locked_shares",
                Token,
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// todo:获取指定范围交易日
        /// 获取指定日期范围内的所有交易日
        /// </summary>
        /// <returns></returns>
        public string GetTradeDays()
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_trade_days",
                Token,
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// todo:获取所有交易日
        /// </summary>
        /// <returns></returns>
        public string GetAllTradeDays()
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_all_trade_days",
                Token,
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// todo:获取期货可交易合约列表
        /// 获取某期货品种在指定日期下的可交易合约标的列表
        /// </summary>
        /// <returns></returns>
        public string GetFutureContracts()
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_future_contracts",
                Token,
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// 获取主力合约
        /// </summary>
        /// <param name="code"></param>
        /// <param name="date"></param>
        /// <returns></returns>
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
        /// todo:模拟JQDataSDK的run_query方法
        /// run_query api 是模拟了JQDataSDK run_query方法获取财务、宏观、期权等数据 
        /// </summary>
        /// <returns></returns>
        public string RunQuery()
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "run_query",
                Token,
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// todo:获取基本财务数据
        /// 查询股票的市值数据、资产负债数据、现金流数据、利润数据、财务指标数据.
        /// </summary>
        /// <returns></returns>
        public string GetFundamentals()
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_fundamentals",
                Token,
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// todo:获取聚宽因子库中所有因子的信息
        /// </summary>
        /// <returns></returns>
        public string GetAllFactors()
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_all_factors",
                Token,
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// todo:聚宽因子库数据
        /// run_query api 是模拟了JQDataSDK run_query方法获取财务、宏观、期权等数据
        /// </summary>
        /// <returns></returns>
        public string GetFactorValues()
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "run_query",
                Token,
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// todo:获取 Alpha 101 因子
        /// 因子来源： 根据 WorldQuant LLC 发表的论文 101 Formulaic Alphas 中给出的 101 个 Alphas 因子公式，我们将公式编写成了函数，方便大家使用
        /// 
        /// </summary>
        /// <returns></returns>
        public string GetAlpha101()
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_alpha101",
                Token,
            });
            var result = PostRequest(body);
            return result;
        }
        /// <summary>
        /// todo:获取 Alpha 191 因子
        /// 因子来源： 根据国泰君安数量化专题研究报告 - 基于短周期价量特征的多因子选股体系给出了 191 个短周期交易型阿尔法因子。为了方便用户快速调用，我们将所有Alpha191因子基于股票的后复权价格做了完整的计算。用户只需要指定fq='post'即可获取全新计算的因子数据。
        /// </summary>
        /// <returns></returns>
        public string GetAlpha191()
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Token is empty");
            }
            var body = JsonSerializer.Serialize(new
            {
                method = "get_alpha191",
                Token,
            });
            var result = PostRequest(body);
            return result;
        }
    }
}
