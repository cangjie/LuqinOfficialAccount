﻿using System; 
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
            if (now - klineDayLastUpdateTime > new TimeSpan(0, 2, 0))
            {
                ForceRefreshKLineDay();
                ComputeRSV(klineDay);
                ComputeKDJ(klineDay);
                ComputeMACD(klineDay);
            }
            if (now - klineWeekLastUpdateTime > new TimeSpan(0, 30, 0))
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
            KLine[] klineDay = new KLine[rvArr.Length];
            for (int i = 0; i < rvArr.Length; i++)
            {
                string[] rvItems = rvArr[i].ToString().Trim().Split(',');
                KLine k = new KLine()
                {
                    type = "day",
                    settleTime = DateTime.Parse(rvItems[1].Trim()).Date,
                    open = double.Parse(rvItems[2].Trim()),
                    settle = double.Parse(rvItems[3].Trim()),
                    high = double.Parse(rvItems[4].Trim()),
                    low = double.Parse(rvItems[5].Trim()),
                    volume = long.Parse(rvItems[6].Trim()),
                    amount = double.Parse(rvItems[7].Trim())
                };
                klineDay[i] = k;
            }
            this.klineDay = klineDay;
            
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

        public void RefreshKLineDay()
        {
            if (klineDay.Length == 0)
            {
                ForceRefreshKLineDay();
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
        

         
    }
}

