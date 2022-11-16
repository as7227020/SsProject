using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Stock
{
    internal class LineNotice
    {
        /// <summary>
        /// Line Notice上的Token https://notify-bot.line.me/my/
        /// </summary>
        static string m_Token = "yNBQmkZeV0PK6vAyIDk3kUgNCUltuiNNg7uD7OlJ2Q4";

        /// <summary>
        /// 向Line設定群組發送訊息
        /// </summary>
        /// <param name="iMessage"></param>
        public static void SendMessageToLineTake(string iMessage)
        {
            string _str = "https://notify-api.line.me/api/notify?status=200&message=" + iMessage;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_str);
            request.Method = "POST";
            request.Headers.Add("Authorization", "Bearer " + m_Token);
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            Byte[] byteArray = encoding.GetBytes(iMessage);

            request.ContentLength = byteArray.Length;
            request.ContentType = @"application/json";

            using (Stream dataStream = request.GetRequestStream())
            {
                dataStream.Write(byteArray, 0, byteArray.Length);
            }
            long length = 0;
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    length = response.ContentLength;
                }
            }
            catch
            {
                throw;
            }
        }
    }
}
