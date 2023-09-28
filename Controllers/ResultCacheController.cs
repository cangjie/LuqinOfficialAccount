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
    public class ResultCacheController : ControllerBase
    {
        private readonly AppDBContext _db;

        private readonly IConfiguration _config;

        private readonly Settings _settings;


        public ResultCacheController(AppDBContext context, IConfiguration config)
        {
            _db = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
        }

        [HttpGet]
        public async Task<ActionResult<ResultCache>> AddNew(string apiName, DateTime alertDate, string gid)
        {
            var l = await _db.ResultCache
                .Where(r => (r.api_name.Trim().Equals(apiName.Trim()) && r.gid.Trim().Equals(gid) && r.alert_date == alertDate))
                .ToListAsync();
            if (l != null && l.Count > 0)
            {
                return Ok(l[0]);
            }
            else
            {
                ResultCache r = new ResultCache()
                {
                    api_name = apiName.Trim(),
                    gid = gid.Trim(),
                    alert_date = alertDate
                };
                await _db.ResultCache.AddAsync(r);
                await _db.SaveChangesAsync();
                return Ok(r);
            }

        }

        //反包高开1%，盘中触及昨日收盘
        [HttpGet("{days}")]
        public async Task<ActionResult<StockFilter>> GetLimitUpAdjustSettleOverHighestAndLimitUpAgain(int days, DateTime startDate, DateTime endDate, string sort = "代码")
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            //dt.Columns.Add("概念", Type.GetType("System.String"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));
            //dt.Columns.Add("缩量", Type.GetType("System.Double"));
            string apiName = "/api/LimitUp/GetLimitUpAdjustSettleOverHighestAndLimitUpAgain";
            DateTime limitStartDate = Util.GetLastTransactDate(startDate, 1, _db);
            DateTime limitEndDate = Util.GetLastTransactDate(endDate, 1, _db);
            var l = await _db.ResultCache
                .Where(r => (r.alert_date >= startDate && r.alert_date <= endDate
                && r.api_name.Trim().Equals(apiName.Trim()))).ToListAsync();
            for (int i = 0; i < l.Count; i++)
            {
                string gid = l[i].gid.Trim();
                DateTime alertDate = l[i].alert_date;
                Stock s = Stock.GetStock(gid.Trim());
                try
                {
                    s.ForceRefreshKLineDay();
                }
                catch
                {
                    continue;
                }
                int alertIndex = s.GetItemIndex(alertDate);
                if (alertIndex < 0 || alertIndex >= s.klineDay.Length)
                {
                    continue;
                }
                if ((s.klineDay[alertIndex + 1].open - s.klineDay[alertIndex].settle) / s.klineDay[alertIndex].settle < 0.01
                    || s.klineDay[alertIndex + 1].low > s.klineDay[alertIndex].settle)
                {
                    continue;
                }
                DataRow dr = dt.NewRow();
                dr["日期"] = s.klineDay[alertIndex + 1].settleTime.Date;
                dr["代码"] = s.gid.Trim();
                dr["名称"] = s.name.Trim();
                dr["买入"] = s.klineDay[alertIndex].settle;
                dr["信号"] = "";
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

            // GET: api/ResultCache
            [HttpGet]
            public async Task<ActionResult<IEnumerable<ResultCache>>> GetResultCache()
            {
                return await _context.ResultCache.ToListAsync();
            }

            // GET: api/ResultCache/5
            [HttpGet("{id}")]
            public async Task<ActionResult<ResultCache>> GetResultCache(int id)
            {
                var resultCache = await _context.ResultCache.FindAsync(id);

                if (resultCache == null)
                {
                    return NotFound();
                }

                return resultCache;
            }

            // PUT: api/ResultCache/5
            // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
            [HttpPut("{id}")]
            public async Task<IActionResult> PutResultCache(int id, ResultCache resultCache)
            {
                if (id != resultCache.id)
                {
                    return BadRequest();
                }

                _context.Entry(resultCache).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ResultCacheExists(id))
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

            // POST: api/ResultCache
            // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
            [HttpPost]
            public async Task<ActionResult<ResultCache>> PostResultCache(ResultCache resultCache)
            {
                _context.ResultCache.Add(resultCache);
                await _context.SaveChangesAsync();

                return CreatedAtAction("GetResultCache", new { id = resultCache.id }, resultCache);
            }

            // DELETE: api/ResultCache/5
            [HttpDelete("{id}")]
            public async Task<IActionResult> DeleteResultCache(int id)
            {
                var resultCache = await _context.ResultCache.FindAsync(id);
                if (resultCache == null)
                {
                    return NotFound();
                }

                _context.ResultCache.Remove(resultCache);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            */
            private bool ResultCacheExists(int id)
        {
            return _db.ResultCache.Any(e => e.id == id);
        }
    }
}
