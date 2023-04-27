using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LuqinOfficialAccount.Models;
using Microsoft.Extensions.Configuration;

using System.Data;

namespace LuqinOfficialAccount.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class LimitUpController : ControllerBase
    {
        private readonly AppDBContext _db;

        private readonly IConfiguration _config;

        private readonly Settings _settings;

        private readonly ChipController chipCtrl;

        public LimitUpController(AppDBContext context, IConfiguration config)
        {
            _db = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
            chipCtrl = new ChipController(_db, _config);
            Util._db = context;
        }

        

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetLimitUpTwiceNew()
        {
            DateTime start = DateTime.Parse("2023-1-1");
            return await _db.LimitUpTwice.Where(l => (l.alert_date > start)).OrderByDescending(l => l.alert_date).Take(10).Select(l => l.gid).Distinct().ToListAsync();
        }
       

        [HttpGet]
        public async Task<ActionResult<IEnumerable<LimitUp>>> GetLimitUpTwice(string startDate, string endDate)
        {
            DateTime start = DateTime.Parse(startDate);
            DateTime end = DateTime.Parse(endDate);
            string sqlStr = "  select * from limit_up a where exists "
                + " ( select 'a' from limit_up b where a.gid = b.gid and b.alert_date = dbo.func_GetLastTransactDate(a.alert_date, 1) )  "
                + " and a.alert_date >= '" + start.Date.ToShortDateString() + "' and a.alert_date <= '" + end.Date.ToShortDateString() + "' ";
            var list = await _db.LimitUp.FromSqlRaw(sqlStr).OrderByDescending(l => l.alert_date).ToListAsync();
            Stock[] stocks = new Stock[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                for (int j = 0; j < Util.stockList.Length; j++)
                {
                    Stock s = Util.stockList[j];
                    if (s.gid.Trim().Equals(list[i].gid.Trim()))
                    {
                        list[i].name = s.name;
                    }
                }
            }
            return list;
        }

        [HttpGet]
        public async Task<ActionResult<CountResult>> CountLimitUpTwiceKDJGoldChipGathered(DateTime startDate, int countDays = 15, double chip = 0.15)
        {
            int successCount = 0;
            int bigSuccessCount = 0;
            ArrayList arr = new ArrayList();
            StockController sc = new StockController(_db, _config);
            var limitUpTwice = await _db.LimitUpTwice.Where(l => (l.alert_date >= startDate && !l.gid.StartsWith("kc")
            //&& l.gid.Trim().Equals("sz001236")
            )).ToListAsync();
            for (int i = 0; i < limitUpTwice.Count; i++)
            {
                try
                {

                    Stock s = (Stock)((OkObjectResult)sc.GetStock(limitUpTwice[i].gid).Result).Value;
                    int limitUpTwiceIndex = s.GetItemIndex(limitUpTwice[i].alert_date.Date);
                    if (limitUpTwiceIndex < 0)
                    {
                        continue;
                    }
                    int topIndex = KLine.GetForwardTopKLineItem(s.klineDay, limitUpTwiceIndex);
                    if (topIndex < 0)
                    {
                        continue;
                    }
                    int buyIndex = -1;
                    bool kdDead = false;
                    for (int j = topIndex + 1; j < s.klineDay.Length; j++)
                    {
                        if (s.klineDay[j].k < s.klineDay[j].d)
                        {
                            kdDead = true;
                        }
                        if (kdDead && s.klineDay[j].k > s.klineDay[j].d)
                        {
                            buyIndex = j;
                            break;
                        }
                    }
                    if (buyIndex <= 0 || buyIndex + countDays >= s.klineDay.Length)
                    {
                        continue;
                    }
                    var chipTopList = await _db.Chip.Where(c => (c.alert_date.Date == s.klineDay[topIndex].settleTime.Date && c.gid.Trim().Equals(s.gid.Trim()))).ToListAsync();
                    if (chipTopList.Count <= 0)
                    {
                        continue;
                    }
                    var chipBuyList = await _db.Chip.Where(c => (c.alert_date.Date == s.klineDay[buyIndex].settleTime.Date && c.gid.Trim().Equals(s.gid.Trim()))).ToListAsync();
                    if (chipBuyList.Count <= 0)
                    {
                        continue;
                    }
                    Chip chipTop = chipTopList[0];
                    Chip chipBuy = chipBuyList[0];

                    if (((chipBuy.cost_95pct - chipTop.cost_5pct) / (chipBuy.cost_95pct + chipTop.cost_5pct)) > chip)
                    {
                        continue;
                    }

                    if (((chipTop.cost_95pct - chipTop.cost_5pct) / (chipTop.cost_95pct + chipTop.cost_5pct)) <
                        ((chipBuy.cost_95pct - chipTop.cost_5pct) / (chipBuy.cost_95pct + chipTop.cost_5pct)))
                    {
                        continue;
                    }



                    CountItem item = new CountItem()
                    {
                        alert_date = s.klineDay[buyIndex].settleTime.Date,
                        gid = s.gid,
                        name = s.name.Trim(),
                        days = countDays,
                        riseRate = new double[countDays],
                        totalRiseRate = 0
                    };



                    bool exists = false;
                    for (int k = 0; k < arr.Count; k++)
                    {
                        CountItem checkDumpItem = (CountItem)arr[k];
                        if (item.gid.Trim().Equals(checkDumpItem.gid.Trim()) && item.alert_date.Date == checkDumpItem.alert_date.Date)
                        {
                            exists = true;
                        }
                    }
                    if (exists)
                    {
                        continue;
                        //arr.Add(item);
                    }


                    double buyPrice = s.klineDay[buyIndex].settle;
                    double maxPrice = 0;
                    for (int j = 0; j < countDays && buyIndex + 1 + j < s.klineDay.Length; j++)
                    {
                        maxPrice = Math.Max(maxPrice, s.klineDay[buyIndex + 1 + j].high);
                        item.riseRate[j] = (s.klineDay[buyIndex + 1 + j].high - buyPrice) / buyPrice;
                    }
                    item.totalRiseRate = (maxPrice - buyPrice) / buyPrice;
                    if (item.totalRiseRate >= 0.01)
                    {
                        successCount++;
                        if (item.totalRiseRate >= 0.05)
                        {
                            bigSuccessCount++;
                        }
                    }


                    arr.Add(item);
                }
                catch
                {
                    
                }



            }
            if (arr.Count == 0)
            {
                return NotFound();
            }
            CountItem[] itemArr = new CountItem[arr.Count];
            for (int i = 0; i < arr.Count; i++)
            {
                itemArr[i] = (CountItem)arr[i];
            }
            
            CountResult result = new CountResult()
            {
                Count = arr.Count,
                SuccessCount = successCount,
                BigSuccessCount = bigSuccessCount,
                SuccessRate = (double)successCount / (double)arr.Count,
                BigSuccessRate = (double)bigSuccessCount / (double)arr.Count,
                list = itemArr
            };

            return Ok(result);
            
        }


        [HttpGet("{days}")]
        public async Task<ActionResult<StockFilter>> GetLimitUpTwice(int days, DateTime startDate, DateTime endDate, string sort = "筹码")
        {
            ChipController chipCtrl = new ChipController(_db, _config);
            var limitupTwiceList = await _db.LimitUpTwice.Where(l => (l.alert_date >= startDate.Date && l.alert_date <= endDate.Date))
                .OrderByDescending(l => l.alert_date).ToListAsync();
            if (limitupTwiceList == null)
            {
                return BadRequest();
            }
            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            dt.Columns.Add("MACD", Type.GetType("System.Double"));
            dt.Columns.Add("筹码", Type.GetType("System.Double"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));

            for (int i = 0; i < limitupTwiceList.Count; i++)
            {
                Stock s = Stock.GetStock(limitupTwiceList[i].gid.Trim());
                try
                {
                    s.RefreshKLine();
                }
                catch
                {
                    continue;
                }
                DateTime alertDate = limitupTwiceList[i].alert_date.Date;
                int alertIndex = s.GetItemIndex(alertDate);
                DataRow dr = dt.NewRow();
                dr["日期"] = alertDate.Date;
                dr["代码"] = s.gid;
                dr["名称"] = s.name;
                dr["信号"] = "";
                dr["买入"] = s.klineDay[alertIndex].settle;
                dr["MACD"] = s.klineDay[alertIndex].macd;
                double chipValue = 0;

                ActionResult<Chip> chipResult = (await chipCtrl.GetChip(s.gid.Trim(), s.klineDay[alertIndex - 1].settleTime.Date));

                if (chipResult.Result.GetType().Name.Trim().Equals("OkObjectResult"))
                {
                    Chip chip = (Chip)((OkObjectResult)chipResult.Result).Value;
                    chipValue = chip.chipDistribute90;
                }
                else
                {
                    if (!s.gid.StartsWith("kc"))
                    {
                        chipResult = (await chipCtrl.GetOne(s.gid.Trim(), s.klineDay[alertIndex - 1].settleTime.Date));
                        if (chipResult.Result.GetType().Name.Trim().Equals("OkObjectResult"))
                        {
                            Chip chip = (Chip)((OkObjectResult)chipResult.Result).Value;
                            chipValue = chip.chipDistribute90;
                        }
                    }
                }
                dr["筹码"] = chipValue;
                if (chipValue < 0.15 && s.klineDay[alertIndex].macd < 1)
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
        public async Task<ActionResult<StockFilter>> GetLimitUpAdjust(int days, DateTime startDate, DateTime endDate, string sort = "MACD")
        {
            startDate = Util.GetLastTransactDate(startDate, 1, _db);
            endDate = Util.GetLastTransactDate(endDate, 1, _db);
            ChipController chipCtrl = new ChipController(_db, _config);
            var limitupTwiceList = await _db.LimitUp.Where(l => (l.alert_date >= Util.GetLastTransactDate(startDate.Date, -1, _db).Date
                && l.alert_date <= Util.GetLastTransactDate(endDate.Date, -1, _db)
                //&& l.gid.Trim().Equals("sz002406")
                ))
                .OrderByDescending(l => l.alert_date).ToListAsync();
            if (limitupTwiceList == null)
            {
                return BadRequest();
            }
            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            dt.Columns.Add("MACD", Type.GetType("System.Double"));
            dt.Columns.Add("筹码", Type.GetType("System.Double"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));

            for (int i = 0; i < limitupTwiceList.Count; i++)
            {
                Stock s = Stock.GetStock(limitupTwiceList[i].gid.Trim());
                try
                {
                    s.RefreshKLine();
                }
                catch
                {
                    continue;
                }
                int lowIndex = -1;
                
                DateTime alertDate = limitupTwiceList[i].alert_date.Date;
                int alertIndex = s.GetItemIndex(alertDate);
                int buyIndex = alertIndex + 1;
                if (buyIndex >= 1 && buyIndex < s.klineDay.Length)
                {
                    lowIndex = KLine.GetBackwardBottomKLineItem(s.klineDay, alertIndex - 1);
                    bool existsLimit = false;
                    for (int j = lowIndex; j < alertIndex; j++)
                    {
                        if (KLine.IsLimitUp(s.klineDay, j))
                        {
                            existsLimit = true;
                            break;
                        }
                    }
                    if (existsLimit)
                    {
                        continue;
                    }
                    if (s.klineDay[buyIndex].j >= 100 || s.klineDay[buyIndex].j <= s.klineDay[alertIndex].j
                        || s.klineDay[buyIndex].j < 50 || s.klineDay[buyIndex].j >= 100)
                    {
                        continue;
                    }
                    if (s.klineDay[buyIndex].open <= s.klineDay[buyIndex].settle
                        || s.klineDay[buyIndex].volume > s.klineDay[alertIndex].volume
                        )
                    {
                        continue;
                    }
                    DataRow dr = dt.NewRow();
                    dr["日期"] = s.klineDay[buyIndex].settleTime.Date;
                    dr["代码"] = s.gid;
                    dr["名称"] = s.name;
                    dr["信号"] = "";
                    dr["买入"] = s.klineDay[buyIndex].settle;
                    dr["MACD"] = s.klineDay[buyIndex].macd;
                    double chipValue = 0;

                    ActionResult<Chip> chipResult = (await chipCtrl.GetChip(s.gid.Trim(), s.klineDay[alertIndex - 1].settleTime.Date));

                    if (chipResult.Result.GetType().Name.Trim().Equals("OkObjectResult"))
                    {
                        Chip chip = (Chip)((OkObjectResult)chipResult.Result).Value;
                        chipValue = chip.chipDistribute90;
                    }
                    else
                    {
                        if (!s.gid.StartsWith("kc"))
                        {
                            chipResult = (await chipCtrl.GetOne(s.gid.Trim(), s.klineDay[alertIndex - 1].settleTime.Date));
                            if (chipResult.Result.GetType().Name.Trim().Equals("OkObjectResult"))
                            {
                                Chip chip = (Chip)((OkObjectResult)chipResult.Result).Value;
                                chipValue = chip.chipDistribute90;
                            }
                        }
                    }



                    if (chipValue == 0 || chipValue > 0.15)
                    {
                        continue;

                    }
                    //if (s.klineDay[buyIndex].macd < s.klineDay[alertIndex].macd)
                    if (s.klineDay[buyIndex].macd>1)
                    {
                        continue;
                    }

                    if (KLine.GetKdjOverSell(s.klineDay, buyIndex) < 10 && s.klineDay[buyIndex].high > s.klineDay[buyIndex - 1].high)
                    {
                        dr["信号"] = "🛍";
                    }


                    dr["筹码"] = chipValue;
                    
                    if (s.klineDay[buyIndex].macd < 0.2)
                    {
                        if (dr["信号"].ToString().IndexOf("🛍") >= 0)
                        {
                            dr["信号"] = "🔥";
                        }
                        else
                        {
                            dr["信号"] = "📈";
                        }
                        
                    }
                    
                    dt.Rows.Add(dr);
                }

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
        public async Task<ActionResult<StockFilter>> LimitUpTwiceSwordTwice(int days, DateTime startDate, DateTime endDate, string sort = "MACD")
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            dt.Columns.Add("MACD", Type.GetType("System.Double"));
            dt.Columns.Add("筹码", Type.GetType("System.Double"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));

            var limitUpTwiceList = await _db.LimitUpTwice
                .Where(l => (l.alert_date >= Util.GetLastTransactDate(startDate, 2, _db)
                && l.alert_date <= Util.GetLastTransactDate(endDate, 2, _db))).ToListAsync();
            for (int i = 0; i < limitUpTwiceList.Count; i++)
            {
                DateTime alertDate = limitUpTwiceList[i].alert_date;
                Stock s = Stock.GetStock(limitUpTwiceList[i].gid.Trim());
                try
                {
                    s.RefreshKLine();
                }
                catch
                {
                    continue;
                }
                int alertIndex = s.GetItemIndex(alertDate);
                if (alertIndex <= 0 || alertIndex + 2 >= s.klineDay.Length)
                {
                    continue;
                }
                if (!KLine.IsLimitUp(s.klineDay, alertIndex) || !KLine.IsLimitUp(s.klineDay, alertIndex - 1))
                {
                    continue;
                }
                if (KLine.IsLimitUp(s.klineDay, alertIndex + 1) || KLine.IsLimitUp(s.klineDay, alertIndex + 2))
                {
                    continue;
                }
                double alertPrice = s.klineDay[alertIndex].settle;
                if (Math.Min(s.klineDay[alertIndex + 1].settle, s.klineDay[alertIndex + 1].open) <= alertPrice
                    || Math.Min(s.klineDay[alertIndex + 2].settle, s.klineDay[alertIndex + 2].open) <= alertPrice)
                {
                    continue;
                }

                double chip = 0;
                try
                {
                    ActionResult<double> chipResult = await chipCtrl.GetChipAll(s.gid, s.klineDay[alertIndex - 1].settleTime.Date);
                    if (chipResult != null && chipResult.Result.GetType().Name.Trim().Equals("OkObjectResult"))
                    {
                        chip = (double)((OkObjectResult)chipResult.Result).Value;
                    }
                }
                catch
                {

                }

                DataRow dr = dt.NewRow();
                dr["日期"] = s.klineDay[alertIndex + 2].settleTime.Date;
                dr["代码"] = s.gid.Trim();
                dr["名称"] = s.name.Trim();
                dr["信号"] = "";
                dr["MACD"] = s.klineDay[alertIndex + 2].macd;
                dr["筹码"] = chip;
                dr["买入"] = s.klineDay[alertIndex + 2].settle;
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
        public async Task<ActionResult<StockFilter>> LimitUpTwiceSettleHighTwice(int days, DateTime startDate, DateTime endDate, string sort = "MACD")
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            dt.Columns.Add("MACD", Type.GetType("System.Double"));
            
            dt.Columns.Add("买入", Type.GetType("System.Double"));

            var doubleSwordResult = (await LimitUpTwiceOverHighTwice(days, startDate, endDate, sort)).Result;
            StockFilter sf = (StockFilter)((OkObjectResult)doubleSwordResult).Value;
            for (int i = 0; sf != null && i < sf.itemList.Count; i++)
            {
                Stock s = Stock.GetStock(sf.itemList[i].gid);
                try
                {
                    s.RefreshKLine();
                }
                catch
                {
                    continue;
                }
                int alertIndex = s.GetItemIndex(sf.itemList[i].alertDate.Date);
                if (alertIndex - 3 < 0 || alertIndex >= s.klineDay.Length)
                {
                    continue;
                }
                if (s.klineDay[alertIndex].settle <= s.klineDay[alertIndex - 1].high
                    || s.klineDay[alertIndex].settle <= s.klineDay[alertIndex - 2].high
                    || s.klineDay[alertIndex].settle <= s.klineDay[alertIndex - 3].high)
                {
                    continue;
                }
                DataRow dr = dt.NewRow();
                dr["日期"] = s.klineDay[alertIndex].settleTime.Date;
                dr["代码"] = s.gid.Trim();
                dr["名称"] = s.name.Trim();
                dr["信号"] = "";
                dr["MACD"] = s.klineDay[alertIndex].macd;
                dr["买入"] = s.klineDay[alertIndex].settle;
                dt.Rows.Add(dr);

            }
            StockFilter sfNew = StockFilter.GetResult(dt.Select("", "日期 desc, " + sort), days);
            try
            {
                return Ok(sfNew);
            }
            catch
            {
                return NotFound();

            }

        }



        [HttpGet("{days}")]
        public async Task<ActionResult<StockFilter>> LimitUpTwiceOverHighTwice(int days, DateTime startDate, DateTime endDate, string sort = "MACD")
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            dt.Columns.Add("MACD", Type.GetType("System.Double"));
            dt.Columns.Add("筹码", Type.GetType("System.Double"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));
            var limitUpTwiceList = await _db.LimitUpTwice
                .Where(l => (l.alert_date >= Util.GetLastTransactDate(startDate, 2, _db)
                && l.alert_date <= Util.GetLastTransactDate(endDate, 2, _db))).ToListAsync();
            for (int i = 0; i < limitUpTwiceList.Count; i++)
            {
                DateTime alertDate = limitUpTwiceList[i].alert_date;
                Stock s = Stock.GetStock(limitUpTwiceList[i].gid.Trim());
                try
                {
                    s.RefreshKLine();
                }
                catch
                {
                    continue;
                }
                int alertIndex = s.GetItemIndex(alertDate);
                if (alertIndex <= 0 || alertIndex + 2 >= s.klineDay.Length)
                {
                    continue;
                }
                if (!KLine.IsLimitUp(s.klineDay, alertIndex) || !KLine.IsLimitUp(s.klineDay, alertIndex - 1))
                {
                    continue;
                }
                if (KLine.IsLimitUp(s.klineDay, alertIndex + 1) || KLine.IsLimitUp(s.klineDay, alertIndex + 2))
                {
                    continue;
                }
                double alertPrice = s.klineDay[alertIndex].settle;
                if (s.klineDay[alertIndex + 1].settle <= alertPrice   || s.klineDay[alertIndex + 2].settle <= alertPrice)
                {
                    continue;
                }
                double chip = 0;
                try
                {
                    ActionResult<double> chipResult = await chipCtrl.GetChipAll(s.gid, s.klineDay[alertIndex - 1].settleTime.Date);
                    if (chipResult != null && chipResult.Result.GetType().Name.Trim().Equals("OkObjectResult"))
                    {
                        chip = (double)((OkObjectResult)chipResult.Result).Value;
                    }
                }
                catch
                {
                }
                DataRow dr = dt.NewRow();
                dr["日期"] = s.klineDay[alertIndex + 2].settleTime.Date;
                dr["代码"] = s.gid.Trim();
                dr["名称"] = s.name.Trim();
                dr["信号"] = "";
                dr["MACD"] = s.klineDay[alertIndex + 2].macd;
                dr["筹码"] = chip;
                dr["买入"] = s.klineDay[alertIndex + 2].settle;
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
        public async Task<ActionResult<StockFilter>> LimitUpOverHighTwice(int days, DateTime startDate, DateTime endDate, string sort = "MACD")
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            dt.Columns.Add("MACD", Type.GetType("System.Double"));
            dt.Columns.Add("筹码", Type.GetType("System.Double"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));
            var limitUpTwiceList = await _db.LimitUp
                .Where(l => (l.alert_date >= Util.GetLastTransactDate(startDate, 2, _db)
                && l.alert_date <= Util.GetLastTransactDate(endDate, 2, _db))).ToListAsync();
            for (int i = 0; i < limitUpTwiceList.Count; i++)
            {
                DateTime alertDate = limitUpTwiceList[i].alert_date;
                Stock s = Stock.GetStock(limitUpTwiceList[i].gid.Trim());
                try
                {
                    s.RefreshKLine();
                }
                catch
                {
                    continue;
                }
                int alertIndex = s.GetItemIndex(alertDate);
                if (alertIndex <= 0 || alertIndex + 2 >= s.klineDay.Length)
                {
                    continue;
                }
                if (!KLine.IsLimitUp(s.klineDay, alertIndex))
                {
                    continue;
                }
                if (KLine.IsLimitUp(s.klineDay, alertIndex + 1) || KLine.IsLimitUp(s.klineDay, alertIndex + 2))
                {
                    continue;
                }
                double alertPrice = s.klineDay[alertIndex].settle;
                if (s.klineDay[alertIndex + 1].settle <= alertPrice || s.klineDay[alertIndex + 2].settle <= alertPrice)
                {
                    continue;
                }
                double chip = 0;
                try
                {
                    ActionResult<double> chipResult = await chipCtrl.GetChipAll(s.gid, s.klineDay[alertIndex - 1].settleTime.Date);
                    if (chipResult != null && chipResult.Result.GetType().Name.Trim().Equals("OkObjectResult"))
                    {
                        chip = (double)((OkObjectResult)chipResult.Result).Value;
                    }
                }
                catch
                {
                }
                DataRow dr = dt.NewRow();
                dr["日期"] = s.klineDay[alertIndex + 2].settleTime.Date;
                dr["代码"] = s.gid.Trim();
                dr["名称"] = s.name.Trim();
                dr["信号"] = "";
                dr["MACD"] = s.klineDay[alertIndex + 2].macd;
                dr["筹码"] = chip;
                dr["买入"] = s.klineDay[alertIndex + 2].settle;
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
        public async Task<ActionResult<StockFilter>> LimitUpTwiceOverSell(int days, DateTime startDate, DateTime endDate, string sort = "MACD")
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            dt.Columns.Add("MACD", Type.GetType("System.Double"));
            dt.Columns.Add("筹码", Type.GetType("System.Double"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));

            var limitUpTwiceList = await _db.LimitUpTwice
                .Where(l => (l.alert_date >= startDate && l.alert_date <= endDate)).ToListAsync();

            for (int i = 0; i < limitUpTwiceList.Count; i++)
            {
                DateTime alertDate = limitUpTwiceList[i].alert_date;
                Stock s = Stock.GetStock(limitUpTwiceList[i].gid.Trim());
                try
                {
                    s.RefreshKLine();
                }
                catch
                {
                    continue;
                }
                int alertIndex = s.GetItemIndex(alertDate);
                if (alertIndex <= 0 || alertIndex  >= s.klineDay.Length)
                {
                    continue;
                }

                if (KLine.GetKdjOverSell(s.klineDay, alertIndex - 1) >= 10)
                {
                    continue;
                }

                double chip = 0;
                try
                {
                    ActionResult<double> chipResult = await chipCtrl.GetChipAll(s.gid, s.klineDay[alertIndex - 1].settleTime.Date);
                    if (chipResult != null && chipResult.Result.GetType().Name.Trim().Equals("OkObjectResult"))
                    {
                        chip = (double)((OkObjectResult)chipResult.Result).Value;
                    }
                }
                catch
                {

                }

                DataRow dr = dt.NewRow();
                dr["日期"] = s.klineDay[alertIndex].settleTime.Date;
                dr["代码"] = s.gid.Trim();
                dr["名称"] = s.name.Trim();
                dr["信号"] = "";
                dr["MACD"] = s.klineDay[alertIndex].macd;
                dr["筹码"] = chip;
                dr["买入"] = s.klineDay[alertIndex].settle;
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

           


        private bool LimitUpExists(string id)
        {
            return _db.LimitUp.Any(e => e.gid == id);
        }
    }
}
