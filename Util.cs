using System;
using System.Web;
using System.Net;
using System.IO;
//using Microsoft.EntityFrameworkCore;
//using StackExchange.Redis;
using System.Linq;

using LuqinOfficialAccount.Models;
namespace LuqinOfficialAccount
{
    public class Util
    {
        public static bool isDev = false;

        public static string workingPath = $"{Environment.CurrentDirectory}";

        public static Stock[] stockList;

        public static void GetStockList()
        {
            var resultValue = RedisClient.redisDb.SetMembers((StackExchange.Redis.RedisKey)"all_gids");

            stockList = new Stock[resultValue.Length];

            for (int i = 0; i < stockList.Length; i++)
            {
                string[] r = resultValue[i].ToString().Trim().Split(' ');

                Stock s = new Stock()
                {
                    name = r[1].Trim(),
                    gid = r[0].Trim()
                };
                stockList[i] = s;
            }
        }

        public static void DownloadFile(string url, string fileName, string path)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET";
            HttpWebResponse res = (HttpWebResponse)req.GetResponse();
            Stream s = res.GetResponseStream();
            string fileNamePath = workingPath + path + "/" + fileName.Trim();
            if (File.Exists(fileNamePath))
            {
                File.Delete(fileNamePath);
            }
            using (FileStream fs = File.OpenWrite(fileNamePath))
            {
                int b = s.ReadByte();
                for (; b != -1;)
                {
                    fs.WriteByte((byte)b);
                    b = s.ReadByte();
                }

                fs.Close();
                fs.Dispose();
            }


            
            s.Close();
            
            res.Close();
            req.Abort();
            
        }

