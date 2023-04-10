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

namespace LuqinOfficialAccount.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class KDJController : ControllerBase
    {
        private readonly AppDBContext _db;

        private readonly IConfiguration _config;

        private readonly Settings _settings;

        private readonly ChipController chipCtrl;

        public KDJController(AppDBContext context, IConfiguration config)
        {
            _db = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
            chipCtrl = new ChipController(_db, _config);
            Util._db = context;
        }


        [HttpGet("{days}")]
        public async Task<ActionResult<StockFilter>> GetOverSell(int days, DateTime startDate, DateTime endDate, string sort = "筹码")
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            dt.Columns.Add("筹码", Type.GetType("System.Double"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));
            dt.Columns.Add("放量", Type.GetType("System.Double"));

            var kdjList = await _db.KDJ.Where(k => (k.alert_time.Date >= startDate.Date
                && k.alert_time.Date <= endDate.Date && k.gid.IndexOf("kc") < 0
                && k.k <= 50 && k.d <= 50 && k.j <= 50)).ToListAsync();
            if (kdjList == null)
            {
                return BadRequest();
            }
            for (int i = 0; i < kdjList.Count; i++)
            {
                Stock s = Stock.GetStock(kdjList[i].gid);
                s.RefreshKLine();
                DateTime alertDate = kdjList[i].alert_time.Date;
                int alertIndex = s.GetItemIndex(alertDate);
                if (alertIndex < 10 || alertIndex >= s.klineDay.Length)
                {
                    continue;
                }
                if (s.klineDay[alertIndex].j < s.klineDay[alertIndex].d)
                {
                    continue;
                }
                double minJ = double.MaxValue;
                for (int j = alertIndex - 1; j >= 0; j--)
                {
                    if (s.klineDay[j].j < s.klineDay[j].k)
                    {
                        minJ = Math.Min(minJ, s.klineDay[j].j);
                    }
                    if (minJ < double.MaxValue && s.klineDay[j].j > s.klineDay[j].k)
                    {
                        break;
                    }
                }

                //////////////
                if (minJ >= 0)
                {
                    continue;
                }

                bool ma20Rise = true;
                for (int j = alertIndex; j >= alertIndex - 5; j--)
                {
                    if (KLine.GetAverageSettlePrice(s.klineDay, j, 20, 0) <= KLine.GetAverageSettlePrice(s.klineDay, j - 1, 20, 0))
                    {
                        ma20Rise = false;
                        break;
                    }
                }
                if (!ma20Rise)
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
                double volDiff = ((double)s.klineDay[alertIndex].volume - s.klineDay[alertIndex - 1].volume) / (double)s.klineDay[alertIndex - 1].volume;

                DataRow dr = dt.NewRow();
                dr["日期"] = alertDate.Date;
                dr["代码"] = s.gid;
                dr["名称"] = s.name;
                dr["买入"] = s.klineDay[alertIndex].settle;
                
                dr["放量"] = volDiff;
                dr["筹码"] = chip;

                double ma5 = KLine.GetAverageSettlePrice(s.klineDay, alertIndex, 5, 0);
                double ma10 = KLine.GetAverageSettlePrice(s.klineDay, alertIndex, 10, 0);
                double ma20 = KLine.GetAverageSettlePrice(s.klineDay, alertIndex, 20, 0);

                if (s.klineDay[alertIndex].settle > ma20
                    && s.klineDay[alertIndex].open < s.klineDay[alertIndex].settle
                    && chip <= 0.15 && volDiff > 0)
                {
                    dr["信号"] = "📈";
                }
                else
                {
                    dr["信号"] = "";
                }


                dt.Rows.Add(dr);

            }

            StockFilter sf = StockFilter.GetResult(dt.Select("", "日期 desc, " + sort), 15);
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
        public async Task<ActionResult<StockFilter>> GetAbove3Line(int days, DateTime startDate, DateTime endDate, string sort = "MACD日")
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            dt.Columns.Add("筹码", Type.GetType("System.Double"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));
            dt.Columns.Add("放量", Type.GetType("System.Double"));
            dt.Columns.Add("MACD日", Type.GetType("System.Int32"));
            

            var kdjList = await _db.KDJ.Where(k => (k.alert_time.Date >= startDate.Date
                && k.alert_time.Date <= endDate.Date && k.gid.IndexOf("kc") < 0
                &&  k.j >= 50 && k.j <= 80  )).ToListAsync();
            var aboveList = await _db.Above3Line
                    .Where(a => (a.alert_date <= endDate.Date
                    && a.alert_date >= Util.GetLastTransactDate(startDate.Date, 3, _db)))
                    .ToListAsync();
            if (kdjList == null)
            {
                return BadRequest();
            }
            for (int i = 0; i < kdjList.Count; i++)
            {
                DateTime alertDate = kdjList[i].alert_time.Date;
                DateTime over3LineDate = DateTime.MaxValue;
                bool exist = false;
                for (int j = 0; j < aboveList.Count; j++)
                {
                    if (kdjList[i].gid.Trim().Equals(aboveList[j].gid.Trim())
                        && aboveList[j].alert_date.Date <= kdjList[i].alert_time.Date
                        && aboveList[j].alert_date.Date >= Util.GetLastTransactDate(kdjList[i].alert_time.Date, 5, _db))
                    {
                        exist = true;
                        over3LineDate = Util.GetLastTransactDate(aboveList[j].alert_date, aboveList[j].above_3_line_days, _db);
                        break;
                    }
                }
                if (!exist)
                {
                    continue;
                }
                if (over3LineDate == DateTime.MaxValue)
                {
                    continue;
                }
                Stock s = Stock.GetStock(kdjList[i].gid);
                s.RefreshKLine();
                
                int alertIndex = s.GetItemIndex(alertDate);
                if (alertIndex <= 0 || alertIndex >= s.klineDay.Length)
                {
                    continue;
                }
                
                int over3LineIndex = s.GetItemIndex(over3LineDate.Date);
                if (over3LineIndex <= 1 || over3LineIndex >= s.klineDay.Length)
                {
                    continue;
                }
                bool haveBigRed = false;

                for (int j = over3LineIndex; j <= alertIndex; j++)
                {
                    if ((s.klineDay[j].settle - s.klineDay[j - 1].settle) / s.klineDay[j - 1].settle >= 0.08)
                    {
                        haveBigRed = true;
                    }
                }
                if (haveBigRed)
                {
                    continue;
                }
                if (s.klineDay[alertIndex].open >= s.klineDay[alertIndex].settle)
                {
                    continue;
                }
                
                if ((s.klineDay[alertIndex].high - s.klineDay[alertIndex].settle * 2) >= (s.klineDay[alertIndex].settle - s.klineDay[alertIndex].low))
                {
                    continue;
                }
                
                int macdDays = 0;
                for (int j = alertIndex; s.klineDay[j].macd >= 0 && j >= 0; j--)
                {
                    macdDays++;
                }
                /*
                double minJ = double.MaxValue;
                for (int j = alertIndex - 1; j >= 0; j--)
                {
                    if (s.klineDay[j].j < s.klineDay[j].k)
                    {
                        minJ = Math.Min(minJ, s.klineDay[j].j);
                    }
                    if (minJ < double.MaxValue && s.klineDay[j].j > s.klineDay[j].k)
                    {
                        break;
                    }
                }

                //////////////
                if (minJ >= 0)
                {
                    continue;
                }
                */


                if (s.klineDay[alertIndex].settle <= KLine.GetAverageSettlePrice(s.klineDay, alertIndex, 3, 3))
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
                double volDiff = ((double)s.klineDay[alertIndex].volume - s.klineDay[alertIndex - 1].volume) / (double)s.klineDay[alertIndex - 1].volume;
                
                DataRow dr = dt.NewRow();
                dr["日期"] = alertDate.Date;
                dr["代码"] = s.gid;
                dr["名称"] = s.name;
                dr["买入"] = s.klineDay[alertIndex].settle;

                dr["放量"] = volDiff;
                dr["筹码"] = chip;
                dr["MACD日"] = macdDays;
                dt.Rows.Add(dr);
            }
            StockFilter sf = StockFilter.GetResult(dt.Select("", "日期 desc, " + sort), 15);
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
        public async Task<ActionResult<StockFilter>> HourAfterWeek(int days, DateTime startDate, DateTime endDate, string sort = "筹码")
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("时间", Type.GetType("System.String"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            dt.Columns.Add("筹码", Type.GetType("System.Double"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));
            //dt.Columns.Add("放量", Type.GetType("System.Double"));
            //dt.Columns.Add("MACD日", Type.GetType("System.Int32"));

            var kdjWeekList = await _db.KDJ.Where(k => (k.alert_time >= startDate.AddDays(-7).Date
                //&& k.gid.Trim().Equals("sh605567")
                && k.alert_time <= endDate.Date && k.alert_type.Trim().Equals("week"))).ToListAsync();
            if (kdjWeekList == null)
            {
                return BadRequest();
            }
            for (int i = 0; i < kdjWeekList.Count; i++)
            {
                DateTime alertDate = kdjWeekList[i].alert_time.Date;
                Stock s = Stock.GetStock(kdjWeekList[i].gid.Trim());
                //s.ForceRefreshKLineDay();
                s.ForceRefreshKLineHour();
                Stock.ComputeRSV(s.klineHour);
                Stock.ComputeKDJ(s.klineHour);
                int alertHourIndex = Stock.GetItemIndex(alertDate.Date.AddHours(15), s.klineHour);
                if (alertHourIndex <= 80 || alertHourIndex >= s.klineHour.Length)
                {
                    continue;
                }
                bool ma20Rise = true;
                int buyIndex = -1;
                for (int j = alertHourIndex - 40;  j < s.klineHour.Length && s.klineHour[j].settleTime.Date <= endDate.Date; j++)
                {
                    if (KLine.GetAverageSettlePrice(s.klineHour, j, 80, 0) < KLine.GetAverageSettlePrice(s.klineHour, j - 1, 80, 0))
                    {
                        ma20Rise = false;
                        break;
                    }
                    if (j > alertHourIndex)
                    {
                        if (s.klineHour[j].k > s.klineHour[j].d && s.klineHour[j - 1].k < s.klineHour[j - 1].d)
                        {
                            buyIndex = j;
                            break;
                        }
                    }
                }
                if (!ma20Rise || buyIndex < 0)
                {
                    //continue;
                }
                if (buyIndex < 0)
                {
                    continue;
                }
                if (dt.Select(" 日期 = '" + s.klineHour[buyIndex].settleTime.Date + "' and 代码 = '" + s.gid.Trim() + "' ").Length > 0)
                {
                    continue;
                }
                DataRow dr = dt.NewRow();
                dr["日期"] = s.klineHour[buyIndex].settleTime.Date;
                dr["时间"] = s.klineHour[buyIndex].settleTime.ToShortTimeString();
                dr["代码"] = s.gid;
                dr["名称"] = s.name;
                dr["买入"] = s.klineHour[buyIndex].settle;
                dr["信号"] = "";

                double chip = 0;
                try
                {
                    ActionResult<double> chipResult = await chipCtrl.GetChipAll(s.gid, s.klineDay[buyIndex - 4].settleTime.Date);
                    if (chipResult != null && chipResult.Result.GetType().Name.Trim().Equals("OkObjectResult"))
                    {
                        chip = (double)((OkObjectResult)chipResult.Result).Value;
                    }
                }
                catch
                {

                }
                dr["筹码"] = chip;
                dt.Rows.Add(dr);
            }
            StockFilter sf = StockFilter.GetResult(dt.Select("", "日期 desc, " + sort), 15);
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
        public async Task<ActionResult<int>> SearchKDJWeekGoldFork(DateTime startDate, DateTime endDate)
        {
            int ret = 0;
            Stock[] sArr = Util.stockList;
            for (int i = 0; i < sArr.Length; i++)
            {
                Stock s = sArr[i];
                s.ForceRefreshKLineWeek();
                Stock.ComputeRSV(s.klineWeek);
                Stock.ComputeKDJ(s.klineWeek);
                int startIndex = Stock.GetItemIndex(startDate, s.klineWeek);
                int endIndex = Stock.GetItemIndex(endDate, s.klineWeek);
                if (startIndex < 1 || startIndex >= s.klineWeek.Length || endIndex < startIndex)
                {
                    continue;
                }

                for (int j = startIndex; j <= endIndex; j++)
                {
                    if (s.klineWeek[j - 1].k < s.klineWeek[j - 1].d
                        && s.klineWeek[j].k > s.klineWeek[j].d)
                    {
                        var kdjList = await _db.KDJ.Where(i => (i.gid.Trim().Equals(s.gid))
                            && i.alert_type.Trim().Equals("week") && i.alert_time.Date == s.klineWeek[j].settleTime.Date).ToListAsync();
                        if (kdjList != null && kdjList.Count > 0)
                        {
                            for (int m = 0; m < kdjList.Count; m++)
                            {
                                try
                                {
                                    _db.KDJ.Remove(kdjList[m]);
                                }
                                catch
                                {

                                }
                            }
                        }
                        KDJ kdj = new KDJ()
                        {
                            alert_type = "week",
                            alert_time = s.klineWeek[j].settleTime.Date,
                            alert_price = s.klineWeek[j].settle,
                            gid = s.gid,
                            k = s.klineWeek[j].k,
                            d = s.klineWeek[j].d,
                            j = s.klineWeek[j].j
                        };
                        await _db.KDJ.AddAsync(kdj);
                        await _db.SaveChangesAsync();

                    }
                }
                
            }
            return Ok(ret);
        }


        /*
        // GET: api/KDJ
        [HttpGet]
        public async Task<ActionResult<IEnumerable<KDJ>>> GetKDJ()
        {
            return await _context.KDJ.ToListAsync();
        }

        // GET: api/KDJ/5
        [HttpGet("{id}")]
        public async Task<ActionResult<KDJ>> GetKDJ(string id)
        {
            var kDJ = await _context.KDJ.FindAsync(id);

            if (kDJ == null)
            {
                return NotFound();
            }

            return kDJ;
        }

        // PUT: api/KDJ/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutKDJ(string id, KDJ kDJ)
        {
            if (id != kDJ.gid)
            {
                return BadRequest();
            }

            _context.Entry(kDJ).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!KDJExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/KDJ
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<KDJ>> PostKDJ(KDJ kDJ)
        {
            _context.KDJ.Add(kDJ);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (KDJExists(kDJ.gid))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetKDJ", new { id = kDJ.gid }, kDJ);
        }

        // DELETE: api/KDJ/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteKDJ(string id)
        {
            var kDJ = await _context.KDJ.FindAsync(id);
            if (kDJ == null)
            {
                return NotFound();
            }

            _context.KDJ.Remove(kDJ);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        
        private bool KDJExists(string id)
        {
            return _context.KDJ.Any(e => e.gid == id);
        }
        */
    }
}
