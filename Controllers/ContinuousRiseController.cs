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
using System.Collections;

namespace LuqinOfficialAccount.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ContinuousRiseController : ControllerBase
    {
        private readonly AppDBContext _db;

        private readonly IConfiguration _config;

        private readonly Settings _settings;

        //private System.Collections.Queue stockQueue;// = new System.Collections.Queue();

        public ContinuousRiseController(AppDBContext context, IConfiguration config)
        {
            _db = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
            Util._db = context;
        }
        /*
        [HttpGet]
        public  ActionResult<int> TestMultiThread()
        {
            Stock[] sList = Util.stockList;
            System.Collections.Queue q = new System.Collections.Queue();
            System.Collections.ArrayList arr = new System.Collections.ArrayList();
            for (int i = 0; i < sList.Length; i++)
            {
                q.Enqueue(sList[i]);
            }

            while (q.Count > 0)
            {
                Task.Run(() => {
                    if (q.Count > 0)
                    {
                        Stock s = (Stock)q.Dequeue();
                        if (s != null)
                        {
                            Console.WriteLine("Thread start for " + s.gid);
                            try
                            {
                                s.ForceRefreshKLineDay();
                                arr.Add(s);
                            }
                            catch
                            {
                                q.Enqueue(s);
                            }
                        }
                    }

                    //Console.WriteLine("Thread end");
                });
                System.Threading.Thread.Sleep(10);
            }



           


           
            bool finish = false;
            while (!finish)
            {
                finish = true;
                int count = 0;
                for (int i = 0; i < arr.Count; i++)
                {
                    Stock s = (Stock)arr[i];
                    if (s.klineDay != null && s.klineDay.Length >= 0)
                    {
                        count++;
                    }
                }
                Console.WriteLine(count.ToString() + " finished.");
                if (count < sList.Length - 1)
                {
                    finish = false;
                }
                System.Threading.Thread.Sleep(1000);
            }
            return 0;
        }
        */
        [HttpGet]
        public async Task<ActionResult<int>> Search(DateTime start, DateTime end)
        {
            int ret = 0;
            Stock[] sArr = Util.stockList;
            ArrayList list = new ArrayList();
            bool done = false;
            Queue q = new Queue();
            for (int i = 0; i < sArr.Length; i++)
            {
                
                Stock s = sArr[i];
                s.ForceRefreshKLineDay();
                
                await Task.Run(() =>
                {

                    done = false;



                    int startIndex = s.GetItemIndex(start);
                    int endIndex = s.GetItemIndex(end);
                    if (startIndex >= 0 && endIndex >= 0)
                    {
                        for (int j = startIndex; j <= endIndex && j < s.klineDay.Length; j++)
                        {
                            int riseDays = 0;
                            for (int k = 0; j - k - 1 >= 0; k++)
                            {
                                if (s.klineDay[j - k].high > s.klineDay[j - k - 1].high
                                    && s.klineDay[j - k].settle > s.klineDay[j - k - 1].settle)
                                {
                                    riseDays++;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            if (riseDays >= 3 && j - riseDays >= 0)
                            {
                                ContinuousRise cr = new ContinuousRise()
                                {
                                    alert_date = s.klineDay[j].settleTime,
                                    gid = s.gid,
                                    rise_days = riseDays,
                                    rise_rate = (s.klineDay[j].settle - s.klineDay[j - riseDays].settle) / s.klineDay[j - riseDays].settle
                                };
                                q.Enqueue(cr);
                            
                            }
                        }
                    }
                    done = true;
                });


            }
            
            System.Threading.Thread.Sleep(5000);
            while (!done || q.Count > 0)
            {
                if (q.Count > 0)
                {
                    ContinuousRise cr = (ContinuousRise)q.Dequeue();
                    if (cr != null)
                    {
                        ContinuousRise crExists = await _db.ContinuousRise.FindAsync(cr.alert_date, cr.gid);
                        Console.WriteLine(cr.gid + " " + cr.alert_date.ToShortDateString());
                        if (crExists == null)
                        {
                            await _db.AddAsync(cr);

                        }
                        else
                        {
                            crExists.rise_days = cr.rise_days;
                            crExists.rise_rate = cr.rise_rate;
                            _db.Entry(crExists).State = EntityState.Modified;
                        }
                        await _db.SaveChangesAsync();
                        ret++;
                    }
                }
                else
                {
                    done = true;
                }
            }

            return ret;
        }

        /*

        // GET: api/ContinuousRise
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ContinuousRise>>> GetContinuousRise()
        {
            return await _context.ContinuousRise.ToListAsync();
        }

        // GET: api/ContinuousRise/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ContinuousRise>> GetContinuousRise(DateTime id)
        {
            var continuousRise = await _context.ContinuousRise.FindAsync(id);

            if (continuousRise == null)
            {
                return NotFound();
            }

            return continuousRise;
        }

        // PUT: api/ContinuousRise/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutContinuousRise(DateTime id, ContinuousRise continuousRise)
        {
            if (id != continuousRise.alert_date)
            {
                return BadRequest();
            }

            _context.Entry(continuousRise).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ContinuousRiseExists(id))
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

        // POST: api/ContinuousRise
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<ContinuousRise>> PostContinuousRise(ContinuousRise continuousRise)
        {
            _context.ContinuousRise.Add(continuousRise);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (ContinuousRiseExists(continuousRise.alert_date))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetContinuousRise", new { id = continuousRise.alert_date }, continuousRise);
        }

        // DELETE: api/ContinuousRise/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContinuousRise(DateTime id)
        {
            var continuousRise = await _context.ContinuousRise.FindAsync(id);
            if (continuousRise == null)
            {
                return NotFound();
            }

            _context.ContinuousRise.Remove(continuousRise);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        

        private bool ContinuousRiseExists(DateTime id)
        {
            return _context.ContinuousRise.Any(e => e.alert_date == id);
        }
        */
    }
}
