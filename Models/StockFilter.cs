using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace LuqinOfficialAccount.Models
{
    public class StockFilter
    {
        public class Item
        { 
            public string gid { get; set; }
            public string name { get; set; }
            public DateTime alertDate { get; set; }
            public string signal { get; set; } = "";
            public double buyPrice { get; set; } = 0;
            public object[] referenceValues { get; set; }
            public double[] dailyRise { get; set; }
            public double totalRise { get; set; }


        }

        public string[] fields { get; set; }
        public int countDays { get; set; }
        public List<Item> itemList { get; set; }


        public static StockFilter GetResult(DataRow[] drArr, int countDays)
        {
            if (drArr.Length <= 0)
            {
                return null;
            }
            StockFilter sf = new StockFilter();
            sf.countDays = countDays;
            DataTable dt = drArr[0].Table;
            string[] fieldsArr = new string[dt.Columns.Count - 5];
            int fieldIndex = 0;
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                switch (dt.Columns[i].Caption.Trim())
                {
                    case "日期":
                    case "名称":
                    case "代码":
                    case "买入":
                    case "总计":
                    case "信号":
                        break;
                    default:
                        fieldsArr[fieldIndex] = dt.Columns[i].Caption.Trim();
                        fieldIndex++;
                        break;
                }
            }
            sf.fields = fieldsArr;

            List<Item> items = new List<Item>();
            for (int i = 0; i < drArr.Length; i++)
            {
                string gid = drArr[i]["代码"].ToString();
                Stock s = Stock.GetStock(gid);
                s.RefreshKLine();
                object[] valueArr = new object[fieldsArr.Length];
                int valueIndex = 0;
                double[] riseArr = new double[countDays];
                DateTime alertDate = DateTime.Parse(drArr[i]["日期"].ToString());
                int alertIndex = s.GetItemIndex(alertDate);
                if (alertIndex < 0)
                {
                    continue;
                }
                double highPrice = 0;
                double buyPrice = double.Parse(drArr[i]["买入"].ToString());
                for (int j = 0; j < countDays; j++)
                {
                    if (alertIndex + j + 1 < s.klineDay.Length)
                    {
                        double currentHigh = s.klineDay[alertIndex + j + 1].high;
                        highPrice = Math.Max(highPrice, currentHigh);
                        riseArr[j] = (currentHigh - buyPrice) / buyPrice;
                    }
                    else
                    {
                        riseArr[j] = -1;
                    }
                }
                double totalRise = (highPrice - buyPrice) / buyPrice;
                for (int j = 0; j < dt.Columns.Count; j++)
                {
                    switch (dt.Columns[j].Caption)
                    {
                        case "日期":
                        case "名称":
                        case "代码":
                        case "买入":
                        case "总计":
                        case "信号":
                            break;
                        default:
                            valueArr[valueIndex] = drArr[i][dt.Columns[j]];
                            valueIndex++;
                            break;
                    }

                }
                Item item = new Item()
                {
                    gid = s.gid.Trim(),
                    name = s.name.Trim(),
                    alertDate = alertDate,
                    buyPrice = buyPrice,
                    signal = drArr[i]["信号"].ToString(),
                    referenceValues = valueArr,
                    dailyRise = riseArr,
                    totalRise = totalRise
                };
                items.Add(item);
            }

            sf.itemList = items;
            return sf;
        }

    }
}
