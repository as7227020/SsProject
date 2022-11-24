using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Globalization;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Stock
{
    public partial class Form1 : Form
    {
        DateTime m_StartAppTime;

        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        static extern bool FreeConsole();

        DataTable m_WatchTable;

        /// <summary>
        /// 模擬表
        /// </summary>
        DataTable m_SimulationTable;
        public Form1()
        {
            InitializeComponent();
           
        }
        System.Timers.Timer watchTimer;
        const int m_watchTime = 3000;

        static string NOW_SELL_PRICE = "nowSellPrice";
        static string NOW_BUY_PRICE = "nowBuyPrice";

        void InitMessgaeToOFF()
        {
            button1.Text = "訊息OFF";
            m_ShowMessage = false;
        }

        void MenuExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        void MenuOpenWindow_Click(object sender, EventArgs e)
        {
            this.Show();
            notifyIcon1.Visible = false;
            WindowState = FormWindowState.Normal;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //m_notifyIconList = new List<string>();
            notifyIcon1.ContextMenuStrip = new ContextMenuStrip();
            notifyIcon1.ContextMenuStrip.Items.Add(RUN_START, null, button2_Click);
            notifyIcon1.ContextMenuStrip.Items.Add("訊息OFF", null, button1_Click);
            notifyIcon1.ContextMenuStrip.Items.Add("輸入監測資料", null, button3_Click);
            notifyIcon1.ContextMenuStrip.Items.Add("開啟畫面", null, MenuOpenWindow_Click);
            notifyIcon1.ContextMenuStrip.Items.Add("關閉程式", null, MenuExit_Click);


            AllocConsole();
            InitMessgaeToOFF();
            m_Progrss = "";
            m_countMsg = 0;
            m_AllMsg = false;
            ShowMessage("開始初始化...",false);
            radioButton1.Checked = false;

            //遮擋版開啟
            panel1.Visible = true;

            dataGridView1.ReadOnly = true;
            dataGridView2.ReadOnly = true;

            dataGridView1.AllowUserToAddRows = false;
            dataGridView2.AllowUserToAddRows = false;

            dataGridView1.RowHeadersVisible = false;
            dataGridView2.RowHeadersVisible = false;

            InitSelcetList();


            //初始化每秒觀看功能
            InitWatchTime();
            //初始化DB資料
            InitDataTable();


            Init_dic_UpdateData();

            UpdateWatchTable();
            UpdateSimulationTable();
            UpdateWatchStr();

            //WatchByTest();
            WatchBySettingTime();

            this.Hide();

            m_StartAppTime = DateTime.Now.ToLocalTime();

            StartWatch();
            ShowMessage("初始化完成!",false);

            GetNowStockData();
        }

        string GetRunTime()
        {
            DateTime _now = DateTime.Now.ToLocalTime();
            TimeSpan ts = _now.Subtract(m_StartAppTime); //兩時間天數相減
            return " 目前運作時間  - " + ts.Days + "天 " + ts.Hours + "時" + ts.Minutes + "分" + ts.Seconds + "秒";
        }

        void Init_dic_UpdateData()
        {
            m_dic_UpdateData = new Dictionary<string, UpdateData>();
            foreach (DataRow row in m_WatchTable.Rows)
            {
                UpdateData _update = new UpdateData();

                _update.SetData(NOW_SELL_PRICE, "0");
                _update.SetData(NOW_BUY_PRICE, "0");
                ////拿目前所有要觀察的股票代號
                m_dic_UpdateData.Add(row["代碼"].ToString(), _update);
            }
        }

        /// <summary>
        /// 向DB先拿一次資料
        /// </summary>
        void InitDataTable()
        {
            m_WatchTable = DataModule.GetWatchDB_RealyWatchData(false);

            m_SimulationTable = DataModule.GetSimulationDB_NowStock();
        }

        void UpdateWatchTable()
        {
            dataGridView1.DataSource = m_WatchTable;
        }

        void UpdateSimulationTable()
        {
            dataGridView2.DataSource = m_SimulationTable;
        }
        int GetIndex(string iKEY)
        {
            for (int i = 0; i < m_WatchTable.Columns.Count; i++)
            {
                if (m_WatchTable.Columns[i].ColumnName == iKEY)
                {
                    return i;
                }
            }
            return 0;
        }

        void AddToSimulationTable(SimulationData iSimulationData)
        {
            DataRow _newRow = m_SimulationTable.NewRow();
            _newRow["單號"] = iSimulationData.Order;
            _newRow["代碼"] = iSimulationData.Code;
            _newRow["買進賣出價格"] = iSimulationData.BuyOrSellPrice;
            _newRow["動作"] = DataModule.Get_eBuyAndSell(iSimulationData.IsBuyOrSell);
            _newRow["執行日期"] = iSimulationData.BuyDate;
            _newRow["完成交易"] = iSimulationData.IsFinishTrade;
            _newRow["目前股價"] = iSimulationData.NowPrice;
            _newRow["損益"] = iSimulationData.NowIncreaseAndDecrease.ToString("f2");

            m_SimulationTable.Rows.Add(_newRow);
        }

        /// <summary>
        ///  交易完成 
        /// </summary>
        /// <param name="iSimulationData"></param>
        void Update_Price_SimulationTable(string iCode, string iOrder, float iNowPrice)
        {
            foreach (DataRow row in m_SimulationTable.Rows)
            {
                string _code = row[GetIndex("代碼")].ToString();
                string _order = row[GetIndex("單號")].ToString();
                if (iCode == _code && _order == iOrder)
                {
                    float _buyPrice = float.Parse(row["買進賣出價格"].ToString());
                    //row["完成交易"] = false;
                    row["目前股價"] = iNowPrice.ToString();
                    row["損益"] = (iNowPrice - _buyPrice).ToString("f2");
                }
            }
        }

        /// <summary>
        /// 模擬表格轉SimulationData (開放一些自定義值)
        /// </summary>
        /// <param name="iOrder">單號</param>
        /// <param name="iCode">代號</param>
        /// <param name="iNowIncreaseAndDecrease">損益</param>
        /// <param name="iNowPrice">目前價格</param>
        /// <param name="iOldTradeState">是否使用之前的完成交易紀錄</param>
        /// <returns></returns>
        SimulationData GetSimulationByOrderAndCodeFromTable(string iOrder, string iCode, float iNowIncreaseAndDecrease, float iNowPrice, bool iOldTradeState = true)
        {
            foreach (DataRow row in m_SimulationTable.Rows)
            {
                string _code = row[GetIndex("代碼")].ToString();
                string _order = row[GetIndex("單號")].ToString();
                if (iCode == _code && _order == iOrder && (bool)row["完成交易"] == false)
                {
                    SimulationData _SimulationData = new SimulationData();
                    _SimulationData.Order = iOrder;
                    _SimulationData.Code = iCode;
                    _SimulationData.BuyOrSellPrice = row["買進賣出價格"].ToString();
                    _SimulationData.IsBuyOrSell = row["動作"].ToString() == "買" ? eBuyAndSell.BUY : eBuyAndSell.SELL;
                    _SimulationData.BuyDate = Convert.ToDateTime(row["執行日期"].ToString());
                    _SimulationData.IsFinishTrade = iOldTradeState == true ? (bool)row["完成交易"] : true;
                    _SimulationData.NowIncreaseAndDecrease = iNowIncreaseAndDecrease;
                    _SimulationData.NowPrice = iNowPrice;
                    return _SimulationData;
                }
            }
            return null;
        }

        /// <summary>
        /// 取得當時買的價格
        /// </summary>
        /// <param name="iCode"></param>
        /// <param name="iOrder"></param>
        float GetBuyPriceRecord_SimulationTableByOrderAndCode(string iOrder, string iCode)
        {
            foreach (DataRow row in m_SimulationTable.Rows)
            {
                string _code = row[GetIndex("代碼")].ToString();
                string _order = row[GetIndex("單號")].ToString();
                if (iCode == _code && _order == iOrder && row["動作"].ToString() == "買" && (bool)row["完成交易"] == false)
                {
                    float _buyPrice = float.Parse(row["買進賣出價格"].ToString());
                    return _buyPrice;
                }
            }
            return 0.0f;
        }

        /// <summary>
        ///  交易完成 
        /// </summary>
        /// <param name="iSimulationData"></param>
        void TradeFinish_Update_SimulationTable(SimulationData iSimulationData)
        {
            if (iSimulationData == null)
            {
                //Console.WriteLine("TradeFinish_Update_SimulationTable 的參數iSimulationData 是空的");
                ShowMessage("TradeFinish_Update_SimulationTable 的參數iSimulationData 是空的");
                return;
            }

            foreach (DataRow row in m_SimulationTable.Rows)
            {
                string _code = row[GetIndex("代碼")].ToString();
                string _order = row[GetIndex("單號")].ToString();
                if (iSimulationData.Code == _code && _order == iSimulationData.Order && row["動作"].ToString() == "買" && (bool)row["完成交易"] == false)
                {
                    row["單號"] = iSimulationData.Order;
                    row["代碼"] = iSimulationData.Code;
                    row["買進賣出價格"] = iSimulationData.BuyOrSellPrice;
                    row["動作"] = DataModule.Get_eBuyAndSell(iSimulationData.IsBuyOrSell);
                    row["執行日期"] = iSimulationData.BuyDate;
                    row["完成交易"] = true;
                    row["目前股價"] = iSimulationData.NowPrice;
                    row["損益"] = iSimulationData.NowIncreaseAndDecrease.ToString("f2");
                }
            }
        }

        bool m_ShowMessage;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_str"></param>
        /// <param name="iIsBeControll">是否受到顯示訊息 限制</param>
        void ShowMessage(string _str, bool iIsBeControll = true)
        {

            if (m_ShowMessage || iIsBeControll == false)
            {
                Console.WriteLine("[" + DateTime.Now.ToLocalTime() + "]  " + _str);
                return;
            }


            m_Progrss += ".";
            if (m_Progrss.Length >= 720)
            {
                m_Progrss = "";
                Console.WriteLine("[" + DateTime.Now.ToLocalTime() + "]  運行中..");
            }

        }

        /// <summary>
        /// 是否要開啟全訊息通知 開啟的話會限制要達到條件
        /// </summary>
        bool m_AllMsg;

        int m_countMsg;
        /// <summary>
        /// 更新目前監測的股票資料(抓來的資料)
        /// </summary>
        void UpdateWatchGridView()
        {

            foreach (DataRow row in m_WatchTable.Rows)
            {
                string _code = row[GetIndex("代碼")].ToString();
                if (m_dic_UpdateData.ContainsKey(_code))
                {

                    //不再追蹤
                    if ((bool)row["移除觀察"] == true)
                        continue;

                    //目前股價
                    float _nowSellPriceData = float.Parse(m_dic_UpdateData[row[GetIndex("代碼")].ToString()].GetData(NOW_SELL_PRICE), CultureInfo.InvariantCulture.NumberFormat);
                    float _nowBuyPriceData = float.Parse(m_dic_UpdateData[row[GetIndex("代碼")].ToString()].GetData(NOW_BUY_PRICE), CultureInfo.InvariantCulture.NumberFormat);
                    // _nowPrice = float.Parse(textBox1.Text);

                    eBuyAndSell _isBuyOrSell = eBuyAndSell.None;
                    float _buyPoint = float.Parse(row["買進點"].ToString());
                    float _sellPoint = float.Parse(row["賣點"].ToString());

                    if (_nowSellPriceData <= _buyPoint)
                    {

                        _isBuyOrSell = eBuyAndSell.BUY;

                        if ((bool)row["模擬中"] == false || m_AllMsg == true)
                        {
                            string _msg = "股票代號:" + _code + " 接觸到買位 : " + _buyPoint.ToString() + "塊  目前股價 : " + _nowSellPriceData + " 塊";
                            ShowMessage(_msg);

                            //LINE通知
                            LineNotice.SendMessageToLineTake(_msg);
                        }
                    }
                    else if (_sellPoint <= _nowBuyPriceData)
                    {
                        _isBuyOrSell = eBuyAndSell.SELL;

                        //因為要賣的話 一定要買 要買的話模擬中一定會先開 (還是關閉的話不自然 除非測試資料..)
                        if ((bool)row["模擬中"] == true || m_AllMsg == true)
                        {
                            string _msg = "股票代號:" + _code + " 已到賣出價格 " + _sellPoint.ToString() + "塊  目前股價 : " + _nowBuyPriceData + " 塊";
                            ShowMessage(_msg);

                            //LINE通知
                            LineNotice.SendMessageToLineTake(_msg);
                        }
                    }
                    string _order = row["單號"].ToString();
                    //加入模擬
                    if (_isBuyOrSell != eBuyAndSell.None)
                    {
                        bool _IsSimulation = (bool)row["模擬中"];

                        if (_isBuyOrSell == eBuyAndSell.BUY)
                        {
                            if (_IsSimulation)
                            {
                                //模擬中.....
                                //更新模擬資料
                                Update_Price_SimulationTable(_code, _order, _nowSellPriceData);
                            }
                            else
                            {
                                //還沒模擬中.....
                                //監測資料DB更新
                                DataModule.UpdateWatchDB_ToFinishTrade(row["單號"].ToString(), true, false);
                                //更新表格
                                row["模擬中"] = true;

                                SimulationData _SimulationData2 = new SimulationData();
                                _SimulationData2.Order = _order;
                                _SimulationData2.Code = _code;
                                _SimulationData2.BuyOrSellPrice = _nowSellPriceData.ToString();
                                _SimulationData2.IsBuyOrSell = _isBuyOrSell;
                                _SimulationData2.BuyDate = DateTime.Now;
                                _SimulationData2.IsFinishTrade = false;
                                _SimulationData2.NowPrice = _nowSellPriceData;
                                _SimulationData2.NowIncreaseAndDecrease = 0;
                                AddToSimulationTable(_SimulationData2);
                                DataModule.Add_SimulationDB(_SimulationData2);
                            }

                        }
                        if (_isBuyOrSell == eBuyAndSell.SELL)
                        {
                            if (_IsSimulation)
                            {
                                //更新觀察欄位
                                row["模擬中"] = false;
                                row["移除觀察"] = true;
                                DataModule.UpdateWatchDB_ToFinishTrade(row["單號"].ToString(), false, true);

                                float _buyPrice = GetBuyPriceRecord_SimulationTableByOrderAndCode(_order, _code);
                                SimulationData _SimulationData2 = new SimulationData();
                                _SimulationData2.Order = _order;
                                _SimulationData2.Code = _code;
                                _SimulationData2.BuyOrSellPrice = _nowBuyPriceData.ToString();
                                _SimulationData2.IsBuyOrSell = _isBuyOrSell;
                                _SimulationData2.BuyDate = DateTime.Now;
                                _SimulationData2.IsFinishTrade = true;
                                _SimulationData2.NowPrice = _nowBuyPriceData;

                                _SimulationData2.NowIncreaseAndDecrease = _nowBuyPriceData - _buyPrice;

                                //曾增畫面欄位一筆
                                AddToSimulationTable(_SimulationData2);
                                //DB新增一筆
                                DataModule.Add_SimulationDB(_SimulationData2);

                                SimulationData _SimulationDataOld = GetSimulationByOrderAndCodeFromTable(_order, _code, (_nowBuyPriceData - _buyPrice), _nowBuyPriceData, false);
                                DataModule.UpdateSimulationDB_(_SimulationDataOld);
                                //更新模擬欄位(之前第一次購買的)
                                TradeFinish_Update_SimulationTable(_SimulationDataOld);
                            }
                            else
                            {
                                //無
                            }

                        }

                    }
                    else
                    {
                        //更新資料
                        //Console.WriteLine("更更新資料   _order:" + _order + "   _code : " + _code);
                        Update_Price_SimulationTable(_code, _order, _nowSellPriceData);
                    }

                    //foreach (DataColumn column in m_WatchTable.Columns)
                    //{

                    //    if (m_Lzt_Watch_Columns.Contains(column.ColumnName))
                    //    {

                    //    }

                    //}
                }

            }
            //UpdateWatchTable();
            UpdateNowIncreaseAndDecrease();
        }
        delegate void SetTextCallBack(string iStr, string iStr2);

        /// <summary>
        /// 每張的價錢
        /// </summary>
        const float m_OneStockPrice = 1000;


        void UpdateText(string istr, string istr2)
        {
            label6.Text = "目前(帳上)損益 : " + istr.ToString();
            label5.Text = "累積(結算)損益 : " + istr2.ToString();
        }
        void UpdateNowIncreaseAndDecrease()
        {
            if (m_SimulationTable == null || m_SimulationTable.Rows.Count <= 0)
                return;

            float _v = 0;
            float _2 = 0;
            foreach (DataRow row in m_SimulationTable.Rows)
            {
                if (row["動作"].ToString() == "賣" && (bool)row["完成交易"] == true)
                {
                    //已經結算的 算是歷史損益
                    _v += float.Parse(row["損益"].ToString())* m_OneStockPrice;
                }
                if (row["動作"].ToString() == "買" && (bool)row["完成交易"] == false)
                {
                    //尚未結算的 目前在單的
                    _2 += float.Parse(row["損益"].ToString())* m_OneStockPrice;
                }
            }

            // label5.Text = "累積損益 : " + _v.ToString();
            //  label6.Text = "目前損益 : " + _2.ToString();
            SetTextCallBack stc = new SetTextCallBack(this.UpdateText);
            this.Invoke(stc, _2.ToString(), _v.ToString());
            // stc.BeginInvoke(_2.ToString());
            //m_Progrss += ".";
            //if (m_Progrss.Length >= 5)
            //{
            //    m_Progrss = "";
            //}
            // label8.Text = m_Progrss;

        }

        void InitWatchTime()
        {
            m_WatchState = false;
            watchTimer = new System.Timers.Timer(m_watchTime);
            watchTimer.Elapsed += WatchTimer_Elapsed;
            watchTimer.AutoReset = true;
            watchTimer.Enabled = true;
            button2.Text = RUN_START;
        }

        bool m_WatchState;
        void StartWatch()
        {
            m_WatchState = true;
            watchTimer.Start();
        }

        void StopWatch()
        {
            m_WatchState = false;
            watchTimer.Stop();
        }

        StringBuilder m_WatchStr;

        void UpdateWatchStr()
        {
            m_WatchStr = new StringBuilder();

            //設定要撈 觀看 內的所有股票
            foreach (DataRow row in m_WatchTable.Rows)
            {
                if (row["櫃或市"].ToString() == "市")
                {
                    m_WatchStr.Append("tse_" + row["代碼"].ToString() + ".tw|");
                }
                else if (row["櫃或市"].ToString() == "櫃")
                {
                    m_WatchStr.Append("otc_" + row["代碼"].ToString() + ".tw|");
                }

            }

        }

        private void WatchTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {

            if (m_WatchState)
                WatchBySettingTime();
            //WatchByTest();
        }

        /// <summary>
        /// 模擬RUN條
        /// </summary>
        string m_Progrss;

        /// <summary>
        /// 循環執行(時間限制)
        /// </summary>
        void WatchBySettingTime()
        {
            DateTime _nowTime = DateTime.Now.ToLocalTime();
            //ShowMessage(_nowTime.Hour.ToString() + " : " + _nowTime.Minute.ToString() + "   " + _nowTime.DayOfWeek.ToString());

            if (m_countMsg == 0)
                LineNotice.SendMessageToLineTake(_nowTime + " 監測啟動...");

            m_countMsg++;

            if (m_countMsg >= 720)
            {
                //大概每30分鐘廣播一次
                m_countMsg = 1;
                LineNotice.SendMessageToLineTake( " "+ GetRunTime());
            }


            if (_nowTime.DayOfWeek != DayOfWeek.Sunday && _nowTime.DayOfWeek != DayOfWeek.Saturday)
            {
                if (_nowTime.Hour >= 9 && _nowTime.Hour <= 14)
                {
                    if (_nowTime.Hour >= 13 && _nowTime.Minute >= 35 || (_nowTime.Hour == 14))
                    {
                        return;
                    }
                    GetNowStockData();
                }
            }

        }

        void GetNowStockData()
        {
            string url = "https://mis.twse.com.tw/stock/api/getStockInfo.jsp";
            url += "?json=1&delay=0&ex_ch=" + m_WatchStr;
            string downloadedData = "";
            using (WebClient wClient = new WebClient())
            {
                // 取得網頁資料
                wClient.Encoding = Encoding.UTF8;
                downloadedData = wClient.DownloadString(url);
                if (downloadedData.Trim().Length > 0)
                {
                    ParseStockData(downloadedData);
                    UpdateWatchGridView();
                }
            }
        }


        /// <summary>
        /// 循環執行(開發測試用)
        /// </summary>
        void WatchByTest()
        {
            DateTime _nowTime = DateTime.Now.ToLocalTime();
            //ShowMessage(_nowTime.Hour.ToString() + " : " + _nowTime.Minute.ToString() + "   " + _nowTime.DayOfWeek.ToString());

            if (m_countMsg == 0)
                LineNotice.SendMessageToLineTake(_nowTime + " 監測啟動...");

            m_countMsg++;

            if (m_countMsg >= 720)
            {
                //大概每30分鐘廣播一次
                m_countMsg = 1;
                LineNotice.SendMessageToLineTake(GetRunTime());
            }
            GetNowStockData();
        }

       


        void ParseStockData(string iGetStr)
        {
            JObject _nodeContainFileList_JObject = JsonConvert.DeserializeObject<JObject>(iGetStr.Trim());
            // var _termsHashJObj = _nodeContainFileList_JObject[RPS_ProjectSettings.termsFileName];

            foreach (var v in _nodeContainFileList_JObject)
            {
                if (v.Key != "msgArray")
                {
                    continue;
                }

                if (v.Key == "msgArray")
                {
                    JArray array = JsonConvert.DeserializeObject<JArray>(v.Value.ToString());

                    if (array != null)
                    {
                        foreach (JObject Jobj in array)
                        {
                            //股票代碼
                            string _code = Jobj["c"].ToString();
                            //價格
                            string _price = Jobj["z"].ToString();
                            //公司名稱 (短)
                            string _name = Jobj["n"].ToString();

                            //公司名稱 (短)
                            string _sell_str = Jobj["a"].ToString();

                            //公司名稱 (短)
                            string _buy_ListStr = Jobj["b"].ToString();

                            string[] _buy = _buy_ListStr.Split('_');
                            string[] _sell = _sell_str.Split('_');

                            ShowMessage(" 代號 : " + _code + "(" + _name + ")  ->   賣價 : " + _buy[0] + "  買價 :  " + _sell[0] + "  成交價 : " + _price);
                            //Console.WriteLine("["+DateTime.Now.ToLocalTime() + "] 代號 : " + _code + "("+ _name + ")  -> " +_price + "塊");

                            if (m_dic_UpdateData.ContainsKey(_code))
                            {
                                if (_price == "-")
                                {
                                    //用相反
                                    m_dic_UpdateData[_code].SetData(NOW_BUY_PRICE, _sell[0]);
                                    m_dic_UpdateData[_code].SetData(NOW_SELL_PRICE, _buy[0]);
                                }
                                else
                                {
                                    m_dic_UpdateData[_code].SetData(NOW_BUY_PRICE, _price);
                                    m_dic_UpdateData[_code].SetData(NOW_SELL_PRICE, _price);
                                }


                            }
                            else
                            {
                                UpdateData _dateUpdate = new UpdateData();
                                if (_price == "-")
                                {// 沒拿到資料 就用委買委賣
                                    _dateUpdate.SetData(NOW_BUY_PRICE, _sell[0]);
                                    _dateUpdate.SetData(NOW_SELL_PRICE, _buy[0]);
                                }
                                else
                                {
                                    _dateUpdate.SetData(NOW_BUY_PRICE, _price);
                                    _dateUpdate.SetData(NOW_SELL_PRICE, _price);
                                }

                                m_dic_UpdateData.Add(_code, _dateUpdate);
                            }

                            //有要看其他資訊在打開看吧~
                            //foreach (var Jobdj in Jobj)
                            //{
                            //    Console.WriteLine(Jobdj.ToString() + "　");
                            //}
                        }
                    }
                    break;
                }
            }
        }

        class UpdateData
        {
            /// <summary>
            /// KEY=要新增的欄位, 撈到的資料
            /// </summary>
            Dictionary<string, string> Dic_ColumnData;

            public UpdateData()
            {
                Dic_ColumnData = new Dictionary<string, string>();
            }
            /// <summary>
            /// 設置資料用這個 
            /// </summary>
            /// <param name="iKey"></param>
            /// <param name="iValue"></param>
            /// <returns></returns>
            public bool SetData(string iKey, string iValue)
            {
                if (Dic_ColumnData.ContainsKey(iKey) == false)
                {
                    Dic_ColumnData.Add(iKey, iValue);
                    return false;
                }

                Dic_ColumnData[iKey] = iValue;
                return true;
            }

            public string GetData(string iKey, string iDefaultValue = "")
            {
                if (Dic_ColumnData.ContainsKey(iKey) == false)
                {
                    Dic_ColumnData.Add(iKey, iDefaultValue);
                    return iDefaultValue;
                }
                return Dic_ColumnData[iKey];
            }
        }

        /// <summary>
        /// KEY=股票代碼 VALUE=欄位的值
        /// </summary>
        Dictionary<string, UpdateData> m_dic_UpdateData;

        void ResetWatchTable()
        {
            StopWatch();
            m_WatchTable = DataModule.GetWatchDB_RealyWatchData(false);
            UpdateWatchStr();
            UpdateWatchTable();
            StartWatch();
            button2.Text = RUN_STOP;
        }

        const string RUN_START = "開始";
        const string RUN_STOP = "停止";

        private void button2_Click(object sender, EventArgs e)
        {
            
            if (m_WatchState == false)
            {
                notifyIcon1.ContextMenuStrip.Items[0].Text = RUN_STOP;
                button2.Text = RUN_STOP;
                StartWatch();
            }
            else
            {
                notifyIcon1.ContextMenuStrip.Items[0].Text = RUN_START;
                button2.Text = RUN_START;
                StopWatch();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            InputNewStockDataForm _InputNewStockDataForm = new InputNewStockDataForm();
            _InputNewStockDataForm.InitInputNewStockDataForm();

            switch (_InputNewStockDataForm.ShowDialog(this))
            {
                case DialogResult.Yes: //Form2中按下ToForm1按鈕
                    this.Show(); //顯示父視窗
                    break;
                case DialogResult.No: //Form2中按下關閉鈕
                    this.Close();  //關閉父視窗 (同時結束應用程式)
                                   //ResetWatchStr();
                    break;
                default:
                    break;
            }

        }

        void ChangeWatchOrderState()
        {
            int _inputOrder = 0;
            bool _isNumber = int.TryParse(textBox2.Text, out _inputOrder);
            if (_isNumber)
            {
                if (DataModule.CheckWatchDB_HasData("單號", _inputOrder.ToString()))
                {
                    DataModule.UpdateWatchDB_ChangeWatchState(_inputOrder.ToString(), true);
                }
                else
                {
                    MessageBox.Show("更新失敗!");
                }
            }
            else
            {
                MessageBox.Show("輸入的資料錯誤! 請填數字");
            }
         
            
        }

        void InitSelcetList()
        {
            comboBox1.Items.Add("目前監測類型");
            comboBox1.Items.Add("全部資料");

            comboBox2.Items.Add("目前在倉股");
            comboBox2.Items.Add("已完成交易");
            comboBox2.Items.Add("全部資料");
            comboBox2.SelectedItem = comboBox2.Items[0];
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem == null)
                return;

            switch (comboBox1.SelectedItem.ToString())
            {
                case "目前監測類型":
                    m_WatchTable = DataModule.GetWatchDB_RealyWatchData(false);
                    break;
                case "全部資料":
                    m_WatchTable = DataModule.GetWatchDB_AllData();
                    break;

            }
            UpdateWatchTable();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (comboBox2.SelectedItem == null)
                return;

            switch (comboBox2.SelectedItem.ToString())
            {
                case "目前在倉股":
                    m_SimulationTable = DataModule.GetSimulationDB_NowStock();
                    break;

                case "已完成交易":
                    m_SimulationTable = DataModule.GetSimulationDB(true);
                    break;

                case "全部資料":
                    m_SimulationTable = DataModule.GetSimulationDB_All();
                    break;

            }
            UpdateSimulationTable();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            ResetWatchTable();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            string _str = textBox2.Text;
            if (_str.Length <= 0)
            {
                return;
            }
            if (DataModule.CheckWatchDB_HasData("單號", _str) == false)
            {
                MessageBox.Show("無該單號資料");
                return;
            }
            DataModule.DeleteWatchDataByOrder(_str);
            MessageBox.Show("刪除完成");
            ResetWatchTable();
            textBox2.Text = "";
        }

        const string SHOW_DETAIL_MESSAGE_OFF = "訊息OFF";
        const string SHOW_DETAIL_MESSAGE_ON = "訊息ON";

        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == SHOW_DETAIL_MESSAGE_OFF)
            {
                notifyIcon1.ContextMenuStrip.Items[1].Text = SHOW_DETAIL_MESSAGE_ON;
                button1.Text = SHOW_DETAIL_MESSAGE_ON;
                m_ShowMessage = true;
            }
            else
            {
                button1.Text = SHOW_DETAIL_MESSAGE_OFF;
                notifyIcon1.ContextMenuStrip.Items[1].Text = SHOW_DETAIL_MESSAGE_OFF;
                m_ShowMessage = false;
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            MessageBox.Show("目前無功能!");
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            //      radioButton1.Checked = !radioButton1.Checked;
            //   

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            m_AllMsg = checkBox1.Checked;
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                notifyIcon1.Visible = true;
                this.Hide();
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
            notifyIcon1.Visible = false;
            WindowState = FormWindowState.Normal;
        }

        private void button9_Click(object sender, EventArgs e)
        {
            ChangeWatchOrderState();
        }

        private void textBox3_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (textBox3.Text == "653214")
                {
                    panel1.Visible = false;
                    textBox3.Text = "";
                }
                else
                {
                    panel1.Visible = true;
                    label9.Text = "輸入錯誤!";
                }
              
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            LineNotice.SendMessageToLineTake("已關閉監測程式...");

        }
    }
}