using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
namespace LuqinOfficialAccount.Models
{
    public class Stock
    {
    
        public string gid { get; set; }

        public string name { get; set; }

        public KLine[] klineDay { get; set; } = new KLine[0];

        public KLine[] klineWeek { get; set; } = new KLine[0];

        public KLine[] klineHour { get; set; } = new KLine[0];

        public DateTime klineDayLastUpdateTime { get; set;} = DateTime.MinValue;

        public DateTime klineHourLastUpdateTime { get; set; } = DateTime.MinValue;

        public DateTime klineWeekLastUpdateTime { get; set;} = DateTime.MinValue;

        public int GetItemIndex(DateTime date)
        {
            //RefreshKLineDay();
            int index = -1;
            for (int i = 0; i < klineDay.Length; i++)
            {
                if (klineDay[i].settleTime >= date)
                {
                    index = i;
                    break;
                }
            }
            return index;
        }

        public void RefreshKLine()
        { 
            DateTime now = DateTime.Now;
            if (now.Hour >= 9 && now.Hour <= 15)
            {
                ForceRefreshKLineDay();
                ComputeRSV(klineDay);
                ComputeKDJ(klineDay);
                ComputeMACD(klineDay);
                ForceRefreshKLineWeek();
                ComputeRSV(klineWeek);
                ComputeKDJ(klineWeek);
                ComputeMACD(klineWeek);
            }
            if (now - klineDayLastUpdateTime > new TimeSpan(0, 2, 0))
            {
                ForceRefreshKLineDay();
                ComputeRSV(klineDay);
                ComputeKDJ(klineDay);
                ComputeMACD(klineDay);
            }
            if (now - klineWeekLastUpdateTime > new TimeSpan(0, 10, 0))
            {
                ForceRefreshKLineWeek();
                ComputeRSV(klineWeek);
                ComputeKDJ(klineWeek);
                ComputeMACD(klineWeek);
            }
        }

        public void ForceRefreshKLineDay()
        {
            string key = gid + "_kline_day";
            StackExchange.Redis.RedisValue[] rvArr = RedisClient.redisDb.SortedSetRangeByScore((StackExchange.Redis.RedisKey)key);
            //KLine[] klineDay = new KLine[rvArr.Length];
            List<KLine> klineList = new List<KLine>();
            for (int i = 0; i < rvArr.Length; i++)
            {
                string[] rvItems = rvArr[i].ToString().Trim().Split(',');
                DateTime settleTime = DateTime.Parse(rvItems[1].Trim()).Date;
                if (!Util.IsTransacDay(settleTime, Util._db))
                {
                    continue;
                }
                KLine k = new KLine()
                {
                    type = "day",
                    settleTime = settleTime,
                    open = double.Parse(rvItems[2].Trim()),
                    settle = double.Parse(rvItems[3].Trim()),
                    high = double.Parse(rvItems[4].Trim()),
                    low = double.Parse(rvItems[5].Trim()),
                    volume = (long)double.Parse(rvItems[6].Trim()),
                    turnOver = 0//double.Parse(rvItems[7].Trim())
                };
                if (rvItems.Length == 9)
                {
                    k.turnOver = double.Parse(rvItems[8].Trim());
                }
                if (klineList.Count == 0 || klineList[klineList.Count - 1].settleTime.Date < k.settleTime.Date)
                {
                    klineList.Add(k);
                }
            }
            this.klineDay = klineList.ToArray<KLine>();
            
        }

        public void ForceRefreshKLineWeek()
        {
            string key = gid + "_kline_week";
            StackExchange.Redis.RedisValue[] rvArr = RedisClient.redisDb.SortedSetRangeByScore((StackExchange.Redis.RedisKey)key);
            KLine[] klineWeek = new KLine[rvArr.Length];
            for (int i = 0; i < rvArr.Length; i++)
            {
                string[] rvItems = rvArr[i].ToString().Trim().Split(',');
                KLine k = new KLine()
                {
                    type = "week",
                    settleTime = DateTime.Parse(rvItems[1].Trim()).Date,
                    open = double.Parse(rvItems[2].Trim()),
                    settle = double.Parse(rvItems[3].Trim()),
                    high = double.Parse(rvItems[4].Trim()),
                    low = double.Parse(rvItems[5].Trim()),
                    volume = long.Parse(rvItems[6].Trim()),
                    amount = double.Parse(rvItems[7].Trim())
                };
                klineWeek[i] = k;
            }
            this.klineWeek = klineWeek;

        }

        public void ForceRefreshKLineHour()
        {
            string key = gid + "_kline_hour";
            StackExchange.Redis.RedisValue[] rvArr = RedisClient.redisDb.SortedSetRangeByScore((StackExchange.Redis.RedisKey)key);
            KLine[] klineHour = new KLine[rvArr.Length];
            for (int i = 0; i < rvArr.Length; i++)
            {
                string[] rvItems = rvArr[i].ToString().Trim().Split(',');
                KLine k = new KLine()
                {
                    type = "hour",
                    settleTime = DateTime.Parse(rvItems[1].Trim()),
                    open = double.Parse(rvItems[2].Trim()),
                    settle = double.Parse(rvItems[3].Trim()),
                    high = double.Parse(rvItems[4].Trim()),
                    low = double.Parse(rvItems[5].Trim()),
                    volume = long.Parse(rvItems[6].Trim()),
                    amount = double.Parse(rvItems[7].Trim())
                };
                klineHour[i] = k;
            }
            this.klineHour = klineHour;

        }