        public static string GetLongTimeStamp(DateTime currentDateTime)
        {
            TimeSpan ts = currentDateTime - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalMilliseconds).ToString();
        }

        public static DateTime GetDateTimeByTimeStamp(long timeStamp)
        {
            DateTime date = DateTime.Parse("1970-1-1 0:0:0");
            return date.AddMilliseconds(timeStamp);
        }

        public static string UrlEncode(string urlStr)
        {
            return HttpUtility.UrlEncode(urlStr.Trim().Replace(" ", "+").Replace("'", "\""));
        }
        public static string GetWebContent(string url)
        {
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "GET";
                HttpWebResponse res = (HttpWebResponse)req.GetResponse();
                Stream s = res.GetResponseStream();
                StreamReader sr = new StreamReader(s);
                string str = sr.ReadToEnd();
                sr.Close();
                s.Close();
                res.Close();
                req.Abort();
                return str;
            }
            catch
            {
                return "";
            }
        }
        public static string GetWebContent(string url, string postData)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "POST";
            Stream sPost = req.GetRequestStream();
            StreamWriter sw = new StreamWriter(sPost);
            sw.Write(postData);
            sw.Close();
            sPost.Close();
            HttpWebResponse res = (HttpWebResponse)req.GetResponse();
            Stream s = res.GetResponseStream();
            StreamReader sr = new StreamReader(s);
            string str = sr.ReadToEnd();
            sr.Close();
            s.Close();
            return str;
        }

        public static  bool IsTransacDay(DateTime date, AppDBContext db)
        {
            bool ret = true;
            if ((date.DayOfWeek == DayOfWeek.Saturday) || (date.DayOfWeek == DayOfWeek.Sunday))
            {
                ret = false;
            }
            if ((date.Date >= DateTime.Parse("2017-10-1") && date.Date <= DateTime.Parse("2017-10-8")) || date.Date == DateTime.Parse("2018-1-1")
                || (date.Date >= DateTime.Parse("2018-2-15") && date.Date <= DateTime.Parse("2018-2-21"))
                || (date.Date >= DateTime.Parse("2018-4-5") && date.Date <= DateTime.Parse("2018-4-8")) || date.Date == DateTime.Parse("2018-4-30") || (date.Date.Month == 5 && date.Date.Day == 1)
                || date.Date == DateTime.Parse("2018-6-18") || date.Date == DateTime.Parse("2018-9-24") || (date.Date >= DateTime.Parse("2018-10-1") && date.Date <= DateTime.Parse("2018-10-7"))
                || (date.Date >= DateTime.Parse("2019-2-4") && (date.Date <= DateTime.Parse("2019-2-10")))
                || (date.Date >= DateTime.Parse("2019-4-5") && (date.Date <= DateTime.Parse("2019-4-7")))
                || (date.Date >= DateTime.Parse("2019-5-1") && (date.Date <= DateTime.Parse("2019-5-4")))
                || (date.Date >= DateTime.Parse("2019-10-1") && (date.Date <= DateTime.Parse("2019-10-7")))
                || (date.Date == DateTime.Parse("2020-1-1") || (date.Date >= DateTime.Parse("2020-1-24") && (date.Date <= DateTime.Parse("2020-2-2"))))
                || date.Date == DateTime.Parse("2020-4-6")
                || (date.Date >= DateTime.Parse("2020-5-1") && date.Date <= DateTime.Parse("2020-5-5")
                || (date.Date >= DateTime.Parse("2020-6-25") && date.Date <= DateTime.Parse("2020-6-28")))
                || (date.Date >= DateTime.Parse("2020-10-1") && date.Date <= DateTime.Parse("2020-10-8"))
                || (date.Date >= DateTime.Parse("2021-1-1") && date.Date <= DateTime.Parse("2021-1-3"))
                || (date.Date >= DateTime.Parse("2021-2-11") && date.Date <= DateTime.Parse("2021-2-17"))
                || (date.Date >= DateTime.Parse("2021-4-4") && date.Date <= DateTime.Parse("2021-4-5"))
                || (date.Date >= DateTime.Parse("2021-5-1") && date.Date <= DateTime.Parse("2021-5-5"))
                || (date.Date >= DateTime.Parse("2021-6-12") && date.Date <= DateTime.Parse("2021-6-14"))
                || (date.Date >= DateTime.Parse("2021-9-20") && date.Date <= DateTime.Parse("2021-9-21"))
                || (date.Date >= DateTime.Parse("2021-10-1") && date.Date <= DateTime.Parse("2021-10-7"))
                || date.Date == DateTime.Parse("2022-1-3")
                || (date.Date >= DateTime.Parse("2022-1-31") && date.Date <= DateTime.Parse("2022-2-4"))
                || (date.Date >= DateTime.Parse("2022-4-4") && date.Date <= DateTime.Parse("2022-4-5"))
                || (date.Date >= DateTime.Parse("2022-5-1") && date.Date <= DateTime.Parse("2022-5-4"))
                || date.Date == DateTime.Parse("2022-6-3") || date.Date == DateTime.Parse("2022-9-12")
                || date.Date == DateTime.Parse("2022-10-1") || date.Date == DateTime.Parse("2022-10-7")
                )
            {
                ret = false;
            }

            var holdayList = db.holiday.Where(h => (h.start_date <= date.Date && h.end_date >= date.Date)).ToList();

            if (holdayList != null && holdayList.Count > 0)
            {
                ret = false;
            }
            

            return ret;
        }

        public static DateTime GetLastTransactDate(DateTime currentDate, int days, AppDBContext db)
        {
            DateTime nowDate = currentDate;
            int i = 0;
            for (; i < days; i++)
            {
                nowDate = nowDate.AddDays(-1);
                if (!Util.IsTransacDay(nowDate, db))
                    i--;
            }
            return nowDate;
        }

        public static string GetName(string gid)
        {
            var resultValue = RedisClient.redisDb.SetMembers((StackExchange.Redis.RedisKey)"all_gids");
            foreach (object o in resultValue)
            {
                Console.WriteLine(o.ToString());
            }
            return "";
        }

    }
}
