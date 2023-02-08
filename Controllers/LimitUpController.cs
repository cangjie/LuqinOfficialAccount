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
        public async Task<ActionResult<IEnumerable<LimitUpTwice>>> GetLimitUpTwiceNew()
        {
            return await _db.LimitUpTwice.ToListAsync();
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
