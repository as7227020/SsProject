using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stock
{
    public class WatchStockClass
    {

        /// <summary>
        /// 單號
        /// </summary>
        public string Order { get; set; }

        /// <summary>
        /// 股票代號
        /// </summary>
        public string Code { get; set; }
        
        /// <summary>
        /// 上櫃 : otc_  / 上市 : tse_
        /// </summary>
        public string otc_Or_tes { get; set; }

        /// <summary>
        /// 購買價
        /// </summary>
        public string BuyPointPrice { get; set; }

        /// <summary>
        /// 賣出價
        /// </summary>
        public string SellPointPrice { get; set; }

        /// <summary>
        /// 創建時間
        /// </summary>
        public DateTime CreateTime { get; set; }

    }

    public enum eBuyAndSell { 
        None = -1,
    BUY = 0,
    SELL = 1
    }

    public class SimulationData
    { 
        /// <summary>
        /// 單號
        /// </summary>
        public string Order { get; set; }

        //股票代號
        public string Code { get; set; }

        /// <summary>
        /// 賣出或買進價格
        /// </summary>
        public string BuyOrSellPrice { get; set; }

        /// <summary>
        /// 是否購買還是賣出
        /// </summary>
        public eBuyAndSell IsBuyOrSell { get; set; }

        /// <summary>
        /// 執行日期
        /// </summary>
        public DateTime BuyDate { get; set; }

       /// <summary>
       /// 第一次登記不算 要買進然後賣出 才算完成交易
       /// </summary>
        public bool IsFinishTrade { get; set; }

        /// <summary>
        /// 目前股價
        /// </summary>
        public float NowPrice { get; set; }

        /// <summary>
        /// 損益
        /// </summary>
        public float NowIncreaseAndDecrease { get; set; }


    }
}
