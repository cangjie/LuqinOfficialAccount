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
            for (int i = 0; i < sf.itemList.Count; i++)
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
        public async Task<double> GetCashAmount(Simulator simulator, DateTime currentDate)
        {
            var transList = await _db.simulatorDailyTrans
                    .Where(t => t.trans_date.Date == currentDate.Date && t.simulator_id == simulator.id).ToListAsync();
            double todayTransAmount = 0;
            for (int i = 0; i < transList.Count; i++)
            {
                todayTransAmount += transList[i].trans_price * transList[i].trans_amount;
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
                todayTransAmount += transList[i].trans_price * transList[i].trans_amount;
            }

            DateTime lastDate = Util.GetLastTransactDate(currentDate, 1, _db);
            var l = await _db.simulatorDaily
                .Where(d => d.simulator_id == simulator.id && d.trans_date.Date == lastDate.Date)
                .ToListAsync();
            SimulatorDaily r = new SimulatorDaily();
            r.simulator_id = simulator.id;
            r.trans_date = currentDate.Date;
            r.stock_amount = 0;
            r.cash_amount = await GetCashAmount(simulator, currentDate);
            if (l != null && l.Count > 0)
            {
                r.stock_amount = l[0].stock_amount + todayTransAmount;
            }
            else
            {
                r.stock_amount = todayTransAmount;
            }
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
                    SimulatorDailyHolding currentH = (SimulatorDailyHolding)holdArray[i];
                    if (currentH.simulator_id == transList[i].simulator_id
                        && currentH.gid.Trim().Equals(transList[i].gid.Trim()))
                    {
                        find = true;
                        currentH.stock_num += transList[i].stock_num;
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
            }

            for (int i = 0; i < holdArray.Count; i++)
            {
                SimulatorDailyHolding h = (SimulatorDailyHolding)holdArray[i];
                if (h.stock_num != 0)
                {
                    h.trans_date = currentDate.Date;
                    await _db.simulatorDailyHolding.AddAsync(h);
                    await _db.SaveChangesAsync();
                }
            }
        }

	}
}

