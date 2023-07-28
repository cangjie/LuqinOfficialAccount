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
using System.Security.AccessControl;
using System.Data;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using System.Runtime.InteropServices.JavaScript;

namespace LuqinOfficialAccount.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class DoubleVolumeController : ControllerBase
    {
        private readonly AppDBContext _context;
        private readonly IConfiguration _config;
        private readonly Settings _settings;
        private readonly ChipController _chipCtrl;
        private readonly ConceptController _conceptCtrl;




        public DoubleVolumeController(AppDBContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
            _chipCtrl = new ChipController(_context, _config);
            _conceptCtrl = new ConceptController(context, config);
        }

        [HttpGet]
        public async Task<ActionResult<int>> SearchWeek(DateTime startDate, DateTime endDate)
        {
            
            int num = 0;
            Stock[] stockArr = Util.stockList;
            for (int i = 0; i < stockArr.Length; i++)
            {
                Stock s = stockArr[i];
                s.ForceRefreshKLineWeek();
                for (int j = s.klineWeek.Length - 1; j > 0; j--)
                {
                    if (s.klineWeek[j].settleTime.Date >= startDate.Date
                        && s.klineWeek[j].settleTime.Date <= endDate.Date
                        && s.klineWeek[j].volume > s.klineWeek[j - 1].volume * 2
                        && s.klineWeek[j].settle > s.klineWeek[j - 1].settle
                        && s.klineWeek[j].settle > s.klineWeek[j].open)
                    {
                        DateTime weekStartDate = s.klineWeek[j].settleTime.Date;
                        DateTime weekEndDate = s.klineWeek[j].settleTime.Date;
                        switch (s.klineWeek[j].settleTime.Date.DayOfWeek)
                        {
                            case DayOfWeek.Tuesday:
                                weekStartDate = weekStartDate.AddDays(-1);
                                break;
                            case DayOfWeek.Wednesday:
                                weekStartDate = weekStartDate.AddDays(-2);
                                break;
                            case DayOfWeek.Thursday:
                                weekStartDate = weekStartDate.AddDays(-3);
                                break;
                            case DayOfWeek.Friday:
                                weekStartDate = weekStartDate.AddDays(-4);
                                break;
                            default:
                                break;
                        }
                        weekEndDate = weekStartDate.AddDays(4);

                        var list = await _context.DoubleVolumeWeek.Where(d => (d.gid.Trim().Equals(s.gid.Trim())
                            && d.alert_date >= weekStartDate.Date && d.alert_date <= weekEndDate.Date )).ToListAsync();
                        double volumeRate = (double)s.klineWeek[j].volume / (double)s.klineWeek[j - 1].volume;
                        double priceRate = (s.klineWeek[j].settle - s.klineWeek[j - 1].settle) / s.klineWeek[j - 1].settle;
                        DoubleVolumeWeek dvw = new DoubleVolumeWeek()
                        {
                            gid = s.gid,
                            alert_date = s.klineWeek[j].settleTime.Date,
                            price_increase = priceRate,
                            volume_increase = volumeRate,
                            high_price = s.klineWeek[j].high
                        };
                        if (list != null && list.Count > 0)
                        {
                            dvw = (DoubleVolumeWeek)list[0];
                            dvw.alert_date = s.klineWeek[j].settleTime.Date;
                            dvw.price_increase = priceRate;
                            dvw.volume_increase = volumeRate;
                            dvw.high_price = s.klineWeek[j].high;
                            _context.Entry(dvw).State = EntityState.Modified;

                        }
                        else
                        {
                            await _context.AddAsync(dvw);
                        }
                        await _context.SaveChangesAsync();
                        num++;

                    }
                }
            }
            return Ok(num);
        }

        [HttpGet]
        public async Task<ActionResult<int>> Search(DateTime startDate, DateTime endDate)
        {
            if (!Util.IsTransacDay(startDate, _context))
            {
                startDate = Util.GetLastTransactDate(startDate, -1, _context);
            }
            if (!Util.IsTransacDay(endDate, _context))
            {
                endDate = Util.GetLastTransactDate(endDate, 1, _context);
            }
            Stock[] stockArr = Util.stockList;
            int num = 0;
            for (int i = 0; i < stockArr.Length; i++)
            {
                Stock s = stockArr[i];
                s.ForceRefreshKLineDay();
                int startIndex = s.GetItemIndex(startDate);
                int endIndex = s.GetItemIndex(endDate);
                if (startIndex < 5)
                {
                    continue;
                }
                for(int j = startIndex; j <= endIndex; j++) 
                {
                    double riseRate = (double)(s.klineDay[j].settle - s.klineDay[j - 1].settle) / (double)s.klineDay[j - 1].settle;
                    if (s.klineDay[j].open < s.klineDay[j].settle 
                        && s.klineDay[j].volume > s.klineDay[j - 1].volume * 2
                        && riseRate > 0.05
                        )
                    {
                        
                        var v = await _context.DoubleVolume.FindAsync(s.gid, s.klineDay[j].settleTime.Date);
                        if (v != null)
                        {
                            continue;
                        }
                        v = new DoubleVolume()
                        {
                            gid = s.gid.Trim(),
                            alert_date = s.klineDay[j].settleTime.Date,
                            volume_increase = (double)s.klineDay[j].volume / (double)s.klineDay[j - 1].volume,
                            price_increase = riseRate,
                            high_price = s.klineDay[j].high
                        };
                        num++;
                        try
                        {
                            await _context.DoubleVolume.AddAsync(v);
                            await _context.SaveChangesAsync();
                        }
                        catch
                        { 
                        
                        }
                    }
                }
            }
            return Ok(num);
        }

        [HttpGet("{days}")]
        public async Task<ActionResult<StockFilter>> GetVolumeDoubleWeekTouchLine20(int days, DateTime startDate, DateTime endDate, string sort = "放量 desc")
        {
            DataTable dt = await GetVolumeDoubleWeekTouch(startDate, endDate, 20, 0);
            
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
        public async Task<ActionResult<StockFilter>> GetVolumeDoubleWeekTouchLine10(int days, DateTime startDate, DateTime endDate, string sort = "放量 desc")
        {
            DataTable dt = await GetVolumeDoubleWeekTouch(startDate, endDate, 10, 0);

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
        public async Task<ActionResult<StockFilter>> GetVolumeDoubleWeekTouchLine33(int days, DateTime startDate, DateTime endDate, string sort = "放量 desc")
        {
            DataTable dt = await GetVolumeDoubleWeekTouch(startDate, endDate, 3, 3);

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
        public async Task<DataTable> GetVolumeDoubleWeekTouch(DateTime startDate, DateTime endDate, int maDays, int replacement)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            dt.Columns.Add("概念", Type.GetType("System.String"));
            dt.Columns.Add("放量", Type.GetType("System.Double"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));

            var list = await _context.DoubleVolumeWeek
                .Where(d => (d.alert_date.Date >= startDate.AddDays(-30)
                && d.alert_date.Date <= endDate.Date.AddDays(-1) && d.price_increase > 0.2))
                .ToListAsync();
            for (int i = 0; list != null && i < list.Count; i++)
            {
                Stock s = Stock.GetStock(list[i].gid);
                s.ForceRefreshKLineDay();
                s.ForceRefreshKLineWeek();
                DateTime alertDate = list[i].alert_date;
                Stock.ComputeRSV(s.klineWeek);
                Stock.ComputeMACD(s.klineWeek);
                Stock.ComputeKDJ(s.klineWeek);
                int alertIndexWeek = Stock.GetItemIndex(alertDate, s.klineWeek);
                if (alertIndexWeek < 1 || alertIndexWeek > s.klineWeek.Length)
                {
                    continue;
                }
                if (s.klineWeek[alertIndexWeek - 1].j > s.klineWeek[alertIndexWeek - 1].d || s.klineWeek[alertIndexWeek - 1].macd >= 0)
                {
                    continue;
                }
                int alertIndex = s.GetItemIndex(s.klineWeek[alertIndexWeek - 1].settleTime.Date) + 1;
                if (alertIndex <= 20 || alertIndex >= s.klineDay.Length)
                {
                    continue;
                }
                int overMa20 = 0;
                int buyIndex = -1;
                double buyPrice = -1;
                double startMa20 = double.MaxValue;
                double endMa20 = double.MinValue;
                for (int j = alertIndex; overMa20 < 2 && j < s.klineDay.Length; j++)
                {
                    double ma20 = KLine.GetAverageSettlePrice(s.klineDay, j, maDays, replacement);
                    if (j == alertIndex)
                    {
                        startMa20 = ma20;
                    }
                    else
                    {
                        endMa20 = ma20;
                    }
                    if (overMa20 == 1 && s.klineDay[j].low < ma20 && s.klineDay[j].settle > ma20)
                    {
                        overMa20++;
                        buyIndex = j;
                        buyPrice = ma20;
                        break;
                    }
                    if (overMa20 == 0 && s.klineDay[j].low > ma20)
                    {
                        overMa20++;
                    }

                }
                if (endMa20 <= startMa20)
                {
                    continue;
                }
                if (buyIndex <= 20)
                {
                    continue;
                }
                if (s.klineDay[buyIndex].settleTime.Date < startDate.Date
                    || s.klineDay[buyIndex].settleTime.Date > endDate.Date)
                {
                    continue;
                }
                DataRow dr = dt.NewRow();
                dr["日期"] = s.klineDay[buyIndex].settleTime.Date;
                dr["代码"] = s.gid.Trim();
                dr["名称"] = s.name.Trim();
                dr["信号"] = "";
                dr["放量"] = (double)s.klineDay[buyIndex].volume / (double)s.klineDay[buyIndex - 1].volume;

                dr["概念"] = "";
                dr["买入"] = buyPrice;
                dt.Rows.Add(dr);
            }
            return dt;
        }


        [HttpGet("{days}")]
        public async Task<ActionResult<StockFilter>> GetVolumeDoubleAgainGreenVolumeReduce(int days, DateTime startDate, DateTime endDate, string sort = "放量")
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            dt.Columns.Add("概念", Type.GetType("System.String"));
            dt.Columns.Add("筹码", Type.GetType("System.Double"));
            dt.Columns.Add("放量", Type.GetType("System.Double"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));

            startDate = Util.GetLastTransactDate(startDate, 1, _context);
            endDate = Util.GetLastTransactDate(endDate, 1, _context);
            StockFilter sf = (StockFilter)((OkObjectResult)(await GetVolumeDoubleAgain(days, startDate, endDate, sort)).Result).Value;
            if (sf == null || sf.itemList == null)
            {
                return NotFound();
            }
            for (int i = 0; i < sf.itemList.Count; i++)
            {
                Stock s = Stock.GetStock(sf.itemList[i].gid.Trim());
                DateTime alertDate = sf.itemList[i].alertDate.Date;
                s.ForceRefreshKLineDay();
                int alertIndex = s.GetItemIndex(alertDate);
                if (alertIndex < 0 || alertIndex > s.klineDay.Length - 1)
                {
                    continue;
                }
                int buyIndex = alertIndex + 1;
                if (s.klineDay[buyIndex].settle > s.klineDay[buyIndex].open
                    || s.klineDay[buyIndex].volume >= s.klineDay[buyIndex - 1].volume)
                {
                    continue;
                }



                double buyPrice = s.klineDay[buyIndex].settle;
                double chipValue = 0;
                ActionResult<Chip> chipResult = (await _chipCtrl.GetChip(s.gid.Trim(), s.klineDay[buyIndex - 1].settleTime.Date));

                if (chipResult.Result.GetType().Name.Trim().Equals("OkObjectResult"))
                {
                    Chip chip = (Chip)((OkObjectResult)chipResult.Result).Value;
                    chipValue = chip.chipDistribute90;
                }
                else
                {
                    if (!s.gid.StartsWith("kc"))
                    {
                        chipResult = (await _chipCtrl.GetOne(s.gid.Trim(), s.klineDay[buyIndex - 1].settleTime.Date));
                        if (chipResult.Result.GetType().Name.Trim().Equals("OkObjectResult"))
                        {
                            Chip chip = (Chip)((OkObjectResult)chipResult.Result).Value;
                            chipValue = chip.chipDistribute90;
                        }
                    }
                }

                ActionResult<string[]> conceptResult = await _conceptCtrl.GetConcept(s.gid);
                string conceptStr = "";
                if (conceptResult != null && conceptResult.Result.GetType().Name.Trim().Equals("OkObjectResult"))
                {
                    string[] cArr = (string[])((OkObjectResult)conceptResult.Result).Value;
                    for (int j = 0; j < cArr.Length; j++)
                    {
                        conceptStr += (j > 0 ? "," : "") + cArr[j].Trim();
                    }
                }

                DataRow dr = dt.NewRow();
                dr["日期"] = s.klineDay[buyIndex].settleTime.Date;
                dr["代码"] = s.gid.Trim();
                dr["名称"] = s.name.Trim();
                dr["信号"] = "";


                dr["概念"] = conceptStr.Trim();
                dr["筹码"] = chipValue;
                dr["放量"] = s.klineDay[buyIndex].volume / s.klineDay[buyIndex - 1].volume;
                dr["买入"] = buyPrice;
                dt.Rows.Add(dr);


            }


            sf = StockFilter.GetResult(dt.Select("", "日期 desc, " + sort), days);
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
        public async Task<ActionResult<StockFilter>> GetVolumeDoubleAgain(int days, DateTime startDate, DateTime endDate, string sort = "放量")
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            dt.Columns.Add("概念", Type.GetType("System.String"));
            dt.Columns.Add("筹码", Type.GetType("System.Double"));
            dt.Columns.Add("放量", Type.GetType("System.Double"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));

            var list = await _context.DoubleVolume
                .Where(d => (d.alert_date.Date <= endDate.Date && d.alert_date.Date >= startDate.Date
                 //.&& d.gid.Trim().Equals("sz002316")
                ))
                .AsNoTracking().ToListAsync();
            for (int i = 0; i < list.Count; i++)
            {
                Stock s = Stock.GetStock(list[i].gid.Trim());
                s.ForceRefreshKLineDay();
                bool valid = true;
                var subList = await _context.DoubleVolume
                    .Where(d => (d.gid.Trim().Equals(s.gid.Trim()) && d.alert_date.Date < list[i].alert_date.Date
                    
                    ))
                    .AsNoTracking().OrderByDescending(d => d.alert_date).ToListAsync();
                int endIndex = s.GetItemIndex(list[i].alert_date.Date);
                double high = s.klineDay[endIndex].high;
                if (subList != null && subList.Count > 0)
                {
                    DoubleVolume dv = subList[0];
                    if (subList.Count > 1)
                    {
                        DoubleVolume dv1 = subList[1];
                        if (dv1.alert_date.Date >= Util.GetLastTransactDate(dv.alert_date.Date, 60, _context))
                        {
                            valid = false;
                            continue;
                        }
                    }
                    if (dv.alert_date.Date >= Util.GetLastTransactDate(list[i].alert_date, 3, _context))
                    {
                        valid = false;
                        continue;
                    }
                    int startIndex = s.GetItemIndex(dv.alert_date.Date);
                    if (startIndex <= 20)
                    {
                        valid = false;
                        continue;
                    }

                    long startVolume = s.klineDay[startIndex].volume;
                    double line3Start = KLine.GetAverageSettlePrice(s.klineDay, startIndex - 10, 3, 3);
                    
                    

                    for (int j = startIndex + 1; j < endIndex && endIndex - startIndex <= 20 ; j++)
                    {
                        high = Math.Max(high, s.klineDay[j].high);
                        if (s.klineDay[j].volume >= startVolume)
                        {
                            valid = false;
                            continue;
                        }
                        if (s.klineDay[j].low < s.klineDay[startIndex].low)
                        {
                            valid = false;
                            continue;
                        }
                        if ((s.klineDay[j].high - s.klineDay[startIndex].high) / s.klineDay[startIndex].high > 0.03)
                        {
                            valid = false;
                            continue;
                        }
                        double curentLine3 = KLine.GetAverageSettlePrice(s.klineDay, j, 3, 3);
                        if (curentLine3 < line3Start)
                        {
                            valid = false;
                            continue;
                        }
                        if ((s.klineDay[j].settle - s.klineDay[j - 1].settle) / s.klineDay[j - 1].settle > 0.03)
                        {
                            valid = false;
                            continue;
                        }

                    }
                    bool isNotNewHigh = false;
                    for (int j = startIndex - 1; j >= 0 && j > startIndex - 60; j--)
                    {
                        if (s.klineDay[j].high > high)
                        {
                            isNotNewHigh = true;
                            break;
                        }
                    }
                    if (!isNotNewHigh)
                    {
                        valid = false;
                        continue;
                    }
                }
                else
                {
                    double curentLine3 = KLine.GetAverageSettlePrice(s.klineDay, endIndex, 3, 3);
                    if (endIndex < 20)
                    {
                        valid = false;
                        continue;
                    }
                    double prevLine3 = KLine.GetAverageSettlePrice(s.klineDay, endIndex - 10, 3, 3);
                    if (Math.Abs(prevLine3 - curentLine3) / prevLine3 >= 0.03)
                    {
                        valid = false;
                        continue;
                    }

                    bool isNotNewHigh = false;
                    for (int j = endIndex - 1; j >= 0 && j > endIndex - 60; j--)
                    {
                        if (s.klineDay[j].high > high)
                        {
                            isNotNewHigh = true;
                            break;
                        }
                    }
                    if (!isNotNewHigh)
                    {
                        valid = false;
                        continue;
                    }
                }
                if (!valid)
                {
                    continue;
                }

                double chipValue = 0;
                int buyIndex = endIndex;
                if (s.klineDay[buyIndex].high - s.klineDay[buyIndex].settle > (s.klineDay[buyIndex].settle - s.klineDay[buyIndex].low) / 2)
                {
                    continue;
                }

                ActionResult<Chip> chipResult = (await _chipCtrl.GetChip(s.gid.Trim(), s.klineDay[buyIndex - 1].settleTime.Date));

                if (chipResult.Result.GetType().Name.Trim().Equals("OkObjectResult"))
                {
                    Chip chip = (Chip)((OkObjectResult)chipResult.Result).Value;
                    chipValue = chip.chipDistribute90;
                }
                else
                {
                    if (!s.gid.StartsWith("kc"))
                    {
                        chipResult = (await _chipCtrl.GetOne(s.gid.Trim(), s.klineDay[buyIndex - 1].settleTime.Date));
                        if (chipResult.Result.GetType().Name.Trim().Equals("OkObjectResult"))
                        {
                            Chip chip = (Chip)((OkObjectResult)chipResult.Result).Value;
                            chipValue = chip.chipDistribute90;
                        }
                    }
                }

                ActionResult<string[]> conceptResult = await _conceptCtrl.GetConcept(s.gid);
                string conceptStr = "";
                if (conceptResult != null && conceptResult.Result.GetType().Name.Trim().Equals("OkObjectResult"))
                {
                    string[] cArr = (string[])((OkObjectResult)conceptResult.Result).Value;
                    for (int j = 0; j < cArr.Length; j++)
                    {
                        conceptStr += (j > 0 ? "," : "") + cArr[j].Trim();
                    }
                }

                double buyPrice = s.klineDay[buyIndex].settle;


                DataRow dr = dt.NewRow();
                dr["日期"] = s.klineDay[buyIndex].settleTime.Date;
                dr["代码"] = s.gid.Trim();
                dr["名称"] = s.name.Trim();
                dr["信号"] = "";
                
                
                dr["概念"] = conceptStr.Trim();
                dr["筹码"] = chipValue;
                dr["放量"] = s.klineDay[buyIndex].volume / s.klineDay[buyIndex - 1].volume;
                dr["买入"] = s.klineDay[buyIndex].settle;
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
        /*
        // GET: api/DoubleVolume
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DoubleVolume>>> GetDoubleVolume()
        {
            return await _context.DoubleVolume.ToListAsync();
        }

        // GET: api/DoubleVolume/5
        [HttpGet("{id}")]
        public async Task<ActionResult<DoubleVolume>> GetDoubleVolume(string id)
        {
            var doubleVolume = await _context.DoubleVolume.FindAsync(id);

            if (doubleVolume == null)
            {
                return NotFound();
            }

            return doubleVolume;
        }

        // PUT: api/DoubleVolume/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutDoubleVolume(string id, DoubleVolume doubleVolume)
        {
            if (id != doubleVolume.gid)
            {
                return BadRequest();
            }

            _context.Entry(doubleVolume).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DoubleVolumeExists(id))
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

        // POST: api/DoubleVolume
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<DoubleVolume>> PostDoubleVolume(DoubleVolume doubleVolume)
        {
            _context.DoubleVolume.Add(doubleVolume);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (DoubleVolumeExists(doubleVolume.gid))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetDoubleVolume", new { id = doubleVolume.gid }, doubleVolume);
        }

        // DELETE: api/DoubleVolume/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDoubleVolume(string id)
        {
            var doubleVolume = await _context.DoubleVolume.FindAsync(id);
            if (doubleVolume == null)
            {
                return NotFound();
            }

            _context.DoubleVolume.Remove(doubleVolume);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        */
        private bool DoubleVolumeExists(string id)
        {
            return _context.DoubleVolume.Any(e => e.gid == id);
        }
    }
}
