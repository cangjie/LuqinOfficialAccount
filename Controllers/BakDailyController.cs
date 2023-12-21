using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LuqinOfficialAccount;
using LuqinOfficialAccount.Models;
using Microsoft.Extensions.Configuration;
using System.Data;
using Newtonsoft.Json;
using System.Security.Cryptography;
//using Microsoft.AspNe
namespace LuqinOfficialAccount.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class BakDailyController: ControllerBase
    {
        private readonly AppDBContext _db;

        private readonly IConfiguration _config;

        private readonly Settings _settings;

        private readonly string tushareToken = "4da2fbec9c2cee373d3aace9f9e200a315a2812dc11267c425010cec";

        private readonly string tushareUrl = "http://api.tushare.pro";

        private class BakDailyResponse
        {
            public class BakDailyResponseData
            {
                public string[] fields { get; set; }
                public object[][] items { get; set; } 
            }
            public BakDailyResponseData data { get; set; }
        }

        public BakDailyController(AppDBContext context, IConfiguration config)
		{
            _db = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
        }

        [HttpGet("{days}")]
        public async Task<ActionResult<List<Inflow>>> GetDailyInflow(DateTime date, int days)
        {
            if (!Util.IsTransacDay(date, _db))
            {
                return NotFound();
            }
            string sql = " select gid, sum(case selling when 0 then 0 else (buying / selling)  end) as inflow from bak_daily  "
                + " where alert_date <= '" + date.Date.ToShortDateString() + "' and  alert_date >= '"
                + Util.GetLastTransactDate(date, days, _db).ToShortDateString() + "' group by gid "
                + " order by  sum(case selling when 0 then 0 else (buying / selling)  end)  desc ";
            var l = await _db.inflow.FromSqlRaw(sql).AsNoTracking().ToListAsync();
            for (int i = 0; i < l.Count; i++)
            {
                l[i].alert_date = date.Date;
            }
            return Ok(l);
        }

        [HttpGet("{days}")]
        public async Task<ActionResult<StockFilter>> LimitUpInflow(int days, DateTime startDate, DateTime endDate, string sort = "流入")
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            dt.Columns.Add("流入", Type.GetType("System.Double"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));

            int inFlowDays = 10;
            int adjustDays = 3;
            startDate = Util.GetLastTransactDate(startDate, adjustDays, _db);
            endDate = Util.GetLastTransactDate(endDate, adjustDays, _db);
            var l = await _db.LimitUp.Where(l => l.alert_date >= startDate.Date && l.alert_date <= endDate.Date)
                .OrderByDescending(l => l.alert_date).AsNoTracking().ToListAsync();

            if (l.Count <= 0)
            {
                return NotFound();
            }
            DateTime countDate = l[0].alert_date.Date;

            List<Inflow> inflowList = (List<Inflow>)((OkObjectResult)(await GetDailyInflow(Util.GetLastTransactDate(countDate, -2, _db), inFlowDays)).Result).Value;


            for (int i = 0; i < l.Count; i++)
            {
                if (countDate.Date != l[i].alert_date.Date)
                {
                    countDate = l[i].alert_date;
                    inflowList = (List<Inflow>)((OkObjectResult)(await GetDailyInflow(Util.GetLastTransactDate(countDate, -2, _db), inFlowDays)).Result).Value;
                }
                Stock s = Stock.GetStock(l[i].gid);
                try
                {
                    s.ForceRefreshKLineDay();
                }
                catch
                {
                    continue;
                }
                int alertIndex = s.GetItemIndex(l[i].alert_date);
                if (alertIndex >= s.klineDay.Length - adjustDays || alertIndex <= 2)
                {
                    continue;
                }

                bool haveLimitUpBefore = false;
                for (int j = alertIndex - 1; j >= 0 && j >= alertIndex - 20; j--)
                {
                    if (KLine.IsLimitUp(s.klineDay, j))
                    {
                        haveLimitUpBefore = true;
                        break;
                    }
                }
                if (haveLimitUpBefore)
                {
                    continue;
                }


                //bool findInflow = false;
                double inflow = 0;
                for (int j = 0; j < inflowList.Count; j++)
                {
                    if (inflowList[j].gid.Trim().Equals(s.gid))
                    {
                        if (inflowList[j].inflow >= inFlowDays)
                        {
                            inflow = inflowList[j].inflow;
                        }
                        break;
                    }
                }
                if (inflow == 0)
                {
                    continue;
                }
                int buyIndex = alertIndex + adjustDays;

                if (s.klineDay[buyIndex].settle < s.klineDay[buyIndex - 1].settle)
                {
                    continue;
                }

                if (KLine.IsLimitUp(s.klineDay, buyIndex - 1) || KLine.IsLimitUp(s.klineDay, buyIndex))
                {
                    continue;
                }

                DataRow dr = dt.NewRow();
                dr["日期"] = s.klineDay[buyIndex].settleTime.Date;
                dr["代码"] = s.gid.Trim();
                dr["名称"] = s.name.Trim();

                //dr["信号"] = "";
                dr["买入"] = s.klineDay[buyIndex].settle;
                dr["流入"] = inflow;
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
        public async Task<ActionResult<StockFilter>> LimitUpTwiceInflow(int days, DateTime startDate, DateTime endDate, string sort = "流入")
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            dt.Columns.Add("流入", Type.GetType("System.Double"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));

            int inFlowDays = 10;
            int adjustDays = 3;
            startDate = Util.GetLastTransactDate(startDate, adjustDays, _db);
            endDate = Util.GetLastTransactDate(endDate, adjustDays, _db);
            var l = await _db.LimitUpTwice.Where(l => l.alert_date >= startDate.Date && l.alert_date <= endDate.Date)
                .OrderByDescending(l => l.alert_date).AsNoTracking().ToListAsync();

            if (l.Count <= 0)
            {
                return NotFound();
            }
            DateTime countDate = l[0].alert_date.Date;

            List<Inflow> inflowList = (List<Inflow>)((OkObjectResult)(await GetDailyInflow(Util.GetLastTransactDate(countDate, -2, _db), inFlowDays)).Result).Value;


            for (int i = 0; i < l.Count; i++)
            {
                if (countDate.Date != l[i].alert_date.Date)
                {
                    countDate = l[i].alert_date;
                    inflowList = (List<Inflow>)((OkObjectResult)(await GetDailyInflow(Util.GetLastTransactDate(countDate, -2, _db), inFlowDays)).Result).Value;
                }
                Stock s = Stock.GetStock(l[i].gid);
                try
                {
                    s.ForceRefreshKLineDay();
                }
                catch
                {
                    continue;
                }
                int alertIndex = s.GetItemIndex(l[i].alert_date);
                if (alertIndex >= s.klineDay.Length - adjustDays || alertIndex <= 2)
                {
                    continue;
                }

                bool haveLimitUpBefore = false;
                for (int j = alertIndex - 2; j >= 0 && j >= alertIndex - 20; j--)
                {
                    if (KLine.IsLimitUp(s.klineDay, j))
                    {
                        haveLimitUpBefore = true;
                        break;
                    }
                }
                if (haveLimitUpBefore)
                {
                    continue;
                }


                //bool findInflow = false;
                double inflow = 0;
                for (int j = 0; j < inflowList.Count; j++)
                {
                    if (inflowList[j].gid.Trim().Equals(s.gid))
                    {
                        if (inflowList[j].inflow >= inFlowDays)
                        {
                            inflow = inflowList[j].inflow;
                        }
                        break;
                    }
                }
                if (inflow == 0)
                {
                    continue;
                }
                int buyIndex = alertIndex + adjustDays;
                
                if (s.klineDay[buyIndex].settle < s.klineDay[buyIndex - 1].settle)
                {
                    continue;
                }
                
                if (KLine.IsLimitUp(s.klineDay, buyIndex - 1) || KLine.IsLimitUp(s.klineDay, buyIndex))
                {
                    continue;
                }
                
                DataRow dr = dt.NewRow();
                dr["日期"] = s.klineDay[buyIndex].settleTime.Date;
                dr["代码"] = s.gid.Trim();
                dr["名称"] = s.name.Trim();
                
                //dr["信号"] = "";
                dr["买入"] = s.klineDay[buyIndex].settle;
                dr["流入"] = inflow;
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
        public async Task<ActionResult<StockFilter>> GetContinousFlowout(int days, DateTime startDate, DateTime endDate, string sort = "流入")
        {
            int reverseDays = 5;
            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            dt.Columns.Add("流入", Type.GetType("System.Double"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));



            for (DateTime i = endDate.Date; i >= startDate.Date; i = i.AddDays(-1))
            {
                var flowInList = (List<Inflow>)((OkObjectResult)(await GetDailyInflow(i, reverseDays)).Result).Value;
                for (int j = 0; j < flowInList.Count; j++)
                {
                    try
                    {
                        Inflow f = flowInList[j];
                        if (f.inflow > reverseDays || f.gid.ToLower().StartsWith("bj"))
                        {
                            continue;
                        }
                        Stock s = Stock.GetStock(f.gid);
                        s.ForceRefreshKLineDay();
                        int alertIndex = s.GetItemIndex(((DateTime)f.alert_date).Date);
                        if (alertIndex <= 60 || alertIndex >= s.klineDay.Length)
                        {
                            continue;
                        }
                        if (KLine.IsLimitUp(s.klineDay, s.gid, alertIndex) || s.klineDay[alertIndex].settle < s.klineDay[alertIndex - 1].settle)
                        {
                            continue;
                        }
                        bool haveLimitDown = false;
                        bool haveLimitUp = false;
                        bool haveAdjustLimitUp = false;
                        for (int k = alertIndex; k >= 1 && k >= alertIndex - reverseDays; k--)
                        {
                            if ((s.klineDay[k].low - s.klineDay[k - 1].settle) / s.klineDay[k - 1].settle <= -0.095)
                            {
                                haveLimitDown = true;
                                break;
                            }
                            if (KLine.IsLimitUp(s.klineDay, s.gid, k))
                            {
                                if (k < alertIndex - 1 && k > alertIndex - 5)
                                {
                                    haveAdjustLimitUp = true;
                                }
                                haveLimitUp = true;
                            }
                        }
                        if (haveLimitDown || !haveLimitUp || (s.klineDay[alertIndex].high - s.klineDay[alertIndex - 1].settle) / s.klineDay[alertIndex - 1].settle > 0.095)
                        {
                            continue;
                        }





                        double ma5 = KLine.GetAverageSettlePrice(s.klineDay, alertIndex, 5, 0);
                        double ma10 = KLine.GetAverageSettlePrice(s.klineDay, alertIndex, 10, 0);
                        double ma20 = KLine.GetAverageSettlePrice(s.klineDay, alertIndex, 20, 0);
                        double ma60 = KLine.GetAverageSettlePrice(s.klineDay, alertIndex, 60, 0);

                        bool rise = false;
                        if (s.klineDay[alertIndex].settle > ma5 && ma5 > ma10 && ma10 > ma20 && ma20 > ma60)
                        {
                            rise = true;
                        }
                        if (!rise)
                        {
                            continue;
                        }

                        if (s.klineDay[alertIndex].high < s.klineDay[alertIndex - 1].high
                            || s.klineDay[alertIndex].high < s.klineDay[alertIndex - 2].high
                            || s.klineDay[alertIndex].high < s.klineDay[alertIndex - 3].high)
                        {
                            continue;
                        }

                        DataRow dr = dt.NewRow();
                        dr["日期"] = s.klineDay[alertIndex].settleTime.Date;
                        dr["代码"] = s.gid.Trim();
                        dr["名称"] = s.name.Trim();
                        if (!haveAdjustLimitUp)
                        {
                            dr["信号"] = "";
                        }
                        else
                        {
                            dr["信号"] = "📈";
                        }
                        //dr["信号"] = "";
                        dr["买入"] = s.klineDay[alertIndex].settle;
                        dr["流入"] = f.inflow;
                        dt.Rows.Add(dr);
                    }
                    catch
                    {

                    }
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

        [HttpGet]
        public async Task<ActionResult<int>> GetDetail()
        {
            int count = 0;
            int pageSize = 1000;
            int currentPage = 0;
            int offset = pageSize * currentPage;
            DateTime date = DateTime.Now.Date;
            for (; ; )
            {
                try
                {
                    string dateStr = date.Year.ToString()
                        + "00".Substring(0, 2 - date.Month.ToString().Length) + date.Month.ToString()
                        + "00".Substring(0, 2 - date.Day.ToString().Length) + date.Day.ToString();
                    string postData = "{\n    \"api_name\": \"bak_daily\",\n    \"token\": \"" + tushareToken + "\",\n    \"params\":{\n        \"trade_date\": \"" + dateStr + "\",\n        \"offset\": " + offset.ToString() + ",\n        \"limit\": " + pageSize.ToString()
                        + "\n    },\n    \"fields\":[\n        \"ts_code\",\n    \"trade_date\",\n    \"name\",\n      \"close\",\n     \"vol\",\n      \"selling\",\n    \"buying\",\n     \"avg_price\",\n    \"strength\",\n    \"activity\",\n   \"avg_turnover\",\n    \"attack\",\n     ]\n}";
                    string retJson = Util.GetWebContent(tushareUrl, postData);
                    BakDailyResponse res = JsonConvert.DeserializeObject<BakDailyResponse>(retJson);
                    if (res.data == null || res.data.items.Length < pageSize || currentPage >= 10)
                    {
                        break;
                    }
                    for (int i = 0; i < res.data.items.Length; i++)
                    {
                        try
                        {
                            object[] item = res.data.items[i];
                            string[] gidArr = item[0].ToString().Split('.');
                            string gid = gidArr[1].ToLower() + gidArr[0];
                            BakDailyDetail dtl = new BakDailyDetail()
                            {
                                id = 0,
                                gid = gid,
                                alert_date = date,
                                price = double.Parse(item[3].ToString()),
                                vol = double.Parse(item[4].ToString()),
                                selling = double.Parse(item[5].ToString()),
                                buying = double.Parse(item[6].ToString()),
                                avg_price = double.Parse(item[7].ToString()),
                                strength = double.Parse(item[8].ToString()),
                                activity = double.Parse(item[9].ToString()),
                                avg_turn_over = double.Parse(item[10].ToString()),
                                attack = double.Parse(item[11].ToString())
                            };
                            await _db.bakDailyDetail.AddAsync(dtl);
                            count++;
                        }
                        catch
                        {

                        }
                    }
                    await _db.SaveChangesAsync();
                }
                catch
                {

                }
            }
            return Ok(count);
        }

        [HttpGet]
        public async Task<ActionResult<int>> GetBakForDays(DateTime startDate, DateTime endDate)
        {
            for (DateTime i = startDate.Date; i <= endDate.Date; i = i.AddDays(1))
            {
                if (Util.IsTransacDay(i, _db))
                {
                    await GetBakDaily(i);
                }
            }
            return Ok(0);
        }
        [HttpGet]
        public async Task<ActionResult<int>> GetBakDaily(DateTime date)
        {
            int pageSize = 1000;
            int currentPage = 0;
            int offset = pageSize * currentPage;
            int count = 0;
            for (; ; )
            {
                try
                {
                    string dateStr = date.Year.ToString()
                    + "00".Substring(0, 2 - date.Month.ToString().Length) + date.Month.ToString()
                    + "00".Substring(0, 2 - date.Day.ToString().Length) + date.Day.ToString();
                    string postData = "{\n    \"api_name\": \"bak_daily\",\n    \"token\": \"" + tushareToken + "\",\n    \"params\":{\n        \"trade_date\": \"" + dateStr + "\",\n        \"offset\": " + offset.ToString() + ",\n        \"limit\": " + pageSize.ToString() + "\n    },\n    \"fields\":[\n        \"ts_code\",\n    \"trade_date\",\n    \"name\",\n    \"pct_change\",\n    \"close\",\n    \"change\",\n    \"open\",\n    \"high\",\n    \"low\",\n    \"pre_close\",\n    \"vol_ratio\",\n    \"turn_over\",\n    \"swing\",\n    \"vol\",\n    \"amount\",\n    \"selling\",\n    \"buying\",\n    \"total_share\",\n    \"float_share\",\n    \"pe\",\n    \"industry\",\n    \"area\",\n    \"float_mv\",\n    \"total_mv\",\n    \"avg_price\",\n    \"strength\",\n    \"activity\",\n    \"avg_turnover\",\n    \"attack\",\n    \"interval_3\",\n    \"interval_6\"\n    ]\n}";
                    string retJson = Util.GetWebContent(tushareUrl, postData);
                    BakDailyResponse res = JsonConvert.DeserializeObject<BakDailyResponse>(retJson);
                    if (res.data == null || currentPage >= 10)
                    {
                        break;
                    }
                    for (int i = 0; i < res.data.items.Length; i++)
                    {
                        try
                        {
                            object[] item = res.data.items[i];
                            string[] gidArr = item[0].ToString().Split('.');
                            string gid = gidArr[1].ToLower() + gidArr[0];
                            Console.WriteLine(gid);
                            bak_daily bd = new bak_daily()
                            {
                                id = 0,
                                gid = gid,
                                alert_date = date,
                                name = item[2].ToString().Trim(),
                                pct_change = double.Parse(item[3].ToString().Trim()),
                                close = double.Parse(item[4].ToString().Trim()),
                                open = double.Parse(item[6].ToString().Trim()),
                                high = double.Parse(item[7].ToString().Trim()),
                                low = double.Parse(item[8].ToString().Trim()),
                                pre_close = double.Parse(item[9].ToString().Trim()),
                                vol_ratio = double.Parse(item[10].ToString().Trim()),
                                turn_over = double.Parse(item[11].ToString().Trim()),
                                vol = double.Parse(item[13].ToString().Trim()),
                                selling = double.Parse(item[15].ToString().Trim()),
                                buying = double.Parse(item[16].ToString().Trim()),
                                indurstry = item[20].ToString().Trim(),
                                area = item[21].ToString().Trim(),
                                strength = double.Parse(item[25].ToString().Trim()),
                                activity = double.Parse(item[26].ToString().Trim())
                            };
                            var l = await _db.bakDaily.Where(b => b.alert_date.Date == date.Date
                                && b.gid.Trim().Equals(bd.gid.Trim())).AsNoTracking()
                                .ToListAsync();
                            if (l.Count == 0)
                            {
                                await _db.bakDaily.AddAsync(bd);
                                count++;
                                //await _db.SaveChangesAsync();
                            }
                            

                        }
                        catch(Exception err)
                        {
                            Console.WriteLine(err.ToString());
                        }
                    }
                    await _db.SaveChangesAsync();
                    currentPage++;
                    offset = pageSize * currentPage;
                }
                catch(Exception err)
                {
                    Console.WriteLine(err.ToString());
                }
            }


       
            return Ok(count);
        }

        [HttpGet("{days}")]
        public async Task<ActionResult<StockFilter>> LimitUpWithSingleHorse(int days, DateTime startDate, DateTime endDate, string sort = "代码")
        {
            DataTable dt = (await LimitUpWithHorse(startDate, endDate, 1));
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
        public async Task<ActionResult<StockFilter>> LimitUpWithDoubleHorse(int days, DateTime startDate, DateTime endDate, string sort = "代码")
        {
            DataTable dt = (await LimitUpWithHorse(startDate, endDate, 2));
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
        public async Task<ActionResult<StockFilter>> LimitUpTwiceWithSingleHorse(int days, DateTime startDate, DateTime endDate, string sort = "代码")
        {
            DataTable dt = (await LimitUpTwiceWithHorse(startDate, endDate, 1));
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
        public async Task<ActionResult<StockFilter>> LimitUpTwiceWithDoubleHorse(int days, DateTime startDate, DateTime endDate, string sort = "代码")
        {
            DataTable dt = (await LimitUpTwiceWithHorse(startDate, endDate, 2));
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

        [NonAction]
        public async Task<DataTable> LimitUpTwiceWithHorse(DateTime startDate, DateTime endDate, int horseNum)
        {
            startDate = Util.GetLastTransactDate(startDate, horseNum, _db);
            endDate = Util.GetLastTransactDate(endDate, horseNum, _db);



            var limL = await _db.LimitUp.FromSqlRaw(" select * from limit_up_twice a where not exists ( "
                + " select 'a' from limit_up b where a.gid = b.gid and b.alert_date >= dbo.func_GetLastTransactDate(a.alert_date, 21) and b.alert_date < dbo.func_GetLastTransactDate(a.alert_date, 1)  "
                + " ) and a.alert_date >= '" + startDate.ToShortDateString() + "' and a.alert_date <= '" + endDate.ToShortDateString() + "' ").AsNoTracking().ToListAsync();

            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));
            dt.Columns.Add("流入", Type.GetType("System.Double"));

            for (int i = 0; i < limL.Count; i++)
            {
                Stock s = Stock.GetStock(limL[i].gid);
                try
                {
                    s.ForceRefreshKLineDay();
                }
                catch
                {
                    continue;
                }
                int alertIndex = s.GetItemIndex(limL[i].alert_date.Date);
                if (alertIndex < 2 || alertIndex >= s.klineDay.Length - horseNum)
                {
                    continue;
                }
                if (!KLine.IsLimitUp(s.klineDay, s.gid, alertIndex)
                    || !KLine.IsLimitUp(s.klineDay, s.gid, alertIndex - 1))
                {
                    continue;
                }
                double limPrice = s.klineDay[alertIndex].settle;
                bool isHorse = true;

                for (int j = 0; j < horseNum; j++)
                {
                    if (s.klineDay[alertIndex + j + 1].open <= limPrice || s.klineDay[alertIndex + j + 1].settle <= limPrice
                        || KLine.IsLimitUp(s.klineDay, alertIndex + j + 1))
                    {
                        isHorse = false;
                        break;
                    }
                }
                if (!isHorse)
                {
                    continue;
                }

                double selling = 0;
                double buying = 0;
                var flowL = await _db.bakDaily.Where(b => b.gid.Trim().Equals(s.gid)
                    && b.alert_date.Date >= s.klineDay[alertIndex + 1].settleTime.Date
                    && b.alert_date.Date <= s.klineDay[alertIndex + horseNum].settleTime.Date)
                    .AsNoTracking().ToListAsync();
                for (int j = 0; j < flowL.Count; j++)
                {
                    selling += flowL[j].selling;
                    buying += flowL[j].buying;
                }
                int buyIndex = alertIndex + horseNum;

                DataRow dr = dt.NewRow();
                dr["日期"] = s.klineDay[buyIndex].settleTime.Date;
                dr["代码"] = s.gid.Trim();
                dr["名称"] = s.name.Trim();
                dr["信号"] = "";
                dr["买入"] = s.klineDay[buyIndex].settle;
                if (selling == 0)
                {
                    dr["流入"] = 0;
                }
                else
                {
                    double flowNum = buying / selling;
                    dr["流入"] = flowNum;
                    if (flowNum < 1)
                    {
                        dr["信号"] = "📈";
                    }
                }
                dt.Rows.Add(dr);
            }
            return dt;
        }

        [NonAction]
        public async Task<DataTable> LimitUpWithHorse(DateTime startDate, DateTime endDate, int horseNum)
        {
            startDate = Util.GetLastTransactDate(startDate, horseNum, _db);
            endDate = Util.GetLastTransactDate(endDate, horseNum, _db);



            var limL = await _db.LimitUp.FromSqlRaw(" select * from limit_up a where not exists ( "
                + " select 'a' from limit_up b where a.gid = b.gid and b.alert_date >= dbo.func_GetLastTransactDate(a.alert_date, 21) and b.alert_date < a.alert_date "
                + " ) and a.alert_date >= '" + startDate.ToShortDateString() + "' and a.alert_date <= '" + endDate.ToShortDateString() + "' ").AsNoTracking().ToListAsync();

            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));
            dt.Columns.Add("流入", Type.GetType("System.Double"));

            for (int i = 0; i < limL.Count; i++)
            {
                Stock s = Stock.GetStock(limL[i].gid);
                try
                {
                    s.ForceRefreshKLineDay();
                }
                catch
                {
                    continue;
                }
                int alertIndex = s.GetItemIndex(limL[i].alert_date.Date);
                if (alertIndex < 2 || alertIndex >= s.klineDay.Length - horseNum)
                {
                    continue;
                }
                if (!KLine.IsLimitUp(s.klineDay, s.gid, alertIndex))
                {
                    continue;
                }
                double limPrice = s.klineDay[alertIndex].settle;
                bool isHorse = true;

                for (int j = 0; j < horseNum; j++)
                {
                    if (s.klineDay[alertIndex + j + 1].open <= limPrice || s.klineDay[alertIndex + j + 1].settle <= limPrice
                        || KLine.IsLimitUp(s.klineDay, alertIndex + j + 1))
                    {
                        isHorse = false;
                        break;
                    }
                }
                if (!isHorse)
                {
                    continue;
                }

                double selling = 0;
                double buying = 0;
                var flowL = await _db.bakDaily.Where(b => b.gid.Trim().Equals(s.gid)
                    && b.alert_date.Date >= s.klineDay[alertIndex + 1].settleTime.Date
                    && b.alert_date.Date <= s.klineDay[alertIndex + horseNum].settleTime.Date)
                    .AsNoTracking().ToListAsync();
                for (int j = 0; j < flowL.Count; j++)
                {
                    selling += flowL[j].selling;
                    buying += flowL[j].buying;
                }
                int buyIndex = alertIndex + horseNum;

                DataRow dr = dt.NewRow();
                dr["日期"] = s.klineDay[buyIndex].settleTime.Date;
                dr["代码"] = s.gid.Trim();
                dr["名称"] = s.name.Trim();
                dr["信号"] = "";
                dr["买入"] = s.klineDay[buyIndex].settle;
                if (selling == 0)
                {
                    dr["流入"] = 0;
                }
                else
                {
                    double flowNum = buying / selling;
                    dr["流入"] = flowNum;
                    if (flowNum < 1)
                    {
                        dr["信号"] = "📈";
                    }
                }
                dt.Rows.Add(dr);
            }
            return dt;
        }
    }
}

