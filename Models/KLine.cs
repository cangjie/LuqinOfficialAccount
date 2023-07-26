using System;
namespace LuqinOfficialAccount.Models
{
	
	public class KLine
	{
		public DateTime settleTime { get; set; }
		public string type { get; set; } = "day";
		public double open { get; set; } = 0;
		public double settle { get; set; } = 0;
		public double high { get; set; } = 0;
		public double low { get; set; } = 0;
		public long volume { get; set; } = 0;
        public double amount { get; set; } = 0;
        public double rsv { get; set; }  = 0;
        public double k { get; set; } = 0;
        public double d { get; set; } = 0;
        public double j { get; set; } = 0;
        public double dif { get; set; } = 0;
        public double dea { get; set; } = 0;
        public double macd { get; set; } = 0;
        public double turnOver { get; set; } = 0;



        public static double GetAverageSettlePrice(KLine[] kArr, int index, int itemsCount, int displacement)
        {
            if (index - displacement - itemsCount + 1 < 0)
                return 0;
            double sum = 0;
            for (int i = 0; i < itemsCount; i++)
            {
                sum = sum + kArr[index - displacement - i].settle;
            }
            return sum / itemsCount;
        }

        public static int GetForwardTopKLineItem(KLine[] kArr, int startIndex)
        {
            if (startIndex >= kArr.Length || startIndex < 0)
            {
                return -1;
            }
            int highIndex = startIndex;
            double highPrice = kArr[highIndex].high;

            bool below3Line = true;
            if (kArr[startIndex].high > GetAverageSettlePrice(kArr, startIndex, 3, 3))
            {
                below3Line = false;
            }

            for (int i = startIndex + 1; i < kArr.Length 
                && (below3Line || kArr[i].high > GetAverageSettlePrice(kArr, i, 3, 3)); i++)
            {
                if (below3Line)
                {
                    if (kArr[i].high > GetAverageSettlePrice(kArr, i, 3, 3))
                    {
                        below3Line = false;
                    }
                }
                if (highPrice <= kArr[i].high)
                { 
                    highPrice = kArr[i].high;
                    highIndex = i;
                }
            }
            return highIndex;
        }

        public static int GetBackwardBottomKLineItem(KLine[] kArr, int startIndex)
        {
            int lowestIndex = startIndex;
            double lowestPrice = kArr[startIndex].low;
            if (startIndex >= kArr.Length || startIndex < 0)
            {
                return -1;
            }
            bool up3Line = false;
            if (kArr[startIndex].low > GetAverageSettlePrice(kArr, startIndex, 3, 3))
            {
                up3Line = true;
            }
            double line3 = GetAverageSettlePrice(kArr, startIndex - 1, 3, 3);
            for (int i = startIndex - 1;
                i >= 1 && (up3Line || kArr[i].low < line3);
                i--)
            {
                if (up3Line)
                {
                    if (kArr[i].low < line3)
                    {
                        up3Line = false;
                    }
                }
                if (kArr[i].low <= lowestPrice)
                {
                    lowestPrice = kArr[i].low;
                    lowestIndex = i;
                }
                line3 = GetAverageSettlePrice(kArr, i - 1, 3, 3);
            }
            return lowestIndex;
        }

        public static int? GetKDJDays(KLine[] kArr, int index)
        {
            if (index < 0 || index >= kArr.Length)
            {
                return null;
            }
            bool isGoldFork = true;
            if (kArr[index].k < kArr[index].d)
            {
                isGoldFork = false;
            }
            int days = 1;
            for (int i = index - 1; i >= 0; i--)
            {
                if (isGoldFork)
                {
                    if (kArr[i].k >= kArr[i].d)
                    {
                        days++;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    if (kArr[i].k <= kArr[i].d)
                    {
                        days++;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            if (isGoldFork)
            {
                return days;
            }
            else
            {
                return -1 * days;
            }

        }

        public static bool IsLimitUp(KLine[] kArr, int index)
        {
            if (index <= 0 || index >= kArr.Length)
            {
                return false;
            }

            KLine current = kArr[index];
            KLine prev = kArr[index - 1];

            if ((current.settle - prev.settle) / prev.settle > 0.095)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        public static double? GetKdjOverSell(KLine[] kArr, int index)
        {
            double ret = double.MaxValue;
            bool kdGold = true;
            if (index >= kArr.Length)
            {
                return null;
            }
            for (int i = index; i >= 0; i--)
            {
                if (kdGold && kArr[i].k < kArr[i].d)
                {
                    kdGold = false;
                    
                }
                if (!kdGold)
                {
                    ret = Math.Min(kArr[i].j, ret);
                }
                if (!kdGold && ret != double.MaxValue && kArr[i].k > kArr[i].d)
                {
                    break;
                }
            }
            return ret;
        }

    }
}

