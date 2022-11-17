using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MyTest
{
    internal class NetModule
    {

        /// <summary>
        /// 取得股票是上市還是上櫃的代號
        /// </summary>
        /// <param name="iCode"></param>
        /// <returns></returns>
        public static void GetMarketType(string iCode , Action<string,string> iCallBack)
        {
            string url = "https://mis.twse.com.tw/stock/api/getStockNames.jsp?n=" + iCode;

            string downloadedData = "";
            using (WebClient wClient = new WebClient())
            {
                // 取得網頁資料
                wClient.Encoding = Encoding.UTF8;
                downloadedData = wClient.DownloadString(url);
                if (downloadedData.Trim().Length > 0)
                {
                    ParseMarketType(downloadedData, iCallBack);
                }
            }
        }


      static void ParseMarketType(string iGetStr , Action<string,string> iCallBack)
        {
            JObject _nodeContainFileList_JObject = JsonConvert.DeserializeObject<JObject>(iGetStr.Trim());
            // var _termsHashJObj = _nodeContainFileList_JObject[RPS_ProjectSettings.termsFileName];

            foreach (var v in _nodeContainFileList_JObject)
            {
                if (v.Key != "datas")
                {
                    continue;
                }

                if (v.Key == "datas")
                {
                    JArray array = JsonConvert.DeserializeObject<JArray>(v.Value.ToString());

                    if (array != null)
                    {
                        foreach (JObject Jobj in array)
                        {
                            //股票代碼
                            string _code = Jobj["c"].ToString();
                            //價格
                            string[] _type = Jobj["key"].ToString().Split('_');
                            //公司名稱 (短)
                            string _name = Jobj["n"].ToString();

                            iCallBack( _type[0], _name);
                            break;
                        }
                        break;
                    }
                }

            }
           // MessageBox.Show("無法取得https://mis.twse.com.tw/stock/api/getStockNames.jsp?n=的資料!");
        }

    }
}
