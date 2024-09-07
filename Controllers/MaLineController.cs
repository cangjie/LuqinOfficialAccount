using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LuqinOfficialAccount;
using LuqinOfficialAccount.Models;
using Microsoft.Extensions.Configuration;
using System.Data;
using Microsoft.AspNetCore.Mvc;

namespace LuqinOfficialAccount.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class MaLineController:ControllerBase
	{
        private readonly AppDBContext _db;

        private readonly IConfiguration _config;

        private readonly Settings _settings;

        private static readonly DateTime nowDate = DateTime.Now.Date;

        public MaLineController(AppDBContext context, IConfiguration config)
		{
            _db = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
            Util._db = context;
            _db.Database.SetCommandTimeout(999);
        }

        [HttpGet("{days}")]
        public async Task<ActionResult<StockFilter>> BigRedUnder3LineFall(int days, DateTime startDate, DateTime endDate, string sort = "代码")
        {
            StockFilter sf = (StockFilter)((OkObjectResult)(await BigRedUnder3Line(days, startDate, endDate, sort)).Result).Value;
            for (int i = 0; sf != null && i < sf.itemList.Count; i++)
            {
                Stock s = Stock.GetStock(sf.itemList[i].gid.Trim());
                DateTime alertDate = sf.itemList[i].alertDate.Date;
                int alertIndex = Stock.GetItemIndex(alertDate, s.klineDay);
                double up3LineSettlePrice = 0;
                for (int j = alertIndex - 1; j >= 0; j--)
                {
                    double line3 = KLine.GetAverageSettlePrice(s.klineDay, j, 3, 3);
                    if (line3 < s.klineDay[j].settle)
                    {
                        up3LineSettlePrice = s.klineDay[j].settle;
                        break;
                    }
                }
                if (up3LineSettlePrice == 0)
                {
                    continue;
                }
                if ((up3LineSettlePrice - s.klineDay[alertIndex].settle) / up3LineSettlePrice < 0.2)
                {
                    continue;
                }
                sf.itemList.RemoveAt(i);
                i--;
            }
            return Ok(sf);
        }


        [HttpGet("{days}")]
        public async Task<ActionResult<StockFilter>> BigRedUnder3Line(int days, DateTime startDate, DateTime endDate, string sort = "代码")
        {
            //startDate = Util.GetLastTransactDate(startDate, 1, _db);
            //endDate = Util.GetLastTransactDate(endDate, 1, _db);
            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));
            dt.Columns.Add("MACD", Type.GetType("System.Int32"));
            dt.Columns.Add("KDJ", Type.GetType("System.Int32"));
            double rate = 0.09;
            var l = await _db.nearLine3BigRed.Where(n => (n.alert_date.Date >= startDate.Date
                && n.alert_date.Date <= endDate && n.rate >= rate && n.low < n.line3))
                .AsNoTracking().ToListAsync();
            for (int i = 0; l != null && i < l.Count; i++)
            {
                Stock s = Stock.GetStock(l[i].gid.Trim());
                try
                {
                    s.ForceRefreshKLineDay();
                    Stock.ComputeMACD(s.klineDay);
                    Stock.ComputeRSV(s.klineDay);
                    Stock.ComputeKDJ(s.klineDay);
                }
                catch
                {

                }
                int alertIndex = s.GetItemIndex(l[i].alert_date.Date);
                if (alertIndex <= 0 || alertIndex >= s.klineDay.Length)
                {
                    continue;
                }
                if ((s.klineDay[alertIndex].settle - s.klineDay[alertIndex - 1].settle) / s.klineDay[alertIndex - 1].settle < rate)
                {
                    continue;
                }
                if (s.klineDay[alertIndex].settle >= l[i].line3 * 1.02)
                {
                    continue;
                }
                /*
                if (alertIndex <= s.klineDay.Length - 1 && s.klineDay[alertIndex + 1].open == 0)
                {
                    continue;
                }
                */
                double buyPrice = -1;
                if (alertIndex < s.klineDay.Length - 1)
                {
                    buyPrice = s.klineDay[alertIndex + 1].open;
                    if (buyPrice == 0)
                    {
                        continue;
                    }
                }
                DataRow dr = dt.NewRow();
                DateTime buyDate = s.klineDay[alertIndex].settleTime.Date;
                buyDate = Util.GetLastTransactDate(buyDate, -1, _db);
                dr["日期"] = buyDate.Date;
                dr["代码"] = s.gid.Trim();
                dr["名称"] = s.name.Trim();
                dr["信号"] = "";
                dr["买入"] = buyPrice;
                int macd = s.macdDays(alertIndex);
                int kdj = s.kdjDays(alertIndex);
                dr["MACD"] = macd;
                dr["KDJ"] = kdj;
                if (macd == 0 && kdj == 0)
                {
                    dr["信号"] = "📈";
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

        [HttpGet("{days}")]
        public async Task<ActionResult<StockFilter>> BigRedUnder3LineRise(int days, DateTime startDate, DateTime endDate, string sort = "代码")
        {
            startDate = Util.GetLastTransactDate(startDate, 1, _db);
            endDate = Util.GetLastTransactDate(endDate, 1, _db);

            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));
            dt.Columns.Add("MACD", Type.GetType("System.Int32"));
            dt.Columns.Add("KDJ", Type.GetType("System.Int32"));

            StockFilter sfOri = (StockFilter)((OkObjectResult)(await BigRedUnder3Line(1, startDate, endDate)).Result).Value;
            for (int i = 0; sfOri != null && sfOri.itemList != null && i < sfOri.itemList.Count; i++)
            {
                Stock s = Stock.GetStock(sfOri.itemList[i].gid.Trim());
                try
                {
                    s.RefreshKLineDay();
                    Stock.ComputeMACD(s.klineDay);
                    Stock.ComputeRSV(s.klineDay);
                    Stock.ComputeKDJ(s.klineDay);
                }
                catch
                {

                }
                int alertIndex = s.GetItemIndex(sfOri.itemList[i].alertDate.Date);
                //int alertIndex = s.GetItemIndex(l[i].alert_date.Date);
                if (alertIndex <= 0 || alertIndex >= s.klineDay.Length - 1)
                {
                    continue;
                }
                int buyIndex = alertIndex;
                if (s.klineDay[buyIndex].high < s.klineDay[buyIndex - 1].high
                    || s.klineDay[buyIndex].low < s.klineDay[buyIndex - 1].low)
                {
                    continue;
                }

                DataRow dr = dt.NewRow();
                //DateTime buyDate = s.klineDay[alertIndex].settleTime.Date;
                //buyDate = Util.GetLastTransactDate(buyDate, -1, _db);

                dr["日期"] = s.klineDay[buyIndex].settleTime.Date;
                dr["代码"] = s.gid.Trim();
                dr["名称"] = s.name.Trim();
                dr["信号"] = "";
                dr["买入"] = s.klineDay[buyIndex].settle;
                int macd = s.macdDays(alertIndex);
                int kdj = s.kdjDays(alertIndex);
                dr["MACD"] = macd;
                dr["KDJ"] = kdj;
                
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

        [HttpGet]
        public async Task SearchNear3LineBigRedForToday()
        {
            await SearchNear3LineBigRed(DateTime.Now.Date, DateTime.Now.Date);
        }

        [HttpGet]
        public async Task SearchNear3LineBigRedForDays(DateTime startDate, DateTime endDate)
        {
            await SearchNear3LineBigRed(startDate, endDate);
        }

        [NonAction]
        public async Task SearchNear3LineBigRed(DateTime startDate, DateTime endDate)
        {
            Stock[] sArr = Util.stockList;
            int k = 0;
            foreach (Stock s in sArr)
            {
                


                try
                {
                    s.ForceRefreshKLineDay();
                }
                catch
                {

                }
                k++;
                Console.WriteLine(k.ToString() + "\t" + s.gid);
                int startIndex = s.GetItemIndex(startDate.Date);
                if (startIndex <= 6)
                {
                    continue;
                }
                for (int i = startIndex; i < s.klineDay.Length && s.klineDay[i].settleTime.Date <= endDate.Date; i++)
                {
                    double line3 = KLine.GetAverageSettlePrice(s.klineDay, i, 3, 3);
                    double line3Prev = KLine.GetAverageSettlePrice(s.klineDay, i - 1, 3, 3);
                    bool cross3Line = false;
                    if (s.klineDay[i - 1].settle < line3Prev && s.klineDay[i].settle > line3)
                    {
                        cross3Line = true;
                    }
                    double rate = s.klineDay[i - 1].settle == 0? 0 :(s.klineDay[i].high - s.klineDay[i - 1].settle) / s.klineDay[i - 1].settle;

                    if (!((rate > 0.06 && s.klineDay[i].low < line3) || cross3Line))
                    {
                        if (rate > 0.07)
                        {
                            BigRed br = new BigRed()
                            {
                                gid = s.gid,
                                alert_date = s.klineDay[i].settleTime.Date,
                                price = s.klineDay[i].settle,
                                rate = rate
                            };
                            try
                            {
                                await _db.bigRed.AddAsync(br);
                                await _db.SaveChangesAsync();
                            }
                            catch
                            {

                            }
                        }
                        continue;
                    }

                    NearLine3BigRed n = await _db.nearLine3BigRed.FindAsync(s.gid.Trim(), s.klineDay[i].settleTime.Date);
                    if (n != null)
                    {
                        n.low = s.klineDay[i].low;
                        n.open = s.klineDay[i].open;
                        n.high = s.klineDay[i].high;
                        n.settle = s.klineDay[i].settle;
                        n.line3 = line3;
                        n.rate = rate;
                        _db.nearLine3BigRed.Entry(n).State = EntityState.Modified;
                    }
                    else
                    {
                        n = new NearLine3BigRed()
                        {
                            alert_date = s.klineDay[i].settleTime.Date,
                            gid = s.gid,
                            open = s.klineDay[i].open,
                            settle = s.klineDay[i].settle,
                            low = s.klineDay[i].low,
                            high = s.klineDay[i].high,
                            line3 = line3,
                            rate = rate
                        };
                        await _db.nearLine3BigRed.AddAsync(n);
                    }
                    

                }
                try
                {
                    await _db.SaveChangesAsync();
                }
                catch
                {

                }
            }
        }

	}
}

