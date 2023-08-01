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

namespace LuqinOfficialAccount.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class FirstLimitUpNewHighsController : ControllerBase
    {
        private readonly AppDBContext _context;
        private readonly IConfiguration _config;
        private readonly Settings _settings;
        private readonly ChipController _chipCtrl;
        private readonly ConceptController _conceptCtrl;

        public FirstLimitUpNewHighsController(AppDBContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
            _chipCtrl = new ChipController(_context, _config);
            _conceptCtrl = new ConceptController(context, config);
        }

        [HttpGet]
        public async Task<ActionResult<int>> Search(DateTime start, DateTime end, int days)
        {
            int num = 0;
            Stock[] stockArr = Util.stockList;
            for (int i = 0; i < stockArr.Length; i++)
            {
                Stock s = stockArr[i];
                s.ForceRefreshKLineDay();
                for (int j = s.klineDay.Length - 1; j >= 0; j--)
                {
                    if (s.klineDay[j].settleTime.Date >= start.Date && s.klineDay[j].settleTime.Date <= end.Date
                        && KLine.IsLimitUp(s.klineDay, j))
                    {
                        bool isNewHigh = true;
                        bool noLimitUp = true;
                        double high = double.MinValue;
                        double low = double.MaxValue;
                        for (int k = j - 1; k >= 0 && k >= j - days; k--)
                        {
                            if (s.klineDay[k].high > s.klineDay[j].settle)
                            {
                                isNewHigh = false;
                                break;
                            }
                            if (KLine.IsLimitUp(s.klineDay, k))
                            {
                                noLimitUp = false;
                                break;
                            }
                            high = Math.Max(s.klineDay[k].high, high);
                            low = Math.Min(s.klineDay[k].low, low);
                        }
                        if (isNewHigh && noLimitUp)
                        {
                            FirstLimitUpNewHigh fln = new FirstLimitUpNewHigh()
                            {
                                gid = s.gid.Trim(),
                                alert_date = s.klineDay[j].settleTime.Date,
                                days = days,
                                high = high,
                                low = low
                            };
                            try
                            {
                                await _context.FirstLimitUpNewHigh.AddAsync(fln);
                                await _context.SaveChangesAsync();
                                num++;

                            }
                            catch
                            { 
                            
                            }
                            
                        }
                    }
                }
            }
            return Ok(num);
        }

        /*
        // GET: api/FirstLimitUpNewHighs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FirstLimitUpNewHigh>>> GetFirstLimitUpNewHigh()
        {
            return await _context.FirstLimitUpNewHigh.ToListAsync();
        }

        // GET: api/FirstLimitUpNewHighs/5
        [HttpGet("{id}")]
        public async Task<ActionResult<FirstLimitUpNewHigh>> GetFirstLimitUpNewHigh(string id)
        {
            var firstLimitUpNewHigh = await _context.FirstLimitUpNewHigh.FindAsync(id);

            if (firstLimitUpNewHigh == null)
            {
                return NotFound();
            }

            return firstLimitUpNewHigh;
        }

        // PUT: api/FirstLimitUpNewHighs/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutFirstLimitUpNewHigh(string id, FirstLimitUpNewHigh firstLimitUpNewHigh)
        {
            if (id != firstLimitUpNewHigh.gid)
            {
                return BadRequest();
            }

            _context.Entry(firstLimitUpNewHigh).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FirstLimitUpNewHighExists(id))
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

        // POST: api/FirstLimitUpNewHighs
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<FirstLimitUpNewHigh>> PostFirstLimitUpNewHigh(FirstLimitUpNewHigh firstLimitUpNewHigh)
        {
            _context.FirstLimitUpNewHigh.Add(firstLimitUpNewHigh);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (FirstLimitUpNewHighExists(firstLimitUpNewHigh.gid))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetFirstLimitUpNewHigh", new { id = firstLimitUpNewHigh.gid }, firstLimitUpNewHigh);
        }

        // DELETE: api/FirstLimitUpNewHighs/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFirstLimitUpNewHigh(string id)
        {
            var firstLimitUpNewHigh = await _context.FirstLimitUpNewHigh.FindAsync(id);
            if (firstLimitUpNewHigh == null)
            {
                return NotFound();
            }

            _context.FirstLimitUpNewHigh.Remove(firstLimitUpNewHigh);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool FirstLimitUpNewHighExists(string id)
        {
            return _context.FirstLimitUpNewHigh.Any(e => e.gid == id);
        }
        */
    }
}
