using System;
using System.Threading.Tasks;
using LuqinOfficialAccount.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Data;


namespace LuqinOfficialAccount.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class OldModelController : ControllerBase
    {
        private readonly AppDBContext _db;

        private readonly IConfiguration _config;

        private readonly Settings _settings;

        private readonly ChipController chipCtrl;

        private readonly ConceptController conceptCtrl;

        private readonly ResultCacheController resultHelper;

        public OldModelController(AppDBContext context, IConfiguration config) 
		{
            _db = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
            chipCtrl = new ChipController(_db, _config);
            Util._db = context;
            conceptCtrl = new ConceptController(context, config);
            resultHelper = new ResultCacheController(context, config);
            _db.Database.SetCommandTimeout(999);
        }

        [HttpGet("{days}")]
        public async Task<ActionResult<StockFilter>> TrafficLight(int days, DateTime startDate, DateTime endDate, string sort = "筹码")
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));
            dt.Columns.Add("缩量", Type.GetType("System.Double"));
            dt.Columns.Add("3线", Type.GetType("System.Double"));
            dt.Columns.Add("现高", Type.GetType("System.Double"));
            dt.Columns.Add("F3", Type.GetType("System.Double"));
            dt.Columns.Add("F5", Type.GetType("System.Double"));
            dt.Columns.Add("前低", Type.GetType("System.Double"));
            dt.Columns.Add("幅度", Type.GetType("System.String"));
            dt.Columns.Add("KDJ日", Type.GetType("System.Int32"));
            dt.Columns.Add("MACD日", Type.GetType("System.Int32"));
            dt.Columns.Add("红绿灯涨", Type.GetType("System.Double"));
            dt.Columns.Add("涨幅", Type.GetType("System.Double"));

            startDate = Util.GetLastTransactDate(startDate, 2, _db);
            endDate = Util.GetLastTransactDate(endDate, 2, _db);

            var limitUpList = await _db.LimitUp.Where(l => l.alert_date >= startDate.Date && l.alert_date <= endDate.Date)
                .AsNoTracking().ToListAsync();

            for (int i = 0; i < limitUpList.Count; i++)
            {
                Stock stock = Stock.GetStock(limitUpList[i].gid);
                stock.ForceRefreshKLineDay();
                int limitUpIndex = stock.GetItemIndex(limitUpList[i].alert_date);

                if (limitUpIndex + 2 >= stock.klineDay.Length)
                {
                    continue;
                }


                if (!KLine.IsLimitUp(stock.klineDay, limitUpIndex))
                {
                    continue;
                }
                if (KLine.IsLimitUp(stock.klineDay, limitUpIndex + 1))
                {
                    continue;
                }

                bool isTrafficLight = false;

                if (limitUpIndex + 2 < stock.klineDay.Length)
                {
                    if (!KLine.IsLimitUp(stock.klineDay, limitUpIndex + 1)
                        && (stock.klineDay[limitUpIndex + 1].settle - stock.klineDay[limitUpIndex].settle) / stock.klineDay[limitUpIndex].settle > -0.095
                        && (stock.klineDay[limitUpIndex + 2].settle - stock.klineDay[limitUpIndex + 1].settle) / stock.klineDay[limitUpIndex + 1].settle > -0.095
                        && stock.klineDay[limitUpIndex + 1].open > stock.klineDay[limitUpIndex + 1].settle
                        && stock.klineDay[limitUpIndex + 2].open <= stock.klineDay[limitUpIndex + 2].settle)
                    {
                        isTrafficLight = true;
                    }
                }

                if (!isTrafficLight)
                {
                    continue;
                }

                int highIndex = 0;
                int lowestIndex = 0;
                double lowest = Util.GetFirstLowestPrice(stock.klineDay, limitUpIndex, out lowestIndex);
                double highest = 0;
                for (int j = limitUpIndex; j < limitUpIndex + 2 && j < stock.klineDay.Length ; j++)
                {
                    if (highest < stock.klineDay[j].high)
                    {
                        highest = stock.klineDay[j].high;
                        highIndex = i;
                    }
                }

                double avarageVolume = 0;
                for (int j = lowestIndex; j < highIndex; i++)
                {
                    avarageVolume = avarageVolume + stock.klineDay[j].volume;
                }
                avarageVolume = (int)Math.Round((double)avarageVolume / (double)(highIndex - lowestIndex), 0);

                int currentIndex = limitUpIndex + 2;

                double f3 = highest - (highest - lowest) * 0.382;
                double f5 = highest - (highest - lowest) * 0.618;
                double line3Price = KLine.GetAverageSettlePrice(stock.klineDay, currentIndex, 3, 3);
                double currentPrice = stock.klineDay[currentIndex].settle;
                double buyPrice = stock.klineDay[limitUpIndex + 2].settle;
                double maxVolume = stock.klineDay[limitUpIndex].volume;
                double todayLowestPrice = 0;
                double volumeReduce = (stock.klineDay[currentIndex].volume - stock.klineDay[currentIndex - 1].volume) / stock.klineDay[currentIndex - 1].volume;
                bool isSortCase = false;
                bool isHorseHead = false;
                int limitUpNum = 0;

                DataRow dr = dt.NewRow();
                dr["日期"] = stock.klineDay[limitUpIndex + 2].settleTime.Date;
                currentIndex = stock.GetItemIndex(stock.klineDay[limitUpIndex + 2].settleTime.Date);
                dr["代码"] = stock.gid.Trim();
                dr["名称"] = stock.name.Trim();


                double maxPrice = Math.Max(stock.klineDay[currentIndex - 1].settle, stock.klineDay[currentIndex - 2].settle);

                dr["红绿灯涨"] = (stock.klineDay[currentIndex].settle - maxPrice) / maxPrice;



                double width = Math.Round(100 * (highest - lowest) / lowest, 2);




                KLine highKLine = stock.klineDay[highIndex];


                dr["缩量"] = volumeReduce;
                dr["现高"] = highest;
                dr["F3"] = f3;
                dr["F5"] = f5;
                dr["前低"] = lowest;
                dr["幅度"] = width.ToString() + "%";

                /*
                double f3ReverseRate = (stock.klineDay[currentIndex].low - f3) / f3;
                double f5ReverseRate = (stock.klineDay[currentIndex].low - f5) / f5;
                double supportPrice = 0;
                if (Math.Abs(f3ReverseRate) > Math.Abs(f5ReverseRate))
                {
                    dr["价差"] = (stock.klineDay[currentIndex].low - f5);
                    supportPrice = f5;
                    dr["类型"] = "F5";

                }
                else
                {
                    dr["价差"] = (stock.klineDay[currentIndex].low - f3);
                    supportPrice = f3;
                    dr["类型"] = "F3";


                }
                */

                line3Price = KLine.GetAverageSettlePrice(stock.klineDay, currentIndex, 3, 3);

               

                dr["3线"] = line3Price;
                //dr["现价"] = currentPrice;

                //dr["评级"] = "";
                //buyPrice = stock.kLineDay[currentIndex].endPrice;


                dr["KDJ日"] = stock.kdjDays(currentIndex);

                dr["MACD日"] = stock.macdDays(currentIndex);

       
                maxPrice = 0;
                //buyPrice = supportPrice;
                dr["买入"] = buyPrice;
                dr["涨幅"] = (buyPrice - stock.klineDay[currentIndex - 1].settle) / stock.klineDay[currentIndex - 1].settle;

                if (stock.klineDay[currentIndex].volume > stock.klineDay[currentIndex - 1].volume)
                {
                    dr["信号"] = dr["信号"].ToString() + "🔴";
                    
                }

                if (Math.Min(stock.klineDay[currentIndex].open, stock.klineDay[currentIndex].settle) > stock.klineDay[currentIndex - 2].high
                && Math.Min(stock.klineDay[currentIndex - 1].open, stock.klineDay[currentIndex - 1].settle) > stock.klineDay[currentIndex - 2].high)
                {
                    dr["信号"] = dr["信号"].ToString() + "🐴";
                }
                if (KLine.IsLimitUp(stock.klineDay, currentIndex))
                {
                    dr["信号"] = dr["信号"].ToString() + "🚩";
                }
                dt.Rows.Add(dr);
            }

            StockFilter sf = StockFilter.GetResult(dt.Select("", "日期 desc, " + sort), days);
            try
            {
                return Ok(sf);
            }
            catch
            {
                return NotFound();

            }

        }

    }
}

