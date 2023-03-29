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
//using Microsoft.AspNetCore.Http.HttpResults;

namespace LuqinOfficialAccount.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class BigRiseController : ControllerBase
    {
        private readonly AppDBContext _context;
        private readonly IConfiguration _config;
        private readonly Settings _settings;

        //public static DateTime now = DateTime.Now;

        public BigRiseController(AppDBContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
        }

        

        [HttpGet]
        public  ActionResult<int> SearchDays(DateTime start, DateTime end)
        {
            for (DateTime i = start; i.Date <= end.Date; i = i.AddDays(1))
            {
                if (Util.IsTransacDay(i, _context))
                {
                    Search(i);
                }
            }
            return Ok(0);
        }

        [HttpGet]
        public ActionResult<int> SearchTodayBigRise()
        {
            DateTime now = DateTime.Now.Date;
            if (Util.IsTransacDay(now, _context))
            {
                Search(now);
            }
            return Ok(0);
        }

        [HttpGet]
        public ActionResult<KLine> SearchLowKLine()
        {
            Stock s = Stock.GetStock("sz002803");
            s.RefreshKLine();
            int i = KLine.GetBackwardBottomKLineItem(s.klineDay, s.klineDay.Length - 1);
            return Ok(s.klineDay[i]);

        }

        [HttpGet]
        public ActionResult<KLine> SearchHighKLine()
        {
            Stock s = Stock.GetStock("sz001339");
            s.RefreshKLine();
            int itemIndex = s.GetItemIndex(DateTime.Parse("2023-2-15"));
            int i = KLine.GetForwardTopKLineItem(s.klineDay, itemIndex);
            return Ok(s.klineDay[i]);

        }

        [NonAction]
        public  void Search(DateTime date)
        {
            Stock[] stockArr = Util.stockList;

            for (int i = 0; i < stockArr.Length; i++)
            {
                Stock s = stockArr[i];
                s.RefreshKLineDay();
                int index = s.GetItemIndex(date);
                if (index < 0 || index >= s.klineDay.Length)
                {
                    continue;
                }
                int topIndex = KLine.GetForwardTopKLineItem(s.klineDay, index);
                if (topIndex <= 0 || topIndex >= s.klineDay.Length)
                {
                    continue;
                }
                int bottomIndex = KLine.GetBackwardBottomKLineItem(s.klineDay, topIndex);
                if (bottomIndex < 0 || bottomIndex >= s.klineDay.Length)
                {
                    continue;
                }
                if (topIndex <= bottomIndex)
                {
                    continue;
                }

                double high = s.klineDay[topIndex].high;
                double low = s.klineDay[bottomIndex].low;
                if ((high - low) / low < 0.3)
                {
                    continue;
                }

                int limitNum = 0;
                int limitTNum = 0;
                for (int j = bottomIndex + 1; j <= topIndex && j < s.klineDay.Length; j++)
                {
                    if (KLine.IsLimitUp(s.klineDay, j))
                    {
                        limitNum++;
                    }
                    if (j > 1 && KLine.IsLimitUp(s.klineDay, j) && KLine.IsLimitUp(s.klineDay, j - 1))
                    {
                        limitTNum++;
                    }
                }


                DateTime bottomDate = s.klineDay[bottomIndex].settleTime.Date;
                DateTime topDate = s.klineDay[topIndex].settleTime.Date;
                var bigRisetList = _context.BigRise.Where(b => (b.gid.Trim().Equals(s.gid.Trim())
                    && b.start_date.Date == bottomDate)).ToList();
                if (bigRisetList.Count == 0)
                {
                    var chipList = _context.Chip.Where(c => (c.gid.Trim().Equals(s.gid.Trim())
                        && c.alert_date.Date == s.klineDay[topIndex].settleTime.Date)).ToList();
                    double chipTop = 0;
                    if (chipList.Count > 0)
                    {
                        Chip chip = chipList[0];
                        chipTop = (chip.cost_95pct - chip.cost_5pct) / (chip.cost_95pct + chip.cost_5pct);
                    }

                    double chipBottom = 0;
                    chipList = _context.Chip.Where(c => (c.gid.Trim().Equals(s.gid.Trim())
                        && c.alert_date.Date == s.klineDay[bottomIndex].settleTime.Date)).ToList();
                    if (chipList.Count > 0)
                    {
                        Chip chip = chipList[0];
                        chipBottom = (chip.cost_95pct - chip.cost_5pct) / (chip.cost_95pct + chip.cost_5pct);
                    }

                    


                    BigRise bigRise = new BigRise()
                    {
                        id = 0,
                        alert_date = s.klineDay[topIndex].settleTime.Date,
                        gid = s.gid.Trim(),
                        alert_chip = chipTop,
                        alert_high = s.klineDay[topIndex].high,
                        start_date = s.klineDay[bottomIndex].settleTime.Date,
                        start_chip = chipBottom,
                        start_low = s.klineDay[bottomIndex].low,
                        limit_up_num = limitNum,
                        limit_up_twice_num = limitTNum,
                        update_date = DateTime.Now

                    };
                    try
                    {
                        _context.BigRise.Add(bigRise);
                        _context.SaveChanges();
                    }
                    catch
                    {

                    }
                    
                }
                else
                {
                    BigRise bigRise = bigRisetList[0];
                    if (bigRise.alert_date.Date < s.klineDay[topIndex].settleTime.Date)
                    {
                        var chipList = _context.Chip.Where(c => (c.gid.Trim().Equals(s.gid.Trim())
                        && c.alert_date.Date == s.klineDay[topIndex].settleTime.Date)).ToList();
                        double chipTop = 0;
                        if (chipList.Count > 0)
                        {
                            Chip chip = chipList[0];
                            chipTop = (chip.cost_95pct - chip.cost_5pct) / (chip.cost_95pct + chip.cost_5pct);
                        }


                        bigRise.alert_date = s.klineDay[topIndex].settleTime.Date;
                        bigRise.alert_chip = chipTop;
                        bigRise.alert_high = s.klineDay[topIndex].high;
                        bigRise.update_date = DateTime.Now;

                        _context.Entry(bigRise).State = EntityState.Modified;
                        try
                        {
                            _context.SaveChanges();
                        }
                        catch
                        {

                        }
                        
                    }
                }
            }


            
        }

        [HttpGet("{days}")]
        public async Task<ActionResult<CountResult>> CountKDJ(int days, DateTime startDate)
        {
            
            var bigRiseList = _context.BigRise.Where(b => (b.limit_up_twice_num > 0
            && b.alert_date <= DateTime.Now.AddDays(-15)
            && b.alert_date >= startDate.Date))
                .OrderByDescending(b => b.alert_date).ToList();
            List<CountItem> list = new List<CountItem>();
            for (int i = 0; i < bigRiseList.Count; i++)
            {
                DateTime alertDate = bigRiseList[i].alert_date.Date;
                Stock s = Stock.GetStock(bigRiseList[i].gid.Trim());
                s.RefreshKLine();
                int alertIndex = s.GetItemIndex(alertDate);
                if (alertIndex < 0)
                {
                    continue;
                }
                bool kdGold = true;
                int buyIndex = -1;

                if (s.klineDay[alertIndex].turnOver >= 22)
                {
                    continue;
                }
                

                for (int j = alertIndex; j < s.klineDay.Length && buyIndex == -1; j++)
                {
                    if (kdGold && s.klineDay[j].k < s.klineDay[j].d)
                    {
                        kdGold = false;
                    }
                    if (!kdGold && s.klineDay[j].k > s.klineDay[j].d)
                    {
                        buyIndex = j;
                        break;
                    }
                }

                if (buyIndex >= 0 && buyIndex + days < s.klineDay.Length)
                {
                    double ma20Top = KLine.GetAverageSettlePrice(s.klineDay, alertIndex, 20, 0);
                    double ma20Buy = KLine.GetAverageSettlePrice(s.klineDay, buyIndex, 20, 0);

                    if (s.klineDay[buyIndex].settle <= ma20Buy || ma20Top >= ma20Buy)
                    {
                        continue;
                    }


                    double chipTop = 0;
                    double chipBuy = 0;

                    var chipList = _context.Chip.Where(c => (c.gid.Trim().Equals(s.gid) && c.alert_date.Date == s.klineDay[alertIndex].settleTime.Date)).ToList();
                    if (chipList.Count > 0)
                    {
                        var chip = chipList[0];
                        chipTop = (chip.cost_95pct - chip.cost_5pct) / (chip.cost_95pct + chip.cost_5pct);
                    }

                    chipList = _context.Chip.Where(c => (c.gid.Trim().Equals(s.gid) && c.alert_date.Date == s.klineDay[buyIndex].settleTime.Date)).ToList();
                    if (chipList.Count > 0)
                    {
                        var chip = chipList[0];
                        chipBuy = (chip.cost_95pct - chip.cost_5pct) / (chip.cost_95pct + chip.cost_5pct);
                    }

                    
                    /*
                    DateTime startDateChip = bigRiseList[i].start_date;
                    int startIndex = s.GetItemIndex(startDateChip);
                    double chipStart = 0;
                    if (startIndex >= 0)
                    {
                        chipList = _context.Chip.Where(c => (c.gid.Trim().Equals(s.gid) && c.alert_date.Date == s.klineDay[startIndex].settleTime.Date)).ToList();
                        if (chipList.Count > 0)
                        {
                            var chip = chipList[0];
                            chipStart = (chip.cost_95pct - chip.cost_5pct) / (chip.cost_95pct + chip.cost_5pct);
                        }
                    }
                    */
                    if (chipTop <=  chipBuy || chipBuy >= 0.15)
                    {
                        continue;
                    }

                    CountItem item = new CountItem()
                    {
                        days = days,
                        gid = s.gid.Trim(),
                        alert_date = s.klineDay[buyIndex].settleTime.Date,
                        name = s.name
                    };
                    item = CountItem.Count(item, "");
                    list.Add(item);
                }

            }

            return Ok(CountResult.GetResult(list));
        }

        [HttpGet("{days}")]
        public async Task<ActionResult<DataTable>> GetKDJ(int days, DateTime currentDate, string sort = "筹码")
        {
            ChipController chipCtrl = new ChipController(_context, _config);
            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            dt.Columns.Add("MACD", Type.GetType("System.Double"));
            dt.Columns.Add("筹码", Type.GetType("System.Double"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));
            
            var bigRiseList = await _context.BigRise.Where(b => b.alert_date >= currentDate.AddDays(-60))
                .OrderByDescending(b => b.alert_date).ToListAsync();

            for (int i = 0; i < bigRiseList.Count; i++)
            {
                Stock s = Stock.GetStock(bigRiseList[i].gid.Trim());
                if (s == null)
                {
                    continue;
                }
                s.RefreshKLine();
                int currentIndex = s.GetItemIndex(currentDate.Date);
                
                if (currentIndex <= 0 || currentIndex >= s.klineDay.Length)
                {
                    continue;
                }
                if (s.klineDay[currentIndex - 1].k >= s.klineDay[currentIndex - 1].d
                    || s.klineDay[currentIndex].k <= s.klineDay[currentIndex].d)
                {
                    continue;
                }

                double ma20Current = KLine.GetAverageSettlePrice(s.klineDay, currentIndex, 20, 0);
                if (s.klineDay[currentIndex].settle <= ma20Current)
                {
                    continue;
                }
                int topIndex = s.GetItemIndex(bigRiseList[i].alert_date.Date);
                if (topIndex <= 0 || topIndex >= s.klineDay.Length)
                {
                    continue;
                }

                double ma20Top = KLine.GetAverageSettlePrice(s.klineDay, topIndex, 20, 0);
                if (ma20Top >= ma20Current)
                {
                    continue;
                }

                bool kdGold = true;
                int buyIndex = 0;
                for (int j = topIndex;  j < currentIndex; j++)
                {
                    if (kdGold && s.klineDay[j].k < s.klineDay[j].d)
                    {
                        kdGold = false;
                    }
                    if (!kdGold && s.klineDay[j].k > s.klineDay[j].d)
                    {
                        buyIndex = j;
                        break;
                    }
                }
                if (buyIndex != currentIndex && buyIndex != 0)
                {
                    continue;
                }

                
                ActionResult<Chip> chipResult = (await chipCtrl.GetChip(s.gid.Trim(), currentDate));
                double chipValue = 0;
                if (chipResult.Result.GetType().Name.Trim().Equals("OkObjectResult"))
                {
                    Chip chip = (Chip)((OkObjectResult)chipResult.Result).Value;
                    chipValue = chip.chipDistribute90;
                }
                
                
                //Chip chip = (Chip)((OkObjectResult)chipResult.Result).Value;
                double buyPrice = s.klineDay[currentIndex].settle;
                DataRow dr = dt.NewRow();
                dr["日期"] = currentDate;
                dr["代码"] = s.gid;
                dr["名称"] = s.name;
                dr["MACD"] = s.klineDay[currentIndex].macd;
                dr["筹码"] = chipValue;
                dr["买入"] = buyPrice;
                dr["信号"] = "";


                dt.Rows.Add(dr);
            }
            StockFilter sf = StockFilter.GetResult(dt.Select("", sort ), 15);
            return Ok(sf);
        }

        [HttpGet("{days}")]
        public async Task<ActionResult<DataTable>> GetKDJForDays(int days, DateTime startDate, DateTime endDate, string sort = "筹码")
        {
            startDate = startDate.Date;
            endDate = endDate.Date;
            if (endDate < startDate)
            {
                return BadRequest();
            }
            ChipController chipCtrl = new ChipController(_context, _config);
            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            dt.Columns.Add("MACD", Type.GetType("System.Double"));
            dt.Columns.Add("筹码", Type.GetType("System.Double"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));

            var bigRiseList = await _context.BigRise.Where(b => b.alert_date >= startDate.AddDays(-60)
                && b.alert_date.Date < endDate
                //&& b.gid.Equals("sz300785")
                ).OrderByDescending(b => b.alert_date).ToListAsync();
            for (int i = 0; i < bigRiseList.Count; i++)
            {
                Stock s = Stock.GetStock(bigRiseList[i].gid.Trim());
                if (s == null)
                {
                    continue;
                }
                s.RefreshKLine();

                int startIndex = s.GetItemIndex(startDate);
                int endIndex = s.GetItemIndex(endDate);
                if (endIndex == -1)
                {
                    endIndex = s.klineDay.Length - 1;
                }
                int topIndex = s.GetItemIndex(bigRiseList[i].alert_date.Date);
                int buyIndex = -1;

                if (startIndex <= 0 || startIndex >= s.klineDay.Length || topIndex >= endIndex
                    || endIndex < startIndex  || endIndex >= s.klineDay.Length )
                {
                    continue;
                }
                bool kdGold = true;
                for (int j = topIndex; j <= endIndex && j < s.klineDay.Length; j++)
                {
                    KLine k = s.klineDay[j];
                    if (k.k < k.d)
                    {
                        kdGold = false;
                    }
                    if (!kdGold && k.k > k.d)
                    {
                        buyIndex = j;
                        break;
                    }
                }
                if (buyIndex == -1 || buyIndex < startIndex || buyIndex > endIndex)
                {
                    continue;
                }
                double ma20Current = KLine.GetAverageSettlePrice(s.klineDay, buyIndex, 20, 0);
                double ma20Top = KLine.GetAverageSettlePrice(s.klineDay, topIndex, 20, 0);
                if (ma20Top >= ma20Current || s.klineDay[buyIndex].settle <= ma20Current)
                {
                    continue;
                }
                ActionResult<Chip> chipResult = (await chipCtrl.GetChip(s.gid.Trim(), s.klineDay[buyIndex].settleTime.Date));
                double chipValue = 0;
                if (chipResult.Result.GetType().Name.Trim().Equals("OkObjectResult"))
                {
                    Chip chip = (Chip)((OkObjectResult)chipResult.Result).Value;
                    chipValue = chip.chipDistribute90;
                }
                double buyPrice = s.klineDay[buyIndex].settle;
                DataRow dr = dt.NewRow();
                dr["日期"] = s.klineDay[buyIndex].settleTime.Date;
                dr["代码"] = s.gid;
                dr["名称"] = s.name;
                dr["MACD"] = s.klineDay[buyIndex].macd;
                dr["筹码"] = chipValue;
                dr["买入"] = buyPrice;
                dr["信号"] = "";


                dt.Rows.Add(dr);
            }
            StockFilter sf = StockFilter.GetResult(dt.Select("", "日期 desc, " + sort), 15);
            return Ok(sf);
           
        }



        /*
        // GET: api/BigRise
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BigRise>>> GetBigRise()
        {
            return await _context.BigRise.ToListAsync();
        }

        // GET: api/BigRise/5
        [HttpGet("{id}")]
        public async Task<ActionResult<BigRise>> GetBigRise(int id)
        {
            var bigRise = await _context.BigRise.FindAsync(id);

            if (bigRise == null)
            {
                return NotFound();
            }

            return bigRise;
        }

        // PUT: api/BigRise/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutBigRise(int id, BigRise bigRise)
        {
            if (id != bigRise.id)
            {
                return BadRequest();
            }

            _context.Entry(bigRise).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BigRiseExists(id))
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

        // POST: api/BigRise
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<BigRise>> PostBigRise(BigRise bigRise)
        {
            _context.BigRise.Add(bigRise);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetBigRise", new { id = bigRise.id }, bigRise);
        }

        // DELETE: api/BigRise/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBigRise(int id)
        {
            var bigRise = await _context.BigRise.FindAsync(id);
            if (bigRise == null)
            {
                return NotFound();
            }

            _context.BigRise.Remove(bigRise);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        */
        private bool BigRiseExists(int id)
        {
            return _context.BigRise.Any(e => e.id == id);
        }
    }
}
