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