        public void RefreshKLineDay()
        {
            if (klineDay.Length == 0)
            {
                ForceRefreshKLineDay();
                ComputeRSV(klineDay);
                ComputeKDJ(klineDay);
                ComputeMACD(klineDay);
            }

        }
        public static double GetLowestPrice(KLine[] kArr)
        {
            double ret = 0;
            foreach (KLine k in kArr)
            {
                if (ret == 0)
                {
                    ret = k.low;
                }
                else
                {
                    ret = Math.Min(ret, k.low);
                }
            }
            return ret;
        }

        public static double GetHighestPrice(KLine[] kArr)
        {
            double ret = 0;
            foreach (KLine k in kArr)
            {
                ret = Math.Max(ret, k.high);
            }
            return ret;
        }

        public static KLine[] GetSubKLine(KLine[] kArr, int startIndex, int num)
        {
            if (startIndex + num > kArr.Length)
                return null;
            KLine[] subArr = new KLine[num];
            for (int i = 0; i < num; i++)
            {
                subArr[i] = kArr[startIndex + i];
            }
            return subArr;
        }

        public static void ComputeRSV(KLine[] kArr)
        {
            int valueN = 8;
            for (int i = valueN - 1; i < kArr.Length; i++)
            {
                KLine[] rsvArr = GetSubKLine(kArr, i - valueN + 1, valueN);
                double lowPrice = GetLowestPrice(rsvArr);
                double hiPrice = GetHighestPrice(rsvArr);
                kArr[i].rsv = 100 * (kArr[i].settle - lowPrice) / (hiPrice - lowPrice);
            }

        }

        public static void ComputeKDJ(KLine[] kArr)
        {
            int valueM1 = 3;
            int valueM2 = 3;
            for (int i = 0; i < kArr.Length; i++)
            {
                if (kArr[i].rsv == 0)
                {
                    kArr[i].k = 50;
                    kArr[i].d = 50;
                    continue;
                }
                kArr[i].k = (kArr[i].rsv + (valueM1 - 1) * kArr[i - 1].k) / valueM1;
                kArr[i].d = (kArr[i].k + (valueM2 - 1) * kArr[i - 1].d) / valueM2;
                kArr[i].j = 3 * kArr[i].k - 2 * kArr[i].d;
            }
        }

        public static double ema(double[] xArr, int currentIndex, int n)
        {
            try
            {
                if (currentIndex == 0)
                    return xArr[currentIndex];
                else
                {
                    try
                    {
                        return (xArr[currentIndex] * 2 + ema(xArr, currentIndex - 1, n) * (double)(n - 1)) / (double)(n + 1);
                    }
                    catch
                    {
                        return xArr[currentIndex];
                    }
                }
            }
            catch
            {
                return xArr[0];
            }
        }

        public static void ComputeMACD(KLine[] kArr)
        {
            int shortDays = 8;
            int longDays = 17;
            int midDays = 9;



            double[] endPirceArr = new double[kArr.Length];
            double[] difArr = new double[kArr.Length];

            for (int i = 0; i < kArr.Length; i++)
            {
                endPirceArr[i] = kArr[i].settle;
                difArr[i] = 0;
            }

            for (int i = 1; i < kArr.Length; i++)
            {
                try
                {
                    kArr[i].dif = ema(endPirceArr, i, shortDays) - ema(endPirceArr, i, longDays);
                    difArr[i] = kArr[i].dif;
                    kArr[i].dea = ema(difArr, i, midDays);
                    kArr[i].macd = (kArr[i].dif - kArr[i].dea) * 2;
                }
                catch
                {
                    break;
                }
            }
        }

        public static double ComputeDMP(KLine[] kArr, int index)
        {
            if (index <= 18 || kArr.Length <= index)
            {
                return 0;
            }
            double dmp = 0;
            ComputeMACD(kArr);
            double tempPrice = kArr[index].settle;
            if (kArr[index].macd == 0)
            {
                dmp = kArr[index].settle;
            }
            else if (kArr[index].macd > 0)
            {
                for (; kArr[index].macd > 0 && kArr[index].settle > 0; kArr[index].settle = kArr[index].settle - 0.01)
                {
                    ComputeMACD(kArr);
                    if (kArr[index].macd <= 0)
                    {
                        dmp = kArr[index].settle;
                        break;
                    }
                }
            }
            else if (kArr[index].macd < 0)
            {
                for (; kArr[index].macd < 0 && kArr[index].settle < 9999; kArr[index].settle = kArr[index].settle + 0.01)
                {
                    ComputeMACD(kArr);
                    if (kArr[index].macd >= 0)
                    {
                        dmp = kArr[index].settle;
                        break;
                    }
                }
            }
            kArr[index].settle = tempPrice;
            ComputeMACD(kArr);
            return dmp;
        }

        public static int GetItemIndex(DateTime date, KLine[]  klineArr)
        {
            //RefreshKLineDay();
            int index = -1;
            for (int i = 0; i < klineArr.Length; i++)
            {
                if (klineArr[i].settleTime >= date)
                {
                    index = i;
                    break;
                }
            }
            return index;
        }

        public static Stock GetStock(string gid)
        {
            Stock[] stockList = Util.stockList;
            Stock ret;
            for (int i = 0; i < stockList.Length; i++)
            {
                if (stockList[i].gid.Trim().Equals(gid.Trim()))
                {
                    ret = stockList[i];
                    return ret;
                    
                }
            }
            return null;
        }
        

         
    }
}

