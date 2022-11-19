using System; 
namespace LuqinOfficialAccount.Models
{
    public class Stock
    {
    
        public string gid { get; set; }

        public string name { get; set; }

        public KLine[] klineDay { get; set; } = new KLine[0];

        public int GetItemIndex(DateTime date)
        {
            RefreshKLineDay();
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

        public void RefreshKLineDay()
        {
            if (klineDay.Length == 0)
            {
                ForceRefreshKLineDay();
            }

        }

    }
}

