using Microsoft.Extensions.Configuration;
using System;
using Xunit;

namespace JQData.Test
{
    public class JQClientTest
    {
        private IConfigurationRoot Configuration { get; set; }
        private JQClient _jqclient;
        public JQClientTest()
        {
            var builder = new ConfigurationBuilder();
            builder.AddUserSecrets<JQClientTest>();

            Configuration = builder.Build();
            _jqclient = new JQClient();
            _jqclient.GetToken(Configuration["JQUser"], Configuration["JQPassword"]);
        }
        [Fact]
        public void GetDominantFutureTest()
        {
            var dominant = _jqclient.GetDominantFuture("AU", new DateTime(2019, 3, 18));
            Assert.NotNull(dominant);
        }

        [Fact]
        public void GetPriceTest()
        {
            var bars = _jqclient.GetPrice("A8888.XDCE", 5000, "1m", "2014-07-31", "");

        }
    }
}
