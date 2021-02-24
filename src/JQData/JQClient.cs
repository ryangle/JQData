//-----------------------------------------------------------------------
// <copyright file="JQClient.cs" Author="ryangle">
// https://github.com/ryangle/JQData
// </copyright>
//-----------------------------------------------------------------------
using JQData.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;

namespace JQData
{
    /// <summary>
    /// 每2秒一次请求，否则会限制访问
    /// webapi使用文档：https://dataapi.joinquant.com/docs
    /// </summary>
    public class JQClient
    {
        private HttpClient _httpClient;
        private string _baseUrl = "https://dataapi.joinquant.com/apis";

        public string Token { set; get; }
        /// <summary>
        /// 获取用户凭证
        /// <remarks>
        /// 调用其他获取数据接口之前，需要先调用本接口获取token。token被作为用户认证使用，当天有效
        /// </remarks>
        /// </summary>
        /// <param name="mob">mob是申请JQData时所填写的手机号</param>
        /// <param name="pwd">Password为聚宽官网登录密码，新申请用户默认为手机号后6位</param>
        /// <returns></returns>
        public string GetToken(string mob, string pwd)
        {
            try
            {
                _httpClient?.Dispose();

                _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                string json = JsonConvert.SerializeObject(new
                {
                    method = "get_token",
                    mob = mob,
                    pwd = pwd
                });

                var content = new StringContent(json);
                var resultTok = _httpClient.PostAsync(_baseUrl, content).Result;
                Token = resultTok.Content.ReadAsStringAsync().Result;
            }
            catch (Exception ex)
            {
                throw ex;
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

                string json = JsonConvert.SerializeObject(new
                {
                    method = "get_current_token",
                    mob = mob,
                    pwd = pwd
                });

                var content = new StringContent(json);
                var resultTok = _httpClient.PostAsync(_baseUrl, content).Result;
                Token = resultTok.Content.ReadAsStringAsync().Result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return Token;
        }
        /// <summary>
        /// todo:获取查询剩余条数
        /// </summary>
        /// <returns></returns>
        public int GetQueryCount()
        {
            return 0;
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
            string body = JsonConvert.SerializeObject(new
            {
                method = "get_all_securities",
                token = Token,
                code = code,
                date = date,
            });
            var bodyContent = new StringContent(body);
            var resultReq = _httpClient.PostAsync(_baseUrl, bodyContent).Result;
            var securityInfo = resultReq.Content.ReadAsStringAsync().Result;
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
            string body = JsonConvert.SerializeObject(new
            {
                method = "get_security_info",
                token = Token,
                code = code
            });
            var bodyContent = new StringContent(body);
            var resultReq = _httpClient.PostAsync(_baseUrl, bodyContent).Result;
            var result = resultReq.Content.ReadAsStringAsync().Result;
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
        /// 获取各种时间周期的bar数据，bar的分割方式与主流股票软件相同， 同时还支持返回当前时刻所在 bar 的数据
        /// </summary>
        /// <param name="code"></param>
        /// <param name="count"></param>
        /// <param name="unit">bar的时间单位, 支持如下周期：1m, 5m, 15m, 30m, 60m, 120m, 1d, 1w, 1M</param>
        /// <param name="end_date"></param>
        /// <param name="fq_ref_date"></param>
        public Bar[] GetPrice(string code, int count, string unit, string end_date, string fq_ref_date)
        {
            string body = JsonConvert.SerializeObject(new
            {
                method = "get_price",
                token = Token,
                code = code,
                count = count,
                unit = unit,
                end_date = end_date,
                fq_ref_date = fq_ref_date
            });
            var bodyContent = new StringContent(body);
            var resultReq = _httpClient.PostAsync(_baseUrl, bodyContent).Result;
            var securityInfo = resultReq.Content.ReadAsStringAsync().Result;
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
        /// todo:获取指定时间段的行情数据
        /// 指定开始时间date和结束时间end_date时间段，获取行情数据
        /// </summary>
        /// <returns></returns>
        public string GetPricePeriod()
        {
            return "";
        }
        /// <summary>
        /// 同GetPricePeriod
        /// </summary>
        /// <returns></returns>
        public string GetBarsPeriod()
        {
            return GetPricePeriod();
        }
        /// <summary>
        /// todo:获取标的当前价
        /// 获取标的的当期价，等同于最新tick中的当前价
        /// </summary>
        /// <returns></returns>
        public string GetCurrentPrice()
        {
            return "";
        }
        /// <summary>
        /// todo:获取股票和基金复权因子
        /// 根据交易时间获取股票和基金复权因子值 
        /// </summary>
        /// <returns></returns>
        public string GetFqFactor()
        {
            return "";
        }
        /// <summary>
        /// todo:获取停牌股票列表
        /// 获取某日停牌股票列表
        /// </summary>
        /// <returns></returns>
        public string GetPauseStocks()
        {
            return "";
        }
        /// <summary>
        /// todo:获取集合竞价时的tick数据
        /// 获取指定时间区间内集合竞价时的tick数据
        /// </summary>
        /// <returns></returns>
        public string GetCallAuction()
        {
            return "";
        }
        /// <summary>
        /// todo:获取最新的 tick 数据
        /// </summary>
        /// <returns></returns>
        public string GetCurrentTick()
        {
            return "";
        }
        /// <summary>
        /// todo:获取多标的最新的 tick 数据
        /// </summary>
        /// <returns></returns>
        public string GetCurrentTicks()
        {
            return "";
        }
        /// <summary>
        /// todo:获取tick数据
        /// 股票部分， 支持 2010-01-01 至今的tick数据，提供买五卖五数据 期货部分， 支持 2010-01-01 至今的tick数据，提供买一卖一数据。 如果要获取主力合约的tick数据，可以先使用get_dominant_future获取主力合约对应的标的 期权部分，支持 2017-01-01 至今的tick数据，提供买五卖五数据
        /// </summary>
        /// <returns></returns>
        public string GetTicks()
        {
            return "";
        }
        /// <summary>
        /// todo:按时间段获取tick数据
        /// 股票部分， 支持 2010-01-01 至今的tick数据，提供买五卖五数据 期货部分， 支持 2010-01-01 至今的tick数据，提供买一卖一数据。 如果要获取主力合约的tick数据，可以先使用get_dominant_future获取主力合约对应的标的 期权部分，支持 2017-01-01 至今的tick数据，提供买五卖五数据
        /// </summary>
        /// <returns></returns>
        public string GetTicksPeriod()
        {
            return "";
        }
        /// <summary>
        /// todo:获取基金净值/股票是否st/期货结算价和持仓量等
        /// </summary>
        /// <returns></returns>
        public string GetExtras()
        {
            return "";
        }
        /// <summary>
        /// todo:基金基础信息数据接口
        /// 获取单个基金的基本信息
        /// </summary>
        /// <returns></returns>
        public string GetFundInfo()
        {
            return "";
        }
        /// <summary>
        /// todo:获取指数成份股
        /// 获取一个指数给定日期在平台可交易的成分股列表
        /// </summary>
        /// <returns></returns>
        public string GetIndexStocks()
        {
            return "";
        }
        /// <summary>
        /// todo:获取指数成份股权重（月度）
        /// 获取指数成份股给定日期的权重数据，每月更新一次
        /// </summary>
        /// <returns></returns>
        public string GetIndexWeights()
        {
            return "";
        }
        /// <summary>
        /// todo:获取行业列表
        /// 按照行业分类获取行业列表
        /// </summary>
        /// <returns></returns>
        public string GetIndestries()
        {
            return "";
        }
        /// <summary>
        /// todo:查询股票所属行业
        /// </summary>
        /// <returns></returns>
        public string GetIndustry()
        {
            return "";
        }
        /// <summary>
        /// todo:获取行业成份股
        /// 获取在给定日期一个行业的所有股票
        /// </summary>
        /// <returns></returns>
        public string GetIndestryStocks()
        {
            return "";
        }
        /// <summary>
        /// todo:获取概念列表
        /// 获取概念板块列表
        /// </summary>
        /// <returns></returns>
        public string GetConcepts(){
            return "";
        }
        /// <summary>
        /// todo:获取概念成份股
        /// 获取在给定日期一个概念板块的所有股票
        /// </summary>
        /// <returns></returns>
        public string GetConceptStocks()
        {
            return "";
        }
        /// <summary>
        /// todo:获取资金流信息
        /// 获取一只股票在一个时间段内的资金流向数据，仅包含股票数据，不可用于获取期货数据
        /// </summary>
        /// <returns></returns>
        public string GetMoneyFlow()
        {
            return "";
        }
        /// <summary>
        /// todo:获取龙虎榜数据
        /// run_query api 是模拟了JQDataSDK run_query方法获取财务、宏观、期权等数据 可查询的数据内容请查看JQData文档
        /// </summary>
        /// <returns></returns>
        public string GetBillboardList()
        {
            return "";
        }
        /// <summary>
        /// todo:获取融资融券信息
        /// 获取一只股票在一个时间段内的融资融券信息
        /// </summary>
        /// <returns></returns>
        public string GetMtss()
        {
            return "";
        }
        /// <summary>
        /// todo:获取融资标的列表
        /// </summary>
        /// <returns></returns>
        public string GetMargincashStocks()
        {
            return "";
        }
        /// <summary>
        /// todo:获取融券标的列表
        /// </summary>
        /// <returns></returns>
        public string GetMarginsecStocks()
        {
            return "";
        }
        /// <summary>
        /// todo:获取限售解禁数据
        /// 获取指定日期区间内的限售解禁数据
        /// </summary>
        /// <returns></returns>
        public string GetLockedShares()
        {
            return "";
        }
        /// <summary>
        /// todo:获取指定范围交易日
        /// 获取指定日期范围内的所有交易日
        /// </summary>
        /// <returns></returns>
        public string GetTradeDays()
        {
            return "";
        }
        /// <summary>
        /// todo:获取所有交易日
        /// </summary>
        /// <returns></returns>
        public string GetAllTradeDays()
        {
            return "";
        }
        /// <summary>
        /// todo:获取期货可交易合约列表
        /// 获取某期货品种在指定日期下的可交易合约标的列表
        /// </summary>
        /// <returns></returns>
        public string GetFutureContracts()
        {
            return "";
        }
        /// <summary>
        /// 获取主力合约
        /// </summary>
        /// <param name="code"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public string GetDominantFuture(string code, DateTime date)
        {
            string body = JsonConvert.SerializeObject(new
            {
                method = "get_dominant_future",
                token = Token,
                code = code,
                date = date.ToString("yyyy-MM-dd")
            });
            var bodyContent = new StringContent(body);
            var resultReq = _httpClient.PostAsync(_baseUrl, bodyContent).Result;
            var result = resultReq.Content.ReadAsStringAsync().Result;
            return result;
        }
        /// <summary>
        /// todo:模拟JQDataSDK的run_query方法
        /// run_query api 是模拟了JQDataSDK run_query方法获取财务、宏观、期权等数据 
        /// </summary>
        /// <returns></returns>
        public string RunQuery()
        {
            return "";
        }
        /// <summary>
        /// todo:获取基本财务数据
        /// 查询股票的市值数据、资产负债数据、现金流数据、利润数据、财务指标数据.
        /// </summary>
        /// <returns></returns>
        public string GetFundamentals()
        {
            return "";
        }
        /// <summary>
        /// todo:获取聚宽因子库中所有因子的信息
        /// </summary>
        /// <returns></returns>
        public string GetAllFactors()
        {
            return "";
        }
        /// <summary>
        /// todo:聚宽因子库数据
        /// run_query api 是模拟了JQDataSDK run_query方法获取财务、宏观、期权等数据
        /// </summary>
        /// <returns></returns>
        public string GetFactorValues()
        {
            return "";
        }
        /// <summary>
        /// todo:获取 Alpha 101 因子
        /// 因子来源： 根据 WorldQuant LLC 发表的论文 101 Formulaic Alphas 中给出的 101 个 Alphas 因子公式，我们将公式编写成了函数，方便大家使用
        /// 
        /// </summary>
        /// <returns></returns>
        public string GetAlpha101()
        {
            return "";
        }
        /// <summary>
        /// todo:获取 Alpha 191 因子
        /// 因子来源： 根据国泰君安数量化专题研究报告 - 基于短周期价量特征的多因子选股体系给出了 191 个短周期交易型阿尔法因子。为了方便用户快速调用，我们将所有Alpha191因子基于股票的后复权价格做了完整的计算。用户只需要指定fq='post'即可获取全新计算的因子数据。
        /// </summary>
        /// <returns></returns>
        public string GetAlpha191()
        {
            return "";
        }
    }
}
