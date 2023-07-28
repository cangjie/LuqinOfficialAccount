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
    [Route("api/[controller]")]
    [ApiController]
    public class DoubleGreenLegController : ControllerBase
    {
        private readonly AppDBContext _context;
        private readonly IConfiguration _config;
        private readonly Settings _settings;
        private readonly ChipController _chipCtrl;
        private readonly ConceptController _conceptCtrl;


        public DoubleGreenLegController(AppDBContext context, IConfiguration config)
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
                for (int j = s.klineDay.Length - 1;
                    j >= 1 && s.klineDay[j].settleTime.Date <= endDate.Date && s.klineDay[j].settleTime.Date >= startDate.Date;
                    j--)
                {
                    KLine currentK = s.klineDay[j];
                    KLine prevK = s.klineDay[j - 1];
                    if ((currentK.settle - currentK.open) / currentK.open >= -0.05
                        || (prevK.settle - prevK.open) / prevK.open >= -0.05)
                    {
                        continue;
                    }
                    DoubleGreenLeg dgl = new DoubleGreenLeg()
                    {
                        gid = s.gid,
                        alert_date = currentK.settleTime.Date,
                        price_rise_rate = (currentK.settle - prevK.settle) / prevK.settle
                    };
                    try
                    {
                        await _context.AddAsync(dgl);
                        await _context.SaveChangesAsync();
                        num++;
                    }
                    catch
                    {

                    }
                }
            }
            return Ok(num);
        }
        /*
            // GET: api/DoubleGreenLeg
            [HttpGet]
            public async Task<ActionResult<IEnumerable<DoubleGreenLeg>>> GetDoubleGreenLeg()
            {
                return await _context.DoubleGreenLeg.ToListAsync();
            }

            // GET: api/DoubleGreenLeg/5
            [HttpGet("{id}")]
            public async Task<ActionResult<DoubleGreenLeg>> GetDoubleGreenLeg(string id)
            {
                var doubleGreenLeg = await _context.DoubleGreenLeg.FindAsync(id);

                if (doubleGreenLeg == null)
                {
                    return NotFound();
                }

                return doubleGreenLeg;
            }

            // PUT: api/DoubleGreenLeg/5
            // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
            [HttpPut("{id}")]
            public async Task<IActionResult> PutDoubleGreenLeg(string id, DoubleGreenLeg doubleGreenLeg)
            {
                if (id != doubleGreenLeg.gid)
                {
                    return BadRequest();
                }

                _context.Entry(doubleGreenLeg).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DoubleGreenLegExists(id))
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

            // POST: api/DoubleGreenLeg
            // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
            [HttpPost]
            public async Task<ActionResult<DoubleGreenLeg>> PostDoubleGreenLeg(DoubleGreenLeg doubleGreenLeg)
            {
                _context.DoubleGreenLeg.Add(doubleGreenLeg);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (DoubleGreenLegExists(doubleGreenLeg.gid))
                    {
                        return Conflict();
                    }
                    else
                    {
                        throw;
                    }
                }

                return CreatedAtAction("GetDoubleGreenLeg", new { id = doubleGreenLeg.gid }, doubleGreenLeg);
            }

            // DELETE: api/DoubleGreenLeg/5
            [HttpDelete("{id}")]
            public async Task<IActionResult> DeleteDoubleGreenLeg(string id)
            {
                var doubleGreenLeg = await _context.DoubleGreenLeg.FindAsync(id);
                if (doubleGreenLeg == null)
                {
                    return NotFound();
                }

                _context.DoubleGreenLeg.Remove(doubleGreenLeg);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            */
            private bool DoubleGreenLegExists(string id)
        {
            return _context.DoubleGreenLeg.Any(e => e.gid == id);
        }
    }
}
