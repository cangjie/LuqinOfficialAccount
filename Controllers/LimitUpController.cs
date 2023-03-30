using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LuqinOfficialAccount.Models;
using Microsoft.Extensions.Configuration;



namespace LuqinOfficialAccount.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class LimitUpController : ControllerBase
    {
        private readonly AppDBContext _db;

        private readonly IConfiguration _config;

        private readonly Settings _settings;

        public LimitUpController(AppDBContext context, IConfiguration config)
        {
            _db = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
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

        /*
        // GET: api/LimitUp
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LimitUp>>> GetLimitUp()
        {
            return await _context.LimitUp.ToListAsync();
        }

        // GET: api/LimitUp/5
        [HttpGet("{id}")]
        public async Task<ActionResult<LimitUp>> GetLimitUp(string id)
        {
            var limitUp = await _context.LimitUp.FindAsync(id);

            if (limitUp == null)
            {
                return NotFound();
            }

            return limitUp;
        }

        // PUT: api/LimitUp/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutLimitUp(string id, LimitUp limitUp)
        {
            if (id != limitUp.gid)
            {
                return BadRequest();
            }

            _context.Entry(limitUp).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LimitUpExists(id))
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

        // POST: api/LimitUp
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<LimitUp>> PostLimitUp(LimitUp limitUp)
        {
            _context.LimitUp.Add(limitUp);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (LimitUpExists(limitUp.gid))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetLimitUp", new { id = limitUp.gid }, limitUp);
        }

        // DELETE: api/LimitUp/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLimitUp(string id)
        {
            var limitUp = await _context.LimitUp.FindAsync(id);
            if (limitUp == null)
            {
                return NotFound();
            }

            _context.LimitUp.Remove(limitUp);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        */


        private bool LimitUpExists(string id)
        {
            return _db.LimitUp.Any(e => e.gid == id);
        }
    }
}
