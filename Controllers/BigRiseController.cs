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
        private readonly ChipController chipCtrl;
        private readonly ConceptController conceptCtrl;

        //public static DateTime now = DateTime.Now;

        public BigRiseController(AppDBContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
            chipCtrl = new ChipController(_context, _config);
            Util._db = context;
            conceptCtrl = new ConceptController(context, config);
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
        public async Task<ActionResult<StockFilter>> GetKDJForDays(int days, DateTime startDate, DateTime endDate, string sort = "筹码")
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
            dt.Columns.Add("放量", Type.GetType("System.Double"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));
            

            var bigRiseList = await _context.BigRise.Where(b => b.alert_date >= startDate.AddDays(-60)
                && b.alert_date.Date < endDate
                //&& b.gid.Equals("sz301297")
                ).OrderByDescending(b => b.alert_date).ToListAsync();
            for (int i = 0; i < bigRiseList.Count; i++)
            {
                Stock s = Stock.GetStock(bigRiseList[i].gid.Trim());
                if (s == null)
                {
                    continue;
                }
                s.RefreshKLine();
                int lastKDGoldIndex = -5;
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
                double minJ = double.MaxValue;
                for (int j = topIndex; j <= endIndex && j < s.klineDay.Length; j++)
                {
                    
                    KLine k = s.klineDay[j];
                    if (k.k < k.d)
                    {
                        kdGold = false;
                        minJ = Math.Min(minJ, k.j);
                    }
                    if (k.k > k.d)
                    {
                        if (!kdGold)
                        {
                            if (minJ <= 0)
                            {
                                buyIndex = j;
                                break;
                            }
                            else
                            {
                                if (k.macd > 0)
                                {
                                    buyIndex = j;
                                    break;
                                }
                            }
                        }
                        lastKDGoldIndex = j;
                        kdGold = true;
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

                double chipValue = 0;

                ActionResult<Chip> chipResult = (await chipCtrl.GetChip(s.gid.Trim(), s.klineDay[buyIndex - 1].settleTime.Date));
                
                if (chipResult.Result.GetType().Name.Trim().Equals("OkObjectResult"))
                {
                    Chip chip = (Chip)((OkObjectResult)chipResult.Result).Value;
                    chipValue = chip.chipDistribute90;
                }
                else
                {
                    if (!s.gid.StartsWith("kc"))
                    {
                        chipResult = (await chipCtrl.GetOne(s.gid.Trim(), s.klineDay[buyIndex - 1].settleTime.Date));
                        if (chipResult.Result.GetType().Name.Trim().Equals("OkObjectResult"))
                        {
                            Chip chip = (Chip)((OkObjectResult)chipResult.Result).Value;
                            chipValue = chip.chipDistribute90;
                        }
                    }
                }
                double buyPrice = s.klineDay[buyIndex].settle;
                DataRow dr = dt.NewRow();
                dr["日期"] = s.klineDay[buyIndex].settleTime.Date;
                dr["代码"] = s.gid;
                dr["名称"] = s.name;
                dr["MACD"] = s.klineDay[buyIndex].macd;
                dr["筹码"] = chipValue;
                dr["买入"] = buyPrice;
                double volumeDiff = (double)(s.klineDay[buyIndex].volume - s.klineDay[buyIndex - 1].volume) / (double)s.klineDay[buyIndex - 1].volume;
                dr["放量"] = volumeDiff;
               
                if (chipValue > 0 && chipValue < 0.15 && Math.Abs(s.klineDay[buyIndex].macd) < 0.5)
                {
                    dr["信号"] = "📈 ";
                }
                else
                {
                    dr["信号"] = "";
                }
                
                if (minJ <= 0)
                {
                    string sig = dr["信号"].ToString().Trim();
                    dr["信号"] = sig + (sig.Trim().Equals("") ? "" : " ") + "🛍";
                }
                
                if (dr["信号"].ToString().IndexOf("🛍") >= 0 && dr["信号"].ToString().IndexOf("📈") >= 0 && volumeDiff > 0)
                {
                    dr["信号"] = "🔥";
                }
                dt.Rows.Add(dr);

            }
            StockFilter sf = StockFilter.GetResult(dt.Select("", "日期 desc, " + sort), days);
            return Ok(sf);
           
        }


        [HttpGet("{days}")]
        public async Task<ActionResult<StockFilter>> GetKDJMACDForHours(int days, DateTime startDate, DateTime endDate, string sort = "筹码")
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
            dt.Columns.Add("放量", Type.GetType("System.Double"));
            dt.Columns.Add("概念", Type.GetType("System.String"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));


            var bigRiseList = await _context.BigRise.Where(b => b.alert_date >= startDate.AddDays(-20)
                && b.alert_date.Date < endDate
                //&& b.gid.Equals("sz000911")
                ).OrderByDescending(b => b.alert_date).ToListAsync();
            for (int i = 0; i < bigRiseList.Count; i++)
            {
                Stock s = Stock.GetStock(bigRiseList[i].gid.Trim());
                if (s == null || s.gid.StartsWith("kc"))
                {
                    continue;
                }

                

                try
                {
                    s.ForceRefreshKLineDay();
                    s.ForceRefreshKLineHour();
                    s.ForceRefreshKLineWeek();
                    Stock.ComputeRSV(s.klineHour);
                    Stock.ComputeKDJ(s.klineHour);
                    Stock.ComputeMACD(s.klineHour);
                    Stock.ComputeRSV(s.klineDay);
                    Stock.ComputeKDJ(s.klineDay);
                    Stock.ComputeMACD(s.klineDay);
                }
                catch
                {

                }
                
                //int lastKDGoldIndex = -5;
                int startIndex = s.GetItemIndex(startDate);
                int endIndex = s.GetItemIndex(endDate);
                if (endIndex == -1)
                {
                    endIndex = s.klineDay.Length - 1;
                }
                int topIndex = s.GetItemIndex(bigRiseList[i].alert_date.Date);
                int buyIndex = -1;

                if (startIndex <= 0 || startIndex >= s.klineDay.Length || topIndex >= endIndex
                    || endIndex < startIndex || endIndex >= s.klineDay.Length)
                {
                    continue;
                }


                int alertIndexHour = Stock.GetItemIndex(s.klineDay[topIndex].settleTime.Date.AddHours(15), s.klineHour);

                if (alertIndexHour <= 0)
                {
                    continue;
                }

                double minJ = double.MaxValue;
                double macd = double.MaxValue;
                double buyPrice = double.MaxValue;
                
                for (int j = alertIndexHour; j < s.klineHour.Length; j++)
                {
                    minJ = Math.Min(s.klineHour[j].j, minJ);
                    if (minJ < 20 && s.klineHour[j - 1].k < s.klineHour[j - 1].d && s.klineHour[j].k > s.klineHour[j].d && s.klineHour[j].d <= 50)
                    {
                        
                        for (int m = j - 2; m >= 1 && m <= j + 2 && m < s.klineHour.Length; m++)
                        {
                            if (s.klineHour[m].macd > -0.1 && s.klineHour[m].macd > s.klineHour[m - 1].macd && s.klineHour[m].macd < 0.1)
                            {
                                
                                buyIndex = s.GetItemIndex(s.klineHour[Math.Max(m, j)].settleTime.Date);
                                //buyPrice = s.klineHour[Math.Max(m, j)].settle;
                                if (buyIndex < 0)
                                {
                                    break;
                                }
                                buyPrice = s.klineDay[buyIndex].settle;
                                macd = s.klineHour[Math.Max(m, j)].macd;
                                break;
                            }
                        }
                       
                    }
                    
                    if (s.klineHour[j - 1].k > s.klineHour[j - 1].d && s.klineHour[j].k < s.klineHour[j].d)
                    {
                        minJ = double.MaxValue;
                    }
                    if (buyIndex >= 0)
                    {
                        break;
                    }
                }

                if (buyIndex <= 0)
                {
                    continue;
                }


                if (s.klineDay[buyIndex].settleTime.Date < startDate.Date || s.klineDay[buyIndex].settleTime.Date > endDate.Date)
                {
                    continue;
                }



                double ma20Current = KLine.GetAverageSettlePrice(s.klineDay, buyIndex, 20, 0);
                double ma20Top = KLine.GetAverageSettlePrice(s.klineDay, topIndex, 20, 0);
                if (ma20Top >= ma20Current || s.klineDay[buyIndex].settle <= ma20Current)
                {
                    continue;
                }

                double chipValue = 0;

                ActionResult<Chip> chipResult = (await chipCtrl.GetChip(s.gid.Trim(), s.klineDay[buyIndex - 1].settleTime.Date));

                if (chipResult.Result.GetType().Name.Trim().Equals("OkObjectResult"))
                {
                    Chip chip = (Chip)((OkObjectResult)chipResult.Result).Value;
                    chipValue = chip.chipDistribute90;
                }
                else
                {
                    if (!s.gid.StartsWith("kc"))
                    {
                        chipResult = (await chipCtrl.GetOne(s.gid.Trim(), s.klineDay[buyIndex - 1].settleTime.Date));
                        if (chipResult.Result.GetType().Name.Trim().Equals("OkObjectResult"))
                        {
                            Chip chip = (Chip)((OkObjectResult)chipResult.Result).Value;
                            chipValue = chip.chipDistribute90;
                        }
                    }
                }


                if (dt.Select(" 代码 = '" + s.gid + "' and 日期 = '" + s.klineDay[buyIndex].settleTime.ToShortDateString() + "' ").Length > 0)
                {
                    continue;
                }

                string conceptStr = "";
                ActionResult<string[]> conceptResult = await conceptCtrl.GetConcept(s.gid);
                if (conceptResult != null && conceptResult.Result.GetType().Name.Trim().Equals("OkObjectResult"))
                {
                    string[] cArr = (string[])((OkObjectResult)conceptResult.Result).Value;
                    for (int j = 0; j < cArr.Length; j++)
                    {
                        conceptStr += (j > 0 ? "," : "") + cArr[j].Trim();
                    }
                }

                //double buyPrice = s.klineDay[buyIndex].settle;
                DataRow dr = dt.NewRow();
                dr["日期"] = s.klineDay[buyIndex].settleTime.Date;
                dr["代码"] = s.gid;
                dr["名称"] = s.name;
                dr["MACD"] = macd;
                dr["筹码"] = chipValue;
                dr["买入"] = buyPrice;
                double volumeDiff = (double)(s.klineDay[buyIndex].volume - s.klineDay[buyIndex - 1].volume) / (double)s.klineDay[buyIndex - 1].volume;
                dr["放量"] = volumeDiff;
                dr["概念"] = conceptStr.Trim();
                int bottomIndex = s.GetItemIndex(bigRiseList[i].start_date.Date);

                if (bottomIndex > 1)
                {
                    for (int j = bottomIndex; j <= topIndex; j++)
                    {
                        if (KLine.IsLimitUp(s.klineDay, j) && KLine.IsLimitUp(s.klineDay, j - 1))
                        {
                            double highPrice = s.klineDay[j].settle;
                            if (j + 2 < s.klineDay.Length && highPrice < Math.Min(s.klineDay[j + 1].open, s.klineDay[j + 1].settle)
                                && highPrice < Math.Min(s.klineDay[j + 2].open, s.klineDay[j + 2].settle) && (j + 1 == topIndex || j + 2 == topIndex))
                            {
                                dr["信号"] = "🛍";
                            }
                            else
                            {
                                dr["信号"] = "📈";
                            }
                            break;
                        }
                    }
                }

                if (s.klineDay[buyIndex].macd > 0 && s.klineDay[buyIndex].k > s.klineDay[buyIndex].j)
                {
                    dr["信号"] = ((!dr["信号"].ToString().Equals(""))? " " : "") + "🔥";
                }

                int kdjHighIndex = buyIndex;
                int priceHighIndex = buyIndex;
                double highPriceShit = s.klineDay[buyIndex].high;
                double highJ = s.klineDay[buyIndex].j;

                for (int m = buyIndex - 1; m >= bottomIndex; m--)
                {
                    if (s.klineDay[m].high >= highPriceShit)
                    {
                        priceHighIndex = m;
                        highPriceShit = s.klineDay[m].high;
                    }
                    if (s.klineDay[m].j >= highJ)
                    {
                        kdjHighIndex = m;
                        highJ = s.klineDay[m].j;
                    }
                }

                if (kdjHighIndex < priceHighIndex)
                {
                    dr["信号"] = dr["信号"].ToString() + ((!dr["信号"].ToString().Equals("")) ? " " : "") + "💩";
                }

                if (dr["信号"].ToString().Trim().Equals(""))
                {
                    dr["信号"] = "👍";
                }

                if (buyIndex + 1 < s.klineDay.Length)
                {
                    if ((s.klineDay[buyIndex + 1].settle - s.klineDay[buyIndex].settle) / s.klineDay[buyIndex].settle < -0.01
                        && ((s.klineDay[buyIndex].settle > KLine.GetAverageSettlePrice(s.klineDay, buyIndex, 10, 0) && s.klineDay[buyIndex + 1].settle < KLine.GetAverageSettlePrice(s.klineDay, buyIndex + 1, 10, 0))
                        || (s.klineDay[buyIndex].settle > KLine.GetAverageSettlePrice(s.klineDay, buyIndex, 20, 0) && s.klineDay[buyIndex + 1].settle < KLine.GetAverageSettlePrice(s.klineDay, buyIndex + 1, 20, 0))
                        || (s.klineDay[buyIndex].settle > KLine.GetAverageSettlePrice(s.klineDay, buyIndex, 60, 0) && s.klineDay[buyIndex + 1].settle < KLine.GetAverageSettlePrice(s.klineDay, buyIndex + 1, 60, 0))))
                    {
                        dr["信号"] = dr["信号"].ToString() + ((!dr["信号"].ToString().Equals("")) ? " " : "") + "🔪";
                    }
                }

                dt.Rows.Add(dr);

            }
            StockFilter sf = StockFilter.GetResult(dt.Select("", "日期 desc, " + sort), days);
            return Ok(sf);

        }
        [HttpGet("{days}")]
        public async Task<ActionResult<StockFilter>> AfterChipsIn(int days, DateTime startDate, DateTime endDate, string sort = "筹码")
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            dt.Columns.Add("筹码", Type.GetType("System.Double"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));

            var limitUpTwiceList = await _context.LimitUpTwice
                .Where(l => (l.alert_date.Date >= startDate.Date && l.alert_date.Date <= endDate.Date)).ToListAsync();
            for (int i = 0; i < limitUpTwiceList.Count; i++)
            {
                string gid = limitUpTwiceList[i].gid.Trim();
                DateTime alertDate = limitUpTwiceList[i].alert_date.Date;
                var bigList = await _context.BigRise
                    .Where(l => (l.gid.Trim().Equals(gid) && l.alert_date < Util.GetLastTransactDate(alertDate, 10, _context).Date))
                    .OrderByDescending(l => l.alert_date).ToListAsync();
                if (bigList.Count <= 0)
                {
                    continue;
                }
                DateTime prevAlertDate = bigList[0].alert_date.Date;
                DateTime prevStartDate = bigList[0].start_date.Date;
                Stock s = Stock.GetStock(gid);
                try
                {
                    s.ForceRefreshKLineDay();
                }
                catch
                {
                    continue;
                }
                
                int alertIndex = s.GetItemIndex(alertDate);
                int prevAlertIndex = s.GetItemIndex(prevAlertDate);
                int prevStartIndex = s.GetItemIndex(prevStartDate);
                if (prevAlertIndex <= 0 || alertIndex <= 0 || prevAlertIndex >= alertIndex || prevStartIndex <= 0)
                {
                    continue;
                }
                if (!KLine.IsLimitUp(s.klineDay, alertIndex) || !KLine.IsLimitUp(s.klineDay, alertIndex - 1))
                {
                    continue;
                }
                double high = s.klineDay[prevAlertIndex].high;
                double low = s.klineDay[prevStartIndex].low;
                double f5 = high - (high - low) * 0.618;
                bool overF5 = false;

                for (int j = prevAlertIndex; j <= alertIndex; j++)
                {
                    if (s.klineDay[j].low <= f5)
                    {
                        overF5 = true;
                        break;
                    }
                }
                if (overF5)
                {
                    continue;
                }

                int buyIndex = alertIndex;
                if (buyIndex < s.klineDay.Length - 1)
                {
                    buyIndex++;
                }
                double chip = 0;
                try
                {
                    ActionResult<double> chipResult = await chipCtrl.GetChipAll(s.gid, s.klineDay[alertIndex - 1].settleTime.Date);
                    if (chipResult != null && chipResult.Result.GetType().Name.Trim().Equals("OkObjectResult"))
                    {
                        chip = (double)((OkObjectResult)chipResult.Result).Value;
                    }
                }
                catch
                {

                }

                DataRow dr = dt.NewRow();
                dr["日期"] = s.klineDay[buyIndex].settleTime.Date;
                dr["代码"] = s.gid.Trim();
                dr["名称"] = s.name.Trim();
                dr["信号"] = "";
                dr["筹码"] = chip;
                
                if (buyIndex > alertIndex
                    && (s.klineDay[buyIndex].open - s.klineDay[alertIndex].settle) / s.klineDay[alertIndex].settle > 0.07)
                {
                    dr["信号"] = dr["信号"].ToString() + (dr["信号"].ToString().Equals("") ? "" : " ") + "🔥";
                }

                dr["买入"] = (buyIndex == alertIndex) ? s.klineDay[alertIndex].settle : s.klineDay[buyIndex].open;
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

        [HttpGet("{days}")]
        public async Task<ActionResult<StockFilter>> AfterChipsInLimitUpOnce(int days, DateTime startDate, DateTime endDate, string sort = "筹码")
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            dt.Columns.Add("筹码", Type.GetType("System.Double"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));

            var limitUpTwiceList = await _context.LimitUp
                .Where(l => (l.alert_date.Date >= startDate.Date && l.alert_date.Date <= endDate.Date)).ToListAsync();
            for (int i = 0; i < limitUpTwiceList.Count; i++)
            {
                string gid = limitUpTwiceList[i].gid.Trim();
                DateTime alertDate = limitUpTwiceList[i].alert_date.Date;
                var bigList = await _context.BigRise
                    .Where(l => (l.gid.Trim().Equals(gid) && l.alert_date < Util.GetLastTransactDate(alertDate, 10, _context).Date))
                    .OrderByDescending(l => l.alert_date).ToListAsync();
                if (bigList.Count <= 0)
                {
                    continue;
                }
                DateTime prevAlertDate = bigList[0].alert_date.Date;
                DateTime prevStartDate = bigList[0].start_date.Date;
                Stock s = Stock.GetStock(gid);
                try
                {
                    s.ForceRefreshKLineDay();
                }
                catch
                {
                    continue;
                }

                int alertIndex = s.GetItemIndex(alertDate);
                int prevAlertIndex = s.GetItemIndex(prevAlertDate);
                int prevStartIndex = s.GetItemIndex(prevStartDate);
                if (prevAlertIndex <= 0 || alertIndex <= 0 || prevAlertIndex >= alertIndex || prevStartIndex <= 0)
                {
                    continue;
                }
                if (!KLine.IsLimitUp(s.klineDay, alertIndex))
                {
                    continue;
                }
                double high = s.klineDay[prevAlertIndex].high;
                double low = s.klineDay[prevStartIndex].low;

                double f5 = high - (high - low) * 0.618;
                bool overF5 = false;

                for (int j = prevAlertIndex; j <= alertIndex; j++)
                {
                    if (s.klineDay[j].low <= f5)
                    {
                        overF5 = true;
                        break;
                    }
                }
                if (overF5)
                {
                    continue;
                }

                int buyIndex = alertIndex;
                if (buyIndex < s.klineDay.Length - 1)
                {
                    buyIndex++;
                }
                double chip = 0;
                try
                {
                    ActionResult<double> chipResult = await chipCtrl.GetChipAll(s.gid, s.klineDay[alertIndex - 1].settleTime.Date);
                    if (chipResult != null && chipResult.Result.GetType().Name.Trim().Equals("OkObjectResult"))
                    {
                        chip = (double)((OkObjectResult)chipResult.Result).Value;
                    }
                }
                catch
                {

                }

                DataRow dr = dt.NewRow();
                dr["日期"] = s.klineDay[buyIndex].settleTime.Date;
                dr["代码"] = s.gid.Trim();
                dr["名称"] = s.name.Trim();
                dr["信号"] = "";
                dr["筹码"] = chip;

                if (buyIndex > alertIndex
                    && (s.klineDay[buyIndex].open - s.klineDay[alertIndex].settle) / s.klineDay[alertIndex].settle > 0.07)
                {
                    dr["信号"] = dr["信号"].ToString() + (dr["信号"].ToString().Equals("") ? "" : " ") + "🔥";
                }

                dr["买入"] = (buyIndex == alertIndex) ? s.klineDay[alertIndex].settle : s.klineDay[buyIndex].open;
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

        [HttpGet("{days}")]
        public async Task<ActionResult<StockFilter>> LowRiseRateWithDoubleVolume(int days, DateTime startDate, DateTime endDate, string sort = "放量")
        {
            string sql = " select * from dbo.func_get_buying_days('"
                + Util.GetLastTransactDate(startDate, 15, _context).ToShortDateString() + "' ,"
                + " '" + endDate.ToShortDateString()
                + "' )  order by alert_date desc";
            var buyingList = await _context.buyingAlert.FromSqlRaw(sql)
                .AsNoTracking().ToListAsync();

            DataTable dt = new DataTable();
            dt.Columns.Add("日期", Type.GetType("System.DateTime"));
            dt.Columns.Add("代码", Type.GetType("System.String"));
            dt.Columns.Add("名称", Type.GetType("System.String"));
            dt.Columns.Add("信号", Type.GetType("System.String"));
            dt.Columns.Add("放量", Type.GetType("System.Double"));
            dt.Columns.Add("买入", Type.GetType("System.Double"));
            dt.Columns.Add("筹码", Type.GetType("System.Double"));
            dt.Columns.Add("流入日期", Type.GetType("System.String"));
            dt.Columns.Add("流入天数", Type.GetType("System.String"));


            var bigList = await _context.BigRise
                .FromSqlRaw(" select * from big_rise a where not exists ( "
                + " select 'a' from big_rise b where a.gid = b.gid and b.alert_date >= dbo.func_GetLastTransactDate(a.alert_date, 60) and b.alert_date < a.alert_date ) "
                + "  and exists(select 'a' from double_volume d where d.gid = a.gid and d.alert_date > a.alert_date and d.alert_date >= '"
                + startDate.Date.ToShortDateString() + "' and d.alert_date <= '" + endDate.Date.ToShortDateString() + "' ) "
                + "  and limit_up_num <= 1 and alert_date >= '" + Util.GetLastTransactDate(startDate, 60, _context).Date.ToShortDateString() + "'  "
                + " and alert_date <= '" + endDate.Date.AddDays(-1).ToShortDateString() + "' " ).ToListAsync();
            if (bigList == null)
            {
                return BadRequest();
            }
            for (int i = 0; i < bigList.Count; i++)
            {
                Stock s = Stock.GetStock(bigList[i].gid);
                s.ForceRefreshKLineDay();
                int bigRiseEndIndex = s.GetItemIndex(bigList[i].alert_date.Date);
                int bigRiseStartIndex = s.GetItemIndex(bigList[i].start_date.Date);
                int endIndex = s.GetItemIndex(endDate.Date);
                if (bigRiseEndIndex < 0 || bigRiseEndIndex > s.klineDay.Length
                    || bigRiseStartIndex < 0 || bigRiseStartIndex > s.klineDay.Length
                    || bigRiseStartIndex >= bigRiseEndIndex   )
                {
                    continue;
                }
                double low = double.MaxValue;
                double high = double.MinValue;
                for (int j = bigRiseStartIndex; j <= bigRiseEndIndex; j++)
                {
                    low = Math.Min(low, s.klineDay[j].low);
                    high = Math.Max(high, s.klineDay[j].high);
                }
                int buyIndex = -1;
                for (int j = bigRiseEndIndex + 1; j < s.klineDay.Length && j <= endIndex; j++)
                {
                    if (s.klineDay[j].low <= low || s.klineDay[j].high >= high)
                    {
                        break;
                    }
                    
                   
                    if (s.klineDay[j].volume > s.klineDay[j - 1].volume * 2
                        && s.klineDay[j].settle > s.klineDay[j - 1].settle
                        && s.klineDay[j].settleTime.Date >= startDate.Date
                        && s.klineDay[j].settleTime.Date <= endDate.Date)
                    {
                        buyIndex = j;
                        break;
                    }
                    

                }
                if (buyIndex == -1)
                {
                    continue;
                }


                double chip = 0;
                try
                {
                    ActionResult<double> chipResult = await chipCtrl.GetChipAll(s.gid, s.klineDay[buyIndex - 1].settleTime.Date);
                    if (chipResult != null && chipResult.Result.GetType().Name.Trim().Equals("OkObjectResult"))
                    {
                        chip = (double)((OkObjectResult)chipResult.Result).Value;
                    }
                }
                catch
                {

                }

                DataRow dr = dt.NewRow();
                dr["日期"] = s.klineDay[buyIndex].settleTime.Date;
                dr["代码"] = s.gid.Trim();
                dr["名称"] = s.name.Trim();
                dr["信号"] = "";
                dr["放量"] = s.klineDay[buyIndex].volume / s.klineDay[buyIndex - 1].volume;
                dr["买入"] = s.klineDay[buyIndex].settle;
                dr["筹码"] = chip;
                dr["流入日期"] = "--";
                dr["流入天数"] = "--";
                for (int j = 0; j < buyingList.Count; j++)
                {
                    BuyingAlert ba = buyingList[j];

                    if (ba.gid.Trim().Equals(s.gid.Trim())
                        && ba.alert_date.Date <= s.klineDay[buyIndex].settleTime.Date)
                    {
                        dr["流入日期"] = "`" + ba.alert_date.ToString("yyyy-MM-dd");
                        dr["流入天数"] = "`" + ba.in_days.ToString() + "天";
                        if (s.klineDay[buyIndex].settleTime.Date - ba.alert_date.Date <= new TimeSpan(3, 0, 0, 0))
                        {
                            dr["信号"] = dr["信号"].ToString() + "🔥";
                        }
                        break;
                    }
                }
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

        

        private bool BigRiseExists(int id)
        {
            return _context.BigRise.Any(e => e.id == id);
        }
    }
}
