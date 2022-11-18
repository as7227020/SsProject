using MyTest;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Stock
{
    public partial class InputNewStockDataForm : Form
    {
        public InputNewStockDataForm()
        {
            InitializeComponent();
            label4.Text = "請輸入監測資料!";
            button2.Text = TES;
            label5.Text = "";
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        public void InitInputNewStockDataForm()
        { 
        
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string _state = "";
            if (button2.Text == TES)
            {
                _state = "市";
            }
            else
            {
                _state = "櫃";
                
            }
            WatchStockClass watchStockClass = new WatchStockClass();
            watchStockClass.Code = textBox1.Text;
            watchStockClass.otc_Or_tes = _state;
            watchStockClass.BuyPointPrice = textBox2.Text;
            watchStockClass.SellPointPrice = textBox3.Text;
            DateTime dateTime = DateTime.Now;
            watchStockClass.CreateTime = dateTime;

            if (float.Parse(watchStockClass.BuyPointPrice) >= float.Parse(watchStockClass.SellPointPrice))
            {
                label4.Text = "購買價不能高於或等於賣點!";
                return;
            }

            if (DataModule.CheckWatchDB_HasData("代碼",watchStockClass.Code) == false)
            {
                DataModule.AddWatchStockClass(watchStockClass);
                label4.Text = label5.Text  + " 增加成功!";
                textBox1.Text = "";
                textBox2.Text = "";
                textBox3.Text = "";
            }
            else
            {
                label4.Text = "目前已經有"+ watchStockClass.Code+ "正在監測了! , 無法增加";
            }

         
        }

        const string TES = "上市";
        const string OTC = "上櫃";

        private void button2_Click(object sender, EventArgs e)
        {
            if (button2.Text == TES)
            {
                button2.Text = OTC;
            }
            else
            {
                button2.Text = TES;
            }
        }

        private void InputNewStockDataForm_Load(object sender, EventArgs e)
        {

        }

        private void textBox1_ControlRemoved(object sender, ControlEventArgs e)
        {
         
        }

        private void textBox1_Leave(object sender, EventArgs e)
        {
            NetModule.GetMarketType(textBox1.Text, (_str,str2) => {
                if (_str == "tse")
                {
                    button2.Text = TES;
                    label5.Text = str2;
                }
                else
                {
                    button2.Text = OTC;
                    label5.Text = str2;
                }
            });
           
        }
    }
}
