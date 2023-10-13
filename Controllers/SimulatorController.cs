using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using LuqinOfficialAccount.Models;
using LuqinOfficialAccount.Models.Simulator;
using System.Threading.Tasks;
using System.Collections;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace LuqinOfficialAccount.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]

    public class SimulatorController : ControllerBase
	{
        private readonly AppDBContext _db;

        private readonly IConfiguration _config;

        private readonly Settings _settings;

        private readonly LimitUpController _limitupHelper;

        public SimulatorController(AppDBContext context, IConfiguration config)
		{
            _db = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
            _limitupHelper = new LimitUpController(context, config);
        }

        [HttpGet]
        public async Task Simulate(int simulatorId)
        {
            Simulator simulator = await _db.simulator.FindAsync(simulatorId);
            for (DateTime i = simulator.from_date; i <= simulator.to_date; i = i.AddDays(1))
            {
                if (Util.IsTransacDay(i, _db))
                {
                    await DealDay(simulator, i);
                }
            }
        }

        [NonAction]
        public async Task DealDay(Simulator simulator, DateTime currentDate)
        {
            //buy
            ArrayList stockArr = await GetStocks(simulator, Util.GetLastTransactDate(currentDate, 1, _db));
            for (int i = 0; i < stockArr.Count; i++)
            {
                
                //buy every day
                StockFilter.Item item = (StockFilter.Item)stockArr[i];
                Stock s = Stock.GetStock(item.gid);
                try
                {
                    s.RefreshKLineDay();
                }
                catch
                {
                    continue;
                }
                int alertIndex = s.GetItemIndex(item.alertDate.Date);
                if (alertIndex < 2 || alertIndex >= s.klineDay.Length - 1)
                {
                    continue;
                }

                await BuyStockDaily(simulator, s, alertIndex + 1);

            }

            //sell
            var holdingList = await _db.simulatorDailyHolding
                .Where(h => h.simulator_id == simulator.id
                && h.trans_date.Date == Util.GetLastTransactDate(currentDate, 1, _db).Date)
                .ToListAsync();
            for (int i = 0; i < holdingList.Count; i++)
            {
                string gid = holdingList[i].gid;
                DateTime sellDate = holdingList[i].trans_date.Date;
                Stock s = Stock.GetStock(gid);
                try
                {
                    s.RefreshKLineDay();
                }
                catch
                {
                    continue;
                }
                int currentIndex = s.GetItemIndex(sellDate);
                if (currentIndex < 1 || currentIndex >= s.klineDay.Length - 1)
                {
                    continue;
                }
                await SellStockDaily(simulator, s, currentIndex + 1 , holdingList[i].stock_num);
            }

            await CreateHoldingList(simulator, currentDate);
            await CreateDailyReport(simulator, currentDate);
        }



        [NonAction]
        public async Task<ArrayList> GetStocks(Simulator simulator, DateTime currentDate)
        {
            ArrayList stockArr = new ArrayList();
            StockFilter sf = new StockFilter();
            switch (simulator.id)
            {
                case 1:
                    sf = (StockFilter)((OkObjectResult)(await _limitupHelper.Reverse(1, currentDate, currentDate, "代码")).Result).Value;
                    break;
                default:
                    break;
            }
            for (int i = 0; sf != null && sf.itemList != null && i < sf.itemList.Count; i++)
            {
                var item = sf.itemList[i];
                if (item.alertDate.Date == currentDate.Date)
                {
                    stockArr.Add(item);
                }
            }
            return stockArr;
        }

        [NonAction]
        public  async Task  BuyStockDaily(Simulator simulator, Stock s, int currentIndex)
        {
            double maxAmount = simulator.total_amount / 10;
            double cashAmount = await GetCashAmount(simulator, s.klineDay[currentIndex].settleTime.Date);
            double transAmount = Math.Min(cashAmount, maxAmount);
            int stockAmount = 100 * (int)(transAmount / s.klineDay[currentIndex].open / 100);
            if (stockAmount <= 0)
            {
                return;
            }
            double buyPrice = 0;
            switch (simulator.id)
            {
                case 1:
                    if (currentIndex > 0 && currentIndex < s.klineDay.Length - 1)
                    {
                        if (s.klineDay[currentIndex].open > s.klineDay[currentIndex - 1].settle
                            && s.klineDay[currentIndex].low < s.klineDay[currentIndex - 1].settle)
                        {
                            buyPrice = s.klineDay[currentIndex - 1].settle;
                        }
                    }
                    break;
                default:
                    break;
            }
            if (buyPrice > 0)
            {
                await Transact(simulator, s.gid, s.name, stockAmount, buyPrice, s.klineDay[currentIndex].settleTime.Date);
            }
        }

        [NonAction]
        public async Task SellStockDaily(Simulator simulator, Stock s, int currentIndex, int stockAmount)
        {
            double sellPrice = 0;
            switch (simulator.id)
            {
                case 1:
                    //买入当天涨停
                    if (KLine.IsLimitUp(s.klineDay, currentIndex - 1))
                    {
                        //次日不涨停
                        if (!KLine.IsLimitUp(s.klineDay, currentIndex))
                        {
                            //收盘卖出
                            sellPrice = s.klineDay[currentIndex].settle;
                        }
                    }
                    //买入当天不涨停
                    else
                    {
                        //买入当天盈利
                        if (s.klineDay[currentIndex - 2].settle < s.klineDay[currentIndex - 1].settle)
                        {
                            //次日不涨停
                            if (!KLine.IsLimitUp(s.klineDay, currentIndex))
                            {
                                //收盘卖出
                                sellPrice = s.klineDay[currentIndex].settle;
                            }
                        }
                        //买入当天亏损
                        else
                        {
                            //可以平进平出
                            if (s.klineDay[currentIndex - 1].high > s.klineDay[currentIndex - 2].settle)
                            {
                                //平进平出
                                sellPrice = s.klineDay[currentIndex - 2].settle;
                            }
                            //没有机会平进平出
                            else
                            {
                                //收盘卖出
                                sellPrice = s.klineDay[currentIndex - 1].settle;
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
            if (sellPrice > 0)
            {
                await Transact(simulator, s.gid, s.name, -1 * stockAmount, sellPrice, s.klineDay[currentIndex].settleTime.Date);
            }
        }

        [NonAction]
        public async Task<double> GetCashAmount(Simulator simulator, DateTime currentDate)
        {
            var transList = await _db.simulatorDailyTrans
                    .Where(t => t.trans_date.Date == currentDate.Date && t.simulator_id == simulator.id).ToListAsync();
            double todayTransAmount = 0;
            for (int i = 0; i < transList.Count; i++)
            {
                todayTransAmount += transList[i].trans_price * transList[i].stock_num;
            }

            DateTime lastDate = Util.GetLastTransactDate(currentDate, 1, _db);
            var l = await _db.simulatorDaily.Where(s => s.trans_date.Date == lastDate.Date && s.simulator_id == simulator.id).ToListAsync();
            if (l == null || l.Count == 0)
            {
                return simulator.total_amount - todayTransAmount;
            }
            else
            {
                double lastCashAmount = l[0].cash_amount;
                

                return lastCashAmount - todayTransAmount;
            }
        }



        [NonAction]
        public async Task Transact(Simulator simulator, string gid, string name, int transNum, double price, DateTime dateTime)
        {
            SimulatorDailyTrans trans = new SimulatorDailyTrans()
            {
                simulator_id = simulator.id,
                gid = gid.Trim(),
                name = name.Trim(),
                trans_amount = transNum * price,
                trans_date = dateTime.Date,
                stock_num = transNum,
                trans_price = price
            };
            await _db.simulatorDailyTrans.AddAsync(trans);
            await _db.SaveChangesAsync();
        }

        [NonAction]
        public async Task CreateDailyReport(Simulator simulator, DateTime currentDate)
        {
            var transList = await _db.simulatorDailyTrans
                    .Where(t => t.trans_date.Date == currentDate.Date && t.simulator_id == simulator.id).ToListAsync();
            double todayTransAmount = 0;
            for (int i = 0; i < transList.Count; i++)
            {
                todayTransAmount += transList[i].trans_price * transList[i].stock_num;
            }

            double cashAmount = simulator.total_amount;

            DateTime lastDate = Util.GetLastTransactDate(currentDate, 1, _db);
            var l = await _db.simulatorDaily
                .Where(d => d.simulator_id == simulator.id && d.trans_date.Date == lastDate.Date)
                .ToListAsync();
            if (l != null && l.Count > 0)
            {
                cashAmount = l[0].cash_amount;
            }

            cashAmount = cashAmount - todayTransAmount;

            double stockAmount = 0;

            var holdingList = await _db.simulatorDailyHolding
                .Where(h => h.simulator_id == simulator.id && h.trans_date.Date == currentDate.Date)
                .ToListAsync();
            for (int i = 0; i < holdingList.Count; i++)
            {   
                stockAmount += holdingList[i].stock_value;
            }

            SimulatorDaily r = new SimulatorDaily();
            r.simulator_id = simulator.id;
            r.trans_date = currentDate.Date;
            r.stock_amount = stockAmount;
            r.cash_amount = cashAmount;
            r.total_amount = r.cash_amount + r.stock_amount;

            var dL = await _db.simulatorDaily
                .Where(d => d.simulator_id == simulator.id && d.trans_date.Date == currentDate.Date)
                .ToListAsync();
            if (dL != null && dL.Count > 0)
            {
                _db.simulatorDaily.Remove((SimulatorDaily)dL[0]);
                await _db.SaveChangesAsync();
            }
            await _db.simulatorDaily.AddAsync(r);
            await _db.SaveChangesAsync();

        }

        [NonAction]
        public async Task CreateHoldingList(Simulator simulator, DateTime currentDate)
        {
            var lastHoldingList = await _db.simulatorDailyHolding
                .Where(h => h.simulator_id == simulator.id && h.trans_date.Date == currentDate.Date)
                .ToListAsync();
            for (int i = 0; lastHoldingList != null && i < lastHoldingList.Count; i++)
            {
                _db.simulatorDailyHolding.Remove(lastHoldingList[i]);
            }
            await _db.SaveChangesAsync();

            DateTime lastDate = Util.GetLastTransactDate(currentDate, 1, _db);
            var holdingList = await _db.simulatorDailyHolding
                .Where(h => h.simulator_id == simulator.id && h.trans_date.Date == lastDate.Date)
                .ToListAsync();
            ArrayList holdArray = new ArrayList();
            for (int i = 0; holdingList != null && i < holdingList.Count; i++)
            {
                SimulatorDailyHolding holding = new SimulatorDailyHolding()
                {
                    simulator_id = simulator.id,
                    trans_date = currentDate.Date,
                    gid = holdingList[i].gid,
                    name = holdingList[i].name,
                    stock_cost = holdingList[i].stock_cost,
                    stock_num = holdingList[i].stock_num,
                    hold_days = holdingList[i].hold_days++
                };
                holdArray.Add(holding);
            }

            var transList = await _db.simulatorDailyTrans
                .Where(s => s.simulator_id == simulator.id && s.trans_date.Date == currentDate.Date)
                .ToListAsync();
            for (int i = 0; i < transList.Count; i++)
            {
                bool find = false;
                for (int j = 0; j < holdArray.Count; j++)
                {
                    SimulatorDailyHolding currentH = (SimulatorDailyHolding)holdArray[j];
                    if (currentH.simulator_id == transList[i].simulator_id
                        && currentH.gid.Trim().Equals(transList[i].gid.Trim()))
                    {
                        find = true;
                        currentH.stock_num += transList[i].stock_num;
                    }
                    
                }
                if (!find)
                {
                    SimulatorDailyHolding holding = new SimulatorDailyHolding()
                    {
                        simulator_id = simulator.id,
                        trans_date = currentDate.Date,
                        gid = transList[i].gid,
                        name = transList[i].name,
                        stock_cost = transList[i].trans_price,
                        stock_num = transList[i].stock_num,
                        hold_days = 0
                    };
                    holdArray.Add(holding);
                }
            }

            for (int i = 0; i < holdArray.Count; i++)
            {
                SimulatorDailyHolding h = (SimulatorDailyHolding)holdArray[i];
                
                if (h.stock_num != 0)
                {
                    Stock s = Stock.GetStock(h.gid);
                    s.RefreshKLineDay();
                    int currentIndex = s.GetItemIndex(h.trans_date.Date);
                    if (currentIndex < 0 || currentIndex >= s.klineDay.Length)
                    {
                        continue;
                    }
                    h.stock_value = h.stock_num * s.klineDay[currentIndex].settle;

                    h.trans_date = currentDate.Date;
                    await _db.simulatorDailyHolding.AddAsync(h);
                    await _db.SaveChangesAsync();
                }
            }
        }

	}
}

