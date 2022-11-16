using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Data;
using System.Data.OleDb;

namespace Stock
{
    public class DataModule
    {
        private static DataModule INSTANCE = null;
        static readonly object padlock = new object(); //用來LOCK建立instance的程序。
        public static DataModule Instance
        {
            get
            {
                if (INSTANCE == null)
                {
                    lock (padlock) //lock此區段程式碼，讓其它thread無法進入。
                    {
                        if (INSTANCE == null)
                        {
                            INSTANCE = new DataModule();
                        }
                    }
                }
                return INSTANCE;
            }
        }



        public static OleDbConnection OleDbOpenConn(string Database)
        {
            string cnstr = string.Format("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + Database);
            OleDbConnection icn = new OleDbConnection();
            icn.ConnectionString = cnstr;
            if (icn.State == ConnectionState.Open) icn.Close();
            icn.Open();
            return icn;
        }
        public static DataTable GetOleDbDataTable(string Database, string OleDbString)
        {
            DataTable myDataTable = new DataTable();
            OleDbConnection icn = OleDbOpenConn(Database);
            OleDbDataAdapter da = new OleDbDataAdapter(OleDbString, icn);
            DataSet ds = new DataSet();
            ds.Clear();
            da.Fill(ds);
            myDataTable = ds.Tables[0];
            if (icn.State == ConnectionState.Open) icn.Close();
            return myDataTable;
        }

        public static void OleDbInsertUpdateDelete(string Database, string OleDbSelectString)
        {
            string cnstr = string.Format("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + Database);
            OleDbConnection icn = OleDbOpenConn(cnstr);
            OleDbCommand cmd = new OleDbCommand(OleDbSelectString, icn);
            cmd.ExecuteNonQuery();
            if (icn.State == ConnectionState.Open) icn.Close();
        }

        public static void OleDbInsertUpdateDelete(string Database, OleDbCommand iOleDbCommand)
        {
            string cnstr = string.Format("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + Database);
            OleDbConnection icn = OleDbOpenConn(cnstr);
            iOleDbCommand.Connection = icn;
            iOleDbCommand.ExecuteNonQuery();
            if (icn.State == ConnectionState.Open) icn.Close();
        }

        void LoadData()
        { 
        }

        public static string Get_eBuyAndSell(eBuyAndSell ieBuyAndSell)
        {
            if (ieBuyAndSell == eBuyAndSell.BUY)
                return "買";
            if (ieBuyAndSell == eBuyAndSell.SELL)
                return "賣";

            return "異常資料";
        }

        public static void Add_SimulationDB(SimulationData iSimulationData)
        {

            string _isBuyOrSell = Get_eBuyAndSell(iSimulationData.IsBuyOrSell);
            OleDbCommand cmd = new OleDbCommand();
            cmd.CommandText = "INSERT INTO SimulationData (單號, 代碼, 買進賣出價格, 動作, 執行日期, 完成交易, 目前股價, 損益)" +
                " VALUES (@單號, @代碼, @買進賣出價格, @動作, @執行日期, @完成交易, @目前股價, @損益)";
            cmd.Parameters.AddWithValue("@單號", iSimulationData.Order);
            cmd.Parameters.AddWithValue("@代碼", iSimulationData.Code);
            cmd.Parameters.AddWithValue("@買進賣出價格", iSimulationData.BuyOrSellPrice);
            cmd.Parameters.AddWithValue("@動作", _isBuyOrSell);
            cmd.Parameters.AddWithValue("@執行日期", iSimulationData.BuyDate.Date);
            cmd.Parameters.AddWithValue("@完成交易", iSimulationData.IsFinishTrade ? 1 : 0);
            cmd.Parameters.AddWithValue("@目前股價", iSimulationData.NowPrice);
            cmd.Parameters.AddWithValue("@損益", iSimulationData.NowIncreaseAndDecrease);


            //string sql = "INSERT INTO 'SimulationData' ('股票代號', '買進賣出價格', '動作')" +
            //    " VALUES ("+ iSimulationData.Code + ", "+ iSimulationData.BuyOrSellPrice+ ", "+ _isBuyOrSell +";";
            OleDbInsertUpdateDelete("StockDB.mdb", cmd);
        }

        public static void DeleteSimulationData(SimulationData iSimulationData)
        {

            string _isBuyOrSell = iSimulationData.IsBuyOrSell == eBuyAndSell.BUY ? "買" : "賣";
            OleDbCommand cmd = new OleDbCommand();
            cmd.CommandText = "DELETE FROM SimulationData WHERE (代碼)" + " = (@代碼)";
            cmd.Parameters.AddWithValue("@代碼", iSimulationData.Code);


            //string sql = "INSERT INTO 'SimulationData' ('股票代號', '買進賣出價格', '動作')" +
            //    " VALUES ("+ iSimulationData.Code + ", "+ iSimulationData.BuyOrSellPrice+ ", "+ _isBuyOrSell +";";
            OleDbInsertUpdateDelete("StockDB.mdb", cmd);
        }

        public static void DeleteWatchDataByOrder(string iOrder)
        {
            OleDbCommand cmd = new OleDbCommand();
            cmd.CommandText = "DELETE FROM WatchStockClass WHERE (單號)" + " = (@單號)";
            cmd.Parameters.AddWithValue("@單號", iOrder);
            OleDbInsertUpdateDelete("StockDB.mdb", cmd);
        }

        public static void UpdateSimulationDB_(SimulationData iSimulationData)
        {
            OleDbCommand cmd = new OleDbCommand();
            cmd.CommandText = "UPDATE SimulationData SET 完成交易 = (@完成交易) , 目前股價 = (@目前股價) ,損益 = (@損益)  WHERE 代碼 = (@代碼) AND 單號 = (@單號)";
            cmd.Parameters.AddWithValue("@完成交易", iSimulationData.IsFinishTrade ? 1 : 0);
            cmd.Parameters.AddWithValue("@目前股價", iSimulationData.NowPrice);
            cmd.Parameters.AddWithValue("@損益", iSimulationData.NowIncreaseAndDecrease);
            cmd.Parameters.AddWithValue("@代碼", iSimulationData.Code);
            cmd.Parameters.AddWithValue("@單號", iSimulationData.Order);

            //string sql = "INSERT INTO 'SimulationData' ('股票代號', '買進賣出價格', '動作')" +
            //    " VALUES ("+ iSimulationData.Code + ", "+ iSimulationData.BuyOrSellPrice+ ", "+ _isBuyOrSell +";";
            OleDbInsertUpdateDelete("StockDB.mdb", cmd);
        }

        public static void UpdateWatchDB_ToFinishTrade(string iSid, bool iIsSimulation, bool iIsRemove)
        {
            OleDbCommand cmd = new OleDbCommand();
            cmd.CommandText = "UPDATE WatchStockClass SET 模擬中 = (@模擬中),移除觀察 = (@移除觀察) WHERE (單號)" + " = (@單號)";
            cmd.Parameters.AddWithValue("@模擬中", iIsSimulation == true ? 1 : 0 );
            cmd.Parameters.AddWithValue("@移除觀察", iIsRemove == true ? 1 : 0);
            cmd.Parameters.AddWithValue("@單號", iSid);

            OleDbInsertUpdateDelete("StockDB.mdb", cmd);
        }

        public static DataTable GetWatchDB_AllData()
        {
            string sql = "select * from WatchStockClass";
            DataTable dt = GetOleDbDataTable("StockDB.mdb", sql);
            return dt;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="iIsRealyData">是否正式資料</param>
        /// <param name="IsRemove">移除觀察</param>
        /// <returns></returns>
        public static DataTable GetWatchDB_RealyWatchData(bool iIsSimulation, bool IsRemove)
        {
            int _IsSimulation = iIsSimulation ? 1 : 0;
            int _isRemove = IsRemove ? 1 : 0;
            string sql = "select * from WatchStockClass where 模擬中 = " + _IsSimulation + " AND 移除觀察 =   "+ _isRemove + " " ;
            DataTable dt = GetOleDbDataTable("StockDB.mdb", sql);
            return dt;
        }

        public static bool CheckWatchDB_HasData(string iColName, string iCode)
        {
            DataTable _dataTable = GetWatchDB_RealyWatchData(false);
            foreach (DataRow row in _dataTable.Rows)
            {
                if (iCode ==  row[iColName].ToString())
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="iIsRealyData">是否正式資料</param>
        /// <param name="IsRemove">移除觀察</param>
        /// <returns></returns>
        public static DataTable GetWatchDB_RealyWatchData(bool IsRemove)
        {
            int _isRemove = IsRemove ? 1 : 0;
            string sql = "select * from WatchStockClass where  移除觀察 =   " + _isRemove + " ";
            DataTable dt = GetOleDbDataTable("StockDB.mdb", sql);
            return dt;
        }

        public static DataTable GetSimulationDB(bool iIsFinish)
        {
            int _isfinish = iIsFinish ? 1 : 0;
            string sql = "select * from SimulationData where 完成交易 =  1 ";
            DataTable dt = GetOleDbDataTable("StockDB.mdb", sql);
            return dt;
        }

        public static DataTable GetSimulationDB_All()
        {
            string sql = "select * from SimulationData";
            DataTable dt = GetOleDbDataTable("StockDB.mdb", sql);
            return dt;
        }
        public static DataTable GetSimulationDB_NowStock()
        {
            int _isfinish = 0;
            string sql = "select * from SimulationData where 完成交易 = " + _isfinish + " AND 動作 = '買' ";
            DataTable dt = GetOleDbDataTable("StockDB.mdb", sql);
            return dt;
        }


        public static DataTable GetCodeManagerDB()
        {
            string sql = "select * from CodeManager";
            DataTable dt = GetOleDbDataTable("StockDB.mdb", sql);
            return dt;
        }

        public static void AddWatchStockClass(WatchStockClass iWatchStockClass)
        {
            OleDbCommand cmd = new OleDbCommand();
            cmd.CommandText = "INSERT INTO WatchStockClass (代碼, 買進點, 賣點, 建立日期, 模擬中, 移除觀察,櫃或市)" +
                " VALUES (@代碼, @買進點, @賣點, @建立日期, @模擬中, @移除觀察, @櫃或市)";
            cmd.Parameters.AddWithValue("@代碼", int.Parse(iWatchStockClass.Code) );
            cmd.Parameters.AddWithValue("@買進點", float.Parse(iWatchStockClass.BuyPointPrice));
            cmd.Parameters.AddWithValue("@賣點", float.Parse( iWatchStockClass.SellPointPrice));
            cmd.Parameters.AddWithValue("@建立日期", DateTime.Now.Date);
            cmd.Parameters.AddWithValue("@模擬中", 0);
            cmd.Parameters.AddWithValue("@移除觀察", 0);
            cmd.Parameters.AddWithValue("@櫃或市", iWatchStockClass.otc_Or_tes);

            OleDbInsertUpdateDelete("StockDB.mdb", cmd);
        }


    }
}
