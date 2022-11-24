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
        /// ������
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
            button1.Text = "�T��OFF";
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
            notifyIcon1.ContextMenuStrip.Items.Add("�T��OFF", null, button1_Click);
            notifyIcon1.ContextMenuStrip.Items.Add("��J�ʴ����", null, button3_Click);
            notifyIcon1.ContextMenuStrip.Items.Add("�}�ҵe��", null, MenuOpenWindow_Click);
            notifyIcon1.ContextMenuStrip.Items.Add("�����{��", null, MenuExit_Click);


            AllocConsole();
            InitMessgaeToOFF();
            m_Progrss = "";
            m_countMsg = 0;
            m_AllMsg = false;
            ShowMessage("�}�l��l��...",false);
            radioButton1.Checked = false;

            //�B�ת��}��
            panel1.Visible = true;

            dataGridView1.ReadOnly = true;
            dataGridView2.ReadOnly = true;

            dataGridView1.AllowUserToAddRows = false;
            dataGridView2.AllowUserToAddRows = false;

            dataGridView1.RowHeadersVisible = false;
            dataGridView2.RowHeadersVisible = false;

            InitSelcetList();


            //��l�ƨC���[�ݥ\��
            InitWatchTime();
            //��l��DB���
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
            ShowMessage("��l�Ƨ���!",false);

            GetNowStockData();
        }

        string GetRunTime()
        {
            DateTime _now = DateTime.Now.ToLocalTime();
            TimeSpan ts = _now.Subtract(m_StartAppTime); //��ɶ��ѼƬ۴�
            return " �ثe�B�@�ɶ�  - " + ts.Days + "�� " + ts.Hours + "��" + ts.Minutes + "��" + ts.Seconds + "��";
        }

        void Init_dic_UpdateData()
        {
            m_dic_UpdateData = new Dictionary<string, UpdateData>();
            foreach (DataRow row in m_WatchTable.Rows)
            {
                UpdateData _update = new UpdateData();

                _update.SetData(NOW_SELL_PRICE, "0");
                _update.SetData(NOW_BUY_PRICE, "0");
                ////���ثe�Ҧ��n�[��Ѳ��N��
                m_dic_UpdateData.Add(row["�N�X"].ToString(), _update);
            }
        }

        /// <summary>
        /// �VDB�����@�����
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
            _newRow["�渹"] = iSimulationData.Order;
            _newRow["�N�X"] = iSimulationData.Code;
            _newRow["�R�i��X����"] = iSimulationData.BuyOrSellPrice;
            _newRow["�ʧ@"] = DataModule.Get_eBuyAndSell(iSimulationData.IsBuyOrSell);
            _newRow["������"] = iSimulationData.BuyDate;
            _newRow["�������"] = iSimulationData.IsFinishTrade;
            _newRow["�ثe�ѻ�"] = iSimulationData.NowPrice;
            _newRow["�l�q"] = iSimulationData.NowIncreaseAndDecrease.ToString("f2");

            m_SimulationTable.Rows.Add(_newRow);
        }

        /// <summary>
        ///  ������� 
        /// </summary>
        /// <param name="iSimulationData"></param>
        void Update_Price_SimulationTable(string iCode, string iOrder, float iNowPrice)
        {
            foreach (DataRow row in m_SimulationTable.Rows)
            {
                string _code = row[GetIndex("�N�X")].ToString();
                string _order = row[GetIndex("�渹")].ToString();
                if (iCode == _code && _order == iOrder)
                {
                    float _buyPrice = float.Parse(row["�R�i��X����"].ToString());
                    //row["�������"] = false;
                    row["�ثe�ѻ�"] = iNowPrice.ToString();
                    row["�l�q"] = (iNowPrice - _buyPrice).ToString("f2");
                }
            }
        }

        /// <summary>
        /// ���������SimulationData (�}��@�Ǧ۩w�q��)
        /// </summary>
        /// <param name="iOrder">�渹</param>
        /// <param name="iCode">�N��</param>
        /// <param name="iNowIncreaseAndDecrease">�l�q</param>
        /// <param name="iNowPrice">�ثe����</param>
        /// <param name="iOldTradeState">�O�_�ϥΤ��e�������������</param>
        /// <returns></returns>
        SimulationData GetSimulationByOrderAndCodeFromTable(string iOrder, string iCode, float iNowIncreaseAndDecrease, float iNowPrice, bool iOldTradeState = true)
        {
            foreach (DataRow row in m_SimulationTable.Rows)
            {
                string _code = row[GetIndex("�N�X")].ToString();
                string _order = row[GetIndex("�渹")].ToString();
                if (iCode == _code && _order == iOrder && (bool)row["�������"] == false)
                {
                    SimulationData _SimulationData = new SimulationData();
                    _SimulationData.Order = iOrder;
                    _SimulationData.Code = iCode;
                    _SimulationData.BuyOrSellPrice = row["�R�i��X����"].ToString();
                    _SimulationData.IsBuyOrSell = row["�ʧ@"].ToString() == "�R" ? eBuyAndSell.BUY : eBuyAndSell.SELL;
                    _SimulationData.BuyDate = Convert.ToDateTime(row["������"].ToString());
                    _SimulationData.IsFinishTrade = iOldTradeState == true ? (bool)row["�������"] : true;
                    _SimulationData.NowIncreaseAndDecrease = iNowIncreaseAndDecrease;
                    _SimulationData.NowPrice = iNowPrice;
                    return _SimulationData;
                }
            }
            return null;
        }

        /// <summary>
        /// ���o��ɶR������
        /// </summary>
        /// <param name="iCode"></param>
        /// <param name="iOrder"></param>
        float GetBuyPriceRecord_SimulationTableByOrderAndCode(string iOrder, string iCode)
        {
            foreach (DataRow row in m_SimulationTable.Rows)
            {
                string _code = row[GetIndex("�N�X")].ToString();
                string _order = row[GetIndex("�渹")].ToString();
                if (iCode == _code && _order == iOrder && row["�ʧ@"].ToString() == "�R" && (bool)row["�������"] == false)
                {
                    float _buyPrice = float.Parse(row["�R�i��X����"].ToString());
                    return _buyPrice;
                }
            }
            return 0.0f;
        }

        /// <summary>
        ///  ������� 
        /// </summary>
        /// <param name="iSimulationData"></param>
        void TradeFinish_Update_SimulationTable(SimulationData iSimulationData)
        {
            if (iSimulationData == null)
            {
                //Console.WriteLine("TradeFinish_Update_SimulationTable ���Ѽ�iSimulationData �O�Ū�");
                ShowMessage("TradeFinish_Update_SimulationTable ���Ѽ�iSimulationData �O�Ū�");
                return;
            }

            foreach (DataRow row in m_SimulationTable.Rows)
            {
                string _code = row[GetIndex("�N�X")].ToString();
                string _order = row[GetIndex("�渹")].ToString();
                if (iSimulationData.Code == _code && _order == iSimulationData.Order && row["�ʧ@"].ToString() == "�R" && (bool)row["�������"] == false)
                {
                    row["�渹"] = iSimulationData.Order;
                    row["�N�X"] = iSimulationData.Code;
                    row["�R�i��X����"] = iSimulationData.BuyOrSellPrice;
                    row["�ʧ@"] = DataModule.Get_eBuyAndSell(iSimulationData.IsBuyOrSell);
                    row["������"] = iSimulationData.BuyDate;
                    row["�������"] = true;
                    row["�ثe�ѻ�"] = iSimulationData.NowPrice;
                    row["�l�q"] = iSimulationData.NowIncreaseAndDecrease.ToString("f2");
                }
            }
        }

        bool m_ShowMessage;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_str"></param>
        /// <param name="iIsBeControll">�O�_������ܰT�� ����</param>
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
                Console.WriteLine("[" + DateTime.Now.ToLocalTime() + "]  �B�椤..");
            }

        }

        /// <summary>
        /// �O�_�n�}�ҥ��T���q�� �}�Ҫ��ܷ|����n�F�����
        /// </summary>
        bool m_AllMsg;

        int m_countMsg;
        /// <summary>
        /// ��s�ثe�ʴ����Ѳ����(��Ӫ����)
        /// </summary>
        void UpdateWatchGridView()
        {

            foreach (DataRow row in m_WatchTable.Rows)
            {
                string _code = row[GetIndex("�N�X")].ToString();
                if (m_dic_UpdateData.ContainsKey(_code))
                {

                    //���A�l��
                    if ((bool)row["�����[��"] == true)
                        continue;

                    //�ثe�ѻ�
                    float _nowSellPriceData = float.Parse(m_dic_UpdateData[row[GetIndex("�N�X")].ToString()].GetData(NOW_SELL_PRICE), CultureInfo.InvariantCulture.NumberFormat);
                    float _nowBuyPriceData = float.Parse(m_dic_UpdateData[row[GetIndex("�N�X")].ToString()].GetData(NOW_BUY_PRICE), CultureInfo.InvariantCulture.NumberFormat);
                    // _nowPrice = float.Parse(textBox1.Text);

                    eBuyAndSell _isBuyOrSell = eBuyAndSell.None;
                    float _buyPoint = float.Parse(row["�R�i�I"].ToString());
                    float _sellPoint = float.Parse(row["���I"].ToString());

                    if (_nowSellPriceData <= _buyPoint)
                    {

                        _isBuyOrSell = eBuyAndSell.BUY;

                        if ((bool)row["������"] == false || m_AllMsg == true)
                        {
                            string _msg = "�Ѳ��N��:" + _code + " ��Ĳ��R�� : " + _buyPoint.ToString() + "��  �ثe�ѻ� : " + _nowSellPriceData + " ��";
                            ShowMessage(_msg);

                            //LINE�q��
                            LineNotice.SendMessageToLineTake(_msg);
                        }
                    }
                    else if (_sellPoint <= _nowBuyPriceData)
                    {
                        _isBuyOrSell = eBuyAndSell.SELL;

                        //�]���n�檺�� �@�w�n�R �n�R���ܼ������@�w�|���} (�٬O�������ܤ��۵M ���D���ո��..)
                        if ((bool)row["������"] == true || m_AllMsg == true)
                        {
                            string _msg = "�Ѳ��N��:" + _code + " �w���X���� " + _sellPoint.ToString() + "��  �ثe�ѻ� : " + _nowBuyPriceData + " ��";
                            ShowMessage(_msg);

                            //LINE�q��
                            LineNotice.SendMessageToLineTake(_msg);
                        }
                    }
                    string _order = row["�渹"].ToString();
                    //�[�J����
                    if (_isBuyOrSell != eBuyAndSell.None)
                    {
                        bool _IsSimulation = (bool)row["������"];

                        if (_isBuyOrSell == eBuyAndSell.BUY)
                        {
                            if (_IsSimulation)
                            {
                                //������.....
                                //��s�������
                                Update_Price_SimulationTable(_code, _order, _nowSellPriceData);
                            }
                            else
                            {
                                //�٨S������.....
                                //�ʴ����DB��s
                                DataModule.UpdateWatchDB_ToFinishTrade(row["�渹"].ToString(), true, false);
                                //��s���
                                row["������"] = true;

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
                                //��s�[�����
                                row["������"] = false;
                                row["�����[��"] = true;
                                DataModule.UpdateWatchDB_ToFinishTrade(row["�渹"].ToString(), false, true);

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

                                //���W�e�����@��
                                AddToSimulationTable(_SimulationData2);
                                //DB�s�W�@��
                                DataModule.Add_SimulationDB(_SimulationData2);

                                SimulationData _SimulationDataOld = GetSimulationByOrderAndCodeFromTable(_order, _code, (_nowBuyPriceData - _buyPrice), _nowBuyPriceData, false);
                                DataModule.UpdateSimulationDB_(_SimulationDataOld);
                                //��s�������(���e�Ĥ@���ʶR��)
                                TradeFinish_Update_SimulationTable(_SimulationDataOld);
                            }
                            else
                            {
                                //�L
                            }

                        }

                    }
                    else
                    {
                        //��s���
                        //Console.WriteLine("���s���   _order:" + _order + "   _code : " + _code);
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
        /// �C�i������
        /// </summary>
        const float m_OneStockPrice = 1000;


        void UpdateText(string istr, string istr2)
        {
            label6.Text = "�ثe(�b�W)�l�q : " + istr.ToString();
            label5.Text = "�ֿn(����)�l�q : " + istr2.ToString();
        }
        void UpdateNowIncreaseAndDecrease()
        {
            if (m_SimulationTable == null || m_SimulationTable.Rows.Count <= 0)
                return;

            float _v = 0;
            float _2 = 0;
            foreach (DataRow row in m_SimulationTable.Rows)
            {
                if (row["�ʧ@"].ToString() == "��" && (bool)row["�������"] == true)
                {
                    //�w�g���⪺ ��O���v�l�q
                    _v += float.Parse(row["�l�q"].ToString())* m_OneStockPrice;
                }
                if (row["�ʧ@"].ToString() == "�R" && (bool)row["�������"] == false)
                {
                    //�|�����⪺ �ثe�b�檺
                    _2 += float.Parse(row["�l�q"].ToString())* m_OneStockPrice;
                }
            }

            // label5.Text = "�ֿn�l�q : " + _v.ToString();
            //  label6.Text = "�ثe�l�q : " + _2.ToString();
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

            //�]�w�n�� �[�� �����Ҧ��Ѳ�
            foreach (DataRow row in m_WatchTable.Rows)
            {
                if (row["�d�Υ�"].ToString() == "��")
                {
                    m_WatchStr.Append("tse_" + row["�N�X"].ToString() + ".tw|");
                }
                else if (row["�d�Υ�"].ToString() == "�d")
                {
                    m_WatchStr.Append("otc_" + row["�N�X"].ToString() + ".tw|");
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
        /// ����RUN��
        /// </summary>
        string m_Progrss;

        /// <summary>
        /// �`������(�ɶ�����)
        /// </summary>
        void WatchBySettingTime()
        {
            DateTime _nowTime = DateTime.Now.ToLocalTime();
            //ShowMessage(_nowTime.Hour.ToString() + " : " + _nowTime.Minute.ToString() + "   " + _nowTime.DayOfWeek.ToString());

            if (m_countMsg == 0)
                LineNotice.SendMessageToLineTake(_nowTime + " �ʴ��Ұ�...");

            m_countMsg++;

            if (m_countMsg >= 720)
            {
                //�j���C30�����s���@��
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
                // ���o�������
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
        /// �`������(�}�o���ե�)
        /// </summary>
        void WatchByTest()
        {
            DateTime _nowTime = DateTime.Now.ToLocalTime();
            //ShowMessage(_nowTime.Hour.ToString() + " : " + _nowTime.Minute.ToString() + "   " + _nowTime.DayOfWeek.ToString());

            if (m_countMsg == 0)
                LineNotice.SendMessageToLineTake(_nowTime + " �ʴ��Ұ�...");

            m_countMsg++;

            if (m_countMsg >= 720)
            {
                //�j���C30�����s���@��
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
                            //�Ѳ��N�X
                            string _code = Jobj["c"].ToString();
                            //����
                            string _price = Jobj["z"].ToString();
                            //���q�W�� (�u)
                            string _name = Jobj["n"].ToString();

                            //���q�W�� (�u)
                            string _sell_str = Jobj["a"].ToString();

                            //���q�W�� (�u)
                            string _buy_ListStr = Jobj["b"].ToString();

                            string[] _buy = _buy_ListStr.Split('_');
                            string[] _sell = _sell_str.Split('_');

                            ShowMessage(" �N�� : " + _code + "(" + _name + ")  ->   ��� : " + _buy[0] + "  �R�� :  " + _sell[0] + "  ����� : " + _price);
                            //Console.WriteLine("["+DateTime.Now.ToLocalTime() + "] �N�� : " + _code + "("+ _name + ")  -> " +_price + "��");

                            if (m_dic_UpdateData.ContainsKey(_code))
                            {
                                if (_price == "-")
                                {
                                    //�άۤ�
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
                                {// �S������ �N�Ωe�R�e��
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

                            //���n�ݨ�L��T�b���}�ݧa~
                            //foreach (var Jobdj in Jobj)
                            //{
                            //    Console.WriteLine(Jobdj.ToString() + "�@");
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
            /// KEY=�n�s�W�����, ���쪺���
            /// </summary>
            Dictionary<string, string> Dic_ColumnData;

            public UpdateData()
            {
                Dic_ColumnData = new Dictionary<string, string>();
            }
            /// <summary>
            /// �]�m��ƥγo�� 
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
        /// KEY=�Ѳ��N�X VALUE=��쪺��
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

        const string RUN_START = "�}�l";
        const string RUN_STOP = "����";

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
                case DialogResult.Yes: //Form2�����UToForm1���s
                    this.Show(); //��ܤ�����
                    break;
                case DialogResult.No: //Form2�����U�����s
                    this.Close();  //���������� (�P�ɵ������ε{��)
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
                if (DataModule.CheckWatchDB_HasData("�渹", _inputOrder.ToString()))
                {
                    DataModule.UpdateWatchDB_ChangeWatchState(_inputOrder.ToString(), true);
                }
                else
                {
                    MessageBox.Show("��s����!");
                }
            }
            else
            {
                MessageBox.Show("��J����ƿ��~! �ж�Ʀr");
            }
         
            
        }

        void InitSelcetList()
        {
            comboBox1.Items.Add("�ثe�ʴ�����");
            comboBox1.Items.Add("�������");

            comboBox2.Items.Add("�ثe�b�ܪ�");
            comboBox2.Items.Add("�w�������");
            comboBox2.Items.Add("�������");
            comboBox2.SelectedItem = comboBox2.Items[0];
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem == null)
                return;

            switch (comboBox1.SelectedItem.ToString())
            {
                case "�ثe�ʴ�����":
                    m_WatchTable = DataModule.GetWatchDB_RealyWatchData(false);
                    break;
                case "�������":
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
                case "�ثe�b�ܪ�":
                    m_SimulationTable = DataModule.GetSimulationDB_NowStock();
                    break;

                case "�w�������":
                    m_SimulationTable = DataModule.GetSimulationDB(true);
                    break;

                case "�������":
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
            if (DataModule.CheckWatchDB_HasData("�渹", _str) == false)
            {
                MessageBox.Show("�L�ӳ渹���");
                return;
            }
            DataModule.DeleteWatchDataByOrder(_str);
            MessageBox.Show("�R������");
            ResetWatchTable();
            textBox2.Text = "";
        }

        const string SHOW_DETAIL_MESSAGE_OFF = "�T��OFF";
        const string SHOW_DETAIL_MESSAGE_ON = "�T��ON";

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
            MessageBox.Show("�ثe�L�\��!");
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
                    label9.Text = "��J���~!";
                }
              
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            LineNotice.SendMessageToLineTake("�w�����ʴ��{��...");

        }
    }
}