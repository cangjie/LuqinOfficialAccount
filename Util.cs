using System;
using System.Web;
using System.Net;
using System.IO;
//using Microsoft.EntityFrameworkCore;
//using StackExchange.Redis;
using System.Linq;
using LuqinOfficialAccount.Models;
using System.Collections.Generic;

namespace LuqinOfficialAccount
{
    public class Util
    {
        public static bool isDev = false;

        public static string workingPath = $"{Environment.CurrentDirectory}";

        public static Stock[] stockList;

        public static AppDBContext _db;

        public static List<Holiday> holidays = new List<Holiday>();

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

        public static   bool IsTransacDay(DateTime date, AppDBContext db)
        {
            bool ret = true;
            if ((date.DayOfWeek == DayOfWeek.Saturday) || (date.DayOfWeek == DayOfWeek.Sunday))
            {
                ret = false;
            }
            if (holidays.Count == 0)
            {
                holidays =  db.holiday.ToList();
            }

            for (int i = 0; i < holidays.Count; i++)
            {
                if (holidays[i].start_date.Date <= date.Date
                    && holidays[i].end_date >= date.Date)
                {
                    ret = false;
                    break;
                }
            }
            return ret;
        }

        public static DateTime GetLastTransactDate(DateTime currentDate, int days, AppDBContext db)
        {
            DateTime nowDate = currentDate;
            int i = 0;
            int dayNum = Math.Abs(days);
            for (; i < dayNum; i++)
            {
                if (days > 0)
                {
                    nowDate = nowDate.AddDays(-1);
                }
                else
                {
                    nowDate = nowDate.AddDays(1);
                }
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

        public static bool IsTwice(Stock s, int index)
        {
           
            if (index < 1 && index > s.klineDay.Length - 1)
            {
                return false;
            }
            if (KLine.IsLimitUp(s.klineDay, s.gid, index - 1)
                && KLine.IsLimitUp(s.klineDay, s.gid, index))
            {
                return true;
            }
            return false;
        }

        public static bool IsReverse(Stock s, int index)
        {
            if (index < 2 || index > s.klineDay.Length - 1)
            {
                return false;
            }
            if (!KLine.IsLimitUp(s.klineDay, s.gid, index))
            {
                return false;
            }
            if (KLine.IsLimitUp(s.klineDay, s.gid, index - 1))
            {
                return false;
            }

            for (int i = index - 2; i >= index - 5 && i > 0; i--)
            {
                if (KLine.IsLimitUp(s.klineDay, s.gid, i))
                {
                    return true;
                }
            }
            return false;
        }

        public static double GetFirstLowestPrice(KLine[] kArr, int index, out int lowestIndex)
        {
            double ret = double.MaxValue;
            int find = 0;
            lowestIndex = 0;
            for (int i = index - 1; i > 0 && find < 2; i--)
            {
                double line3Pirce = KLine.GetAverageSettlePrice(kArr, i, 3, 3);
                ret = Math.Min(ret, kArr[i].low);
                if (ret == kArr[i].low)
                {
                    lowestIndex = i;
                }
                if (kArr[i].settle < line3Pirce)
                {
                    find = 1;
                }
                if (kArr[i].low >= line3Pirce && find == 1)
                {
                    find = 2;
                }
            }
            return ret;
        }


    }
}
