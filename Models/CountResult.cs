using System;
using System.Collections.Generic;

namespace LuqinOfficialAccount.Models
{
    public class CountItem
    { 
        public DateTime alert_date { get; set; }
        public string gid { get; set; }
        public string name { get; set; }

        public int days { get; set; }

        public double[] riseRate { get; set; }

        public double totalRiseRate { get; set; }

        public static CountItem Count(CountItem item, string buyPoint)
        {
            string gid = item.gid;
            DateTime alertDate = item.alert_date.Date;
            Stock s = Stock.GetStock(gid.Trim());
            s.RefreshKLine();
            int itemIndex = s.GetItemIndex(alertDate);
            if (itemIndex < 0)
            {
                return null;
            }
            double buyPrice = 0;
            switch (buyPoint)
            {
                default:
                    buyPrice = s.klineDay[itemIndex].settle;
                    break;
            }
            if (buyPrice == 0)
            {
                return null;
            }
            double maxPrice = 0;
            double[] riseRateArr = new double[item.days];
            for (int i = 0; i < item.days && itemIndex + i + 1 < s.klineDay.Length; i++)
            {
                double currentHigh = s.klineDay[itemIndex + i + 1].high;
                maxPrice = Math.Max(maxPrice, currentHigh);
                riseRateArr[i] = (currentHigh - buyPrice) / buyPrice;
            }
            double totalHighRate = (maxPrice - buyPrice) / buyPrice;
            item.riseRate = riseRateArr;
            item.totalRiseRate = totalHighRate;
            return item;
        }

    }
    public class CountResult
    {
        public int Count { get; set; }
        public int SuccessCount { get; set; }
        public int BigSuccessCount { get; set; }

        public double SuccessRate { get; set; }

        public double BigSuccessRate { get; set; }

        public CountItem[] list { get; set; }

        public static CountResult GetResult(List<CountItem> list)
        {
            if (list.Count == 0)
            {
                return null;
            }
            int sucCount = 0;
            int bigSucCount = 0;
            
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].totalRiseRate > 0.01)
                {
                    sucCount++;
                    if (list[i].totalRiseRate > 0.05)
                    {
                        bigSucCount++;
                    }
                }
            }


            CountResult r = new CountResult()
            {
                Count = list.Count,
                SuccessCount = sucCount,
                BigSuccessCount = bigSucCount,
                SuccessRate = (double)sucCount / (double)list.Count,
                BigSuccessRate = (double)bigSucCount / (double)list.Count,
                list = list.ToArray()

            };
            return r;
        }

    }
}
