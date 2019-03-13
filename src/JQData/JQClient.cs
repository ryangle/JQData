﻿using JQData.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;

namespace JQData
{
    public class JQClient
    {
        public static HttpClient HttpClient;
        private string _baseUrl = "https://dataapi.joinquant.com/apis";

        public JQClient()
        {
            HttpClient = new HttpClient();
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public string Token { set; get; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mob">mob是申请JQData时所填写的手机号</param>
        /// <param name="pwd">Password为聚宽官网登录密码，新申请用户默认为手机号后6位</param>
        /// <returns></returns>
        public string GetToken(string mob, string pwd)
        {
            string json = JsonConvert.SerializeObject(new
            {
                method = "get_token",
                mob = mob,
                pwd = pwd
            });

            var content = new StringContent(json);
            var resultTok = HttpClient.PostAsync(_baseUrl, content).Result;
            Token = resultTok.Content.ReadAsStringAsync().Result;
            Sleep();
            return Token;
        }

        /// <summary>
        /// code: 标的代码
        /// display_name: 中文名称
        /// name: 缩写简称
        /// start_date: 上市日期
        /// end_date: 退市日期，如果没有退市则为2200-01-01
        /// type: 类型，stock(股票)，index(指数)，etf(ETF基金)，fja（分级A），fjb（分级B）
        /// </summary>
        /// <param name="token"></param>
        public Security[] QueryAllSecurities(string code, string date)
        {
            string body = JsonConvert.SerializeObject(new
            {
                method = "get_all_securities",
                token = Token,
                code = code,
                date = date,
            });
            var bodyContent = new StringContent(body);
            var resultReq = HttpClient.PostAsync(_baseUrl, bodyContent).Result;
            var securityInfo = resultReq.Content.ReadAsStringAsync().Result;
            Sleep();
            var securitiesStr = securityInfo.Split('\n');
            //跳过表头
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
        /// 
        /// </summary>
        /// <param name="code"></param>
        /// <param name="count"></param>
        /// <param name="unit">bar的时间单位, 支持如下周期：1m, 5m, 15m, 30m, 60m, 120m, 1d, 1w, 1M</param>
        /// <param name="end_date"></param>
        /// <param name="fq_ref_date"></param>
        public string[] GetPrice(string code, int count, string unit, string end_date, string fq_ref_date)
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
            var resultReq = HttpClient.PostAsync(_baseUrl, bodyContent).Result;
            var securityInfo = resultReq.Content.ReadAsStringAsync().Result;
            Sleep();
            return securityInfo.Split('\n');
        }
        private void Sleep()
        {
            Thread.Sleep(2000);
        }
    }
}
