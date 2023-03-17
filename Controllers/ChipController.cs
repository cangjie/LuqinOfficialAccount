using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LuqinOfficialAccount;
using LuqinOfficialAccount.Models;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;

namespace LuqinOfficialAccount.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ChipController : ControllerBase
    {
        private readonly AppDBContext _db;
        private readonly IConfiguration _config;
        private readonly Settings _settings;

        public struct ApiResult
        {
            public string msg;
            public ApiData data;
        }

        public struct ApiData
        {
            public string[][] items;
        }

        public ChipController(AppDBContext context, IConfiguration config)
        {
            _db = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
        }


        [HttpGet("{currentDate}")]
        public async Task GetChips(DateTime currentDate)
        {
            DateTime startDate = currentDate.Date.AddMonths(-2);

            //var gidList = await _db.LimitUpTwice.Where(l => (l.alert_date >= startDate.Date && !l.gid.StartsWith("kc"))).Select(l => l.gid).Distinct().ToListAsync();
            //var gidList = await _db.LimitUpTwice.ToListAsync();
            var gidList = Util.stockList;
            for (int i = 0; i < gidList.Length; i++)
            {
                string gid = gidList[i].gid.ToString().Trim();
                var chipList = await _db.Chip.Where(c => c.gid.Trim().Equals(gid)).OrderByDescending(c => c.id).ToListAsync();
                bool exists = false;
                if (chipList.Count > 0)
                {
                    if (chipList[0].alert_date >= currentDate.Date.AddDays(-1).Date)
                    {
                        exists = true;
                    }
                }
                if (!exists)
                {
                    await RequestChipData(gid, currentDate);
                }
            }
            //return Ok(0);
        }

        [NonAction]
        public async Task RequestChipData(string gid, DateTime currentDate)
        {
            gid = gid.ToLower();
            string newGid = gid;
            if (newGid.StartsWith("sh"))
            {
                newGid = newGid.Substring(2, 6) + ".SH";
            }
            else if (newGid.StartsWith("sz"))
            {
                newGid = newGid.Substring(2, 6) + ".SZ";
            }
            else
            {
                return;
            }

            DateTime endDate = currentDate.Date;
            DateTime startDate = currentDate.Date.AddMonths(-6);
            
            string startDateStr = startDate.Year.ToString() + startDate.Month.ToString().PadLeft(2, '0') + startDate.Day.ToString().PadLeft(2, '0');
            string endDateStr = endDate.Year.ToString() + endDate.Month.ToString().PadLeft(2, '0') + endDate.Day.ToString().PadLeft(2, '0');
            string postData = "{   \"api_name\": \"cyq_perf\","
                + "\"token\": \"" + _settings.tushare_token.Trim() + "\","
                + "\"params\":{       \"ts_code\" : \"" + newGid.Trim() + "\","
                + "\"start_date\": \"" + startDateStr + "\", "
                + "\"end_date\": \"" + endDateStr + "\"    },"
                + "\"fields\": \"\"}";
            string retJson = "";
            try
            {
                retJson = Util.GetWebContent("http://api.tushare.pro", postData);
                
                ApiResult result = JsonConvert.DeserializeObject<ApiResult>(retJson);
                for (int i = 0; i < result.data.items.Length; i++)
                {
                    string[] item = result.data.items[i];
                    Chip chip = new Chip();
                    chip.gid = gid;
                    
                    for (int j = 0; j < item.Length; j++)
                    {
                        string v = item[j];
                        switch (j)
                        {
                            case 1:
                                DateTime alertDate = DateTime.Parse(v.Substring(0, 4) + "-" + v.Substring(4, 2) + "-" + v.Substring(6, 2));
                                chip.alert_date = alertDate.Date;
                                break;
                            case 2:
                                chip.his_low = double.Parse(v);
                                break;
                            case 3:
                                chip.his_high = double.Parse(v);
                                break;
                            case 4:
                                chip.cost_5pct = double.Parse(v);
                                break;
                            case 5:
                                chip.cost_15pct = double.Parse(v);
                                break;
                            case 6:
                                chip.cost_50pct = double.Parse(v);
                                break;
                            case 7:
                                chip.cost_85pct = double.Parse(v);
                                break;
                            case 8:
                                chip.cost_95pct = double.Parse(v);
                                break;
                            case 9:
                                chip.weight_avg = double.Parse(v);
                                break;
                            case 10:
                                chip.winner_rate = double.Parse(v);
                                break;
                            default:
                                break;
                        }
                    }
                    var chipList = await _db.Chip.Where(c => c.alert_date == chip.alert_date && c.gid.Trim().Equals(chip.gid.Trim())).ToListAsync();
                    if (chipList.Count == 0)
                    {
                        chip.id = 0;
                        _db.Chip.Add(chip);
                        _db.SaveChanges();
                        
                    }
                }

            }
            catch
            {
                Console.WriteLine(retJson);
            }
            //await _db.SaveChangesAsync();

        }

        [HttpGet]
        public async Task GetTodayChips()
        {
            DateTime now = DateTime.Now.Date;
            await GetChips(now);
        }


        /*
        // GET: api/Chip
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Chip>>> GetChip()
        {
            return await _context.Chip.ToListAsync();
        }

        // GET: api/Chip/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Chip>> GetChip(int id)
        {
            var chip = await _context.Chip.FindAsync(id);

            if (chip == null)
            {
                return NotFound();
            }

            return chip;
        }

        // PUT: api/Chip/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutChip(int id, Chip chip)
        {
            if (id != chip.id)
            {
                return BadRequest();
            }

            _context.Entry(chip).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ChipExists(id))
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

        // POST: api/Chip
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Chip>> PostChip(Chip chip)
        {
            _context.Chip.Add(chip);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetChip", new { id = chip.id }, chip);
        }

        // DELETE: api/Chip/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteChip(int id)
        {
            var chip = await _context.Chip.FindAsync(id);
            if (chip == null)
            {
                return NotFound();
            }

            _context.Chip.Remove(chip);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        */
        private bool ChipExists(int id)
        {
            return _db.Chip.Any(e => e.id == id);
        }
    }
}
