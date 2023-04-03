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
    public class MACDController : ControllerBase
    {
        private readonly AppDBContext _db;

        private readonly IConfiguration _config;

        private readonly Settings _settings;

        private readonly ChipController chipCtrl;


        public MACDController(AppDBContext context, IConfiguration config)
        {
            _db = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
            chipCtrl = new ChipController(_db, _config);
        }
        [HttpGet("{days}")]
        public async Task<ActionResult<StockFilter>> MACDGoldForkLow(int days, DateTime startDate, DateTime endDate, string sort = "筹码")
        {
            
            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            dt.Columns.Add("筹码", Type.GetType("System.Double"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));
            dt.Columns.Add("放量", Type.GetType("System.Double"));
            var macdList = await _db.MACD.Where(m => (m.alert_type.Trim().Equals("day")
                //&& m.gid.Trim().Equals("sh600833")
                && m.alert_time.Date >= startDate.Date && m.alert_time.Date <= endDate.Date
                && m.dea <= 0 && m.dif <= 0)).ToListAsync();
            if (macdList == null)
            {
                return BadRequest();
            }
            for (int i = 0; i < macdList.Count; i++)
            {
                DateTime alertDate = macdList[i].alert_time.Date;
                Stock s = Stock.GetStock(macdList[i].gid.Trim());
                try
                {
                    s.RefreshKLine();
                }
                catch
                {
                    continue;
                }
                int alertIndex = s.GetItemIndex(alertDate);
                if (alertIndex < 1 || alertIndex > s.klineDay.Length)
                {
                    continue;
                }
                if (s.klineDay[alertIndex].macd < 0 || s.klineDay[alertIndex].dif > 0 || s.klineDay[alertIndex].dea > 0)
                {
                    continue;
                }
                double ma5 = KLine.GetAverageSettlePrice(s.klineDay, alertIndex, 5, 0);
                double ma10 = KLine.GetAverageSettlePrice(s.klineDay, alertIndex, 10, 0);
                double ma20 = KLine.GetAverageSettlePrice(s.klineDay, alertIndex, 20, 0);
                double buyPrice = s.klineDay[alertIndex].settle;


                if (buyPrice <= ma5 || buyPrice <= ma10 || buyPrice <= ma20)
                {
                    continue;
                }

                
                
                //double buyPrice = s.klineDay[alertIndex].settle;
                DataRow dr = dt.NewRow();
                dr["日期"] = alertDate.Date;
                dr["代码"] = s.gid;
                dr["名称"] = s.name;
                dr["买入"] = buyPrice;
                dr["信号"] = "";
                double volDiff = ((double)s.klineDay[alertIndex].volume - s.klineDay[alertIndex - 1].volume) / (double)s.klineDay[alertIndex - 1].volume;
                dr["放量"] = volDiff;

                if (volDiff <= 0)
                {
                    continue;
                }

                double chipValue = 0;

                ActionResult<Chip> chipResult = (await chipCtrl.GetChip(s.gid.Trim(), s.klineDay[alertIndex - 1].settleTime.Date));

                if (chipResult.Result.GetType().Name.Trim().Equals("OkObjectResult"))
                {
                    Chip chip = (Chip)((OkObjectResult)chipResult.Result).Value;
                    chipValue = chip.chipDistribute90;
                }
                else
                {
                    if (!s.gid.StartsWith("kc"))
                    {
                        chipResult = (await chipCtrl.GetOne(s.gid.Trim(), s.klineDay[alertIndex - 1].settleTime.Date));
                        if (chipResult.Result.GetType().Name.Trim().Equals("OkObjectResult"))
                        {
                            Chip chip = (Chip)((OkObjectResult)chipResult.Result).Value;
                            chipValue = chip.chipDistribute90;
                        }
                    }
                }
                if (chipValue == 0 || chipValue > 0.15)
                {
                    continue;
                }

                dr["筹码"] = chipValue;

                if (ma5 > ma10)
                {
                    dr["信号"] = "📈";
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



        /*
        // GET: api/MACD
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MACD>>> GetMACD()
        {
            return await _context.MACD.ToListAsync();
        }

        // GET: api/MACD/5
        [HttpGet("{id}")]
        public async Task<ActionResult<MACD>> GetMACD(string id)
        {
            var mACD = await _context.MACD.FindAsync(id);

            if (mACD == null)
            {
                return NotFound();
            }

            return mACD;
        }

        // PUT: api/MACD/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMACD(string id, MACD mACD)
        {
            if (id != mACD.gid)
            {
                return BadRequest();
            }

            _context.Entry(mACD).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MACDExists(id))
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

        // POST: api/MACD
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<MACD>> PostMACD(MACD mACD)
        {
            _context.MACD.Add(mACD);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (MACDExists(mACD.gid))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetMACD", new { id = mACD.gid }, mACD);
        }

        // DELETE: api/MACD/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMACD(string id)
        {
            var mACD = await _context.MACD.FindAsync(id);
            if (mACD == null)
            {
                return NotFound();
            }

            _context.MACD.Remove(mACD);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool MACDExists(string id)
        {
            return _context.MACD.Any(e => e.gid == id);
        }
        */
    }
}
